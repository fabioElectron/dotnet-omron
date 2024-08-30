namespace RICADO.Omron.Requests
{
    internal class ReadMemoryAreaBitRequest : FINSRequest
    {

        internal readonly ushort Length;

        public ReadMemoryAreaBitRequest(OmronPLC plc, ushort address, byte startBitIndex, ushort length, MemoryBitDataType dataType)
            : base(plc, (byte)FunctionCodes.MemoryArea, (byte)MemoryAreaFunctionCodes.Read)
        {
            Length = length;
            Body = new byte[6];
            Body[0] = (byte)dataType;// Memory Area Data Type
            // Address
            Body[1] = (byte)(address >> 8);
            Body[2] = (byte)(address & 0x00FF);
            Body[3] = startBitIndex;// Bit Index
            // Length
            Body[4] = (byte)(length >> 8);
            Body[5] = (byte)(length & 0x00FF);
        }

    }
}
