using RICADO.Omron.Requests;
using System;

namespace RICADO.Omron.Responses
{
    internal class ReadProgramAreaResponse
    {
        internal static byte[] ExtractValues(ReadProgramAreaRequest request, FINSResponse response)
        {
            if (response.Data.Length < request.ByteCount)
                throw new FINSException("The Response Data Length of '" + response.Data.Length.ToString() + "' was too short - Expecting a Length of '" + (request.ByteCount * 2).ToString() + "'");

            byte[] values = new byte[request.ByteCount];
            Array.Copy(response.Data, values, request.ByteCount);
            return values;
        }
    }
}
