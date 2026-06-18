using System;
using System.Collections.Generic;
using System.Text;

namespace pFemmeExample.Shared.Models
{
    internal class CyclePhases
    {
    }

    public class CyclePhasesModel
    {
        /// <summary>
        /// The actual, last recorded cycle start date from the database.
        /// </summary>
        public DateTime? LastCycleStartDay { get; set; }

        /// <summary>
        /// The projected start date of the current cycle phase, 
        /// extrapolated if the user hasn't logged a new period yet.
        /// </summary>
        public DateTime? LastCycleStartDayProjected { get; set; }
        /// <inheritdoc />
        public int fertile_phase { get; set; }

        /// <inheritdoc />
        public int infertile_phase { get; set; }

        /// <inheritdoc />
        public int avgDurationMin { get; set; }

        /// <inheritdoc />
        public int avgDurationMax { get; set; }

        public double MedianCycleLength { get; set; } = 28;
    }
}
