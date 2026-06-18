
using BlazorCore.Services.Dam;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BlazorCore.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class AuthUsersExtendModel : ICloneable
    {
        /// <inheritdoc />
        public int ID { get; set; }

        /// <inheritdoc />
        public string? UnixTS { get; set; }

        /// <inheritdoc />
        public string? AuthUsers_UnixTS { get; set; }

        /// <inheritdoc />
        public string? DisplayName { get; set; }

        /// <inheritdoc />
        //[JsonConverter(typeof(SqliteImageConverter))]
        public string? imgJpegThumbnail { get; set; }

        /// <inheritdoc />
        public long LastUpdateUnixTS { get; set; } = 0;

        /// <summary>
        /// Logical bridge to the base AuthUsersModel. 
        /// Not persisted in Realm but used for in-memory relations.
        /// </summary>
        public AuthUsersModel? Int__AuthUsers { get; set; }

        /// <inheritdoc />
        public object Clone() => MemberwiseClone();
    }

    public class AuthUsersAuthUsersExtendModel() : IBasisModel
    {
        public string EmailHash { get; set; } = "0";
        public string PasswordHash { get; set; } = string.Empty;
        public bool active { get; set; } = false;
        public bool TermsAccepted { get; set; } = false;
        public string IdP { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string imgJpegThumbnail { get; set; } = string.Empty;


        public int ID { get; set; }
        public string AuthUsers_UnixTS { get; set; } = string.Empty;
        public string? UnixTS { get; set; }
        public long LastUpdateUnixTS { get; set; } = 0;
        public bool Int__MigrationToMSSQL { get; set; } = false;
        public bool Int__MigrationToSqLite { get; set; } = false;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }


}
