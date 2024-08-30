using System.Linq;
using RICADO.Omron.Requests;

namespace RICADO.Omron.Responses
{
    internal class ReadMemoryAreaBitResponse
    {
        
        internal static bool[] ExtractValues(ReadMemoryAreaBitRequest request, FINSResponse response)
        {
            if(response.Data.Length < request.Length)
                throw new FINSException("The Response Data Length of '" + response.Data.Length.ToString() + "' was too short - Expecting a Length of '" + request.Length.ToString() + "'");

            return response.Data.Select(value => value != 0).ToArray();
        }

    }
}
