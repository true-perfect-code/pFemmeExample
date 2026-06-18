using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Otp;
using BlazorCore.Services.SqlClient;
using OtpNet;

namespace pFemmeExample.Web.Services
{
    /// <summary>
    /// Blazor Server implementation of IOtpService.
    /// Security-sensitive code (Pepper, Hashing, Decryption) runs ONLY on the server.
    /// </summary>
    public class Otp : IOtpBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalStateBase _globalState;

        public Otp(
            IServiceProvider serviceProvider,
            IGlobalStateBase globalState)
        {
            _serviceProvider = serviceProvider;
            _globalState = globalState;
        }

        /// <summary>
        /// Generates a new OTP secret (server-side only).
        /// </summary>
        public async Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters)
        {
            OtpModel result = new();
            result.err = "no_otp_code"; // Default value

            try
            {
                // Read pepper (server-side only)
                byte[]? Pepper = null;
                var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);

                if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
                {
                    var pepperFilePath = resultConfigurationFile.out_value_str;
                    try
                    {
                        using (pFemmeExample.Shared.Services.Security.SecurityServer aes = new())
                        {
                            Pepper = aes.GetPepper(pepperFilePath);
                        }
                    }
                    catch
                    {
                        // TODO: Logging
                    }
                }

                if (Pepper != null)
                {
                    // OTP - Generation
                    byte[] secretKey = KeyGeneration.GenerateRandomKey();
                    string base32Secret = Base32Encoding.ToString(secretKey).TrimEnd('=');

                    if (!string.IsNullOrEmpty(base32Secret))
                    {
                        result.secret = base32Secret; // Can only be sent to client once

                        using (var aes = new pFemmeExample.Shared.Services.Security.SecurityServer())
                        {
                            base32Secret = aes.EncryptBase32Secret(base32Secret, Pepper!);
                        }

                        // Database storage
                        var db_para = new Dictionary<string, string>
                        {
                            { "@Case_", "SaveOtp>>AuthUsers" },
                            { "@UnixTS", otpParameters.UnixTS },
                            { "@OtpBackupCode", otpParameters.OtpBackupCode },
                            { "@otp", base32Secret },
                            { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                            { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() }
                        };

                        var _dam = _serviceProvider.GetRequiredService<IDamBase>();
                        ScalarModel result_otp = await _dam.Save(db_para)!;

                        if (string.IsNullOrEmpty(result_otp.out_err) && result_otp.out_value_str != null)
                        {
                            if (result_otp.out_value_str.ToLower().StartsWith("updated:"))
                            {
                                result.err = "";
                            }
                        }
                        else
                        {
                            if (result_otp.out_err != null)
                                result.err = result_otp.out_err;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.err = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Checks login status against the server (with Pepper/Hashing).
        /// Security-sensitive code – runs ONLY on the server.
        /// </summary>
        public async Task<bool> CheckServerLoginState(OtpParametersModel login)
        {
            bool result = false;
            byte[]? Pepper = null;
            BlazorCore.Services.Authentication.LoginModel login_clone = new();

            // Read pepper (server-side only)
            var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);

            if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
            {
                var pepperFilePath = resultConfigurationFile.out_value_str;
                try
                {
                    using (pFemmeExample.Shared.Services.Security.SecurityServer aes = new())
                    {
                        Pepper = aes.GetPepper(pepperFilePath);
                    }
                }
                catch
                {
                    // TODO: Logging
                }
            }

            if (Pepper != null)
            {
                using (pFemmeExample.Shared.Services.Security.SecurityServer aes = new())
                {
                    login_clone.Password = aes.HashCredentials(login.Password, login.Account, Pepper!);
                    login_clone.Account = aes.HashUsername(login.Account, Pepper!);
                }
            }

            Dictionary<string, string> db_para = new()
            {
                { "@Case_", "CheckAccount>>AuthUsers" },
                { "@EmailHash", login_clone.Account! },
                { "@PasswordHash", login_clone.Password! },
                { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() } // Cloud only
            };

            var _dam = _serviceProvider.GetRequiredService<IDamBase>();
            ScalarModel resultOtp = await _dam.Scalar(db_para)!;

            if (resultOtp != null && string.IsNullOrEmpty(resultOtp.out_err))
            {
                result = resultOtp.out_value_bool;
            }

            return result;
        }

        /// <summary>
        /// Deletes an OTP key (server-side only).
        /// Supports both backup code and 6-digit OTP code.
        /// </summary>
        public async Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false)
        {
            ScalarModel result = new();

            try
            {
                byte[]? Pepper = null;
                OtpParametersModel otpParameters_clone = new();

                otpParameters_clone.AuthUsers_UnixTS = otpParameters.AuthUsers_UnixTS;
                otpParameters_clone.OtpBackupCode = otpParameters.OtpBackupCode;

                var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);

                if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
                {
                    var pepperFilePath = resultConfigurationFile.out_value_str;
                    try
                    {
                        using (pFemmeExample.Shared.Services.Security.SecurityServer aes = new())
                        {
                            Pepper = aes.GetPepper(pepperFilePath);
                        }
                    }
                    catch
                    {
                        // TODO: Logging
                    }
                }

                if (Pepper != null)
                {
                    // Validation via Account/Password or UnixTS
                    if (!string.IsNullOrEmpty(otpParameters.Account) && !string.IsNullOrEmpty(otpParameters.Password))
                    {
                        if (isHashed)
                        {
                            otpParameters_clone.Password = otpParameters.Password;
                            otpParameters_clone.Account = otpParameters.Account;
                        }
                        else
                        {
                            using (pFemmeExample.Shared.Services.Security.SecurityServer aes = new())
                            {
                                otpParameters_clone.Password = aes.HashCredentials(otpParameters.Password, otpParameters.Account, Pepper!);
                                otpParameters_clone.Account = aes.HashUsername(otpParameters.Account, Pepper!);
                            }
                        }
                    }

                    Dictionary<string, string> db_para = new()
                    {
                        { "@Case_", "DeleteOtp>>AuthUsers" },
                        { "@EmailHash", otpParameters_clone.Account },
                        { "@PasswordHash", otpParameters_clone.Password },
                        { "@AuthUsers_UnixTS", otpParameters_clone.AuthUsers_UnixTS },
                        { "@OtpBackupCode", otpParameters_clone.OtpBackupCode },
                        { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() }
                    };

                    var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                    // If no 6-digit OTP code provided, use backup code
                    if (string.IsNullOrEmpty(otpParameters.OtpUserDigitInput))
                    {
                        result = await _dam.ExecQuery(db_para)!;
                    }
                    else
                    {
                        // 2FA via 6-digit OTP code
                        db_para["@Case_"] = "SelectOtp>>AuthUsers";
                        ScalarModel resultSelectOtp = await _dam.Scalar(db_para);

                        if (resultSelectOtp != null && resultSelectOtp.out_value_str != null && string.IsNullOrEmpty(resultSelectOtp.out_err))
                        {
                            switch (resultSelectOtp.out_value_str)
                            {
                                case "no_user":
                                case "locked":
                                case "no_otp":
                                case "":
                                    resultSelectOtp.out_value_bool = false;
                                    break;

                                default: // OTP code returned
                                    bool verifyTotp = VerifyTotp(resultSelectOtp.out_value_str, otpParameters.OtpUserDigitInput, Pepper);
                                    resultSelectOtp.out_value_str = verifyTotp ? "1" : "0";

                                    if (verifyTotp)
                                    {
                                        db_para["@Case_"] = "DeleteOtpByAuthUsers_UnixTS>>AuthUsers";
                                        result = await _dam.ExecQuery(db_para);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"[ERROR]={ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Verifies a TOTP code against the secret.
        /// </summary>
        private static bool VerifyTotp(string secret, string userInputCode, byte[] pepper)
        {
            bool result = false;

            using (pFemmeExample.Shared.Services.Security.SecurityServer aes = new())
            {
                string decryptedBase32 = aes.DecryptBase32Secret(secret, pepper!);
                byte[] secretBytes = Base32Encoding.ToBytes(decryptedBase32);

                var totp = new Totp(secretBytes);
                result = totp.VerifyTotp(userInputCode, out _, new VerificationWindow(previous: 2, future: 2));
            }

            return result;
        }
    }
}