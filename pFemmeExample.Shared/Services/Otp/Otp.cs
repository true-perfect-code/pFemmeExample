using BlazorCore.Services.Dam;
using BlazorCore.Services.Otp;
using BlazorCore.Services.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pFemmeExample.Shared.Services.Otp
{
    /// <summary>
    /// Shared Implementierung von IOtpService für WASM, Capacitor und Blazor Server.
    /// Nutzt IDamBase für anonyme Server-Aufrufe (kein Token erforderlich für 2FA).
    /// </summary>
    public class Otp : IOtpBase
    {
        private readonly IServiceProvider _serviceProvider;

        public Otp(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Generiert einen neuen OTP-Secret auf dem Server.
        /// </summary>
        public async Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters)
        {
            OtpModel result = new();

            try
            {
                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", "SaveOtp>>AuthUsers" },
                    { "@UnixTS", otpParameters.UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode },
                    { "@otp", "" }, // wird auf dem WebServer generiert, verschlüsselt und gesetzt
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS }
                };

                var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                // Achtung: Zu diesem Zeitpunkt (bei dem eine 2FA noch nicht erfolgt ist) dürfen wir
                // kein Token haben. Folglich müssen wir die 2FA Validierung über den anonymen Endpoint durchführen
                ScalarModel resultOtp = await _dam!.AnonymousQuery(db_para)!;

                if (resultOtp != null && resultOtp.out_value_str != null)
                {
                    result.secret = resultOtp.out_value_str;

                    if (string.IsNullOrEmpty(resultOtp.out_err) && resultOtp.out_value_str != null)
                    {
                        if (resultOtp.out_value_str.ToLower().StartsWith("updated:"))
                        {
                            result.err = "";
                        }
                    }
                    else
                    {
                        if (resultOtp.out_err != null)
                            result.err = resultOtp.out_err;
                    }
                }
                else
                    result.err = "no_otp";
            }
            catch (Exception ex)
            {
                result.err = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Prüft den Login-Status gegen den Server.
        /// </summary>
        public async Task<bool> CheckServerLoginState(OtpParametersModel otpParameters)
        {
            var result = false;

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "CheckAccount>>AuthUsers" },
                    { "@EmailHash", otpParameters.Account },
                    { "@PasswordHash", otpParameters.Password },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                    { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() }
                };

                // Achtung: Zu diesem Zeitpunkt (bei dem eine 2FA noch nicht erfolgt ist) dürfen wir
                // kein Token haben. Folglich müssen wir die 2FA Validierung über den anonymen Endpoint durchführen
                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                ScalarModel resultCheckOtp = await dam!.AnonymousQuery(db_para)!;

                if (resultCheckOtp != null && string.IsNullOrEmpty(resultCheckOtp.out_err))
                    result = resultCheckOtp.out_value_bool;
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        /// <summary>
        /// Löscht einen OTP-Schlüssel vom Server.
        /// </summary>
        public async Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false)
        {
            ScalarModel result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "DeleteOtp>>AuthUsers" },
                    { "@EmailHash", otpParameters.Account },
                    { "@PasswordHash", otpParameters.Password },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode }, // Falls Benutzer die 2FA über Backupcode zurücksetzen muss
                    { "tmp_userinputiode", otpParameters.OtpUserDigitInput }, // ...oder halt über 6-stelligen OTP-Eingabe
                    { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() }
                };

                var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                // Achtung: Zu diesem Zeitpunkt (bei dem eine 2FA noch nicht erfolgt ist) dürfen wir
                // kein Token haben. Folglich müssen wir die 2FA Validierung über den anonymen Endpoint durchführen
                result = await _dam!.AnonymousQuery(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = $"[ERROR]={ex.Message}";
            }

            return result;
        }
    }
}