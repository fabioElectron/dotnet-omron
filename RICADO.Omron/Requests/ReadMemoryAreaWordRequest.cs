using System;
using System.Linq;

namespace RICADO.Omron.Requests
{
    internal class ReadMemoryAreaWordRequest : FINSRequest
    {
        internal readonly ushort Length;

        public ReadMemoryAreaWordRequest(OmronPLC plc, ushort startAddress, ushort length, enMemoryWordDataType dataType)
            : base(plc, (byte)enFunctionCode.MemoryArea, (byte)enMemoryAreaFunctionCode.Read)
        {
            Length = length;
            Body = new byte[6];
            Body[0] = (byte)dataType;// Memory Area Data Type
            var bytes = BitConverter.GetBytes(startAddress).Reverse().ToArray();// Address
            Body[1] = bytes[0];
            Body[2] = bytes[1];
            Body[3] = 0x00;// Reserved
            bytes = BitConverter.GetBytes(length).Reverse().ToArray();// Length
            Body[4] = bytes[0];
            Body[5] = bytes[1];
        }

    }
}
