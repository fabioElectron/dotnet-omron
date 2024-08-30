namespace RICADO.Omron.Results
{
    public class ReadWordsResult
    {
        public int BytesSent;
        public int PacketsSent;
        public int BytesReceived;
        public int PacketsReceived;
        public double Duration;
        public ushort[] Values;
        public byte MainResponseCode;
        public byte SubResponseCode;
    }
}
