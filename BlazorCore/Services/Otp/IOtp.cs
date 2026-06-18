using BlazorCore.Services.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorCore.Services.Otp
{
    public interface IOtpBase
    {
        /// <summary>
        /// Generiert einen neuen OTP-Secret und speichert ihn auf dem Server.
        /// </summary>
        /// <param name="otpParameters">Parameter (UnixTS, AuthUsers_UnixTS, etc.)</param>
        /// <returns>OtpModel mit dem generierten Secret oder Fehlermeldung</returns>
        Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters);

        /// <summary>
        /// Prüft den Login-Status gegen den Server (für 2FA).
        /// </summary>
        /// <param name="otpParameters">Parameter (Account, Password, AuthUsers_UnixTS)</param>
        /// <returns>True wenn Login gültig, sonst False</returns>
        Task<bool> CheckServerLoginState(OtpParametersModel otpParameters);

        /// <summary>
        /// Löscht einen OTP-Schlüssel vom Server.
        /// </summary>
        /// <param name="otpParameters">Parameter mit Benutzerdaten</param>
        /// <param name="isHashed">Optional: Ob die Werte bereits gehasht sind</param>
        /// <returns>ScalarModel mit Erfolgsstatus</returns>
        Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false);
    }
}
