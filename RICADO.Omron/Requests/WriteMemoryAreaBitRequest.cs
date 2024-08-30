using System;
using System.Linq;

namespace RICADO.Omron.Requests
{
    internal class WriteMemoryAreaBitRequest : FINSRequest
    {

        public WriteMemoryAreaBitRequest(OmronPLC plc, ushort address, byte startBitIndex, enMemoryBitDataType dataType, bool[] values) 
            : base(plc, (byte)enFunctionCode.MemoryArea, (byte)enMemoryAreaFunctionCode.Write)
        {
            Body = new byte[6 + values.Length];
            Body[0] = (byte)dataType;// Memory Area Data Type
            var bytes = BitConverter.GetBytes(address).Reverse().ToArray();// Address
            Body[1] = bytes[0];
            Body[2] = bytes[1];
            Body[3] = startBitIndex;// Bit Index
            bytes = BitConverter.GetBytes((ushort)values.Length).Reverse().ToArray();// Length
            Body[4] = bytes[0];
            Body[5] = bytes[1];
            // Bit Values
            for (int i = 0; i < values.Length; i++)
            {
                Body[6 + i] = values[i] ? (byte)1 : (byte)0;
            }
        }

    }
}
