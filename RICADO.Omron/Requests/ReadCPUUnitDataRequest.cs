namespace RICADO.Omron.Requests
{
    internal class ReadCPUUnitDataRequest : FINSRequest
    {

        public ReadCPUUnitDataRequest(OmronPLC plc) 
            : base(plc, (byte)enFunctionCode.MachineConfiguration, (byte)enMachineConfigurationFunctionCode.ReadCPUUnitData)
        {
            Body = [0];// Read Data
        }

    }
}
