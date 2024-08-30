using System;
using System.Linq;

namespace RICADO.Omron.Requests
{
    internal class ReadMemoryAreaBitRequest : FINSRequest
    {

        internal readonly ushort Length;

        public ReadMemoryAreaBitRequest(OmronPLC plc, ushort address, byte startBitIndex, ushort length, enMemoryBitDataType dataType)
            : base(plc, (byte)enFunctionCode.MemoryArea, (byte)enMemoryAreaFunctionCode.Read)
        {
            Length = length;
            Body = new byte[6];
            Body[0] = (byte)dataType;// Memory Area Data Type
            var bytes = BitConverter.GetBytes(address).Reverse().ToArray();// Address
            Body[1] = bytes[0];
            Body[2] = bytes[1];
            Body[3] = startBitIndex;// Bit Index
            bytes = BitConverter.GetBytes(length).Reverse().ToArray();// Length
            Body[4] = bytes[0];
            Body[5] = bytes[1];
        }

    }
}
