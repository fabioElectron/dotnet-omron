namespace RICADO.Omron.Requests
{
    internal class WriteMemoryAreaBitRequest : FINSRequest
    {

        public WriteMemoryAreaBitRequest(OmronPLC plc, ushort address, byte startBitIndex, MemoryBitDataType dataType, bool[] values) 
            : base(plc, (byte)FunctionCodes.MemoryArea, (byte)MemoryAreaFunctionCodes.Write)
        {
            Body = new byte[6 + values.Length];
            Body[0] = (byte)dataType;// Memory Area Data Type
            // Address
            Body[1] = (byte)(address >> 8);
            Body[2] = (byte)(address & 0x00FF);
            Body[3] = startBitIndex;// Bit Index
            // Length
            ushort length = (ushort)values.Length;
            Body[4] = (byte)(length >> 8);
            Body[5] = (byte)(length & 0x00FF);
            // Bit Values
            for (int i = 0; i < values.Length; i++)
            {
                Body[6 + i] = values[i] ? (byte)1 : (byte)0;
            }
        }

    }
}
