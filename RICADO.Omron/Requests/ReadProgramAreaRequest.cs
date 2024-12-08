namespace RICADO.Omron.Requests
{
    internal class ReadProgramAreaRequest : FINSRequest
    {
        internal readonly ushort ByteCount;

        public ReadProgramAreaRequest(OmronPLC plc, uint address, ushort byteCount, bool includeLastAddressData)
            : base(plc, (byte)FunctionCodes.ProgramArea, (byte)ProgramAreaFunctionCodes.Read)
        {
            ByteCount = byteCount;
            if (includeLastAddressData)
            {
                byteCount = (ushort)(byteCount | 0x8000);
                ByteCount++;
            }
            Body = new byte[6];
            Body[0] = 0xFF;
            Body[1] = 0xFF;
            // Address
            Body[2] = (byte)(address >> 24);
            Body[3] = (byte)(address >> 16);
            Body[4] = (byte)(address >> 8);
            Body[5] = (byte)(address & 0x000000FF);
            // Length
            Body[6] = (byte)(byteCount >> 8);
            Body[7] = (byte)(byteCount & 0x00FF);
        }
    }
}
