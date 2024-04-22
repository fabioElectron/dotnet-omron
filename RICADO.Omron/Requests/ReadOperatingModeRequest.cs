using System.Collections.Generic;

namespace RICADO.Omron.Requests
{
    internal class ReadOperatingModeRequest : FINSRequest
    {
        #region Constructor

        public ReadOperatingModeRequest(OmronPLC plc) : base(plc)
        {
        }

        #endregion


        #region Internal Methods

        internal static ReadOperatingModeRequest CreateNew(OmronPLC plc)
        {
            return new ReadOperatingModeRequest(plc)
            {
                FunctionCode = (byte)enFunctionCode.Status,
                SubFunctionCode = (byte)enStatusFunctionCode.ReadCPUUnitStatus,
            };
        }

        #endregion


        #region Protected Methods

        protected override List<byte> BuildRequestData()
        {
            return new List<byte>();
        }

        #endregion
    }

}
