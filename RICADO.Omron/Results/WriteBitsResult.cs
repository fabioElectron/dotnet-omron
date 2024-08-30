﻿using System;

namespace RICADO.Omron.Results
{
    public struct WriteBitsResult
    {
        public int BytesSent;
        public int PacketsSent;
        public int BytesReceived;
        public int PacketsReceived;
        public double Duration;
    }
}
