namespace RICADO.Omron.Requests
{
    internal class ReadCPUUnitDataRequest : FINSRequest
    {

        public ReadCPUUnitDataRequest(OmronPLC plc) 
            : base(plc, (byte)FunctionCodes.MachineConfiguration, (byte)MachineConfigurationFunctionCodes.ReadCPUUnitData)
        {
            Body = [0];// Read Data
        }

    }
}
