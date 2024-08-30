using System;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Omron.Channels;
using RICADO.Omron.Requests;
using RICADO.Omron.Responses;
using RICADO.Omron.Results;

namespace RICADO.Omron
{
    // TODO: Add Documentation to all Classes, Interfaces, Structs and Enums

    public class OmronPLC : IDisposable
    {

        #region Private Fields

        private enPLCType _plcType = enPLCType.Unknown;
        private bool _isInitialized;
        private bool disposedValue;
        private readonly object _isInitializedLock = new object();

        #endregion

        #region Internal Properties

        internal EthernetChannel Channel { get; private set; }

        internal bool IsNSeries => _plcType switch
        {
            enPLCType.NJ101 => true,
            enPLCType.NJ301 => true,
            enPLCType.NJ501 => true,
            enPLCType.NX1P2 => true,
            enPLCType.NX102 => true,
            enPLCType.NX701 => true,
            enPLCType.NY512 => true,
            enPLCType.NY532 => true,
            enPLCType.NJ_NX_NY_Series => true,
            _ => false,
        };

        internal bool IsCSeries => _plcType switch
        {
            enPLCType.CP1 => true,
            enPLCType.CJ2 => true,
            enPLCType.C_Series => true,
            _ => false,
        };

        #endregion

        #region Public Properties

        public byte LocalNodeID { get; private set; }

        public byte RemoteNodeID { get; private set; }

        public enConnectionMethod ConnectionMethod { get; private set; }

        public string RemoteHost { get; private set; }

        public int Port { get; private set; } = 9600;

        public int Timeout { get; set; }

        public int Retries { get; set; }

        public enPLCType PLCType { get; private set; }

        public bool IsInitialized
        {
            get
            {
                lock(_isInitializedLock)
                {
                    return _isInitialized;
                }
            }
        }

        public string ControllerModel { get; private set; }

        public string ControllerVersion { get; private set; }

        public ushort MaximumReadWordLength => _plcType == enPLCType.CP1 ? (ushort)499 : (ushort)999;

        public ushort MaximumWriteWordLength => _plcType == enPLCType.CP1 ? (ushort)496 : (ushort)996;

        #endregion

        #region Ctor and Dispose

        public OmronPLC(byte localNodeId, byte remoteNodeId, enConnectionMethod connectionMethod, string remoteHost, int port = 9600, int timeout = 2000, int retries = 1)
        {
            if(localNodeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localNodeId), "The Local Node ID cannot be set to 0");
            }

            if(localNodeId == 255)
            {
                throw new ArgumentOutOfRangeException(nameof(localNodeId), "The Local Node ID cannot be set to 255");
            }

            LocalNodeID = localNodeId;

            if (remoteNodeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remoteNodeId), "The Remote Node ID cannot be set to 0");
            }

            if (remoteNodeId == 255)
            {
                throw new ArgumentOutOfRangeException(nameof(remoteNodeId), "The Remote Node ID cannot be set to 255");
            }

            if(remoteNodeId == localNodeId)
            {
                throw new ArgumentException("The Remote Node ID cannot be the same as the Local Node ID", nameof(remoteNodeId));
            }

            RemoteNodeID = remoteNodeId;

            ConnectionMethod = connectionMethod;

            if (remoteHost == null)
            {
                throw new ArgumentNullException(nameof(remoteHost), "The Remote Host cannot be Null");
            }

            if(remoteHost.Length == 0)
            {
                throw new ArgumentException("The Remote Host cannot be Empty", nameof(remoteHost));
            }

            RemoteHost = remoteHost;

            if(port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "The Port cannot be less than 1");
            }

            Port = port;

            if(timeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "The Timeout Value cannot be less than 1");
            }

            Timeout = timeout;

            if(retries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retries), "The Retries Value cannot be Negative");
            }

            Retries = retries;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (Channel != null)
                    {
                        Channel.Dispose();

                        Channel = null;
                    }

                    lock (_isInitializedLock)
                    {
                        _isInitialized = false;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~OmronPLC()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion


        #region Public Methods

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == true)
                {
                    return;
                }
            }

            // Initialize the Channel
            if (ConnectionMethod == enConnectionMethod.UDP)
            {
                try
                {
                    Channel = new EthernetUDPChannel(RemoteHost, Port);

                    await Channel.InitializeAsync(Timeout, cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    throw new OmronException("Failed to Create the Ethernet UDP Communication Channel for Omron PLC '" + RemoteHost + ":" + Port + "' - The underlying Socket Connection has been Closed");
                }
                catch (TimeoutException)
                {
                    throw new OmronException("Failed to Create the Ethernet UDP Communication Channel within the Timeout Period for Omron PLC '" + RemoteHost + ":" + Port + "'");
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    throw new OmronException("Failed to Create the Ethernet UDP Communication Channel for Omron PLC '" + RemoteHost + ":" + Port + "'", e);
                }
            }
            else if (ConnectionMethod == enConnectionMethod.TCP)
            {
                try
                {
                    Channel = new EthernetTCPChannel(RemoteHost, Port);

                    await Channel.InitializeAsync(Timeout, cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    throw new OmronException("Failed to Create the Ethernet TCP Communication Channel for Omron PLC '" + RemoteHost + ":" + Port + "' - The underlying Socket Connection has been Closed");
                }
                catch (TimeoutException)
                {
                    throw new OmronException("Failed to Create the Ethernet TCP Communication Channel within the Timeout Period for Omron PLC '" + RemoteHost + ":" + Port + "'");
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    throw new OmronException("Failed to Create the Ethernet TCP Communication Channel for Omron PLC '" + RemoteHost + ":" + Port + "'", e);
                }
            }

            await RequestControllerInformation(cancellationToken);

            lock(_isInitializedLock)
            {
                _isInitialized = true;
            }
        }

        public Task<ReadBitsResult> ReadBitAsync(ushort address, byte bitIndex, enMemoryBitDataType dataType, CancellationToken cancellationToken)
        {
            return ReadBitsAsync(address, bitIndex, 1, dataType, cancellationToken);
        }

        public async Task<ReadBitsResult> ReadBitsAsync(ushort address, byte startBitIndex, byte length, enMemoryBitDataType dataType, CancellationToken cancellationToken)
        {
            lock(_isInitializedLock)
            {
                if(_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }
            
            if (startBitIndex > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(startBitIndex), "The Start Bit Index cannot be greater than 15");
            }

            if (length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The Length cannot be Zero");
            }

            if (startBitIndex + length > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The Start Bit Index and Length combined are greater than the Maximum Allowed of 16 Bits");
            }

            if (ValidateBitDataType(dataType) == false)
            {
                throw new ArgumentException("The Data Type '" + Enum.GetName(typeof(enMemoryBitDataType), dataType) + "' is not Supported on this PLC", nameof(dataType));
            }

            if (ValidateBitAddress(address, dataType) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(address), "The Address is greater than the Maximum Address for the '" + Enum.GetName(typeof(enMemoryBitDataType), dataType) + "' Data Type");
            }

            ReadMemoryAreaBitRequest request = new ReadMemoryAreaBitRequest(this, address, startBitIndex, length, dataType);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            return new ReadBitsResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
                Values = ReadMemoryAreaBitResponse.ExtractValues(request, requestResult.Response),
            };
        }

        public Task<ReadWordsResult> ReadWordAsync(ushort address, enMemoryWordDataType dataType, CancellationToken cancellationToken)
        {
            return ReadWordsAsync(address, 1, dataType, cancellationToken);
        }

        public async Task<ReadWordsResult> ReadWordsAsync(ushort startAddress, ushort length, enMemoryWordDataType dataType, CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }

            if (length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The Length cannot be Zero");
            }

            if (length > MaximumReadWordLength)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The Length cannot be greater than " + MaximumReadWordLength.ToString());
            }

            if (ValidateWordDataType(dataType) == false)
            {
                throw new ArgumentException("The Data Type '" + Enum.GetName(typeof(enMemoryWordDataType), dataType) + "' is not Supported on this PLC", nameof(dataType));
            }

            if (ValidateWordStartAddress(startAddress, length, dataType) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(startAddress), "The Start Address and Length combined are greater than the Maximum Address for the '" + Enum.GetName(typeof(enMemoryWordDataType), dataType) + "' Data Type");
            }

            ReadMemoryAreaWordRequest request = new ReadMemoryAreaWordRequest(this, startAddress, length, dataType);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            return new ReadWordsResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
                Values = ReadMemoryAreaWordResponse.ExtractValues(request, requestResult.Response),
                MainResponseCode = requestResult.Response.MainResponseCode,
                SubResponseCode = requestResult.Response.SubResponseCode
            };
        }

        public Task<WriteBitsResult> WriteBitAsync(bool value, ushort address, byte bitIndex, enMemoryBitDataType dataType, CancellationToken cancellationToken)
        {
            return WriteBitsAsync(new bool[] { value }, address, bitIndex, dataType, cancellationToken);
        }

        public async Task<WriteBitsResult> WriteBitsAsync(bool[] values, ushort address, byte startBitIndex, enMemoryBitDataType dataType, CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }

            if (startBitIndex > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(startBitIndex), "The Start Bit Index cannot be greater than 15");
            }
            
            if(values.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "The Values Array cannot be Empty");
            }

            if(startBitIndex + values.Length > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "The Values Array Length was greater than the Maximum Allowed of 16 Bits");
            }

            if (ValidateBitDataType(dataType) == false)
            {
                throw new ArgumentException("The Data Type '" + Enum.GetName(typeof(enMemoryBitDataType), dataType) + "' is not Supported on this PLC", nameof(dataType));
            }

            if (ValidateBitAddress(address, dataType) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(address), "The Address is greater than the Maximum Address for the '" + Enum.GetName(typeof(enMemoryBitDataType), dataType) + "' Data Type");
            }

            WriteMemoryAreaBitRequest request = new WriteMemoryAreaBitRequest(this, address, startBitIndex, dataType, values);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            WriteMemoryAreaBitResponse.Validate(request, requestResult.Response);

            return new WriteBitsResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
            };
        }

        public Task<WriteWordsResult> WriteWordAsync(short value, ushort address, enMemoryWordDataType dataType, CancellationToken cancellationToken)
        {
            return WriteWordsAsync(new short[] { value }, address, dataType, cancellationToken);
        }

        public async Task<WriteWordsResult> WriteWordsAsync(short[] values, ushort startAddress, enMemoryWordDataType dataType, CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }

            if (values.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "The Values Array cannot be Empty");
            }

            if(values.Length > MaximumWriteWordLength)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "The Values Array Length cannot be greater than " + MaximumWriteWordLength.ToString());
            }

            if (ValidateWordDataType(dataType) == false)
            {
                throw new ArgumentException("The Data Type '" + Enum.GetName(typeof(enMemoryWordDataType), dataType) + "' is not Supported on this PLC", nameof(dataType));
            }

            if (ValidateWordStartAddress(startAddress, values.Length, dataType) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(startAddress), "The Start Address and Values Array Length combined are greater than the Maximum Address for the '" + Enum.GetName(typeof(enMemoryWordDataType), dataType) + "' Data Type");
            }

            WriteMemoryAreaWordRequest request = new WriteMemoryAreaWordRequest(this, startAddress, dataType, values);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            WriteMemoryAreaWordResponse.Validate(request, requestResult.Response);

            return new WriteWordsResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
                MainResponseCode = requestResult.Response.MainResponseCode,
                SubResponseCode = requestResult.Response.SubResponseCode
            };
        }

        public async Task<ReadClockResult> ReadClockAsync(CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }

            ReadClockRequest request = new ReadClockRequest(this);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            ReadClockResponse.ClockResult result = ReadClockResponse.ExtractClock(request, requestResult.Response);

            return new ReadClockResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
                Clock = result.ClockDateTime,
                DayOfWeek = result.DayOfWeek
            };
        }

        public Task<WriteClockResult> WriteClockAsync(DateTime newDateTime, CancellationToken cancellationToken)
        {
            return WriteClockAsync(newDateTime, (int)newDateTime.DayOfWeek, cancellationToken);
        }

        public async Task<WriteClockResult> WriteClockAsync(DateTime newDateTime, int newDayOfWeek, CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }

            DateTime minDateTime = new DateTime(1998, 1, 1, 0, 0, 0);

            if (newDateTime < minDateTime)
            {
                throw new ArgumentOutOfRangeException(nameof(newDateTime), "The Date Time Value cannot be less than '" + minDateTime.ToString() + "'");
            }

            DateTime maxDateTime = new DateTime(2069, 12, 31, 23, 59, 59);

            if (newDateTime > maxDateTime)
            {
                throw new ArgumentOutOfRangeException(nameof(newDateTime), "The Date Time Value cannot be greater than '" + maxDateTime.ToString() + "'");
            }

            if(newDayOfWeek < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newDayOfWeek), "The Day of Week Value cannot be less than 0");
            }

            if(newDayOfWeek > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(newDayOfWeek), "The Day of Week Value cannot be greater than 6");
            }
            
            WriteClockRequest request = new WriteClockRequest(this, newDateTime, (byte)newDayOfWeek);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            WriteClockResponse.Validate(request, requestResult.Response);

            return new WriteClockResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
            };
        }

        public async Task<ReadCycleTimeResult> ReadCycleTimeAsync(CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }

            if (IsNSeries == true && _plcType != enPLCType.NJ101 && _plcType != enPLCType.NJ301 && _plcType != enPLCType.NJ501)
            {
                throw new OmronException("Read Cycle Time is not Supported on the NX/NY Series PLC");
            }

            ReadCycleTimeRequest request = new ReadCycleTimeRequest(this);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            ReadCycleTimeResponse.CycleTimeResult result = ReadCycleTimeResponse.ExtractCycleTime(request, requestResult.Response);

            return new ReadCycleTimeResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
                MinimumCycleTime = result.MinimumCycleTime,
                MaximumCycleTime = result.MaximumCycleTime,
                AverageCycleTime = result.AverageCycleTime,
            };
        }

        public async Task<ReadOperatingModeResult> ReadOperatingModeAsync(CancellationToken cancellationToken)
        {
            lock (_isInitializedLock)
            {
                if (_isInitialized == false)
                {
                    throw new OmronException("This Omron PLC must be Initialized first before any Requests can be Processed");
                }
            }

            ReadOperatingModeRequest request = new ReadOperatingModeRequest(this);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            ReadOperatingModeResponse.OperatingModeResult result = ReadOperatingModeResponse.ExtractOperatingMode(request, requestResult.Response);

            return new ReadOperatingModeResult
            {
                BytesSent = requestResult.BytesSent,
                PacketsSent = requestResult.PacketsSent,
                BytesReceived = requestResult.BytesReceived,
                PacketsReceived = requestResult.PacketsReceived,
                Duration = requestResult.Duration,
                Status = result.Status,
                Mode = result.Mode,
                FatalErrorData = result.FatalErrorData,
                NonFatalErrorData = result.NonFatalErrorData,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage
            };
        }

        public int GetAreaSize(enMemoryWordDataType area)
        {
            return area switch
            {
                enMemoryWordDataType.DataMemory => (_plcType == enPLCType.NX1P2 ? 16000 : 32768),
                enMemoryWordDataType.CommonIO => 6144,
                enMemoryWordDataType.Work => 512,
                enMemoryWordDataType.Holding => 1536,//TODO FB 512
                enMemoryWordDataType.Auxiliary => (_plcType == enPLCType.CJ2 ? 11536 : 960),
                enMemoryWordDataType.ExtendedMemoryBank0 => 32768,
                enMemoryWordDataType.ExtendedMemoryBank1 => 32768,
                enMemoryWordDataType.ExtendedMemoryBank2 => 32768,
                enMemoryWordDataType.ExtendedMemoryBank3 => 32768,
                _ => 0,
            };
        }

        #endregion

        #region Private Methods

        private bool ValidateBitAddress(ushort address, enMemoryBitDataType dataType)
        {
            return dataType switch
            {
                enMemoryBitDataType.DataMemory => address < GetAreaSize(enMemoryWordDataType.DataMemory),
                enMemoryBitDataType.CommonIO => address < GetAreaSize(enMemoryWordDataType.CommonIO),
                enMemoryBitDataType.Work => address < GetAreaSize(enMemoryWordDataType.Work),
                enMemoryBitDataType.Holding => address < GetAreaSize(enMemoryWordDataType.Holding),
                enMemoryBitDataType.Auxiliary => address < GetAreaSize(enMemoryWordDataType.Auxiliary),
                _ => false,
            };
        }

        private bool ValidateBitDataType(enMemoryBitDataType dataType)
        {
            return dataType switch
            {
                enMemoryBitDataType.DataMemory => _plcType != enPLCType.CP1,
                enMemoryBitDataType.CommonIO => true,
                enMemoryBitDataType.Work => true,
                enMemoryBitDataType.Holding => true,
                enMemoryBitDataType.Auxiliary => !IsNSeries,
                _ => false,
            };
        }

        private bool ValidateWordStartAddress(ushort startAddress, int length, enMemoryWordDataType dataType)
        {
            return dataType switch
            {
                enMemoryWordDataType.DataMemory => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.CommonIO => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.Work => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.Holding => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.Auxiliary => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.ExtendedMemoryBank0 => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.ExtendedMemoryBank1 => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.ExtendedMemoryBank2 => startAddress + (length - 1) < GetAreaSize(dataType),
                enMemoryWordDataType.ExtendedMemoryBank3 => startAddress + (length - 1) < GetAreaSize(dataType),
                _ => false,
            };
        }

        private bool ValidateWordDataType(enMemoryWordDataType dataType)
        {
            return dataType switch
            {
                enMemoryWordDataType.DataMemory => true,
                enMemoryWordDataType.CommonIO => true,
                enMemoryWordDataType.Work => true,
                enMemoryWordDataType.Holding => true,
                enMemoryWordDataType.Auxiliary => !IsNSeries,
                enMemoryWordDataType.ExtendedMemoryBank0 => true,
                enMemoryWordDataType.ExtendedMemoryBank1 => true,
                enMemoryWordDataType.ExtendedMemoryBank2 => true,
                enMemoryWordDataType.ExtendedMemoryBank3 => true,
                _ => false,
            };
        }

        private async Task RequestControllerInformation(CancellationToken cancellationToken)
        {
            ReadCPUUnitDataRequest request = new ReadCPUUnitDataRequest(this);

            ProcessRequestResult requestResult = await Channel.ProcessRequestAsync(request, Timeout, Retries, cancellationToken);

            ReadCPUUnitDataResponse.CPUUnitDataResult result = ReadCPUUnitDataResponse.ExtractData(requestResult.Response);

            if(result.ControllerModel != null && result.ControllerModel.Length > 0)
            {
                ControllerModel = result.ControllerModel;

                if (ControllerModel.StartsWith("NJ101"))
                {
                    _plcType = enPLCType.NJ101;
                }
                else if (ControllerModel.StartsWith("NJ301"))
                {
                    _plcType = enPLCType.NJ301;
                }
                else if (ControllerModel.StartsWith("NJ501"))
                {
                    _plcType = enPLCType.NJ501;
                }
                else if (ControllerModel.StartsWith("NX1P2"))
                {
                    _plcType = enPLCType.NX1P2;
                }
                else if (ControllerModel.StartsWith("NX102"))
                {
                    _plcType = enPLCType.NX102;
                }
                else if (ControllerModel.StartsWith("NX701"))
                {
                    _plcType = enPLCType.NX701;
                }
                else if(ControllerModel.StartsWith("NJ") || ControllerModel.StartsWith("NX") || ControllerModel.StartsWith("NY"))
                {
                    _plcType = enPLCType.NJ_NX_NY_Series;
                }
                else if(ControllerModel.StartsWith("CJ2"))
                {
                    _plcType = enPLCType.CJ2;
                }
                else if(ControllerModel.StartsWith("CP1"))
                {
                    _plcType = enPLCType.CP1;
                }
                else if(ControllerModel.StartsWith("C"))
                {
                    _plcType = enPLCType.C_Series;
                }
                else
                {
                    _plcType = enPLCType.Unknown;
                }
            }

            if(result.ControllerVersion != null && result.ControllerVersion.Length > 0)
            {
                ControllerVersion = result.ControllerVersion;
            }
        }

        

        #endregion

    }
}
