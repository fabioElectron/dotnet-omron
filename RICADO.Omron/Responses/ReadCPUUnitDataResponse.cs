using System;
using System.Collections.Generic;
using System.Text;

namespace RICADO.Omron.Responses
{

    internal class CPUUnitDataResult
    {
        internal string ControllerModel;
        internal string ControllerVersion;
    }

    internal class ReadCPUUnitDataResponse
    {

        internal const int CONTROLLER_MODEL_LENGTH = 20;
        internal const int CONTROLLER_VERSION_LENGTH = 20;
        internal const int SYSTEM_RESERVED_LENGTH = 40;
        internal const int AREA_DATA_LENGTH = 12;



        internal static CPUUnitDataResult ExtractData(FINSResponse response)
        {
            int expectedLength = CONTROLLER_MODEL_LENGTH + CONTROLLER_VERSION_LENGTH + SYSTEM_RESERVED_LENGTH + AREA_DATA_LENGTH;

            if (response.Data.Length < expectedLength)
                throw new FINSException("The Response Data Length of '" + response.Data.Length.ToString() + "' was too short - Expecting a Length of '" + expectedLength.ToString() + "'");

            ReadOnlyMemory<byte> data = response.Data;
            return new CPUUnitDataResult()
            {
                ControllerModel = ExtractStringValue(data.Slice(0, CONTROLLER_MODEL_LENGTH).ToArray()),
                ControllerVersion = ExtractStringValue(data.Slice(CONTROLLER_MODEL_LENGTH, CONTROLLER_VERSION_LENGTH).ToArray())
            };
        }

        private static string ExtractStringValue(byte[] bytes)
        {
            List<byte> stringBytes = new List<byte>(bytes.Length);

            foreach (byte byteValue in bytes)
            {
                if (byteValue > 0)
                    stringBytes.Add(byteValue);
                else
                    break;
            }

            if (stringBytes.Count == 0)
                return "";

            return ASCIIEncoding.ASCII.GetString(stringBytes.ToArray()).Trim();
        }

    }
}
