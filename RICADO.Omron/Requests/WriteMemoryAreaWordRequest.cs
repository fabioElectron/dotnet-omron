namespace RICADO.Omron.Requests
{
    internal class WriteMemoryAreaWordRequest : FINSRequest
    {

        public WriteMemoryAreaWordRequest(OmronPLC plc, ushort address, MemoryWordDataType dataType, ushort[] values)
            : base(plc, (byte)FunctionCodes.MemoryArea, (byte)MemoryAreaFunctionCodes.Write)
        {
            Body = new byte[6 + 2 * values.Length];
            Body[0] = (byte)dataType;// Memory Area Data Type
            // Address
            Body[1] = (byte)(address >> 8);
            Body[2] = (byte)(address & 0x00FF);
            Body[3] = 0x00;// Reserved
            // Length
            ushort length = (ushort)values.Length;
            Body[4] = (byte)(length >> 8);
            Body[5] = (byte)(length & 0x00FF);
            // Word Values
            for (int i = 0; i < values.Length; i++)
            {
                Body[6 + i * 2] = (byte)(values[i] >> 8);
                Body[6 + i * 2 + 1] = (byte)(values[i] & 0x00FF);
            }
        }

    }
}
