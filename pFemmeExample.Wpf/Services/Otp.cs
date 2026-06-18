using BlazorCore.Services.Dam;
using BlazorCore.Services.Otp;
using BlazorCore.Services.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace pFemmeExample.Wpf.Services
{
    /// <summary>
    /// WPF implementation of IOtpService.
    /// Uses anonymous server queries for OTP operations (no token required for 2FA).
    /// </summary>
    public class Otp : IOtpBase
    {
        private readonly IServiceProvider _serviceProvider;

        public Otp(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Generates a new OTP secret via server-side call.
        /// </summary>
        /// <param name="otpParameters">Parameters for OTP generation (UnixTS, AuthUsers_UnixTS, etc.)</param>
        /// <returns>OtpModel with generated secret or error message</returns>
        public async Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters)
        {
            try
            {
                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", "SaveOtp>>AuthUsers" },
                    { "@UnixTS", otpParameters.UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode },
                    { "@otp", "" },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS }
                };

                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                ScalarModel resultOtp = await dam.AnonymousQuery(db_para)!;

                if (resultOtp != null && resultOtp.out_value_str != null)
                {
                    if (string.IsNullOrEmpty(resultOtp.out_err) && resultOtp.out_value_str.ToLower().StartsWith("updated:"))
                    {
                        return new OtpModel { secret = resultOtp.out_value_str, err = "" };
                    }
                    return new OtpModel { secret = resultOtp.out_value_str, err = resultOtp.out_err ?? "" };
                }
                return new OtpModel { err = "no_otp" };
            }
            catch (Exception ex)
            {
                return new OtpModel { err = ex.Message };
            }
        }

        /// <summary>
        /// Checks login status against the server.
        /// </summary>
        /// <param name="otpParameters">Parameters with account, password, and AuthUsers_UnixTS</param>
        /// <returns>True if login is valid, otherwise false</returns>
        public async Task<bool> CheckServerLoginState(OtpParametersModel otpParameters)
        {
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

                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                ScalarModel resultCheckOtp = await dam.AnonymousQuery(db_para)!;
                return resultCheckOtp != null && string.IsNullOrEmpty(resultCheckOtp.out_err) && resultCheckOtp.out_value_bool;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes an OTP key from the server.
        /// Supports both backup code and 6-digit OTP code.
        /// </summary>
        /// <param name="otpParameters">Parameters with user data and OTP code</param>
        /// <param name="isHashed">Optional: Whether values are already hashed</param>
        /// <returns>ScalarModel with success status</returns>
        public async Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false)
        {
            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "DeleteOtp>>AuthUsers" },
                    { "@EmailHash", otpParameters.Account },
                    { "@PasswordHash", otpParameters.Password },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode },
                    { "tmp_userinputiode", otpParameters.OtpUserDigitInput },
                    { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() }
                };
                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                return await dam.AnonymousQuery(db_para)!;
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = ex.Message };
            }
        }
    }
}
