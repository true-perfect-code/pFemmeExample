using BlazorCore.Services.AppState;
using BlazorCore.Services.GlobalState;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Text.Json;

namespace BlazorCore.Services.Logging
{
    public class Logging : ILogging
    {
        private IJSRuntime? _js;
        private readonly IGlobalStateBase _globalState;

        public Logging(IGlobalStateBase globalState)
        {
            _globalState = globalState;
        }

        public void Initialize(IJSRuntime js)
        {
            _js = js;
        }

        public async Task Log(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null, bool isDebugEnabledExt = false)
        {
            var isDebugEnabled = isDebugEnabledExt ? isDebugEnabledExt : _globalState.ConfigGeneral.IsDebugEnabled;
            if (!isDebugEnabled && level != AppLogLevel.Error)
                return;

            var method = level.ToString().ToLower();

            if (_js == null)
            {
                Debug.WriteLine($"[NATIVE-ONLY] {level}: {msg}");
                return;
            }

            try
            {
                if (data != null)
                    await _js.InvokeVoidAsync($"console.{method}", $"{msg}", data);
                else
                    await _js.InvokeVoidAsync($"console.{method}", $"{msg}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JS-LOG-FAILED] {level}: {msg} | Error: {ex.Message}");

                if (data != null)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(data);
                        Debug.WriteLine($"   Data: {json}");
                    }
                    catch { /* Serialisierung fehlgeschlagen */ }
                }
            }
        }

        public void LogVoid(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Log(msg, level, data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Fatal LogVoid Error: " + ex.Message);
                }
            });
        }

        public Task Warn(string msg, object? data = null) =>
            Log(msg, AppLogLevel.Warn, data);

        public Task Error(string msg, object? data = null) =>
            Log(msg, AppLogLevel.Error, data);
    }
}
