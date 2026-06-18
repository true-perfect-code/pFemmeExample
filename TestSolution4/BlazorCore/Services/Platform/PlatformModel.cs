using System.Xml.Serialization;

namespace BlazorCore.Services.Platform
{
    public class PlatformModelBase
    {
    }

    ///// <summary>
    ///// Represents the root element for a collection of language translations.
    ///// This class is used to deserialize an XML file containing translation entries.
    ///// </summary>
    //[XmlRoot("Translations")]
    //public class Translations
    //{
    //    /// <summary>
    //    /// Gets or sets the list of translation entries.
    //    /// Each entry contains a phrase in multiple languages.
    //    /// </summary>
    //    [XmlElement("Entry")]
    //    public List<Utility.TranslationsEntry>? Entries { get; set; }
    //}

    ///// <summary>
    ///// Represents a single translation entry with phrases in various languages.
    ///// </summary>
    //public class TranslationsEntry
    //{
    //    /// <summary>
    //    /// Gets or sets the English translation.
    //    /// </summary>
    //    [XmlElement("en-US")]
    //    public string? EN { get; set; }

    //    /// <summary>
    //    /// Gets or sets the German translation.
    //    /// </summary>
    //    [XmlElement("DE")]
    //    public string? DE { get; set; }

    //    /// <summary>
    //    /// Gets or sets the Arabic translation.
    //    /// </summary>
    //    [XmlElement("AR")]
    //    public string? AR { get; set; }

    //    /// <summary>
    //    /// Gets or sets the Chinese translation.
    //    /// </summary>
    //    [XmlElement("ZH")]
    //    public string? ZH { get; set; }

    //    /// <summary>
    //    /// Gets or sets the French translation.
    //    /// </summary>
    //    [XmlElement("FR")]
    //    public string? FR { get; set; }

    //    /// <summary>
    //    /// Gets or sets the Indonesian translation.
    //    /// </summary>
    //    [XmlElement("ID")]
    //    public string? ID { get; set; }

    //    /// <summary>
    //    /// Gets or sets the Portuguese (Brazil) translation.
    //    /// </summary>
    //    [XmlElement("BR")]
    //    public string? BR { get; set; }

    //    /// <summary>
    //    /// Gets or sets the Spanish translation.
    //    /// </summary>
    //    [XmlElement("SP")]
    //    public string? SP { get; set; }

    //    /// <summary>
    //    /// Gets or sets the Serbian, Croatian, Bosnian... translation.
    //    /// </summary>
    //    [XmlElement("BL")]
    //    public string? BL { get; set; }

    //    /// <summary>
    //    /// Gets or sets the Hindi translation.
    //    /// </summary>
    //    [XmlElement("HI")]
    //    public string? HI { get; set; }
    //}


    /// <summary>
    /// Container für die Plattform-Bestimmung
    /// </summary>
    public enum PLATFORMS : int
    {
        WINDOWS_SERVER,
        WINDOWS_CLIENT,
        WASM,
        ANDROID,
        IOS,
        //MACCATALYST,
        MAC_CLIENT,
        WINDOWS_API,
        Unknown
    }

    //public class CurrPlatform
    //{
    //    public PLATFORMS Platform;
    //}

    /// <summary>
    /// Container für die Plattform-Bestimmung
    /// </summary>
    public enum THEMEMODE : int
    {
        DEFAULT,
        MONOCHROME
    }



    //public class LocalStorageModel
    //{
    //    //public readonly string storageLocation = "storagelocation__"; // Ist in static Appl() unter StorageLocation hinterlegt, weil beim Start benötigt wird!
    //    //public readonly string oauthToken = "oauth_token__"; // Ist in static Appl() unter OauthToken hinterlegt, weil beim Start benötigt wird!
    //    public readonly string language = "language__";
    //    public readonly string ltrrtl = "ltrrtl__";
    //    public readonly string fontfamily = "fontfamily__";
    //    public readonly string fontsize = "fontsize__";
    //    public readonly string fontweigh = "fontweigh__";
    //    public readonly string fontspacing = "fontspacing__";
    //    public readonly string fontlineheight = "fontlineheight__";
    //    public readonly string thememode = "thememode__";
    //    public readonly string realmkey = "realmkey__";
    //    //public readonly string realmdelete = "realmdelete__";

    //    public readonly string lastFailedReset2fa = "last_failed_reset2fa__";
    //    //public readonly string realmPassword = "realm_password__";
    //    public readonly string lastFailedLogin = "last_failed_login__";
    //    public readonly string accessibility = "accessibility__";
    //    public readonly string accessibilitylandingpage = "accessibilitylandingpage__";
    //    public readonly string accessibilitySmartview = "accessibilitysmartview__";
    //    public readonly string identityProvider = "idp__";
    //    public readonly string design = "design__";
    //    //public readonly string all = "all__";
    //    public readonly string Unknown = "Unknown";

    //    public readonly string pin = "pin__";

    //    public readonly string storeurls = "storeurls__";
    //}


    public class WindowDimensions
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }



}
