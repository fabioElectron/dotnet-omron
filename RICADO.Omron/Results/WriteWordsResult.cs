﻿using System;

namespace RICADO.Omron.Results
{
    public struct WriteWordsResult
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