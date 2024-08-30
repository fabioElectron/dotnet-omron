using System;

namespace RICADO.Omron.Channels
{
    internal struct ReceiveMessageResult
    {
        internal Memory<byte> Message;
        internal int Bytes;
        internal int Packets;
    }
}
