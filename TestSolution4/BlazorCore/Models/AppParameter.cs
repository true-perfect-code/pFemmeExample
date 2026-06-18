using System.Diagnostics.CodeAnalysis;

namespace BlazorCore.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class AppParameterModel : ICloneable
    {
        /// <inheritdoc />
        public string? UnixTS { get; set; }

        /// <inheritdoc />
        public int ID { get; set; }

        /// <inheritdoc />
        public string? AuthUsers_UnixTS { get; set; }

        /// <inheritdoc />
        public string? ParameterName { get; set; }

        /// <inheritdoc />
        public string? ParameterValue { get; set; }

        /// <inheritdoc />
        public string? Details { get; set; }

        /// <inheritdoc />
        public string? Scope { get; set; }

        /// <inheritdoc />
        public long LastUpdateUnixTS { get; set; } = 0;

        /// <inheritdoc />
        public object Clone() => MemberwiseClone();
    }

}
