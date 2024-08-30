namespace RICADO.Omron.Requests
{
    internal class ReadOperatingModeRequest : FINSRequest
    {

        public ReadOperatingModeRequest(OmronPLC plc) 
            : base(plc, (byte)FunctionCodes.Status, (byte)StatusFunctionCodes.ReadCPUUnitStatus)
        {
            Body = [];
        }

    }
}
