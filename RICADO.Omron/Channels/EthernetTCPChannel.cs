﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Omron.Requests;
using RICADO.Omron.Responses;

namespace RICADO.Omron.Channels
{
    internal sealed class EthernetTCPChannel : IDisposable
    {

        internal const int TCP_HEADER_LENGTH = 16;

        private byte _requestId = 0;
        private TcpClient _client;
        private SemaphoreSlim _semaphore;
        private bool disposedValue;
        private byte errorCounter = 0;
        private bool initOk = false;

        internal string RemoteHost { get; }
        internal int Port { get; }
        internal byte LocalNodeID { get; private set; }
        internal byte RemoteNodeID { get; private set; }

        internal event EventHandler NeedReinit;

        #region ctor and dispose

        public EthernetTCPChannel(string remoteHost, int port)
        {
            RemoteHost = remoteHost;
            Port = port;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                DestroyClient();
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~EthernetTCPChannel()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region internal methods

        internal async Task InitializeAsync(int timeout, CancellationToken cancellationToken)
        {
            initOk = false;
            try
            {
#if CONCURRENT
                if (await _semaphore.WaitAsync(timeout, cancellationToken))
                {
                try
                {
#endif
                DestroyClient();
                await InitializeClientAsync(timeout, cancellationToken);
                errorCounter = 0;
                initOk = true;
#if CONCURRENT
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                else
                {
                    throw new TimeoutException("Timeout out waiting for semaphore");
                }
#endif
            }
            catch (ObjectDisposedException)
            {
                throw new OmronException("Failed to Re-Connect to Omron PLC '" + RemoteHost + ":" + Port + "' - The underlying Socket Connection was Closed");
            }
            catch (TimeoutException)
            {
                throw new OmronException("Failed to Re-Connect within the Timeout Period to Omron PLC '" + RemoteHost + ":" + Port + "'");
            }
            catch (SocketException e)
            {
                throw new OmronException("Failed to Re-Connect to Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }
        }

        internal async Task<ProcessRequestResult> ProcessRequestAsync(FINSRequest request, int timeout, int retries, CancellationToken cancellationToken)
        {
            if (!initOk)
                throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");

            int attempts = 0;
            Memory<byte> responseMessage = new Memory<byte>();
            int bytesSent = 0;
            int packetsSent = 0;
            int bytesReceived = 0;
            int packetsReceived = 0;
            DateTime startTimestamp = DateTime.UtcNow;

            if (await _semaphore.WaitAsync(timeout, cancellationToken))
            {
                try
                {
                    while (attempts <= retries)
                    {
                        try
                        {
                            if (attempts > 0)
                            {
                                await InitializeAsync(timeout, cancellationToken);
                            }

                            //Stopwatch sw = Stopwatch.StartNew();

                            // Build the Request into a Message we can Send
                            byte[] requestMessage = request.BuildMessage(GetNextRequestId());

                            // Send the Message
#if CONCURRENT
                            SendMessageResult sendResult = await SendMessageAsync(TcpCommandCode.FINSFrame, requestMessage, timeout, cancellationToken);
#else
                            SendMessageResult sendResult = SendMessage(TcpCommandCode.FINSFrame, requestMessage, timeout);
#endif
                            bytesSent += sendResult.Bytes;
                            packetsSent += sendResult.Packets;

                            // Receive a Response
#if CONCURRENT
                            ReceiveMessageResult receiveResult = await ReceiveMessageAsync(TcpCommandCode.FINSFrame, timeout, cancellationToken);
#else
                            ReceiveMessageResult receiveResult = ReceiveMessage(TcpCommandCode.FINSFrame, timeout);
#endif
                            bytesReceived += receiveResult.Bytes;
                            packetsReceived += receiveResult.Packets;
                            responseMessage = receiveResult.Message;

                            //sw.Stop();
                            //Console.WriteLine($"PLC RTT {sw.ElapsedMilliseconds}ms");
                            //sw = null;

                            break;
                        }
                        catch (Exception)
                        {
                            // Increment the Attempts
                            attempts++;
                        }
                    }

                    if (attempts > retries)
                    {
                        IncrementErrorCounter(timeout, cancellationToken);
                        throw new OmronException("Max retries");
                    }

                    try
                    {
                        return new ProcessRequestResult
                        {
                            BytesSent = bytesSent,
                            PacketsSent = packetsSent,
                            BytesReceived = bytesReceived,
                            PacketsReceived = packetsReceived,
                            Duration = DateTime.UtcNow.Subtract(startTimestamp).TotalMilliseconds,
                            Response = FINSResponse.CreateNew(responseMessage, request),
                        };
                    }
                    catch (FINSException e)
                    {
                        if (e.Message.Contains("Service ID") && responseMessage.Length >= 9 && responseMessage.Span[9] != request.ServiceID)
                        {
#if CONCURRENT
                            PurgeReceiveBuffer(timeout, cancellationToken).Wait(cancellationToken);
#else
                            PurgeReceiveBuffer(timeout);
#endif
                        }
                        IncrementErrorCounter(timeout, cancellationToken);
                        throw new OmronException("Received a FINS Error Response from Omron PLC '" + RemoteHost + ":" + Port + "'", e);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            else
            {
                IncrementErrorCounter(timeout, cancellationToken);
                throw new TimeoutException("Timeout out waiting for semaphore");
            }
        }

        #endregion

        private void IncrementErrorCounter(int timeout, CancellationToken cancellationToken)
        {
            errorCounter++;
            if (errorCounter >= 5)
            {
                NeedReinit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void DestroyClient()
        {
            try
            {
                _client?.Dispose();
            }
            finally
            {
                _client = null;
            }
        }

        private async Task InitializeClientAsync(int timeout, CancellationToken cancellationToken)
        {
            _client = new TcpClient();

            //_client.ReceiveBufferSize = 10000;
            //_client.SendBufferSize = 10000;
            _client.ReceiveTimeout = timeout;
            _client.SendTimeout = timeout;
            _client.NoDelay = true;

            await _client.ConnectAsync(RemoteHost, Port, cancellationToken);

            _client.Client.ReceiveTimeout = timeout;
            _client.Client.SendTimeout = timeout;

            try
            {
                // Send Auto-Assign Client Node Request
#if CONCURRENT
                SendMessageResult sendResult = await SendMessageAsync(TcpCommandCode.NodeAddressToPLC, new byte[4], timeout, cancellationToken);
#else
                SendMessageResult sendResult = SendMessage(TcpCommandCode.NodeAddressToPLC, new byte[4], timeout);
#endif

                // Receive Client Node ID
#if CONCURRENT
                ReceiveMessageResult receiveResult = await ReceiveMessageAsync(TcpCommandCode.NodeAddressFromPLC, timeout, cancellationToken);
#else
                ReceiveMessageResult receiveResult = ReceiveMessage(TcpCommandCode.NodeAddressFromPLC, timeout);
#endif

                if (receiveResult.Message.Length < 8)
                {
                    throw new OmronException("Failed to Negotiate a TCP Connection with Omron PLC '" + RemoteHost + ":" + Port + "' - TCP Negotiation Message Length was too Short");
                }

                byte[] tcpNegotiationMessage = receiveResult.Message.Slice(0, 8).ToArray();

                if (tcpNegotiationMessage[3] == 0 || tcpNegotiationMessage[3] == 255)
                {
                    throw new OmronException("Failed to Negotiate a TCP Connection with Omron PLC '" + RemoteHost + ":" + Port + "' - TCP Negotiation Message contained an Invalid Local Node ID");
                }

                LocalNodeID = tcpNegotiationMessage[3];

                if (tcpNegotiationMessage[7] == 0 || tcpNegotiationMessage[7] == 255)
                {
                    throw new OmronException("Failed to Negotiate a TCP Connection with Omron PLC '" + RemoteHost + ":" + Port + "' - TCP Negotiation Message contained an Invalid Remote Node ID");
                }

                RemoteNodeID = tcpNegotiationMessage[7];
            }
            catch (OmronException e)
            {
                throw new OmronException("Failed to Negotiate a TCP Connection with Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }
        }

#if CONCURRENT
        private async Task PurgeReceiveBuffer(int timeout, CancellationToken cancellationToken)
#else
        private void PurgeReceiveBuffer(int timeout)
#endif
        {
            try
            {
                if (_client.Connected == false)
                {
                    return;
                }

                if (_client.Available == 0)
                {
#if CONCURRENT
                    await Task.Delay(timeout / 4);
#else
                    Task.Delay(timeout / 4).Wait();
#endif
                }

                DateTime startTimestamp = DateTime.UtcNow;
#if CONCURRENT
                Memory<byte> buffer = new byte[2000];
#else
                byte[] buffer = new byte[2048];
#endif

                while (_client.Connected && _client.Available > 0 && DateTime.UtcNow.Subtract(startTimestamp).TotalMilliseconds < timeout)
                {
                    try
                    {
#if CONCURRENT
                        await _client.Client.ReceiveAsync(buffer, cancellationToken);
#else
                        _client.Client.Receive(buffer);
#endif
                    }
                    catch
                    {
                        return;
                    }
                }
            }
            catch
            {
            }
        }

        private byte GetNextRequestId()
        {
            if (_requestId == byte.MaxValue)
            {
                _requestId = byte.MinValue;
            }
            else
            {
                _requestId++;
            }

            return _requestId;
        }

#if CONCURRENT
        private async Task<SendMessageResult> SendMessageAsync(TcpCommandCode command, byte[] message, int timeout, CancellationToken cancellationToken)
#else
        private SendMessageResult SendMessage(TcpCommandCode command, byte[] message, int timeout)
#endif
        {
            SendMessageResult result = new SendMessageResult
            {
                Bytes = 0,
                Packets = 0,
            };

            ReadOnlyMemory<byte> tcpMessage = BuildFinsTcpMessage(command, message);

            try
            {
#if CONCURRENT
                result.Bytes += await _client.Client.SendAsync(tcpMessage, cancellationToken);
#else
                result.Bytes += _client.Client.Send(tcpMessage.ToArray());
#endif
                result.Packets += 1;
            }
            catch (ObjectDisposedException)
            {
                throw new OmronException("Failed to Send FINS Message to Omron PLC '" + RemoteHost + ":" + Port + "' - The underlying Socket Connection was Closed");
            }
            catch (TimeoutException)
            {
                throw new OmronException("Failed to Send FINS Message within the Timeout Period to Omron PLC '" + RemoteHost + ":" + Port + "'");
            }
            catch (SocketException e)
            {
                throw new OmronException("Failed to Send FINS Message to Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }

            return result;
        }

#if CONCURRENT
        private async Task<ReceiveMessageResult> ReceiveMessageAsync(TcpCommandCode command, int timeout, CancellationToken cancellationToken)
#else
        private ReceiveMessageResult ReceiveMessage(TcpCommandCode command, int timeout)
#endif
        {
            ReceiveMessageResult result = new ReceiveMessageResult
            {
                Bytes = 0,
                Packets = 0,
                Message = new Memory<byte>(),
            };

            try
            {
                List<byte> receivedData = new List<byte>();
                DateTime startTimestamp = DateTime.UtcNow;

                while (DateTime.UtcNow.Subtract(startTimestamp).TotalMilliseconds < timeout && receivedData.Count < TCP_HEADER_LENGTH)
                {
#if CONCURRENT
                    Memory<byte> buffer = new byte[4096];
                    int receivedBytes = await _client.Client.ReceiveAsync(buffer, cancellationToken);
                    //int receivedBytes = await _client.GetStream().ReadAsync(buffer, cancellationToken);
#else
                    var byteBuffer = new byte[4096];
                    int receivedBytes = _client.Client.Receive(byteBuffer);
                    Memory<byte> buffer = byteBuffer;
#endif

                    if (receivedBytes > 0)
                    {
                        receivedData.AddRange(buffer.Slice(0, receivedBytes).ToArray());

                        result.Bytes += receivedBytes;
                        result.Packets += 1;
                    }
                }

                #region checks

                if (receivedData.Count == 0)
                {
                    throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - No Data was Received");
                }

                if (receivedData.Count < TCP_HEADER_LENGTH)
                {
                    throw new OmronException("Failed to Receive FINS Message within the Timeout Period from Omron PLC '" + RemoteHost + ":" + Port + "'");
                }

                if (receivedData[0] != 'F' || receivedData[1] != 'I' || receivedData[2] != 'N' || receivedData[3] != 'S')
                {
                    throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - The TCP Header was Invalid");
                }

                byte[] tcpHeader = receivedData.GetRange(0, TCP_HEADER_LENGTH).ToArray();

                int tcpMessageDataLength = (int)BitConverter.ToUInt32(new byte[] { receivedData[7], receivedData[6], receivedData[5], receivedData[4] }) - 8;

                if (tcpMessageDataLength <= 0 || tcpMessageDataLength > short.MaxValue)
                {
                    throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - The TCP Message Length was Invalid");
                }

                if (receivedData[11] == 3 || receivedData[15] != 0)
                {
                    switch (receivedData[15])
                    {
                        case 1:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: The FINS Identifier (ASCII Code) was Invalid.");

                        case 2:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: The Data Length is too Long.");

                        case 3:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: The Command is not Supported.");

                        case 20:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: All Connections are in Use.");

                        case 21:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: The Specified Node is already Connected.");

                        case 22:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: Attempt to Access a Protected Node from an Unspecified IP Address.");

                        case 23:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: The Client FINS Node Address is out of Range.");

                        case 24:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: The same FINS Node Address is being used by the Client and Server.");

                        case 25:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: All the Node Addresses Available for Allocation have been Used.");

                        default:
                            throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - Omron TCP Error: Unknown Code '" + receivedData[15] + "'");
                    }
                }

                if (receivedData[8] != 0 || receivedData[9] != 0 || receivedData[10] != 0 || receivedData[11] != (byte)command)
                {
                    throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - The TCP Command Received '" + receivedData[11] + "' did not match Expected Command '" + (byte)command + "'");
                }

                if (command == TcpCommandCode.FINSFrame && tcpMessageDataLength < FINSResponse.HEADER_LENGTH + FINSResponse.COMMAND_LENGTH + FINSResponse.RESPONSE_CODE_LENGTH)
                {
                    throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - The TCP Message Length was too short for a FINS Frame");
                }

                #endregion

                receivedData.RemoveRange(0, TCP_HEADER_LENGTH);

                if (receivedData.Count < tcpMessageDataLength)
                {
                    startTimestamp = DateTime.UtcNow;

                    while (DateTime.UtcNow.Subtract(startTimestamp).TotalMilliseconds < timeout && receivedData.Count < tcpMessageDataLength)
                    {
#if CONCURRENT
                        Memory<byte> buffer = new byte[4096];
                        TimeSpan receiveTimeout = TimeSpan.FromMilliseconds(timeout).Subtract(DateTime.UtcNow.Subtract(startTimestamp));
                        int receivedBytes = await _client.Client.ReceiveAsync(buffer, cancellationToken);
#else
                        var byteBuffer = new byte[4096];
                        int receivedBytes = _client.Client.Receive(byteBuffer);
                        Memory<byte> buffer = byteBuffer;
#endif


                        if (receivedBytes > 0)
                        {
                            receivedData.AddRange(buffer.Slice(0, receivedBytes).ToArray());
                        }

                        result.Bytes += receivedBytes;
                        result.Packets += 1;
                    }
                }

                #region checks
                if (receivedData.Count == 0)
                {
                    throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - No Data was Received after TCP Header");
                }

                if (receivedData.Count < tcpMessageDataLength)
                {
                    throw new OmronException("Failed to Receive FINS Message within the Timeout Period from Omron PLC '" + RemoteHost + ":" + Port + "'");
                }

                if (command == TcpCommandCode.FINSFrame && receivedData[0] != 0xC0 && receivedData[0] != 0xC1)
                {
                    throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - The FINS Header was Invalid");
                }
                #endregion

                result.Message = receivedData.ToArray();
            }
            catch (ObjectDisposedException)
            {
                throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "' - The underlying Socket Connection was Closed");
            }
            catch (TimeoutException)
            {
                throw new OmronException("Failed to Receive FINS Message within the Timeout Period from Omron PLC '" + RemoteHost + ":" + Port + "'");
            }
            catch (SocketException e)
            {
                throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return result;
        }

        private static ReadOnlyMemory<byte> BuildFinsTcpMessage(TcpCommandCode command, byte[] message)
        {
            var bytes = Enumerable.Repeat((byte)0x00, 16 + message.Length).ToArray();
            // FINS Message Identifier
            bytes[0] = (byte)'F';
            bytes[1] = (byte)'I';
            bytes[2] = (byte)'N';
            bytes[3] = (byte)'S';
            // Length of Message = Command + Error Code + Message Data
            uint length = Convert.ToUInt32(4 + 4 + message.Length);
            bytes[4] = (byte)((length & 0xFF000000) >> 24);
            bytes[5] = (byte)((length & 0x00FF0000) >> 16);
            bytes[6] = (byte)((length & 0x0000FF00) >> 8);
            bytes[7] = (byte)(length & 0x000000FF);

            //var le = BitConverter.IsLittleEndian;

            // Command
            bytes[11] = (byte)command;

            // Error Code - 0

            for (int i = 0; i < message.Length; i++)
                bytes[16 + i] = message[i];

            return bytes;
        }

    }
}
