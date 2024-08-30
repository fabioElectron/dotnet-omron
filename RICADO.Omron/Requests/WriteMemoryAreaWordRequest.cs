using System;
using System.Linq;

namespace RICADO.Omron.Requests
{
    internal class WriteMemoryAreaWordRequest : FINSRequest
    {

        public WriteMemoryAreaWordRequest(OmronPLC plc, ushort startAddress, enMemoryWordDataType dataType, short[] values)
            : base(plc, (byte)enFunctionCode.MemoryArea, (byte)enMemoryAreaFunctionCode.Write)
        {
            Body = new byte[6 + 2 * values.Length];
            Body[0] = (byte)dataType;// Memory Area Data Type
            var bytes = BitConverter.GetBytes(startAddress).Reverse().ToArray();// Address
            Body[1] = bytes[0];
            Body[2] = bytes[1];
            Body[3] = 0x00;// Reserved
            bytes = BitConverter.GetBytes((ushort)values.Length).Reverse().ToArray();// Length
            Body[4] = bytes[0];
            Body[5] = bytes[1];
            // Word Values
            for (int i = 0; i < values.Length; i++)
            {
                bytes = BitConverter.GetBytes(values[i]).Reverse().ToArray();
                Body[6 + i * 2] = bytes[0];
                Body[6 + i * 2 + 1] = bytes[1];
            }
        }

    }
}
