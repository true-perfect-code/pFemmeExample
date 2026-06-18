using System;
using System.Collections.Generic;
using System.Text;

namespace pFemmeExample.Shared.Models
{
    internal class CyclesInfo
    {
    }

    public class CycleSummaryModel
    {
        public DateTime? FirstDayCycle { get; set; }
        public DateTime? LastDayCycle { get; set; }
        public int Duration { get; set; }
    }

    public class CyclePeriodInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int IntensitySum { get; set; }
        public int PainSum { get; set; }
        public int HeadacheSum { get; set; }
        public int DayCount { get; set; }
    }
}
