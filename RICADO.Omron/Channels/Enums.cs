namespace RICADO.Omron.Channels
{

    internal enum TcpCommandCode : byte
    {
        NodeAddressToPLC = 0,
        NodeAddressFromPLC = 1,
        FINSFrame = 2,
    }

    public enum SocketConnectionStatus : byte
    {
        DISCONNECTED,
        CONNECTING,
        CONNECTED,
        CONNECTIONFAILED,
        CONNECTIONLOST_TIMEOUT,
        CONNECTIONCLOSED_REMOTE,
        NOT_FOUND,
        DISCONNECTING
    }

    public enum CommandResult
    {
        Ok = 0,
        BadRequest = 1,
        InternalError = 2,
        NotConnected = 3,
        Timeout = 4,
        OtherError = 8,
    }
}
