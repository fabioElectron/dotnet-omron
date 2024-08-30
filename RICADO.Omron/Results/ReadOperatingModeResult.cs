namespace RICADO.Omron.Results
{
    public class ReadOperatingModeResult
    {
        public int BytesSent;
        public int PacketsSent;
        public int BytesReceived;
        public int PacketsReceived;
        public double Duration;
        public byte Status;
        public byte Mode;
        public ushort FatalErrorData;
        public ushort NonFatalErrorData;
        public byte ErrorCode;
        public string ErrorMessage;
    }
}
