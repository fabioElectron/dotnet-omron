using System;

namespace RICADO.Omron.Channels
{
    internal class ReceiveMessageResult
    {
        internal Memory<byte> Message;
        internal int Bytes;
        internal int Packets;
    }
}
