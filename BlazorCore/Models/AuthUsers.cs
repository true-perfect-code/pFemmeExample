//using Realms;
using BlazorCore.Services.Dam;
using System.Diagnostics.CodeAnalysis;

namespace BlazorCore.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class AuthUsersModel : IBasisModel, IMigrationState, ICloneable
    {
        /// <inheritdoc />
        public int ID { get; set; }

        /// <inheritdoc />
        public string? UnixTS { get; set; }

        /// <inheritdoc />
        public string? EmailHash { get; set; }

        /// <inheritdoc />
        public string? PasswordHash { get; set; }

        /// <inheritdoc />
        public bool active { get; set; } = true;

        /// <inheritdoc />
        public bool TermsAccepted { get; set; } = true;

        /// <inheritdoc />
        public string? IdP { get; set; }

        /// <inheritdoc />
        public string? IdPClientIdent { get; set; }

        /// <inheritdoc />
        public string? IdPToken { get; set; }

        /// <inheritdoc />
        public string? UserRole { get; set; }

        /// <inheritdoc />
        public long LastUpdateUnixTS { get; set; } = 0;

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTimeOffset? LastLogin { get; set; }

        public byte[]? otp { get; set; }

        #region Internal
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool Int__activateTask { get; set; } = false;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool Int__MigrationToCloud { get; set; } = false;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool Int__MigrationToLocal { get; set; } = false;

        public int sorter { get; set; } = 0;

        //[System.ComponentModel.DataAnnotations.Schema.NotMapped]
        //public int AuthUsers_ID { get; set; } = 0;
        #endregion

        public object Clone() => MemberwiseClone();
    }

}
