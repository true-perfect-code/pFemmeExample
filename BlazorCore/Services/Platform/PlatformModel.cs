using System.Xml.Serialization;

namespace BlazorCore.Services.Platform
{
    public class PlatformModelBase
    {
    }

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

    /// <summary>
    /// Container für die Plattform-Bestimmung
    /// </summary>
    public enum THEMEMODE : int
    {
        DEFAULT,
        MONOCHROME
    }

    public class WindowDimensions
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }



}
