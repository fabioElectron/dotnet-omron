namespace RICADO.Omron
{
    

    public enum ConnectionStatus
    {
        Undefined,
        Initalized,
        Connected,
        ConnectionFault,
        Reconnecting,
        Disconnected
    }

    public enum PlcTypes
    {
        NJ101,
        NJ301,
        NJ501,
        NX1P2,
        NX102,
        NX701,
        NY512,
        NY532,
        NJ_NX_NY_Series,
        CJ2,
        CP1,
        C_Series,
        Unknown,
    }

    public enum MemoryBitDataType : byte
    {
        DataMemory = 0x2,
        CommonIO = 0x30,
        Work = 0x31,
        Holding = 0x32,
        Auxiliary = 0x33,
    }

    public enum MemoryWordDataType : byte
    {
        DataMemory = 0x82,
        CommonIO = 0xB0,
        Work = 0xB1,
        Holding = 0xB2,
        Auxiliary = 0xB3,
        ExtendedMemoryBank0 = 0xA0, // 0x50 on CJ2
        ExtendedMemoryBank1 = 0xA1, // 0x51 on CJ2
        ExtendedMemoryBank2 = 0xA2, // 0x52 on CJ2
        ExtendedMemoryBank3 = 0xA3, // 0x53 on CJ2
    }

    internal enum FunctionCodes : byte
    {
        MemoryArea = 0x01,
        ParameterArea = 0x02,
        ProgramArea = 0x03,
        OperatingMode = 0x04,
        MachineConfiguration = 0x05,
        Status = 0x06,
        TimeData = 0x07,
        MessageDisplay = 0x09,
        AccessRights = 0x0C,
        ErrorLog = 0x21,
        FINSWriteLog = 0x21,
        FileMemory = 0x22,
        Debugging = 0x23,
        SerialGateway = 0x28,
    }

    internal enum MemoryAreaFunctionCodes : byte
    {
        Read = 0x01,
        Write = 0x02,
        Fill = 0x03,
        MultipleRead = 0x04,
        Transfer = 0x05,
    }

    internal enum ParameterAreaFunctionCodes : byte
    {
        Read = 0x01,
        Write = 0x02,
        Fill = 0x03,
    }

    internal enum ProgramAreaFunctionCodes : byte
    {
        Read = 0x06,
        Write = 0x07,
        Clear = 0x08,
    }

    internal enum OperatingModeFunctionCodes : byte
    {
        RunMode = 0x01,
        StopMode = 0x02,
    }

    internal enum MachineConfigurationFunctionCodes : byte
    {
        ReadCPUUnitData = 0x01,
        ReadConnectionData = 0x02,
    }

    internal enum StatusFunctionCodes : byte
    {
        ReadCPUUnitStatus = 0x01,
        ReadCycleTime = 0x20,
    }

    internal enum TimeDataFunctionCodes : byte
    {
        ReadClock = 0x01,
        WriteClock = 0x02,
    }

    internal enum MessageDisplayFunctionCodes : byte
    {
        Read = 0x20,
    }

    internal enum AccessRightsFunctionCodes : byte
    {
        Acquire = 0x01,
        ForcedAcquire = 0x02,
        Release = 0x03,
    }

    internal enum ErrorLogFunctionCodes : byte
    {
        ClearMessages = 0x01,
        Read = 0x02,
        ClearLog = 0x03,
    }

    internal enum FinsWriteLogFunctionCodes : byte
    {
        Read = 0x40,
        Clear = 0x41,
    }

    internal enum FileMemoryFunctionCodes : byte
    {
        ReadFileName = 0x01,
        ReadSingleFile = 0x02,
        WriteSingleFile = 0x03,
        FormatMemory = 0x04,
        DeleteFile = 0x05,
        CopyFile = 0x07,
        ChangeFileName = 0x08,
        MemoryAreaTransfer = 0x0A,
        ParameterAreaTransfer = 0x0B,
        ProgramAreaTransfer = 0x0C,
        CreateOrDeleteDirectory = 0x15,
    }

    internal enum DebuggingFunctionCodes : byte
    {
        ForceBits = 0x01,
        ClearForcedBits = 0x02,
    }

    internal enum SerialGatewayFunctionCodes : byte
    {
        ConvertToCompoWayFCommand = 0x03,
        ConvertToModbusRTUCommand = 0x04,
        ConvertToModbusASCIICommand = 0x05,
    }

    public enum EndCode : byte
    {
        NormalCompletion = 0x00,
        LocalNodeError = 0x01,
        ControllerError = 0x03,
        ServiceNotSupported = 0x04,
        RoutingTableError = 0x05,
        CommandFormatError = 0x10,
        ParameterError = 0x11,
        ReadNotPossinle = 0x20,
        WriteNotPossible = 0x21,
        NotExecutableInCurrentMode = 0x22,
        NoSuchDevice = 0x23,
        CannotStartOrStop = 0x24,
        UnitError = 0x25,
        CommandError = 0x26,
        AccessRightError = 0x30,
        Abort = 0x40
    }

}
