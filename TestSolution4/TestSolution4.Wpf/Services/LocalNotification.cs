using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Win32;
using BlazorCore.Services.LocalNotification;
using BlazorCore.Services.SqlClient;
using TestSolution4.Shared.Services.GlobalState;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Claims;
using System.Text;
using System.Windows;
using Windows.ApplicationModel;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using static TestSolution4.Wpf.Services.LocalNotification;

namespace TestSolution4.Wpf.Services
{
    public class LocalNotification : ILocalNotification
    {
        //private readonly Shared.Services.AppState.IAppState _appState;
        private readonly Shared.Services.GlobalState.IGlobalState _globalState;

        public string NotificationIdentifier { get; set; } = string.Empty;
        //private readonly string AppGroupName = pE.Utility.Appl.ApplicationName;
        //private readonly string Aumid = pE.Utility.Appl.Aumid;
        private bool _initialized;
        private Platform platform = new();

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public Task<ScalarModel> RequestNotificationPermissionAsync()
            => Task.FromResult(new ScalarModel { out_value_bool = true, out_value_str = "granted" });

        public LocalNotification(Shared.Services.GlobalState.IGlobalState globalState)
        {
            _globalState = globalState;
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (_initialized) return;

                // Die ID explizit setzen
                int hresult = SetCurrentProcessExplicitAppUserModelID(Shared.Global.Configuration.ConfigGeneral.Aumid);

                if (hresult != 0) // 0 = S_OK
                {
                    platform.Log($"Warning: SetCurrentProcessExplicitAppUserModelID failed with HRESULT: 0x{hresult:X8}", isError: true);
                }
                else
                {
                    platform.Log("AppUserModelID successfully set for current process.");
                }

                _initialized = true;
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                platform.Log($"Fehler bei Service-Init: {ex.Message}", isError: true);
            }
        }

        // Assuming ScalarModel and platform are accessible in this scope
        public async Task<ScalarModel> ScheduleNotification(string identifier, string title, string body, DateTime? dateTime = null)
        {
            await Task.Delay(10);
            var result = new ScalarModel();
            try
            {
                platform.Log("ScheduleNotification started (COM-based)");

                string appId = Shared.Global.Configuration.ConfigGeneral.Aumid;

                // 1. Alle Inhalte sauber für XML vorbereiten (nur EINMAL escapen)
                string escapedTitle = System.Security.SecurityElement.Escape(title ?? "");
                string escapedBody = System.Security.SecurityElement.Escape(body ?? "");

                // 2. Arguments vorbereiten und escapen
                string rawArgs = $"action=view&id={identifier}";
                string escapedArgs = System.Security.SecurityElement.Escape(rawArgs);

                // 3. Finales XML (ohne führende Leerzeichen/Absätze vor dem <toast>)
                // Wir nutzen pE.Utility.Appl.CLSID direkt.
                string xml = $@"<toast launch=""{escapedArgs}"" activatorGuid=""{Shared.Global.Configuration.ConfigGeneral.CLSID}""><visual><binding template=""ToastGeneric""><text>{escapedTitle}</text><text>{escapedBody}</text></binding></visual><actions><action content=""Anzeigen"" arguments=""{escapedArgs}""/></actions></toast>";

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                var notifier = ToastNotificationManager.CreateToastNotifier(appId);

                if (dateTime.HasValue && dateTime.Value > DateTime.Now)
                {
                    var localTime = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Local);

                    // Wir nutzen Tag für die Identifizierung, genau wie bei den sofortigen Toasts
                    var scheduledToast = new ScheduledToastNotification(xmlDoc, new DateTimeOffset(localTime))
                    {
                        Tag = identifier,
                        Group = _globalState.ConfigGeneral.ApplicationName //AppGroupName // Optional: Hilft beim massenweisen Löschen
                    };

                    notifier.AddToSchedule(scheduledToast);
                    result.out_value_str = "Scheduled";
                }
                else
                {
                    var toast = new ToastNotification(xmlDoc)
                    {
                        Tag = identifier,
                        Group = Shared.Global.Configuration.ConfigGeneral.ApplicationName
                    };

                    notifier.Show(toast);
                    result.out_value_str = "Shown";
                }

                result.out_value_bool = true;
            }
            catch (Exception ex)
            {
                string errorCode = string.Format("0x{0:X8}", ex.HResult);
                platform.Log($"CRITICAL ERROR: {ex.Message} (HRESULT: {errorCode})", isError: true);
                result.out_value_bool = false;
                result.out_err = $"Notification Error: {ex.Message}";
            }

            return result;
        }

        public async Task<ScalarModel> SyncNotificationsAsync(IEnumerable<NotificationSyncModel> cloudNotifications)
        {
            try
            {
                await InitializeAsync();
                var notifier = ToastNotificationManager.CreateToastNotifier(Shared.Global.Configuration.ConfigGeneral.Aumid);

                // 1. Aktuell geplante Windows-Toasts abrufen
                var localScheduled = notifier.GetScheduledToastNotifications().ToList();

                // 2. Veraltete Toasts entfernen (die nicht mehr in der Cloud-Liste sind)
                foreach (var local in localScheduled)
                {
                    // Wir nutzen die UnixTS (Id) als Tag-Vergleich
                    if (!cloudNotifications.Any(c => c.Id == local.Tag))
                    {
                        notifier.RemoveFromSchedule(local);
                        //Debug.WriteLine($"Sync: Removed obsolete notification {local.Tag}");
                        platform.Log($"Sync: Removed obsolete notification {local.Tag}");
                    }
                }

                // 3. Lokale Liste nach dem Löschen neu laden für präzisen Vergleich
                var currentLocal = notifier.GetScheduledToastNotifications().ToList();

                foreach (var cloud in cloudNotifications)
                {
                    var existing = currentLocal.FirstOrDefault(l => l.Tag == cloud.Id);

                    if (existing != null)
                    {
                        // WICHTIGE ANPASSUNG FÜR UNIX/UTC LOGIK:
                        // Wir wandeln beide Zeiten explizit in UTC um, bevor wir die Differenz berechnen.
                        // cloud.TargetDate kommt aus deinem TodoModel (RecordDateTimeUI) -> LocalDateTime.
                        // existing.DeliveryTime wird von Windows oft als DateTimeOffset geliefert.

                        var localUtc = existing.DeliveryTime.UtcDateTime;
                        var cloudUtc = cloud.TargetDate.ToUniversalTime();

                        var timeDiff = Math.Abs((localUtc - cloudUtc).TotalSeconds);

                        // Wenn die Abweichung größer als 5 Sekunden ist (Rundungsschutz), 
                        // wird der Termin aktualisiert.
                        if (timeDiff > 5)
                        {
                            notifier.RemoveFromSchedule(existing);
                            await ScheduleNotification(cloud.Id, cloud.Title, cloud.Body, cloud.TargetDate);
                            //Debug.WriteLine($"Sync: Updated notification {cloud.Id} (Time changed by {timeDiff:F1}s)");
                            platform.Log($"Sync: Updated notification {cloud.Id} (Time changed by {timeDiff:F1}s)");
                        }
                    }
                    else
                    {
                        // Neu planen, da für diese UnixTS noch kein lokaler Toast existiert
                        await ScheduleNotification(cloud.Id, cloud.Title, cloud.Body, cloud.TargetDate);
                        //Debug.WriteLine($"Sync: Scheduled new notification {cloud.Id}");
                        platform.Log($"Sync: Scheduled new notification {cloud.Id}");
                    }
                }

                return new ScalarModel { out_value_bool = true };
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Fehler beim Sync: {ex.Message}");
                platform.Log($"Fehler beim Sync: {ex.Message}", isError: true);
                return new ScalarModel { out_value_bool = false, out_err = ex.Message };
            }
        }

        public Task<ScalarModel> RemovePendingNotification(string identifier)
        {
            var notifier = ToastNotificationManager.CreateToastNotifier(Shared.Global.Configuration.ConfigGeneral.Aumid);
            foreach (var s in notifier.GetScheduledToastNotifications().Where(n => n.Tag == identifier))
                notifier.RemoveFromSchedule(s);
            return Task.FromResult(new ScalarModel { out_value_bool = true });
        }

        public Task<ScalarModel> RemoveAllPendingNotifications()
        {
            var notifier = ToastNotificationManager.CreateToastNotifier(Shared.Global.Configuration.ConfigGeneral.Aumid);
            foreach (var s in notifier.GetScheduledToastNotifications())
                notifier.RemoveFromSchedule(s);
            return Task.FromResult(new ScalarModel { out_value_bool = true });
        }

        public Task<ScalarModel> RemoveDeliveredNotification(string identifier)
        {
            ToastNotificationManager.History.Remove(identifier, _globalState.ConfigGeneral.ApplicationName, Shared.Global.Configuration.ConfigGeneral.Aumid);
            return Task.FromResult(new ScalarModel { out_value_bool = true });
        }

        public Task<ScalarModel> RemoveAllDeliveredNotifications()
        {
            ToastNotificationManager.History.Clear(Shared.Global.Configuration.ConfigGeneral.Aumid);
            return Task.FromResult(new ScalarModel { out_value_bool = true });
        }
    }



    ////////////////////////////////////////////////////////
    // https://forums.fivetechsupport.com/viewtopic.php?t=32263

    #region Notification Management
    public static class NotificationManager
    {
        //private const string Clsid = "{D6B6F2B4-9E92-4B2E-9B65-8E4F4A9F9C01}";
        private static string Clsid = Shared.Global.Configuration.ConfigGeneral.CLSID;

        public static void Initialize(string aumid)
        {
            if (ShortcutHelper.IsPackaged()) return;

            // 1. COM Server registrieren
            //RegisterComServer();

            // 2. Shortcut erstellen / aktualisieren
            ShortcutHelper.CreateShortcutIfMissing(aumid);

            // 3. AppUserModelId-Key sauber setzen
            //EnsureAppUserModelId(aumid, Clsid);
        }

        //public static void EnsureAppUserModelId(string aumid, string clsid)
        //{
        //    using (var baseKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppUserModelId", true))
        //    {
        //        using (var key = baseKey.CreateSubKey(aumid))
        //        {
        //            // Setzt die CLSID für den ToastActivator explizit
        //            key.SetValue("CustomActivator", clsid, RegistryValueKind.String);

        //            // Optional: andere Werte sauber zurücksetzen
        //            key.SetValue("DisplayName", "pMunus Desktop", RegistryValueKind.String);
        //            key.SetValue("IconUri", "", RegistryValueKind.String);
        //        }
        //    }
        //}

        //private static void RegisterComServer()
        //{
        //    string exePath = Process.GetCurrentProcess().MainModule.FileName;

        //    // Register the CLSID for the COM server (Unpackaged)
        //    // Path: HKCU\Software\Classes\CLSID\{GUID}\LocalServer32
        //    using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{Clsid}\LocalServer32"))
        //    {
        //        // The default value must be the path to your EXE
        //        key.SetValue("", $"\"{exePath}\"");
        //    }
        //}
    }
    #endregion


    #region COM Infrastructure
    // This GUID must match your pMunus CLSID
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("D6B6F2B4-9E92-4B2E-9B65-8E4F4A9F9C01")]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    public class NotificationActivator : INotificationActivationCallback
    {
        public void Activate(string appUserModelId, string invokedArgs, NativeNotificationTextHeader[] data, uint dataCount)
        {
            // This is called by Windows even if the app is closed
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Here we will call your shared logic to navigate to the calendar
                // platform.Log("Toast activated with args: " + invokedArgs);
                HandleActivation(invokedArgs);
            });
        }

        private void HandleActivation(string args)
        {
            // Logic to pass the arguments to your Blazor app
        }
    }

    /// <summary>
    /// Basic ClassFactory for our NotificationActivator.
    /// This is a standard COM requirement to allow Windows to instantiate our class.
    /// </summary>
    public class ClassFactory : IClassFactory
    {
        public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;
            if (pUnkOuter != IntPtr.Zero) Marshal.ThrowExceptionForHR(-2147221232); // CLASS_E_NOAGGREGATION

            var activator = new NotificationActivator();
            var iid = riid;
            ppvObject = Marshal.GetComInterfaceForObject(activator, typeof(INotificationActivationCallback));
            return 0; // S_OK
        }

        public int LockServer(bool fLock) => 0;
    }
    #endregion


    #region Win32 & COM Interop
    /// <summary>
    /// The exact COM interface Windows looks for to handle toast notification activation.
    /// This is the standard Windows Shell interface for notification callbacks.
    /// </summary>
    [ComImport]
    [Guid("045245D3-D202-4E14-AC84-06F1D156435F")] // Fixed IID for Toast Activation
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INotificationActivationCallback
    {
        /// <summary>
        /// Called by Windows when a user interacts with a toast notification.
        /// </summary>
        /// <param name="appUserModelId">The AppUserModelId of the application.</param>
        /// <param name="invokedArgs">Arguments passed from the toast XML (e.g., launch arguments).</param>
        /// <param name="data">An array of additional data elements, such as user input from text boxes.</param>
        /// <param name="dataCount">The number of elements in the data array.</param>
        void Activate(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] NativeNotificationTextHeader[] data,
            uint dataCount);
    }

    /// <summary>
    /// Represents a key-value pair for notification data, mapped to the native Windows structure.
    /// Used for capturing user input and system data from toast activations.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NativeNotificationTextHeader
    {
        /// <summary>
        /// The key or identifier for the data field (e.g., the ID of a text input).
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)] public string Key;

        /// <summary>
        /// The value associated with the key (e.g., the text entered by the user).
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)] public string Value;
    }

    [ComImport, Guid("00000001-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IClassFactory
    {
        [PreserveSig]
        int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
        [PreserveSig]
        int LockServer(bool fLock);
    }

    [ComImport, Guid("0000010B-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
    #endregion


    #region Helper
    /// <summary>
    /// Helper to create a Start Menu shortcut with AUMID and CLSID.
    /// Implements the two-stage save process to avoid Windows Shell caching issues.
    /// </summary>
    public static class ShortcutHelper
    {
        // Clsid from pE.Utility.Appl.CLSID
        private static string ToastGuid = Shared.Global.Configuration.ConfigGeneral.CLSID;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

        [DllImport("propsys.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int InitPropVariantFromString([MarshalAs(UnmanagedType.LPWStr)] string psz, out PropVariant pvar);

        [DllImport("propsys.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int InitPropVariantFromCLSID(ref Guid clsid, out PropVariant pvar);

        [DllImport("Ole32.dll", PreserveSig = true)]
        private static extern int PropVariantClear(ref PropVariant pvar);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHGetPropertyStoreFromParsingName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr zero,
            [In] GETPROPERTYSTOREFLAGS flags,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppv);

        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PropertyKey pkey);
            void GetValue(ref PropertyKey key, out PropVariant pv);
            void SetValue(ref PropertyKey key, ref PropVariant pv);
            void Commit();
        }

        private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        /// <summary>
        /// Determines if the application is "Packaged" (MSIX) or "Unpackaged" (Win32/Exe).
        /// </summary>
        public static bool IsPackaged()
        {
            int length = 0;
            StringBuilder sb = new StringBuilder(0);
            int result = GetCurrentPackageFullName(ref length, sb);

            return result != APPMODEL_ERROR_NO_PACKAGE;
        }

        /// <summary>
        /// Creates the shortcut in the Start Menu if it doesn't exist.
        /// Necessary for Unpackaged apps to receive Toast callbacks.
        /// </summary>
        public static void CreateShortcutIfMissing(string aumid)
        {
            // Path: %Appdata%\Microsoft\Windows\Start Menu\Programs\pMunus.lnk
            string shortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs\pMunus.lnk");

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }

            InstallShortcut(shortcutPath, aumid);
        }

        private static void InstallShortcut(string shortcutPath, string aumid)
        {
            try
            {
                // 1. IShellLink Objekt initialisieren
                IShellLinkW shellLink = (IShellLinkW)new CShellLink();
                shellLink.SetPath(Environment.ProcessPath);
                shellLink.SetWorkingDirectory(AppContext.BaseDirectory);

                // 2. IPropertyStore direkt vom ShellLink-Objekt holen (In-Memory)
                // Das ist stabiler als SHGetPropertyStoreFromParsingName nach dem Speichern!
                IPropertyStore propertyStore = (IPropertyStore)shellLink;

                // 3. AUMID in den PropertyStore schreiben
                PropVariant pvId = PropVariant.FromString(aumid);
                // Nutze ref für PropertyKeys, wie in deinem Code definiert
                propertyStore.SetValue(ref PropertyKeys.AppUserModel_ID, ref pvId);
                pvId.Dispose();

                // 4. ToastActivatorCLSID in den PropertyStore schreiben (DER ENTSCHEIDENDE SCHRITT!)
                // Ohne diesen Eintrag im Shortcut findet Windows den COM-Server nicht ("Element nicht gefunden").
                Guid toastGuid = new Guid(ToastGuid);
                PropVariant pvClsid = PropVariant.FromGuid(toastGuid);
                propertyStore.SetValue(ref PropertyKeys.AppUserModel_ToastActivatorCLSID, ref pvClsid);
                pvClsid.Dispose();

                // 5. Änderungen im PropertyStore bestätigen
                propertyStore.Commit();

                // 6. Erst JETZT den Shortcut mit allen Metadaten physisch speichern
                IPersistFile persistFile = (IPersistFile)shellLink;
                persistFile.Save(shortcutPath, true);

                // 7. Ressourcen freigeben
                Marshal.ReleaseComObject(propertyStore);
                Marshal.ReleaseComObject(shellLink);

                // 8. Registry-Infrastruktur sicherstellen
                EnsureToastActivatorClsidInRegistry(aumid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Vollständiger Shortcut-Fehler: {ex}");
            }
        }

        //private static void EnsureToastActivatorClsidInRegistry(string aumid)
        //{
        //    try
        //    {
        //        string clsid = ToastGuid;

        //        // Wir schreiben beide Varianten ("CustomActivator" und "ToastActivatorCLSID"),
        //        // um maximale Kompatibilität mit verschiedenen Windows-Builds zu erreichen.
        //        using (var aumidKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{aumid}"))
        //        {
        //            aumidKey.SetValue("CustomActivator", clsid, RegistryValueKind.String);
        //            aumidKey.SetValue("ToastActivatorCLSID", clsid, RegistryValueKind.String);
        //        }

        //        using (var clsidKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\LocalServer32"))
        //        {
        //            clsidKey.SetValue("", $"\"{Environment.ProcessPath}\"");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Registry CLSID Fehler: {ex}");
        //    }
        //}
        /// <summary>
        /// Centralized registry management. Ensures AUMID, CLSID, and COM Server 
        /// paths are correctly registered for Windows Toast activation.
        /// </summary>
        /// <summary>
        /// Centralized registry management. 
        /// Combines standard AUMID registration with critical AppID and Notification 
        /// setting keys required for successful Toast activation on all Windows builds.
        /// </summary>
        private static void EnsureToastActivatorClsidInRegistry(string aumid)
        {
            try
            {
                // Diese GUID muss exakt "{D6B6F2B4-9E92-4B2E-9B65-8E4F4A9F9C01}" sein
                string clsid = ToastGuid;
                string exePath = Environment.ProcessPath;

                // 1. Pfad im AppID-Zweig erstellen (Skript Punkt 1)
                // HKCU:\Software\Classes\AppID\$guid
                using (var appIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppID\{clsid}"))
                {
                    // New-ItemProperty -Name "AppId" -Value $aumid
                    appIdKey.SetValue("AppId", aumid, RegistryValueKind.String);
                }

                // 2. Pfad im CLSID-Zweig sicherstellen (Skript Punkt 2)
                // HKCU:\Software\Classes\CLSID\$guid
                using (var clsidKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}"))
                {
                    // 3. Verknüpfung der CLSID mit der AppID (Skript Punkt 3)
                    // New-ItemProperty -Name "AppID" -Value $guid
                    clsidKey.SetValue("AppID", clsid, RegistryValueKind.String);

                    // 4. LocalServer32 Pfad (Skript Punkt 4)
                    // $localServerPath = "$clsidPath\LocalServer32"
                    using (var localServerKey = clsidKey.CreateSubKey("LocalServer32"))
                    {
                        // New-ItemProperty -Name "(Default)" -Value "`"$exePath`""
                        localServerKey.SetValue("", $"\"{exePath}\"");
                    }
                }

                // ZUSATZ aus deinem zweiten Script-Fix (Notifications Einschalten)
                using (var settingsKey = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\{aumid}"))
                {
                    settingsKey.SetValue("ShowInActionCenter", 1, RegistryValueKind.DWord);
                    settingsKey.SetValue("Enabled", 1, RegistryValueKind.DWord);
                }

                // HINWEIS: Der Standard AppUserModelId-Key (CustomActivator) bleibt drin, 
                // da er für die Verknüpfung mit dem Shortcut zwingend ist.
                using (var aumidKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{aumid}"))
                {
                    aumidKey.SetValue("CustomActivator", clsid, RegistryValueKind.String);
                    aumidKey.SetValue("ToastActivatorCLSID", clsid, RegistryValueKind.String);
                    aumidKey.SetValue("DisplayName", "pMunus Desktop", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Registry synchronization error: {ex}");
            }
        }

        #region COM & Structs

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl); void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([In, MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([In, MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey); void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd); void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([In, MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([In, MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([In, MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        //[ComImport, Guid("00021401-0000-0000-C000-000000000046"), ClassInterface(ClassInterfaceType.None)]
        //class CShellLink { }
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        class CShellLink
        {
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct PropertyKey
        {
            public Guid fmtid; public uint pid;
            public PropertyKey(Guid guid, uint id) { fmtid = guid; pid = id; }
        }

        [Flags]
        enum GETPROPERTYSTOREFLAGS : uint { GPS_READWRITE = 0x00000002 }

        static class PropertyKeys
        {
            private static readonly Guid AppUserModelGuid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
            public static PropertyKey AppUserModel_ID = new PropertyKey(AppUserModelGuid, 5);
            public static PropertyKey AppUserModel_ToastActivatorCLSID = new PropertyKey(AppUserModelGuid, 26);
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)] // Fixiert die Größe auf den COM-Standard für x64
        public struct PropVariant
        {
            [FieldOffset(0)]
            public ushort vt;

            // Diese Lücke (Offset 2 bis 7) wird von Windows erwartet. 
            // Durch FieldOffset(8) für den Pointer ist sie implizit da, 
            // aber wir müssen sicherstellen, dass die Gesamtstruktur groß genug ist.

            [FieldOffset(8)]
            public IntPtr pointerValue;

            public static PropVariant FromString(string value)
            {
                var pv = new PropVariant();
                pv.vt = 31; // VT_LPWSTR
                pv.pointerValue = Marshal.StringToCoTaskMemUni(value);
                return pv;
            }

            public static PropVariant FromGuid(Guid guid)
            {
                var pv = new PropVariant();
                pv.vt = 72; // VT_CLSID
                            // Wir reservieren exakt 16 Bytes für die GUID
                pv.pointerValue = Marshal.AllocCoTaskMem(16);
                Marshal.StructureToPtr(guid, pv.pointerValue, false);
                return pv;
            }

            public void Dispose()
            {
                // WICHTIG: PropVariantClear gibt den durch AllocCoTaskMem 
                // allozierten Speicher korrekt frei, wenn vt = VT_CLSID gesetzt ist.
                PropVariantClear(ref this);
            }

            [DllImport("ole32.dll")]
            private static extern int PropVariantClear(ref PropVariant pvar);
        }
        #endregion
    }
    #endregion
    ////////////////////////////////////////////////////////


}