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
            // check if the time is inside the maintenance period
            return time >= Start && time < End;
        }


    }
}
