using RICADO.Omron.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace RICADO.Omron.Responses
{
    internal class ReadOperatingModeResponse
    {
        #region Constants

        internal const int STATUS_ITEM_LENGTH = 1;
        internal const int MODE_ITEM_LENGTH = 1;
        internal const int FATAL_ERROR_ITEM_LENGTH = 2;
        internal const int NON_FATAL_ERROR_LENGTH = 2;
        internal const int ERROR_CODE_LENGTH = 2;
        internal const int ERROR_MESSAGE_LENGTH = 0; // or 16 if error code != 0

        #endregion


        #region Internal Methods

        internal static OperatingModeResult ExtractOperatingMode(ReadOperatingModeRequest request, FINSResponse response)
        {
            var totalLength = STATUS_ITEM_LENGTH + MODE_ITEM_LENGTH + FATAL_ERROR_ITEM_LENGTH + NON_FATAL_ERROR_LENGTH + ERROR_CODE_LENGTH + ERROR_MESSAGE_LENGTH;
            if (response.Data.Length < totalLength)
            {
                throw new FINSException("The Response Data Length of '" + response.Data.Length.ToString() + "' was too short - Expecting a Length of '" + (totalLength).ToString() + "'");
            }

            ReadOnlyMemory<byte> data = response.Data;
            byte errorCode = BCDConverter.ToByte(data.ToArray()[STATUS_ITEM_LENGTH + MODE_ITEM_LENGTH + FATAL_ERROR_ITEM_LENGTH + NON_FATAL_ERROR_LENGTH]);

            return new OperatingModeResult
            {
                Status = data.ToArray()[0],
                Mode = data.ToArray()[STATUS_ITEM_LENGTH],
                FatalErrorData = BitConverter.ToUInt16(new byte[] { data.ToArray()[STATUS_ITEM_LENGTH + MODE_ITEM_LENGTH + 1], data.ToArray()[STATUS_ITEM_LENGTH + MODE_ITEM_LENGTH] }),
                NonFatalErrorData = BitConverter.ToUInt16(new byte[] { data.ToArray()[STATUS_ITEM_LENGTH + MODE_ITEM_LENGTH + FATAL_ERROR_ITEM_LENGTH + 1], data.ToArray()[STATUS_ITEM_LENGTH + MODE_ITEM_LENGTH + FATAL_ERROR_ITEM_LENGTH] }),
                ErrorCode = errorCode,
                ErrorMessage = (errorCode == 0) ? string.Empty : extractStringValue(data.Slice(totalLength, 16).ToArray())
            };
    }

    #endregion


    #region Private Methods

    private static string extractStringValue(byte[] bytes)
    {
        List<byte> stringBytes = new List<byte>(bytes.Length);

        foreach (byte byteValue in bytes)
        {
            if (byteValue > 0)
            {
                stringBytes.Add(byteValue);
            }
            else
            {
                break;
            }
        }

        if (stringBytes.Count == 0)
        {
            return "";
        }

        return ASCIIEncoding.ASCII.GetString(stringBytes.ToArray()).Trim();
    }

    #endregion


    #region Structs

    internal struct OperatingModeResult
    {
        internal byte Status;
        internal byte Mode;
        internal ushort FatalErrorData;
        internal ushort NonFatalErrorData;
        internal byte ErrorCode;
        internal string ErrorMessage;
    }

    #endregion
}
}
