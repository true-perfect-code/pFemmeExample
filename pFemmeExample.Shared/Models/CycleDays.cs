using System;
using System.Collections.Generic;
using System.Text;

namespace pFemmeExample.Shared.Models
{
    internal class CycleDays
    {
    }

    public class CycleDaysModel
    {
        public double MedianCycleLength { get; set; }
        public int FromCycleStar { get; set; }
        public int CurrentCycleDay { get; set; }
        public int DaysRemaining { get; set; }
        public double avgCycleDuration { get; set; }
        public DateTime? LastCycleStartDay { get; set; }
        public DateTime? Ovulation { get; set; }
        public DateTime? OvulationBar { get; set; }
        public DateTime? NextCyclePredicted { get; set; }
    }
}
