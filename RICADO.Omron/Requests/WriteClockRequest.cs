using System;

namespace RICADO.Omron.Requests
{
    internal class WriteClockRequest : FINSRequest
    {

        public WriteClockRequest(OmronPLC plc, DateTime dateTime, byte dayOfWeek)
            : base(plc, (byte)enFunctionCode.TimeData, (byte)enTimeDataFunctionCode.WriteClock)
        {
            Body = [
                BCDConverter.GetBCDByte((byte)(dateTime.Year % 100)),// Year (Last 2 Digits)
                BCDConverter.GetBCDByte((byte)dateTime.Month),// Month
                BCDConverter.GetBCDByte((byte)dateTime.Day),// Day
                BCDConverter.GetBCDByte((byte)dateTime.Hour),// Hour
                BCDConverter.GetBCDByte((byte)dateTime.Minute),// Minute
                BCDConverter.GetBCDByte((byte)dateTime.Second),// Second
                BCDConverter.GetBCDByte(dayOfWeek)// Day of Week
                ];
        }

    }
}
