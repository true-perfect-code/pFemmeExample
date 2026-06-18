
using BlazorCore.Services.Dam;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BlazorCore.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class SharingUsersModel : ICloneable
    {
        /// <inheritdoc />
        public int ID { get; set; }

        /// <inheritdoc />
        public string? UnixTS { get; set; }

        /// <inheritdoc />
        public string? AuthUsers_UnixTS { get; set; }

        /// <inheritdoc />
        public string? AuthUsers_ShareTo_UnixTS { get; set; }

        /// <inheritdoc />
        public int SharingStatus { get; set; }

        /// <inheritdoc />
        public long LastUpdateUnixTS { get; set; } = 0;

        #region Internal
        public string? Int__AuthUsers_UnixTS { get; set; }
        public string Int__Alias { get; set; } = string.Empty;
        public string Int__AliasImgJpegThumbnail { get; set; } = string.Empty;
        [JsonIgnore] public bool Int__IsChecked { get; set; } = false;
        #endregion

        public object Clone() => MemberwiseClone();
    }

    //[MapTo("SharingUsers")]
    //[Preserve]
    //internal partial class SharingUsersEntity : RealmObject, ISharingUsers, Services.Realm.IAutoIncrementEntity
    //{
    //    /// <inheritdoc />
    //    [PrimaryKey]
    //    [MapTo("ID")]
    //    public int ID { get; set; }

    //    /// <inheritdoc />
    //    [Indexed]
    //    [MapTo("UnixTS")]
    //    public string UnixTS { get; set; } = string.Empty;

    //    /// <inheritdoc />
    //    [Indexed]
    //    [MapTo("AuthUsers_UnixTS")]
    //    public string AuthUsers_UnixTS { get; set; } = string.Empty;

    //    /// <inheritdoc />
    //    [Indexed]
    //    [MapTo("AuthUsers_ShareTo_UnixTS")]
    //    public string AuthUsers_ShareTo_UnixTS { get; set; } = string.Empty;

    //    /// <inheritdoc />
    //    [MapTo("SharingStatus")]
    //    public int SharingStatus { get; set; }

    //    /// <inheritdoc />
    //    [MapTo("LastUpdateUnixTS")]
    //    public long LastUpdateUnixTS { get; set; } = 0;
    //}

    public enum SHARING_STATUS
    {
        REQUEST = 0,
        ACCEPT = 1,
        REJECT = 2
    }

    // Wird bei SharingInfo.razor genutzt, um Sharinguser anzuzeigen und zu selektieren.
    public class SharingInfoModel() : IBasisModel
    {
        public string AuthUsers_UnixTS { get; set; } = string.Empty;
        public string AuthUsers_ShareTo_UnixTS { get; set; } = string.Empty;
        public bool IsChecked { get; set; } = false;
        public string Alias { get; set; } = string.Empty;
        public string AliasImgJpegThumbnail { get; set; } = string.Empty;


        public int ID { get; set; }
        public string UnixTS { get; set; } = string.Empty;
        public long LastUpdateUnixTS { get; set; } = 0;
        public bool Int__MigrationToMSSQL { get; set; } = false;
        public bool Int__MigrationToSqLite { get; set; } = false;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    // Wird bei SharingInfo.razor genutzt, um Sharinguser anzuzeigen und zu selektieren.
    public class SharingInfoJsonModel()
    {
        public string IsChecked { get; set; } = "0";
        public string AuthUsers_UnixTS { get; set; } = string.Empty;
        public string AuthUsers_ShareFrom_UnixTS { get; set; } = string.Empty;
        public string UnixTS { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }


}
