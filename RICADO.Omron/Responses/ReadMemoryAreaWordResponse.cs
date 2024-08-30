using System.Collections.Generic;
using RICADO.Omron.Requests;

namespace RICADO.Omron.Responses
{
    internal class ReadMemoryAreaWordResponse
    {
        
        internal static ushort[] ExtractValues(ReadMemoryAreaWordRequest request, FINSResponse response)
        {
            if (response.Data.Length < request.Length * 2)
                throw new FINSException("The Response Data Length of '" + response.Data.Length.ToString() + "' was too short - Expecting a Length of '" + (request.Length * 2).ToString() + "'");

            List<ushort> values = new();
            for(int i = 0; i < request.Length * 2; i += 2)
                values.Add((ushort)(response.Data[i + 1] | (response.Data[i] << 8)));

            return values.ToArray();
        }

    }
}
