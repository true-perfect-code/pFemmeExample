using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace pFemmeExample.Shared.Models.Tests
{
    internal class PlatformModel
    {
    }

    /// <summary>
    /// Minimal test implementation of IPlatformBase
    /// </summary>
    public class PlatformTestMinimal : IPlatformBase
    {
        public string GetBaseDirectory() => AppDomain.CurrentDomain.BaseDirectory;
        public PLATFORMS GetCurrPlatform() => PLATFORMS.WINDOWS_CLIENT;
        public p11.UI.NativeDevice GetCurrDevice() => p11.UI.NativeDevice.WINDOWS;
        public string GetDeviceInfo() => "TestDevice_Integration";

        public Task<string> GetFormFactor() => Task.FromResult("Desktop");
        public Task<double> GetWindowWidth() => Task.FromResult(1920.0);
        public Task<double> GetWindowHeight() => Task.FromResult(1080.0);
        public Task<string> GetIdiomPlatform() => Task.FromResult("Desktop");
        public Task CopyTextToClipboard(string text) => Task.CompletedTask;
        public Task ShareText(string title, string text) => Task.CompletedTask;
        public Task<string?> DirectoryPicker() => Task.FromResult<string?>(null);
        public Task<ScalarModel> FilePicker(string Filter, string Title) => Task.FromResult(new ScalarModel());
        public Task<ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "")
            => Task.FromResult(new ScalarModel());
        public Task OpenExternalUrl(string url) => Task.CompletedTask;
        public Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false) => Task.FromResult<string?>(null);
        public Task RegisterNativeNavigationAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
            => Task.CompletedTask;
        public Task SetSwipeBackStateAsync(bool enabled) => Task.CompletedTask;
        public Task ForceResetSwipeAsync() => Task.CompletedTask;
        public Task ExitAppAsync() => Task.CompletedTask;
        public Task NavigateBackAsync() => Task.CompletedTask;
        public Task<ScalarModel> InitializeJSAsync() => Task.FromResult(new ScalarModel());
    }
}
