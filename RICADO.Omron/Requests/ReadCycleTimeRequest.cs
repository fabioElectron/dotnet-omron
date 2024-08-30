namespace RICADO.Omron.Requests
{
    internal class ReadCycleTimeRequest : FINSRequest
    {

        public ReadCycleTimeRequest(OmronPLC plc) 
            : base(plc, (byte)FunctionCodes.Status, (byte)StatusFunctionCodes.ReadCycleTime)
        {
            Body = [0x01];// Read Cycle Time
        }

    }
}
