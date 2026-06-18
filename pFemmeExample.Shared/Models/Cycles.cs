using BlazorCore.Services.Dam;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace pFemmeExample.Shared.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class CyclesModel : ICloneable
    {
        /// <inheritdoc />
        public int ID { get; set; }

        /// <inheritdoc />
        public string? UnixTS { get; set; }

        /// <inheritdoc />
        public string? AuthUsers_UnixTS { get; set; }

        /// <inheritdoc />
        public string Details { get; set; } = string.Empty;

        public DateTime? RecordDate { get; set; }

        public bool bleeding { get; set; }

        public int intensity { get; set; } = 0;

        public int pain { get; set; } = 0;

        public int headache { get; set; } = 0;

        public int fatigue { get; set; } = 0;

        public int nausea { get; set; } = 0;

        public int cramps { get; set; } = 0;

        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }

        /// <inheritdoc />
        public long LastUpdateUnixTS { get; set; } = 0;

        /// <inheritdoc />
        public object Clone() => MemberwiseClone();

        public override bool Equals(object? obj)
        {
            // Falls das andere Objekt kein CyclesModel ist oder null, sind sie ungleich
            if (obj is not CyclesModel other) return false;

            // Hier vergleichen wir jeden einzelnen Wert präzise
            return ID == other.ID &&
                   UnixTS == other.UnixTS &&
                   AuthUsers_UnixTS == other.AuthUsers_UnixTS &&
                   Details == other.Details &&
                   RecordDate == other.RecordDate &&
                   bleeding == other.bleeding &&
                   intensity == other.intensity &&
                   pain == other.pain &&
                   headache == other.headache &&
                   fatigue == other.fatigue &&
                   nausea == other.nausea &&
                   cramps == other.cramps &&
                   created_at == other.created_at &&
                   updated_at == other.updated_at &&
                   LastUpdateUnixTS == other.LastUpdateUnixTS;
        }

        // Wenn man Equals überschreibt, sollte man aus Performance-Gründen auch GetHashCode überschreiben
        public override int GetHashCode() => ID.GetHashCode();
    }

    public class CyclesCompareModel : CyclesModel, IMigrationState, IBasisModel
    {
        /// <summary>
        /// Flag indicating whether this entity needs to be migrated TO cloud
        /// </summary>
        public bool Int__MigrationToCloud { get; set; }

        /// <summary>
        /// Flag indicating whether this entity needs to be migrated TO local storage
        /// </summary>
        public bool Int__MigrationToLocal { get; set; }
    }

    public class CycleDurationModel
    {
        public DateTime? FirstDayCycle { get; set; }
        public DateTime? LastDayCycle { get; set; }
    }

    public class CycleIntervalModel
    {
        public DateTime FirstDayCycle { get; set; }
        public int DayDifferenceToLastCycle { get; set; }
    }

}