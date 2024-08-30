namespace RICADO.Omron.Requests
{
    internal class ReadMemoryAreaWordRequest : FINSRequest
    {
        internal readonly ushort Length;

        public ReadMemoryAreaWordRequest(OmronPLC plc, ushort address, ushort length, MemoryWordDataType dataType)
            : base(plc, (byte)FunctionCodes.MemoryArea, (byte)MemoryAreaFunctionCodes.Read)
        {
            Length = length;
            Body = new byte[6];
            Body[0] = (byte)dataType;// Memory Area Data Type
            // Address
            Body[1] = (byte)(address >> 8);
            Body[2] = (byte)(address & 0x00FF);
            Body[3] = 0x00;// Reserved
            // Length
            Body[4] = (byte)(length >> 8);
            Body[5] = (byte)(length & 0x00FF);
        }

    }
}
