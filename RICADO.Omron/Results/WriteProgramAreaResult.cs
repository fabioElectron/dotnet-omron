namespace RICADO.Omron.Results
{
    internal class WriteProgramAreaResult
    {
        public int BytesSent;
        public int PacketsSent;
        public int BytesReceived;
        public int PacketsReceived;
        public double Duration;
        public byte MainResponseCode;
        public byte SubResponseCode;
    }
}
