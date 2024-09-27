namespace RICADO.Omron.Requests
{
    internal class WriteOperatingModeRequest : FINSRequest
    {
        public WriteOperatingModeRequest(OmronPLC plc, bool run, bool monitor) 
            : base(plc, (byte)FunctionCodes.OperatingMode, run? (byte)OperatingModeFunctionCodes.RunMode : (byte)OperatingModeFunctionCodes.StopMode)
        {
            if(run)
            {
                if(monitor)
                    Body = [0xFF, 0xFF, 0x02];
                else
                    Body = [0xFF, 0xFF, 0x04];
            }
            else
            Body = [0xFF,0xFF];
        }
    }
}
