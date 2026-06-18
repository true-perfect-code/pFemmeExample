using pFemmeExample.Wpf.Services;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace pFemmeExample.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Cookie used to unregister the COM server later
        private uint _cookie;
        //private const string Clsid = "D6B6F2B4-9E92-4B2E-9B65-8E4F4A9F9C01";
        private string Clsid = Shared.Global.Configuration.ConfigGeneral.CLSID;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Setup Infrastructure (Registry & Shortcut) for Unpackaged
            // pE.Utility.Appl.Aumid is used here as requested
            string aumid = Shared.Global.Configuration.ConfigGeneral.Aumid;
            //NotificationManager.Initialize(aumid);

            // 2. Register the COM Server so Windows can find this running instance
            //RegisterComServer(); -> nur wenn lokale Notifikation verwendet wird!

            // 3. Handle potential Protocol/Args Launch
            // If the app was started via a Toast click while closed, 
            // the arguments will be in e.Args
            if (e.Args.Length > 0)
            {
                // Logic to handle startup arguments (e.g. deep linking)
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up the COM registration on exit
            if (_cookie != 0)
            {
                CoRevokeClassObject(_cookie);
            }

            base.OnExit(e);
        }

        //private void RegisterComServer()
        //{
        //    // We only need to manually register the COM object in Unpackaged mode.
        //    // In Packaged mode, the Manifest handles the activation.
        //    if (ShortcutHelper.IsPackaged()) return;

        //    try
        //    {
        //        Guid clsidGuid = new Guid(Clsid);
        //        var factory = new ClassFactory(); // We need a simple ClassFactory for COM

        //        int hr = CoRegisterClassObject(
        //            ref clsidGuid,
        //            factory,
        //            CLSCTX_LOCAL_SERVER,
        //            REGCLS_MULTIPLEUSE,
        //            out _cookie);

        //        if (hr < 0)
        //        {
        //            Marshal.ThrowExceptionForHR(hr);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Use your platform.Log here
        //        Console.WriteLine($"Failed to register COM server: {ex.Message}");
        //    }
        //}

        #region COM Registration P/Invoke

        private const uint CLSCTX_LOCAL_SERVER = 4;
        private const uint REGCLS_MULTIPLEUSE = 1;

        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern int CoRegisterClassObject(
            [In] ref Guid rclsid,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
            uint dwClsContext,
            uint flags,
            out uint lpdwRegister);

        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern int CoRevokeClassObject(uint dwRegister);

        #endregion
    }

}
