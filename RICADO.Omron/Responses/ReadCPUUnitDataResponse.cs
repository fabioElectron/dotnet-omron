using System;
using System.Collections.Generic;
using System.Text;

namespace RICADO.Omron.Responses
{

    internal class CPUUnitDataResult
    {
        internal string ControllerModel;
        internal string ControllerVersion;
        internal byte DipSwitchStatus;
        internal byte EMBankCount;
        internal ushort ProgramAreaSizeKW;
        internal byte BitAreasSizeKB; // always 23
        internal ushort DMAreaSizeW; // always 32768
        internal byte TimersCount; // always 8
        internal byte EMBankCountNonFile; // 1 bank = 32768 words
        internal byte MemoryCardType; // 0 no memory card, 4 flash memory
        internal ushort MemoryCardSizeKB; 
    }

    internal class ReadCPUUnitDataResponse
    {

        internal const int CONTROLLER_MODEL_LENGTH = 20;
        internal const int CONTROLLER_VERSION_LENGTH = 20;
        internal const int SYSTEM_RESERVED_LENGTH = 40;
        internal const int AREA_DATA_LENGTH = 12;

        internal const int AREA_DATA_INDEX = CONTROLLER_MODEL_LENGTH + CONTROLLER_VERSION_LENGTH + SYSTEM_RESERVED_LENGTH;

        internal static CPUUnitDataResult ExtractData(FINSResponse response)
        {
            int expectedLength = CONTROLLER_MODEL_LENGTH + CONTROLLER_VERSION_LENGTH + SYSTEM_RESERVED_LENGTH + AREA_DATA_LENGTH;

            if (response.Data.Length < expectedLength)
                throw new FINSException("The Response Data Length of '" + response.Data.Length.ToString() + "' was too short - Expecting a Length of '" + expectedLength.ToString() + "'");

            ReadOnlyMemory<byte> data = response.Data;
            return new CPUUnitDataResult()
            {
                ControllerModel = ExtractStringValue(data.Slice(0, CONTROLLER_MODEL_LENGTH).ToArray()),
                ControllerVersion = ExtractStringValue(data.Slice(CONTROLLER_MODEL_LENGTH, CONTROLLER_VERSION_LENGTH).ToArray()),
                DipSwitchStatus = response.Data[CONTROLLER_MODEL_LENGTH + CONTROLLER_VERSION_LENGTH],
                EMBankCount = response.Data[CONTROLLER_MODEL_LENGTH + CONTROLLER_VERSION_LENGTH + 1],
                ProgramAreaSizeKW = (ushort)(response.Data[AREA_DATA_INDEX + 1] | (response.Data[AREA_DATA_INDEX + 0] << 8)),
                BitAreasSizeKB = response.Data[AREA_DATA_INDEX + 2],
                DMAreaSizeW = (ushort)(response.Data[AREA_DATA_INDEX + 4] | (response.Data[AREA_DATA_INDEX + 3] << 8)),
                TimersCount = response.Data[AREA_DATA_INDEX + 5],
                EMBankCountNonFile = response.Data[AREA_DATA_INDEX + 6],
                MemoryCardType = response.Data[AREA_DATA_INDEX + 9],
                MemoryCardSizeKB = (ushort)(response.Data[AREA_DATA_INDEX + 11] | (response.Data[AREA_DATA_INDEX + 10] << 8))
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
