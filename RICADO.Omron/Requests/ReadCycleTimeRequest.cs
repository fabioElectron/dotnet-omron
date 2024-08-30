namespace RICADO.Omron.Requests
{
    internal class ReadCycleTimeRequest : FINSRequest
    {

        public ReadCycleTimeRequest(OmronPLC plc) 
            : base(plc, (byte)enFunctionCode.Status, (byte)enStatusFunctionCode.ReadCycleTime)
        {
            Body = [0x01];// Read Cycle Time
        }

    }
}
