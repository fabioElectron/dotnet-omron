namespace RICADO.Omron.Requests
{
    internal class WriteOperatingModeRequest : FINSRequest
    {
        public WriteOperatingModeRequest(OmronPLC plc, bool run) 
            : base(plc, (byte)FunctionCodes.OperatingMode, run? (byte)OperatingModeFunctionCodes.RunMode : (byte)OperatingModeFunctionCodes.StopMode)
        {
            Body = [];
        }
    }
}
