namespace RICADO.Omron.Requests
{
    internal class ReadClockRequest : FINSRequest
    {

        public ReadClockRequest(OmronPLC plc)
            : base(plc, (byte)enFunctionCode.TimeData, (byte)enTimeDataFunctionCode.ReadClock)
        {
            Body = [];
        }

    }
}
