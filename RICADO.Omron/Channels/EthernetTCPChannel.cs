using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Omron.Requests;
using RICADO.Omron.Responses;
using RICADO.Sockets;

namespace RICADO.Omron.Channels
{
    internal sealed class EthernetTCPChannel : IDisposable
    {

        internal const int TCP_HEADER_LENGTH = 16;

        private byte _requestId = 0;
        private TcpClient _client;
        private SemaphoreSlim _semaphore;

        internal string RemoteHost { get; }
        internal int Port { get; }
        internal byte LocalNodeID { get; private set; }
        internal byte RemoteNodeID { get; private set; }

        #region ctor and dispose

        public EthernetTCPChannel(string remoteHost, int port)
        {
            RemoteHost = remoteHost;
            Port = port;

            _semaphore = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
            DestroyClient();
        }

        #endregion

        #region internal methods

        internal async Task InitializeAsync(int timeout, CancellationToken cancellationToken)
        {
            if (!_semaphore.Wait(0))
            {
                await _semaphore.WaitAsync(cancellationToken);
            }

            try
            {
                DestroyClient();

                await InitializeClient(timeout, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal async Task<ProcessRequestResult> ProcessRequestAsync(FINSRequest request, int timeout, int retries, CancellationToken cancellationToken)
        {
            int attempts = 0;
            Memory<byte> responseMessage = new Memory<byte>();
            int bytesSent = 0;
            int packetsSent = 0;
            int bytesReceived = 0;
            int packetsReceived = 0;
            DateTime startTimestamp = DateTime.UtcNow;

            while (attempts <= retries)
            {
                if (!_semaphore.Wait(0))
                {
                    await _semaphore.WaitAsync(cancellationToken);
                }

                try
                {
                    if (attempts > 0)
                    {
                        await DestroyAndInitializeClient(timeout, cancellationToken);
                    }

                    // Build the Request into a Message we can Send
                    byte[] requestMessage = request.BuildMessage(GetNextRequestId());

                    // Send the Message
                    SendMessageResult sendResult = await SendMessageAsync(TcpCommandCode.FINSFrame, requestMessage, timeout, cancellationToken);

                    bytesSent += sendResult.Bytes;
                    packetsSent += sendResult.Packets;

                    // Receive a Response
                    ReceiveMessageResult receiveResult = await ReceiveMessageAsync(TcpCommandCode.FINSFrame, timeout, cancellationToken);

                    bytesReceived += receiveResult.Bytes;
                    packetsReceived += receiveResult.Packets;
                    responseMessage = receiveResult.Message;

                    break;
                }
                catch (Exception)
                {
                    if (attempts >= retries)
                    {
                        throw;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                // Increment the Attempts
                attempts++;
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
                    if (!_semaphore.Wait(0))
                    {
                        await _semaphore.WaitAsync(cancellationToken);
                    }

                    try
                    {
                        await PurgeReceiveBuffer(timeout, cancellationToken);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                throw new OmronException("Received a FINS Error Response from Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }
        }

        #endregion

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

        private async Task InitializeClient(int timeout, CancellationToken cancellationToken)
        {
            _client = new TcpClient(RemoteHost, Port);

            await _client.ConnectAsync(timeout, cancellationToken);

            try
            {
                // Send Auto-Assign Client Node Request
                SendMessageResult sendResult = await SendMessageAsync(TcpCommandCode.NodeAddressToPLC, new byte[4], timeout, cancellationToken);

                // Receive Client Node ID
                ReceiveMessageResult receiveResult = await ReceiveMessageAsync(TcpCommandCode.NodeAddressFromPLC, timeout, cancellationToken);

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

        private async Task DestroyAndInitializeClient(int timeout, CancellationToken cancellationToken)
        {
            DestroyClient();

            try
            {
                await InitializeClient(timeout, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                throw new OmronException("Failed to Re-Connect to Omron PLC '" + RemoteHost + ":" + Port + "' - The underlying Socket Connection was Closed");
            }
            catch (TimeoutException)
            {
                throw new OmronException("Failed to Re-Connect within the Timeout Period to Omron PLC '" + RemoteHost + ":" + Port + "'");
            }
            catch (System.Net.Sockets.SocketException e)
            {
                throw new OmronException("Failed to Re-Connect to Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }
        }

        private async Task PurgeReceiveBuffer(int timeout, CancellationToken cancellationToken)
        {
            try
            {
                if (_client.Connected == false)
                {
                    return;
                }

                if (_client.Available == 0)
                {
                    await Task.Delay(timeout / 4);
                }

                DateTime startTimestamp = DateTime.UtcNow;
                Memory<byte> buffer = new byte[2000];

                while (_client.Connected && _client.Available > 0 && DateTime.UtcNow.Subtract(startTimestamp).TotalMilliseconds < timeout)
                {
                    try
                    {
                        await _client.ReceiveAsync(buffer, timeout, cancellationToken);
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

        private async Task<SendMessageResult> SendMessageAsync(TcpCommandCode command, byte[] message, int timeout, CancellationToken cancellationToken)
        {
            SendMessageResult result = new SendMessageResult
            {
                Bytes = 0,
                Packets = 0,
            };

            ReadOnlyMemory<byte> tcpMessage = BuildFinsTcpMessage(command, message);

            try
            {
                result.Bytes += await _client.SendAsync(tcpMessage, timeout, cancellationToken);
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
            catch (System.Net.Sockets.SocketException e)
            {
                throw new OmronException("Failed to Send FINS Message to Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }

            return result;
        }

        private async Task<ReceiveMessageResult> ReceiveMessageAsync(TcpCommandCode command, int timeout, CancellationToken cancellationToken)
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
                    Memory<byte> buffer = new byte[4096];
                    TimeSpan receiveTimeout = TimeSpan.FromMilliseconds(timeout).Subtract(DateTime.UtcNow.Subtract(startTimestamp));

                    if (receiveTimeout.TotalMilliseconds >= 50)
                    {
                        int receivedBytes = await _client.ReceiveAsync(buffer, receiveTimeout, cancellationToken);

                        if (receivedBytes > 0)
                        {
                            receivedData.AddRange(buffer.Slice(0, receivedBytes).ToArray());

                            result.Bytes += receivedBytes;
                            result.Packets += 1;
                        }
                    }
                }

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

                receivedData.RemoveRange(0, TCP_HEADER_LENGTH);

                if (receivedData.Count < tcpMessageDataLength)
                {
                    startTimestamp = DateTime.UtcNow;

                    while (DateTime.UtcNow.Subtract(startTimestamp).TotalMilliseconds < timeout && receivedData.Count < tcpMessageDataLength)
                    {
                        Memory<byte> buffer = new byte[4096];
                        TimeSpan receiveTimeout = TimeSpan.FromMilliseconds(timeout).Subtract(DateTime.UtcNow.Subtract(startTimestamp));

                        if (receiveTimeout.TotalMilliseconds >= 50)
                        {
                            int receivedBytes = await _client.ReceiveAsync(buffer, receiveTimeout, cancellationToken);

                            if (receivedBytes > 0)
                            {
                                receivedData.AddRange(buffer.Slice(0, receivedBytes).ToArray());
                            }

                            result.Bytes += receivedBytes;
                            result.Packets += 1;
                        }
                    }
                }

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
            catch (System.Net.Sockets.SocketException e)
            {
                throw new OmronException("Failed to Receive FINS Message from Omron PLC '" + RemoteHost + ":" + Port + "'", e);
            }

            return result;
        }

        private ReadOnlyMemory<byte> BuildFinsTcpMessage(TcpCommandCode command, byte[] message)
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
