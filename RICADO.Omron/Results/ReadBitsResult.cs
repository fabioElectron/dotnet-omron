using System;

namespace RICADO.Omron.Results
{
    public struct ReadBitsResult
    {
        public int BytesSent;
        public int PacketsSent;
        public int BytesReceived;
        public int PacketsReceived;
        public double Duration;
        public bool[] Values;
    }
}
