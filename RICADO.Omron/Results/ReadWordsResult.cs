using System;

namespace RICADO.Omron.Results
{
    public struct ReadWordsResult
    {
        public int BytesSent;
        public int PacketsSent;
        public int BytesReceived;
        public int PacketsReceived;
        public double Duration;
        public short[] Values;
        public byte MainResponseCode;
        public byte SubResponseCode;
    }
}
