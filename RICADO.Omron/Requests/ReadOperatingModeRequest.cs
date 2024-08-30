namespace RICADO.Omron.Requests
{
    internal class ReadOperatingModeRequest : FINSRequest
    {

        public ReadOperatingModeRequest(OmronPLC plc) 
            : base(plc, (byte)enFunctionCode.Status, (byte)enStatusFunctionCode.ReadCPUUnitStatus)
        {
            Body = [];
        }

    }
}
