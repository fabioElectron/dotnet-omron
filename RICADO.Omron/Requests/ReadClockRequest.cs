namespace RICADO.Omron.Requests
{
    internal class ReadClockRequest : FINSRequest
    {

        public ReadClockRequest(OmronPLC plc)
            : base(plc, (byte)FunctionCodes.TimeData, (byte)TimeDataFunctionCodes.ReadClock)
        {
            Body = [];
        }

    }
}
