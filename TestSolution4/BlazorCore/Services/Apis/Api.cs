#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
using Microsoft.Extensions.DependencyInjection;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BlazorCore.Services.Apis
{
    public class ApiBase : IApiBase
    {
        private readonly HttpClient _httpClient;
        private readonly IGlobalStateBase? _globalState;
        //private readonly IAppStateBase? _appState;

        //private readonly IServiceProvider serviceProvider;
        public ApiBase(IServiceProvider serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();
        }


        /// <summary>
        /// Liefert nach der Authentifizierung ein Token vom WebApi Server
        /// </summary>
        /// <param name="_clientid">Enthält die ClientId</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ClientStorageModel</returns>
        public async Task<ClientStorageModel> GetTokenDataTPC(UserWebApi user)
        {
            var tokenData = new ClientStorageModel();

            try
            {
                user.DisplayError = "1";
                //var response = await _httpClient.PostAsJsonAsync(_globalState!.ConfigWebapi.url_GetTokenDataTPCuser, user);
                // Serialisierung:
                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_GetTokenDataTPCuser, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    //var authContent = await response.Content.ReadAsStringAsync();
                    //var res = JsonSerializer.Deserialize<Dictionary<string, object>>(authContent);
                    var authContent = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize(authContent, JsonContext.Default.DictionaryStringObject);

                    if (res != null)
                    {
                        string msg = "";
                        string token = "";
                        string payload = "";

                        if (user.EncryptDecrypt == "1")
                        {
                            using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                            {
                                msg = String.IsNullOrEmpty(res["msg"].ToString()) ? "" : aes.Decrypt(res["msg"].ToString()!);
                                token = String.IsNullOrEmpty(res["token"].ToString()) ? "" : aes.Decrypt(res["token"].ToString()!);
                                if (!String.IsNullOrEmpty(token))
                                    payload = token.Split('.')[1];
                            }
                        }
                        else
                        {
                            msg = res["msg"].ToString()!;
                            token = res["token"].ToString()!;
                            if (!String.IsNullOrEmpty(token))
                                payload = token.Split('.')[1];
                        }

                        if (!String.IsNullOrEmpty(msg))
                        {
                            tokenData.UnixTS = string.Empty;
                            if (msg.StartsWith(_globalState.ConfigGeneral.WebapiExceptionError))
                                tokenData.out_err = token;
                            else
                                tokenData.out_err = msg;
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(payload))
                            {
                                var jsonBytes = _globalState.ParseBase64WithoutPadding(payload);
                                //var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
                                var keyValuePairs = JsonSerializer.Deserialize(jsonBytes, JsonContext.Default.DictionaryStringObject);

                                if (!string.IsNullOrEmpty(token) && keyValuePairs != null)
                                {
                                    tokenData.WebApiToken = token;
                                    if (keyValuePairs.ContainsKey("unix_ts") && keyValuePairs["unix_ts"] != null)
                                    {
                                        tokenData.UnixTS = keyValuePairs["unix_ts"].ToString()!;
                                    }
                                    else
                                    {
                                        tokenData.UnixTS = string.Empty;
                                    }
                                }
                                else
                                {
                                    tokenData.out_err = "no_token";
                                }
                            }
                            else
                                tokenData.out_err = "Token-Payload is empty";
                        }
                    }
                    else
                    {
                        tokenData.out_err = "Response content deserialization failed";
                    }
                }
                else
                {
                    tokenData.out_err = $"HTTP response failed: {response.ReasonPhrase}";
                }
            }
            catch (HttpRequestException ex)
            {
                tokenData.out_err = $"HTTP request failed: {ex.Message}";
            }
            catch (JsonException ex)
            {
                tokenData.out_err = $"JSON deserialization failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                tokenData.out_err = $"An unexpected error occurred: {ex.Message}";
            }

            return tokenData;
        }

        /// <summary>
        /// Liefert nach der Authentifizierung ein Token vom WebApi Server
        /// </summary>
        /// <param name="_clientid">Enthält die ClientId</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ClientStorageModel</returns>
        public async Task<ClientStorageModel> GetTokenDataIDP(UserWebApi user)
        {
            var tokenData = new ClientStorageModel();

            try
            {
                //user.DisplayError = "1";
                //var response = await _httpClient.PostAsJsonAsync(_globalState!.ConfigWebapi.url_GetTokenDataIDPuser, user);
                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_GetTokenDataIDPuser, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    //var authContent = await response.Content.ReadAsStringAsync();
                    //var res = JsonSerializer.Deserialize<Dictionary<string, object>>(authContent);
                    var authContent = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize(authContent, JsonContext.Default.DictionaryStringObject);

                    if (res != null)
                    {
                        string msg = "";
                        string token = "";
                        string payload = "";

                        if (user.EncryptDecrypt == "1")
                        {
                            using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                            {
                                msg = String.IsNullOrEmpty(res["msg"].ToString()) ? "" : aes.Decrypt(res["msg"].ToString()!);
                                token = String.IsNullOrEmpty(res["token"].ToString()) ? "" : aes.Decrypt(res["token"].ToString()!);
                                if (!String.IsNullOrEmpty(token))
                                    payload = token.Split('.')[1];
                            }
                        }
                        else
                        {
                            msg = res["msg"].ToString()!;
                            token = res["token"].ToString()!;
                            if (!String.IsNullOrEmpty(token))
                                payload = token.Split('.')[1];
                        }

                        if (!String.IsNullOrEmpty(msg))
                        {
                            tokenData.UnixTS = string.Empty;
                            if (msg.StartsWith(_globalState.ConfigGeneral.WebapiExceptionError))
                                tokenData.out_err = token;
                            else
                                tokenData.out_err = msg;
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(payload))
                            {
                                var jsonBytes = _globalState.ParseBase64WithoutPadding(payload);
                                //var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
                                var keyValuePairs = JsonSerializer.Deserialize(jsonBytes, JsonContext.Default.DictionaryStringObject);

                                if (!string.IsNullOrEmpty(token) && keyValuePairs != null)
                                {
                                    tokenData.WebApiToken = token;
                                    if (keyValuePairs.ContainsKey("unix_ts") && keyValuePairs["unix_ts"] != null)
                                    {
                                        tokenData.UnixTS = keyValuePairs["unix_ts"].ToString()!;
                                    }
                                    else
                                    {
                                        tokenData.UnixTS = string.Empty;
                                    }
                                }
                                else
                                {
                                    tokenData.out_err = "no_token";
                                }
                            }
                            else
                                tokenData.out_err = "Token-Payload is empty";
                        }
                    }
                    else
                    {
                        tokenData.out_err = "Response content deserialization failed";
                    }
                }
                else
                {
                    tokenData.out_err = $"HTTP response failed: {response.ReasonPhrase}";
                }
            }
            catch (HttpRequestException ex)
            {
                tokenData.out_err = $"HTTP request failed: {ex.Message}";
            }
            catch (JsonException ex)
            {
                tokenData.out_err = $"JSON deserialization failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                tokenData.out_err = $"An unexpected error occurred: {ex.Message}";
            }

            return tokenData;
        }

        /// <summary>
        /// Änder das Benutzerpasswort
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
        public async Task<ScalarModel> ChangePassword(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                //user.DisplayError = "1";
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_ChangePassword);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                //request.Content = JsonContent.Create(user);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                //var content = await response.Content.ReadAsStringAsync();
                //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
                //{
                //    PropertyNameCaseInsensitive = true
                //});
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                if (data != null)
                {
                    if (user.EncryptDecrypt == "1")
                    {
                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                        {
                            result.out_err = string.IsNullOrEmpty(data.out_err) ? "" : aes.Decrypt(data.out_err);
                            result.out_value_str = string.IsNullOrEmpty(data.out_value_str) ? "" : aes.Decrypt(data.out_value_str);
                        }
                        result.out_value_bool = data.out_value_bool!;
                        result.out_value_int = data.out_value_int!;
                        result.out_value_dbl = data.out_value_dbl!;
                        result.out_value_long = data.out_value_long!;
                    }
                    else
                    {
                        result.out_err = data.out_err!;
                        result.out_value_str = data.out_value_str!;

                        result.out_value_bool = data.out_value_bool!;
                        result.out_value_int = data.out_value_int!;
                        result.out_value_dbl = data.out_value_dbl!;
                        result.out_value_long = data.out_value_long!;
                    }

                    //if (int.TryParse(result.out_value_str, out int tmpInt))
                    //{
                    //    result.out_value_int = tmpInt;
                    //}

                    //if (double.TryParse(result.out_value_str, out double tmpDbl))
                    //{
                    //    result.out_value_dbl = tmpDbl;
                    //}

                    //if (bool.TryParse(result.out_value_str, out bool tmpBool))
                    //{
                    //    result.out_value_bool = tmpBool;
                    //}
                    //if (result.out_value_str == "1")
                    //{
                    //    result.out_value_bool = true;
                    //}
                }
                else
                {
                    result.out_err = "Error: Deserialized data is null";
                }
            }
            catch (HttpRequestException ex)
            {
                result.out_err = $"HTTP request failed: {ex.Message}";
            }
            catch (JsonException ex)
            {
                result.out_err = $"JSON deserialization failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.out_err = $"An unexpected error occurred: {ex.Message}";
            }

            return result;
        }

        ///// <summary>
        ///// Generiert otp Schlüssel auf dem Server
        ///// </summary>
        ///// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        ///// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
        //public async Task<ScalarModel> ManageOtp(MANAGE_OTP otpmana, UserWebApi user)
        //{
        //    var result = new ScalarModel();

        //    string url = string.Empty;
        //    switch (otpmana)
        //    {
        //        case MANAGE_OTP.GENERATE:
        //            url = _globalState!.ConfigWebapi.url_GenerateOtpKey;
        //            break;
        //        case MANAGE_OTP.DELETE:
        //            url = _globalState!.ConfigWebapi.url_DeleteOtpKey;
        //            break;
        //        case MANAGE_OTP.VALIDATE:
        //            url = _globalState!.ConfigWebapi.url_ValidateOtpCode;
        //            break;
        //        default:
        //            break;
        //    }

        //    if (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            user.DisplayError = "1";
        //            using var request = new HttpRequestMessage(HttpMethod.Post, url);
        //            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
        //            //request.Content = JsonContent.Create(user);
        //            request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

        //            using var response = await _httpClient.SendAsync(request);

        //            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //            {
        //                result.out_err = "Error 401: Unauthorized request.";
        //                return result;
        //            }

        //            response.EnsureSuccessStatusCode();

        //            //var content = await response.Content.ReadAsStringAsync();
        //            //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
        //            //{
        //            //    PropertyNameCaseInsensitive = true
        //            //});
        //            var content = await response.Content.ReadAsStringAsync();
        //            var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

        //            if (data != null)
        //            {
        //                if (user.EncryptDecrypt == "1")
        //                {
        //                    using (Security aes = new())
        //                    {
        //                        result.out_err = string.IsNullOrEmpty(data.out_err) ? "" : aes.Decrypt(data.out_err);
        //                        result.out_value_str = string.IsNullOrEmpty(data.out_value_str) ? "" : aes.Decrypt(data.out_value_str);
        //                    }
        //                }
        //                else
        //                {
        //                    result.out_err = data.out_err!;
        //                    result.out_value_str = data.out_value_str!;
        //                }

        //                if (int.TryParse(result.out_value_str, out int tmpInt))
        //                {
        //                    result.out_value_int = tmpInt;
        //                }

        //                if (double.TryParse(result.out_value_str, out double tmpDbl))
        //                {
        //                    result.out_value_dbl = tmpDbl;
        //                }

        //                if (bool.TryParse(result.out_value_str, out bool tmpBool))
        //                {
        //                    result.out_value_bool = tmpBool;
        //                }
        //                if (result.out_value_str == "1")
        //                {
        //                    result.out_value_bool = true;
        //                }
        //            }
        //            else
        //            {
        //                result.out_err = "Error: Deserialized data is null";
        //            }
        //        }
        //        catch (HttpRequestException ex)
        //        {
        //            result.out_err = $"HTTP request failed: {ex.Message}";
        //        }
        //        catch (JsonException ex)
        //        {
        //            result.out_err = $"JSON deserialization failed: {ex.Message}";
        //        }
        //        catch (Exception ex)
        //        {
        //            result.out_err = $"An unexpected error occurred: {ex.Message}";
        //        }
        //    }

        //    return result;
        //}

        /// <summary>
        /// Liefert ein Scalarwert vom WebApi Server (z.B. Count(*) aus DB)
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
        public async Task<ScalarModel> GetScalar(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                //user.DisplayError = "1";
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_GetScalar);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                //request.Content = JsonContent.Create(user);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                //var content = await response.Content.ReadAsStringAsync();
                //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
                //{
                //    PropertyNameCaseInsensitive = true
                //});
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                if (data != null)
                {
                    if (user.EncryptDecrypt == "1")
                    {
                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                        {
                            result.out_err = aes.Decrypt(data.out_err!);
                            result.out_value_str = aes.Decrypt(data.out_value_str!);
                        }

                        result.out_value_bool = data.out_value_bool!;
                        result.out_value_int = data.out_value_int!;
                        result.out_value_dbl = data.out_value_dbl!;
                        result.out_value_long = data.out_value_long!;
                    }
                    else
                    {
                        result.out_err = data.out_err!;
                        result.out_value_str = data.out_value_str!;

                        result.out_value_bool = data.out_value_bool!;
                        result.out_value_int = data.out_value_int!;
                        result.out_value_dbl = data.out_value_dbl!;
                        result.out_value_long = data.out_value_long!;
                    }

                    if (user.IsByte == "1")
                    {
                        if (!string.IsNullOrEmpty(result.out_value_str))
                        {
                            result.out_bytes = Encoding.UTF8.GetBytes(result.out_value_str);
                        }
                    }
                    //else
                    //{
                    //    if (int.TryParse(result.out_value_str, out int tmpInt))
                    //    {
                    //        result.out_value_int = tmpInt;
                    //    }

                    //    if (long.TryParse(result.out_value_str, out long tmplong))
                    //    {
                    //        result.out_value_long = tmplong;
                    //    }

                    //    if (double.TryParse(result.out_value_str, out double tmpDbl))
                    //    {
                    //        result.out_value_dbl = tmpDbl;
                    //    }

                    //    if (bool.TryParse(result.out_value_str, out bool tmpBool))
                    //    {
                    //        result.out_value_bool = tmpBool;
                    //    }
                    //    if (result.out_value_str == "1")
                    //    {
                    //        result.out_value_bool = true;
                    //    }
                    //}
                }
                else
                {
                    result.out_err = "Error: Deserialized data is null";
                }
            }
            catch (HttpRequestException ex)
            {
                result.out_err = $"HTTP request failed: {ex.Message}";
            }
            catch (JsonException ex)
            {
                result.out_err = $"JSON deserialization failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.out_err = $"An unexpected error occurred: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Sendet Daten zum WebApi Server (z.B. um Daten speichern)
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
        public async Task<ScalarModel> PostData(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                //user.DisplayError = "1";
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_SetData);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                //request.Content = JsonContent.Create(user);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                //var content = await response.Content.ReadAsStringAsync();
                //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
                //{
                //    PropertyNameCaseInsensitive = true
                //});
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                if (data != null)
                {
                    if (user.EncryptDecrypt == "1")
                    {
                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                        {
                            result.out_err = aes.Decrypt(data.out_err!);
                            result.out_value_str = aes.Decrypt(data.out_value_str!);
                        }

                        result.out_value_bool = data.out_value_bool!;
                        result.out_value_int = data.out_value_int!;
                        result.out_value_dbl = data.out_value_dbl!;
                        result.out_value_long = data.out_value_long!;
                    }
                    else
                    {
                        result.out_err = data.out_err!;
                        result.out_value_str = data.out_value_str!;

                        result.out_value_bool = data.out_value_bool!;
                        result.out_value_int = data.out_value_int!;
                        result.out_value_dbl = data.out_value_dbl!;
                        result.out_value_long = data.out_value_long!;
                    }

                    //if (int.TryParse(result.out_value_str, out int tmpInt))
                    //{
                    //    result.out_value_int = tmpInt;
                    //}

                    //if (long.TryParse(result.out_value_str, out long tmplong))
                    //{
                    //    result.out_value_long = tmplong;
                    //}

                    //if (double.TryParse(result.out_value_str, out double tmpDbl))
                    //{
                    //    result.out_value_dbl = tmpDbl;
                    //}

                    //if (bool.TryParse(result.out_value_str, out bool tmpBool))
                    //{
                    //    result.out_value_bool = tmpBool;
                    //}
                    //if (result.out_value_str == "1")
                    //{
                    //    result.out_value_bool = true;
                    //}
                }
                else
                {
                    result.out_err = "Error: Deserialized data is null";
                }
            }
            catch (HttpRequestException ex)
            {
                result.out_err = $"HTTP request failed: {ex.Message}";
            }
            catch (JsonException ex)
            {
                result.out_err = $"JSON deserialization failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.out_err = $"An unexpected error occurred: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Anonyme Abfrage an WebApi Server (z.B. um Daten zu erhalten, ohne dass der Client angemeldet ist)
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
        public async Task<ScalarModel> AnonymousQuery(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                //var response = await _httpClient.PostAsJsonAsync(_globalState!.ConfigWebapi.url_Anonymous, user);
                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_Anonymous, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();

                    //var content = await response.Content.ReadAsStringAsync();
                    //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
                    //{
                    //    PropertyNameCaseInsensitive = true
                    //});
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                    if (data != null)
                    {
                        if (user.EncryptDecrypt == "1")
                        {
                            using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                            {
                                result.out_err = string.IsNullOrEmpty(data.out_err) ? "" : aes.Decrypt(data.out_err!);
                                result.out_value_str = string.IsNullOrEmpty(data.out_value_str) ? "" : aes.Decrypt(data.out_value_str!);
                            }
                            result.out_value_bool = data.out_value_bool!;
                            result.out_value_int = data.out_value_int!;
                            result.out_value_dbl = data.out_value_dbl!;
                            result.out_value_long = data.out_value_long!;
                        }
                        else
                        {
                            result.out_err = data.out_err!;
                            result.out_value_str = data.out_value_str!;

                            result.out_value_bool = data.out_value_bool!;
                            result.out_value_int = data.out_value_int!;
                            result.out_value_dbl = data.out_value_dbl!;
                            result.out_value_long = data.out_value_long!;
                        }

                        //if (int.TryParse(result.out_value_str, out int tmpInt))
                        //{
                        //    result.out_value_int = tmpInt;
                        //}

                        //if (long.TryParse(result.out_value_str, out long tmplong))
                        //{
                        //    result.out_value_long = tmplong;
                        //}

                        //if (double.TryParse(result.out_value_str, out double tmpDbl))
                        //{
                        //    result.out_value_dbl = tmpDbl;
                        //}

                        //if (bool.TryParse(result.out_value_str, out bool tmpBool))
                        //{
                        //    result.out_value_bool = tmpBool;
                        //}
                        //if (result.out_value_str == "1")
                        //{
                        //    result.out_value_bool = true;
                        //}
                    }
                    else
                    {
                        result.out_err = "Error: Deserialized data is null";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                result.out_err = $"HTTP request failed: {ex.Message}";
            }
            catch (JsonException ex)
            {
                result.out_err = $"JSON deserialization failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.out_err = $"An unexpected error occurred: {ex.Message}";
            }

            return result;
        }

        ///// <summary>
        ///// Liefert eine Liste (Tabelle) vom WebApi Server (z.B. SELECT * aus DB)
        ///// </summary>
        ///// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        ///// <returns>Rückgabewert ist ein Objekt vom Typ ReaderModel</returns>
        //public async Task<ReaderModel<T>> GetRows<T>(UserWebApi user)
        //{
        //    var result = new ReaderModel<T>();

        //    try
        //    {
        //        using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_GetRows);
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
        //        //request.Content = JsonContent.Create(user);
        //        request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

        //        using var response = await _httpClient.SendAsync(request);

        //        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //        {
        //            result.out_err = "Error 401: Unauthorized request.";
        //            return result;
        //        }

        //        response.EnsureSuccessStatusCode();

        //        var content = await response.Content.ReadAsStringAsync();
        //        //var readerDynamicModel = JsonSerializer.Deserialize<ReaderDynamicModel>(content, new JsonSerializerOptions
        //        //{
        //        //    PropertyNameCaseInsensitive = true
        //        //});
        //        // Deserialisierung des ReaderDynamicModel ist AOT-sicher:
        //        var readerDynamicModel = JsonSerializer.Deserialize(content, JsonContext.Default.ReaderDynamicModel);

        //        if (readerDynamicModel == null)
        //        {
        //            throw new InvalidOperationException("Deserialized response is null");
        //        }

        //        if (user.EncryptDecrypt == "1")
        //        {
        //            using (Security aes = new())
        //            {
        //                result.out_err = aes.Decrypt(readerDynamicModel.out_err!);
        //                result.out_json = aes.Decrypt(readerDynamicModel.out_json!);
        //            }
        //        }
        //        else
        //        {
        //            result.out_err = readerDynamicModel.out_err!;
        //            result.out_json = readerDynamicModel.out_json!;
        //        }

        //        // Generische List Deserialisierung verwendet den neuen, isolierten Helper.
        //        // Dieser ist NICHT Trimming-sicher (markiert mit RequiresUnreferencedCode in JsonUtility).
        //        result.out_list = JsonUtility.DeserializeListSafe<T>(result.out_json);

        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        result.out_err = $"HTTP request failed: {ex.Message}";
        //    }
        //    catch (JsonException ex)
        //    {
        //        result.out_err = $"JSON deserialization failed: {ex.Message}";
        //    }
        //    catch (Exception ex)
        //    {
        //        result.out_err = $"An unexpected error occurred: {ex.Message}";
        //    }

        //    return result;
        //}
        /// <summary>
        /// Liefert eine Liste (Tabelle) vom WebApi Server (AOT-sicher durch TypeInfo-Übergabe)
        /// </summary>
        /// <param name="user">Model Objekt enthält alle Informationen vom Client</param>
        /// <param name="listTypeInfo">Der AOT-sichere JsonTypeInfo für List<T?> aus dem JsonContext.</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ReaderModel</returns>
        //public async Task<ReaderModel<T>> GetRows<T>(UserWebApi user, JsonTypeInfo<List<T?>> listTypeInfo) where T : class
        public async Task<ReaderModel<T>> GetRows<T>(UserWebApi user, JsonTypeInfo<List<T?>> listTypeInfo)
        {
            var result = new ReaderModel<T>();

            try
            {
                //user.DisplayError = "1";
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_GetRows);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                // Serialisierung des UserWebApi ist bereits AOT-sicher:
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                // Deserialisierung des ReaderDynamicModel ist bereits AOT-sicher:
                var readerDynamicModel = JsonSerializer.Deserialize(content, JsonContext.Default.ReaderDynamicModel);

                if (readerDynamicModel == null)
                {
                    throw new InvalidOperationException("Deserialized response is null");
                }

                if (user.EncryptDecrypt == "1")
                {
                    using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                    {
                        result.out_err = aes.Decrypt(readerDynamicModel.out_err!);
                        result.out_json = aes.Decrypt(readerDynamicModel.out_json!);
                    }
                }
                else
                {
                    result.out_err = readerDynamicModel.out_err!;
                    result.out_json = readerDynamicModel.out_json!;
                }

                // NEU: AOT-sichere Deserialisierung
                // Wir verwenden den statisch generierten TypeInfo anstelle der Reflection-basierten Methode.
                result.out_list = JsonSerializer.Deserialize(result.out_json, listTypeInfo);

            }
            catch (HttpRequestException ex)
            {
                result.out_err = $"HTTP request failed: {ex.Message}";
            }
            catch (JsonException ex)
            {
                result.out_err = $"JSON deserialization failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.out_err = $"An unexpected error occurred: {ex.Message}";
            }

            return result;
        }

        ///// <summary>
        ///// Prüft, ob der Token beim WebApi Server noch gültig ist.
        ///// </summary>
        ///// <param name="user">Model Objekt enthält alle Informationen vom Client</param>
        ///// <returns>Rückgabewert ist ein Objekt vom Typ TokenCheckModel</returns>
        //public async Task<TokenCheckModel> CheckToken(UserWebApi user)
        //{
        //    var result = new TokenCheckModel();

        //    try
        //    {
        //        using var request = new HttpRequestMessage(HttpMethod.Get, _globalState!.ConfigWebapi.url_CheckToken);
        //        request.Headers.Authorization =
        //            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);

        //        using var response = await _httpClient.SendAsync(request);

        //        // -------------------------
        //        // Token ungültig? → 401
        //        // -------------------------
        //        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //        {
        //            result.IsValid = false;
        //            result.out_err = "Error 401: Unauthorized request.";
        //            return result;
        //        }

        //        // -------------------------
        //        // andere Fehler?
        //        // -------------------------
        //        response.EnsureSuccessStatusCode();

        //        var content = await response.Content.ReadAsStringAsync();
        //        var data = JsonSerializer.Deserialize(content, JsonContext.Default.TokenCheckModel);

        //        if (data != null)
        //        {
        //            if (user.EncryptDecrypt == "1")
        //            {
        //                using (Security aes = new())
        //                {
        //                    result.out_err = aes.Decrypt(data.out_err!);
        //                    result.IsValid = aes.Decrypt(data.IsValidStr!) == "1";
        //                }
        //            }
        //            else
        //            {
        //                result.out_err = data.out_err!;
        //                result.IsValid = data.IsValid;
        //            }
        //        }
        //        else
        //        {
        //            result.out_err = "Error: Deserialized data is null";
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        result.out_err = $"HTTP request failed: {ex.Message}";
        //    }
        //    catch (JsonException ex)
        //    {
        //        result.out_err = $"JSON deserialization failed: {ex.Message}";
        //    }
        //    catch (Exception ex)
        //    {
        //        result.out_err = $"An unexpected error occurred: {ex.Message}";
        //    }

        //    return result;
        //}
        /// <summary>
        /// Prüft, ob der Token beim WebApi Server noch gültig ist (basierend auf dem HTTP-Statuscode).
        /// </summary>
        /// <param name="user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>True, wenn das Token gültig ist (Statuscode 200), andernfalls False.</returns>
        public async Task<bool> CheckToken(UserWebApi user)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, _globalState!.ConfigWebapi.url_CheckToken);
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);

                using var response = await _httpClient.SendAsync(request);

                // -------------------------
                // Token gültig? → 200 OK
                // -------------------------
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }

                // -------------------------
                // Token ungültig/Autorisierung fehlgeschlagen? → 401 Unauthorized (oder 403 Forbidden)
                // -------------------------
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return false;
                }

                // -------------------------
                // Andere Fehler (z.B. 5xx Serverfehler)
                // Hier können Sie entscheiden, ob Sie im Fehlerfall (z.B. Server nicht erreichbar) 
                // true/false zurückgeben oder eine Exception werfen wollen.
                // Für diesen Anwendungsfall ist false sicherer.
                // -------------------------
                return false;
            }
            catch (Exception ex)
            {
                // Ausnahmebehandlung für Netzwerkfehler, Timeout usw.
                // Für eine Token-Prüfung ist es sinnvoll, im Fehlerfall von einem ungültigen/nicht prüfbaren Token auszugehen.
                System.Diagnostics.Debug.WriteLine($"Error during token check: {ex.Message}");
                return false;
            }
        }


    }
}
#pragma warning restore CA1416




//#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
//using Microsoft.Extensions.DependencyInjection;
//using BlazorCore.Services.Dam;
//using BlazorCore.Services.GlobalState;
//using BlazorCore.Services.SqlClient;
//using BlazorCore.Utility;
//using System.Net.Http.Json;
//using System.Text;
//using System.Text.Json;

//namespace BlazorCore.Services.Apis
//{
//    public class ApiBase : IApiBase
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IGlobalStateBase? _globalState;

//        //private readonly IServiceProvider serviceProvider;
//        public ApiBase(IServiceProvider serviceProvider)
//        {
//            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
//            _httpClient.Timeout = TimeSpan.FromSeconds(30);
//            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();
//        }


//        /// <summary>
//        /// Liefert nach der Authentifizierung ein Token vom WebApi Server
//        /// </summary>
//        /// <param name="_clientid">Enthält die ClientId</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ClientStorageModel</returns>
//        public async Task<ClientStorageModel> GetTokenDataTPC(UserWebApi user)
//        {
//            var tokenData = new ClientStorageModel();

//            try
//            {
//                //user.DisplayError = "1";
//                //var response = await _httpClient.PostAsJsonAsync(_globalState!.ConfigWebapi.url_GetTokenDataTPCuser, user);
//                // Serialisierung:
//                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
//                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_GetTokenDataTPCuser, requestContent);

//                if (response.IsSuccessStatusCode)
//                {
//                    //var authContent = await response.Content.ReadAsStringAsync();
//                    //var res = JsonSerializer.Deserialize<Dictionary<string, object>>(authContent);
//                    var authContent = await response.Content.ReadAsStringAsync();
//                    var res = JsonSerializer.Deserialize(authContent, JsonContext.Default.DictionaryStringObject);

//                    if (res != null)
//                    {
//                        string msg = "";
//                        string token = "";
//                        string payload = "";

//                        if (user.EncryptDecrypt == "1")
//                        {
//                            using (Security aes = new())
//                            {
//                                msg = String.IsNullOrEmpty(res["msg"].ToString()) ? "" : aes.Decrypt(res["msg"].ToString()!);
//                                token = String.IsNullOrEmpty(res["token"].ToString()) ? "" : aes.Decrypt(res["token"].ToString()!);
//                                if (!String.IsNullOrEmpty(token))
//                                    payload = token.Split('.')[1];
//                            }
//                        }
//                        else
//                        {
//                            msg = res["msg"].ToString()!;
//                            token = res["token"].ToString()!;
//                            if (!String.IsNullOrEmpty(token))
//                                payload = token.Split('.')[1];
//                        }

//                        if (!String.IsNullOrEmpty(msg))
//                        {
//                            tokenData.UnixTS = string.Empty;
//                            if (msg.StartsWith(_globalState.WebapiExceptionError))
//                                tokenData.out_err = token;
//                            else
//                                tokenData.out_err = msg;
//                        }
//                        else
//                        {
//                            if (!String.IsNullOrEmpty(payload))
//                            {
//                                var jsonBytes = _globalState.ParseBase64WithoutPadding(payload);
//                                //var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
//                                var keyValuePairs = JsonSerializer.Deserialize(jsonBytes, JsonContext.Default.DictionaryStringObject);

//                                if (!string.IsNullOrEmpty(token) && keyValuePairs != null)
//                                {
//                                    tokenData.WebApiToken = token;
//                                    if (keyValuePairs.ContainsKey("unix_ts") && keyValuePairs["unix_ts"] != null)
//                                    {
//                                        tokenData.UnixTS = keyValuePairs["unix_ts"].ToString()!;
//                                    }
//                                    else
//                                    {
//                                        tokenData.UnixTS = string.Empty;
//                                    }
//                                }
//                                else
//                                {
//                                    tokenData.out_err = "no_token";
//                                }
//                            }
//                            else
//                                tokenData.out_err = "Token-Payload is empty";
//                        }
//                    }
//                    else
//                    {
//                        tokenData.out_err = "Response content deserialization failed";
//                    }
//                }
//                else
//                {
//                    tokenData.out_err = $"HTTP response failed: {response.ReasonPhrase}";
//                }
//            }
//            catch (HttpRequestException ex)
//            {
//                tokenData.out_err = $"HTTP request failed: {ex.Message}";
//            }
//            catch (JsonException ex)
//            {
//                tokenData.out_err = $"JSON deserialization failed: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                tokenData.out_err = $"An unexpected error occurred: {ex.Message}";
//            }

//            return tokenData;
//        }

//        /// <summary>
//        /// Liefert nach der Authentifizierung ein Token vom WebApi Server
//        /// </summary>
//        /// <param name="_clientid">Enthält die ClientId</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ClientStorageModel</returns>
//        public async Task<ClientStorageModel> GetTokenDataIDP(UserWebApi user)
//        {
//            var tokenData = new ClientStorageModel();

//            try
//            {
//                //var response = await _httpClient.PostAsJsonAsync(_globalState!.ConfigWebapi.url_GetTokenDataIDPuser, user);
//                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
//                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_GetTokenDataIDPuser, requestContent);

//                if (response.IsSuccessStatusCode)
//                {
//                    //var authContent = await response.Content.ReadAsStringAsync();
//                    //var res = JsonSerializer.Deserialize<Dictionary<string, object>>(authContent);
//                    var authContent = await response.Content.ReadAsStringAsync();
//                    var res = JsonSerializer.Deserialize(authContent, JsonContext.Default.DictionaryStringObject);

//                    if (res != null)
//                    {
//                        string msg = "";
//                        string token = "";
//                        string payload = "";

//                        if (user.EncryptDecrypt == "1")
//                        {
//                            using (Security aes = new())
//                            {
//                                msg = String.IsNullOrEmpty(res["msg"].ToString()) ? "" : aes.Decrypt(res["msg"].ToString()!);
//                                token = String.IsNullOrEmpty(res["token"].ToString()) ? "" : aes.Decrypt(res["token"].ToString()!);
//                                if (!String.IsNullOrEmpty(token))
//                                    payload = token.Split('.')[1];
//                            }
//                        }
//                        else
//                        {
//                            msg = res["msg"].ToString()!;
//                            token = res["token"].ToString()!;
//                            if (!String.IsNullOrEmpty(token))
//                                payload = token.Split('.')[1];
//                        }

//                        if (!String.IsNullOrEmpty(msg))
//                        {
//                            tokenData.UnixTS = string.Empty;
//                            if (msg.StartsWith(_globalState.WebapiExceptionError))
//                                tokenData.out_err = token;
//                            else
//                                tokenData.out_err = msg;
//                        }
//                        else
//                        {
//                            if (!String.IsNullOrEmpty(payload))
//                            {
//                                var jsonBytes = _globalState.ParseBase64WithoutPadding(payload);
//                                //var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
//                                var keyValuePairs = JsonSerializer.Deserialize(jsonBytes, JsonContext.Default.DictionaryStringObject);

//                                if (!string.IsNullOrEmpty(token) && keyValuePairs != null)
//                                {
//                                    tokenData.WebApiToken = token;
//                                    if (keyValuePairs.ContainsKey("unix_ts") && keyValuePairs["unix_ts"] != null)
//                                    {
//                                        tokenData.UnixTS = keyValuePairs["unix_ts"].ToString()!;
//                                    }
//                                    else
//                                    {
//                                        tokenData.UnixTS = string.Empty;
//                                    }
//                                }
//                                else
//                                {
//                                    tokenData.out_err = "no_token";
//                                }
//                            }
//                            else
//                                tokenData.out_err = "Token-Payload is empty";
//                        }
//                    }
//                    else
//                    {
//                        tokenData.out_err = "Response content deserialization failed";
//                    }
//                }
//                else
//                {
//                    tokenData.out_err = $"HTTP response failed: {response.ReasonPhrase}";
//                }
//            }
//            catch (HttpRequestException ex)
//            {
//                tokenData.out_err = $"HTTP request failed: {ex.Message}";
//            }
//            catch (JsonException ex)
//            {
//                tokenData.out_err = $"JSON deserialization failed: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                tokenData.out_err = $"An unexpected error occurred: {ex.Message}";
//            }

//            return tokenData;
//        }

//        /// <summary>
//        /// Änder das Benutzerpasswort
//        /// </summary>
//        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
//        public async Task<ScalarModel> ChangePassword(UserWebApi user)
//        {
//            var result = new ScalarModel();

//            try
//            {
//                //user.DisplayError = "1";
//                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_ChangePassword);
//                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
//                //request.Content = JsonContent.Create(user);
//                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

//                using var response = await _httpClient.SendAsync(request);

//                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//                {
//                    result.out_err = "Error 401: Unauthorized request.";
//                    return result;
//                }

//                response.EnsureSuccessStatusCode();

//                //var content = await response.Content.ReadAsStringAsync();
//                //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
//                //{
//                //    PropertyNameCaseInsensitive = true
//                //});
//                var content = await response.Content.ReadAsStringAsync();
//                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

//                if (data != null)
//                {
//                    if (user.EncryptDecrypt == "1")
//                    {
//                        using (Security aes = new())
//                        {
//                            result.out_err = string.IsNullOrEmpty(data.out_err) ? "" : aes.Decrypt(data.out_err);
//                            result.out_value_str = string.IsNullOrEmpty(data.out_value_str) ? "" : aes.Decrypt(data.out_value_str);
//                        }
//                    }
//                    else
//                    {
//                        result.out_err = data.out_err!;
//                        result.out_value_str = data.out_value_str!;
//                    }

//                    if (int.TryParse(result.out_value_str, out int tmpInt))
//                    {
//                        result.out_value_int = tmpInt;
//                    }

//                    if (double.TryParse(result.out_value_str, out double tmpDbl))
//                    {
//                        result.out_value_dbl = tmpDbl;
//                    }

//                    if (bool.TryParse(result.out_value_str, out bool tmpBool))
//                    {
//                        result.out_value_bool = tmpBool;
//                    }
//                    if (result.out_value_str == "1")
//                    {
//                        result.out_value_bool = true;
//                    }
//                }
//                else
//                {
//                    result.out_err = "Error: Deserialized data is null";
//                }
//            }
//            catch (HttpRequestException ex)
//            {
//                result.out_err = $"HTTP request failed: {ex.Message}";
//            }
//            catch (JsonException ex)
//            {
//                result.out_err = $"JSON deserialization failed: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                result.out_err = $"An unexpected error occurred: {ex.Message}";
//            }

//            return result;
//        }

//        /// <summary>
//        /// Generiert otp Schlüssel auf dem Server
//        /// </summary>
//        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
//        public async Task<ScalarModel> ManageOtp(MANAGE_OTP otpmana, UserWebApi user)
//        {
//            var result = new ScalarModel();

//            string url = string.Empty;
//            switch (otpmana)
//            {
//                case MANAGE_OTP.GENERATE:
//                    url = _globalState!.ConfigWebapi.url_GenerateOtpKey;
//                    break;
//                case MANAGE_OTP.DELETE:
//                    url = _globalState!.ConfigWebapi.url_DeleteOtpKey;
//                    break;
//                case MANAGE_OTP.VALIDATE:
//                    url = _globalState!.ConfigWebapi.url_ValidateOtpCode;
//                    break;
//                default:
//                    break;
//            }

//            if (!string.IsNullOrEmpty(url))
//            {
//                try
//                {
//                    //user.DisplayError = "1";
//                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
//                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
//                    //request.Content = JsonContent.Create(user);
//                    request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

//                    using var response = await _httpClient.SendAsync(request);

//                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//                    {
//                        result.out_err = "Error 401: Unauthorized request.";
//                        return result;
//                    }

//                    response.EnsureSuccessStatusCode();

//                    //var content = await response.Content.ReadAsStringAsync();
//                    //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
//                    //{
//                    //    PropertyNameCaseInsensitive = true
//                    //});
//                    var content = await response.Content.ReadAsStringAsync();
//                    var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

//                    if (data != null)
//                    {
//                        if (user.EncryptDecrypt == "1")
//                        {
//                            using (Security aes = new())
//                            {
//                                result.out_err = string.IsNullOrEmpty(data.out_err) ? "" : aes.Decrypt(data.out_err);
//                                result.out_value_str = string.IsNullOrEmpty(data.out_value_str) ? "" : aes.Decrypt(data.out_value_str);
//                            }
//                        }
//                        else
//                        {
//                            result.out_err = data.out_err!;
//                            result.out_value_str = data.out_value_str!;
//                        }

//                        if (int.TryParse(result.out_value_str, out int tmpInt))
//                        {
//                            result.out_value_int = tmpInt;
//                        }

//                        if (double.TryParse(result.out_value_str, out double tmpDbl))
//                        {
//                            result.out_value_dbl = tmpDbl;
//                        }

//                        if (bool.TryParse(result.out_value_str, out bool tmpBool))
//                        {
//                            result.out_value_bool = tmpBool;
//                        }
//                        if (result.out_value_str == "1")
//                        {
//                            result.out_value_bool = true;
//                        }
//                    }
//                    else
//                    {
//                        result.out_err = "Error: Deserialized data is null";
//                    }
//                }
//                catch (HttpRequestException ex)
//                {
//                    result.out_err = $"HTTP request failed: {ex.Message}";
//                }
//                catch (JsonException ex)
//                {
//                    result.out_err = $"JSON deserialization failed: {ex.Message}";
//                }
//                catch (Exception ex)
//                {
//                    result.out_err = $"An unexpected error occurred: {ex.Message}";
//                }
//            }

//            return result;
//        }

//        /// <summary>
//        /// Liefert ein Scalarwert vom WebApi Server (z.B. Count(*) aus DB)
//        /// </summary>
//        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
//        public async Task<ScalarModel> GetScalar(UserWebApi user)
//        {
//            var result = new ScalarModel();

//            try
//            {
//                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_GetScalar);
//                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
//                //request.Content = JsonContent.Create(user);
//                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

//                using var response = await _httpClient.SendAsync(request);

//                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//                {
//                    result.out_err = "Error 401: Unauthorized request.";
//                    return result;
//                }

//                response.EnsureSuccessStatusCode();

//                //var content = await response.Content.ReadAsStringAsync();
//                //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
//                //{
//                //    PropertyNameCaseInsensitive = true
//                //});
//                var content = await response.Content.ReadAsStringAsync();
//                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

//                if (data != null)
//                {
//                    if (user.EncryptDecrypt == "1")
//                    {
//                        using (Security aes = new())
//                        {
//                            result.out_err = aes.Decrypt(data.out_err!);
//                            result.out_value_str = aes.Decrypt(data.out_value_str!);
//                        }
//                    }
//                    else
//                    {
//                        result.out_err = data.out_err!;
//                        result.out_value_str = data.out_value_str!;
//                    }

//                    if (user.IsByte == "1")
//                    {
//                        if (!string.IsNullOrEmpty(result.out_value_str))
//                        {
//                            result.out_bytes = Encoding.UTF8.GetBytes(result.out_value_str);
//                        }
//                    }
//                    else
//                    {
//                        if (int.TryParse(result.out_value_str, out int tmpInt))
//                        {
//                            result.out_value_int = tmpInt;
//                        }

//                        if (long.TryParse(result.out_value_str, out long tmplong))
//                        {
//                            result.out_value_long = tmplong;
//                        }

//                        if (double.TryParse(result.out_value_str, out double tmpDbl))
//                        {
//                            result.out_value_dbl = tmpDbl;
//                        }

//                        if (bool.TryParse(result.out_value_str, out bool tmpBool))
//                        {
//                            result.out_value_bool = tmpBool;
//                        }
//                        if (result.out_value_str == "1")
//                        {
//                            result.out_value_bool = true;
//                        }
//                    }
//                }
//                else
//                {
//                    result.out_err = "Error: Deserialized data is null";
//                }
//            }
//            catch (HttpRequestException ex)
//            {
//                result.out_err = $"HTTP request failed: {ex.Message}";
//            }
//            catch (JsonException ex)
//            {
//                result.out_err = $"JSON deserialization failed: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                result.out_err = $"An unexpected error occurred: {ex.Message}";
//            }

//            return result;
//        }

//        /// <summary>
//        /// Sendet Daten zum WebApi Server (z.B. um Daten speichern)
//        /// </summary>
//        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
//        public async Task<ScalarModel> PostData(UserWebApi user)
//        {
//            var result = new ScalarModel();

//            try
//            {
//                user.DisplayError = "1";
//                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_SetData);
//                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
//                //request.Content = JsonContent.Create(user);
//                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

//                using var response = await _httpClient.SendAsync(request);

//                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//                {
//                    result.out_err = "Error 401: Unauthorized request.";
//                    return result;
//                }

//                response.EnsureSuccessStatusCode();

//                //var content = await response.Content.ReadAsStringAsync();
//                //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
//                //{
//                //    PropertyNameCaseInsensitive = true
//                //});
//                var content = await response.Content.ReadAsStringAsync();
//                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

//                if (data != null)
//                {
//                    if (user.EncryptDecrypt == "1")
//                    {
//                        using (Security aes = new())
//                        {
//                            result.out_err = aes.Decrypt(data.out_err!);
//                            result.out_value_str = aes.Decrypt(data.out_value_str!);
//                        }
//                    }
//                    else
//                    {
//                        result.out_err = data.out_err!;
//                        result.out_value_str = data.out_value_str!;
//                    }

//                    if (int.TryParse(result.out_value_str, out int tmpInt))
//                    {
//                        result.out_value_int = tmpInt;
//                    }

//                    if (long.TryParse(result.out_value_str, out long tmplong))
//                    {
//                        result.out_value_long = tmplong;
//                    }

//                    if (double.TryParse(result.out_value_str, out double tmpDbl))
//                    {
//                        result.out_value_dbl = tmpDbl;
//                    }

//                    if (bool.TryParse(result.out_value_str, out bool tmpBool))
//                    {
//                        result.out_value_bool = tmpBool;
//                    }
//                    if (result.out_value_str == "1")
//                    {
//                        result.out_value_bool = true;
//                    }
//                }
//                else
//                {
//                    result.out_err = "Error: Deserialized data is null";
//                }
//            }
//            catch (HttpRequestException ex)
//            {
//                result.out_err = $"HTTP request failed: {ex.Message}";
//            }
//            catch (JsonException ex)
//            {
//                result.out_err = $"JSON deserialization failed: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                result.out_err = $"An unexpected error occurred: {ex.Message}";
//            }

//            return result;
//        }

//        /// <summary>
//        /// Anonyme Abfrage an WebApi Server (z.B. um Daten zu erhalten, ohne dass der Client angemeldet ist)
//        /// </summary>
//        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ScalarModel</returns>
//        public async Task<ScalarModel> AnonymousQuery(UserWebApi user)
//        {
//            var result = new ScalarModel();

//            try
//            {
//                //var response = await _httpClient.PostAsJsonAsync(_globalState!.ConfigWebapi.url_Anonymous, user);
//                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
//                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_Anonymous, requestContent);

//                if (response.IsSuccessStatusCode)
//                {
//                    response.EnsureSuccessStatusCode();

//                    //var content = await response.Content.ReadAsStringAsync();
//                    //var data = JsonSerializer.Deserialize<ScalarModel>(content, new JsonSerializerOptions
//                    //{
//                    //    PropertyNameCaseInsensitive = true
//                    //});
//                    var content = await response.Content.ReadAsStringAsync();
//                    var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

//                    if (data != null)
//                    {
//                        if (user.EncryptDecrypt == "1")
//                        {
//                            using (Security aes = new())
//                            {
//                                result.out_err = string.IsNullOrEmpty(data.out_err) ? "" : aes.Decrypt(data.out_err!);
//                                result.out_value_str = string.IsNullOrEmpty(data.out_value_str) ? "" : aes.Decrypt(data.out_value_str!);
//                            }
//                        }
//                        else
//                        {
//                            result.out_err = data.out_err!;
//                            result.out_value_str = data.out_value_str!;
//                        }

//                        if (int.TryParse(result.out_value_str, out int tmpInt))
//                        {
//                            result.out_value_int = tmpInt;
//                        }

//                        if (long.TryParse(result.out_value_str, out long tmplong))
//                        {
//                            result.out_value_long = tmplong;
//                        }

//                        if (double.TryParse(result.out_value_str, out double tmpDbl))
//                        {
//                            result.out_value_dbl = tmpDbl;
//                        }

//                        if (bool.TryParse(result.out_value_str, out bool tmpBool))
//                        {
//                            result.out_value_bool = tmpBool;
//                        }
//                        if (result.out_value_str == "1")
//                        {
//                            result.out_value_bool = true;
//                        }
//                    }
//                    else
//                    {
//                        result.out_err = "Error: Deserialized data is null";
//                    }
//                }
//            }
//            catch (HttpRequestException ex)
//            {
//                result.out_err = $"HTTP request failed: {ex.Message}";
//            }
//            catch (JsonException ex)
//            {
//                result.out_err = $"JSON deserialization failed: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                result.out_err = $"An unexpected error occurred: {ex.Message}";
//            }

//            return result;
//        }

//        /// <summary>
//        /// Liefert eine Liste (Tabelle) vom WebApi Server (z.B. SELECT * aus DB)
//        /// </summary>
//        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
//        /// <returns>Rückgabewert ist ein Objekt vom Typ ReaderModel</returns>
//        public async Task<ReaderModel<T>> GetRows<T>(UserWebApi user)
//        {
//            var result = new ReaderModel<T>();

//            try
//            {
//                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_GetRows);
//                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
//                //request.Content = JsonContent.Create(user);
//                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

//                using var response = await _httpClient.SendAsync(request);

//                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//                {
//                    result.out_err = "Error 401: Unauthorized request.";
//                    return result;
//                }

//                response.EnsureSuccessStatusCode();

//                var content = await response.Content.ReadAsStringAsync();
//                //var readerDynamicModel = JsonSerializer.Deserialize<ReaderDynamicModel>(content, new JsonSerializerOptions
//                //{
//                //    PropertyNameCaseInsensitive = true
//                //});
//                var readerDynamicModel = JsonSerializer.Deserialize(content, JsonContext.Default.ReaderDynamicModel);

//                if (readerDynamicModel == null)
//                {
//                    throw new InvalidOperationException("Deserialized response is null");
//                }

//                if (user.EncryptDecrypt == "1")
//                {
//                    using (Security aes = new())
//                    {
//                        result.out_err = aes.Decrypt(readerDynamicModel.out_err!);
//                        result.out_json = aes.Decrypt(readerDynamicModel.out_json!);
//                    }
//                }
//                else
//                {
//                    result.out_err = readerDynamicModel.out_err!;
//                    result.out_json = readerDynamicModel.out_json!;
//                }

//                //result.out_list = JsonSerializer.Deserialize<List<T?>>(result.out_json, new JsonSerializerOptions
//                //{
//                //    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
//                //});
//                result.out_list = JsonUtility.DeserializeListSafe<T>(result.out_json);
//            }
//            catch (HttpRequestException ex)
//            {
//                result.out_err = $"HTTP request failed: {ex.Message}";
//            }
//            catch (JsonException ex)
//            {
//                result.out_err = $"JSON deserialization failed: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                result.out_err = $"An unexpected error occurred: {ex.Message}";
//            }

//            return result;
//        }


//    }
//}
//#pragma warning restore CA1416
