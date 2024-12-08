namespace RICADO.Omron.Results
{
    public class ReadProgramAreaResult
    {
        public int BytesSent;
        public int PacketsSent;
        public int BytesReceived;
        public int PacketsReceived;
        public double Duration;
        public byte[] Values;
        public byte MainResponseCode;
        public byte SubResponseCode;
    }
}
