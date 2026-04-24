using System;

namespace DanfossSPGroup7.Domain
{
    public class MaintenancePeriod
    {
        public DateTime Start { get; }
        public DateTime End { get; }

        public MaintenancePeriod(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public bool Contains(DateTime time)
        {
            return time >= Start && time < End;
        }


    }
}