using BlazorCore.Services.AppState;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorCore.Services.Logging
{
    public interface ILogging
    {
        /// <summary>
        /// Initializes the logging service with JS runtime.
        /// Called once during app startup.
        /// </summary>
        void Initialize(IJSRuntime js);

        /// <summary>
        /// Logs a message to console (browser or native debug output).
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="level">Log level (Log, Warn, Error).</param>
        /// <param name="data">Optional data to serialize and log.</param>
        /// <param name="isDebugEnabled">Force log even if debug mode is off.</param>
        Task Log(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null, bool isDebugEnabled = false);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        Task Warn(string msg, object? data = null);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        Task Error(string msg, object? data = null);

        /// <summary>
        /// Fire-and-forget log – never throws.
        /// </summary>
        void LogVoid(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null);
    }
}
