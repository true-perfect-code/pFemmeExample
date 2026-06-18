//using Microsoft.JSInterop;
//using BlazorCore.Services.AppState;
//using BlazorCore.Services.Media;
//using BlazorCore.Services.Platform;
//using BlazorCore.Services.SqlClient;
//using TestSolution4.Shared.Pages.Common.Media;
//using System;
//using System.IO;
//using System.Threading.Tasks;
//using System.Windows;

//namespace TestSolution4.Wpf.Services
//{
//    /// <summary>
//    /// WPF-spezifische Implementierung des Media-Services.
//    /// Erbt von der Shared Media-Komponente, um die WebView-Kamera-Funktionalität zu nutzen,
//    /// überschreibt jedoch die Bildauswahl für einen nativen Windows-Dialog.
//    /// </summary>
//    public class MediaWpf : Media
//    {
//        /// <summary>
//        /// Konstruktor für den WPF-Service.
//        /// Da die Basisklasse von ComponentBase erbt, befüllen wir die protected Properties manuell.
//        /// </summary>
//        public MediaWpf(IJSRuntime js, IPlatformBase platform, IAppStateBase appState)
//        {
//            // Zuweisung an die protected Properties der Basisklasse (pE.Services.Media.Media)
//            _js = js;
//            _platform = platform;
//            _appState = appState;
//        }

//        /// <summary>
//        /// Überschreibt den PickPhotoAsync speziell für WPF, 
//        /// um den nativen Windows OpenFileDialog zu nutzen.
//        /// </summary>
//        public override async Task<ScalarModel> PickPhotoAsync(
//            Microsoft.AspNetCore.Components.ElementReference? fileInput = null,
//            int? imageSize = null,
//            int? quality = null,
//            bool crop = false,
//            int? thumbSize = null)
//        {
//            if (_appState != null)
//                await _appState.Log("[Media] WPF: Starte nativen Windows OpenFileDialog");

//            bool? dialogResult = false;
//            string selectedFilePath = string.Empty;

//            try
//            {
//                // WPF-Dialoge müssen im UI-Thread (STA) ausgeführt werden.
//                // Blazor-Calls kommen oft aus dem Worker-Pool.
//                await Application.Current.Dispatcher.InvokeAsync(() =>
//                {
//                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
//                    {
//                        Filter = "Bilder (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
//                        Title = "Bild auswählen",
//                        Multiselect = false
//                    };

//                    dialogResult = openFileDialog.ShowDialog();
//                    if (dialogResult == true)
//                    {
//                        selectedFilePath = openFileDialog.FileName;
//                    }
//                });

//                if (dialogResult == true && !string.IsNullOrEmpty(selectedFilePath))
//                {
//                    if (_appState != null)
//                        await _appState.Log($"[Media] WPF: Datei ausgewählt: {selectedFilePath}");

//                    byte[] bytes = await File.ReadAllBytesAsync(selectedFilePath);

//                    return new ScalarModel
//                    {
//                        out_value_bool = true,
//                        out_bytes = bytes
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                if (_appState != null)
//                    await _appState.Error($"[Media] WPF PickPhoto Exception: {ex.Message}");

//                return new ScalarModel
//                {
//                    out_value_bool = false,
//                    out_err = ex.Message
//                };
//            }

//            // Fallback für Abbruch durch User
//            return new ScalarModel
//            {
//                out_value_bool = false,
//                out_err = "User_Cancelled"
//            };
//        }

//        /* HINWEIS: 
//           CapturePhotoAsync, StartCameraAsync und StopCameraAsync werden hier NICHT überschrieben.
//           WPF nutzt somit automatisch die Logik der Basisklasse (pE.Services.Media.Media),
//           welche die Kamera über die WebView2-Schnittstelle (JavaScript pE_Web.media) anspricht.
//        */
//    }
//}

