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
        /// Retrieves an app-specific token from the WebAPI server for TPC (Third Party / internal) authentication.
        /// Used for classic registration and login with email + password (with optional 2FA support).
        /// </summary>
        /// <param name="user">Model object containing JsonPara with EmailHash, PasswordHash, and optional registration/2FA flags</param>
        /// <returns>Returns a ClientStorageModel with WebApiToken and UnixTS</returns>
        public async Task<ClientStorageModel> GetTokenDataTPC(UserWebApi user)
        {
            var tokenData = new ClientStorageModel();

            try
            {
                // Set 'user.DisplayError = API_CONST.TRUE_VALUE;' if error messages should be returned.
                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_GetTokenDataTPCuser, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var authContent = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize(authContent, JsonContext.Default.DictionaryStringObject);

                    if (res != null)
                    {
                        string msg = "";
                        string token = "";
                        string payload = "";

                        if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
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
        /// Retrieves an app-specific token from the WebAPI server after external IDP authentication.
        /// Used for "Login with Google/Microsoft/Apple" flows.
        /// </summary>
        /// <param name="user">Model object containing JsonPara with @IdPClientIdent (external IDP identifier)</param>
        /// <returns>Returns a ClientStorageModel with WebApiToken and UnixTS</returns>
        public async Task<ClientStorageModel> GetTokenDataIDP(UserWebApi user)
        {
            var tokenData = new ClientStorageModel();

            try
            {
                // Set 'user.DisplayError = API_CONST.TRUE_VALUE;' if error messages should be returned.
                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_GetTokenDataIDPuser, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var authContent = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize(authContent, JsonContext.Default.DictionaryStringObject);

                    if (res != null)
                    {
                        string msg = "";
                        string token = "";
                        string payload = "";

                        if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
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
        /// Changes the user's password.
        /// </summary>
        /// <param name="user">Model object containing token and JsonPara with old/new password hashes</param>
        /// <returns>Returns a ScalarModel object with the result or error message</returns>
        public async Task<ScalarModel> ChangePassword(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                // Set 'user.DisplayError = API_CONST.TRUE_VALUE;' if error messages should be returned.
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_ChangePassword);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                if (data != null)
                {
                    if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
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
        /// Retrieves a scalar value (e.g., COUNT(*) from database) from the WebAPI server.
        /// </summary>
        /// <param name="user">Model object containing all client information (token, parameters, flags)</param>
        /// <returns>Returns a ScalarModel object with the result or error message</returns>
        public async Task<ScalarModel> GetScalar(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                // Set 'user.DisplayError = API_CONST.TRUE_VALUE;' if error messages should be returned.
                user.DisplayError = "1";
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_GetScalar);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                if (data != null)
                {
                    if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
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

                    if (user.IsByte == API_CONST.TRUE_VALUE)
                    {
                        if (!string.IsNullOrEmpty(result.out_value_str))
                        {
                            result.out_bytes = Encoding.UTF8.GetBytes(result.out_value_str);
                        }
                    }
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
        /// Sends data to the WebAPI server (e.g., for saving/updating data in the database).
        /// </summary>
        /// <param name="user">Model object containing all client information (token, parameters, flags)</param>
        /// <returns>Returns a ScalarModel object with the result or error message</returns>
        public async Task<ScalarModel> PostData(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                // Set 'user.DisplayError = API_CONST.TRUE_VALUE;' if error messages should be returned.
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_SetData);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                if (data != null)
                {
                    if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
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
        /// Performs an anonymous query to the WebAPI server (e.g., to retrieve data without authentication).
        /// Used for public endpoints like login, feedback forms, or OTP handling.
        /// </summary>
        /// <param name="user">Model object containing all client information (parameters, flags)</param>
        /// <returns>Returns a ScalarModel object with the result or error message</returns>
        public async Task<ScalarModel> AnonymousQuery(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                var requestContent = JsonContent.Create(user, JsonContext.Default.UserWebApi);
                var response = await _httpClient.PostAsync(_globalState!.ConfigWebapi.url_Anonymous, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                    if (data != null)
                    {
                        if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
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

        /// <summary>
        /// Executes an AI operation (Chat, Embedding, etc.) on the WebAPI server.
        /// This is an authenticated endpoint (requires valid token).
        /// </summary>
        /// <param name="user">User context containing AI parameters (JsonPara with @Case_, prompts, etc.)</param>
        /// <returns>
        /// ScalarModel containing:
        /// - out_err: Error message if operation failed (empty on success)
        /// - out_value_str: AI response text (for chat completion)
        /// - Optional: out_value_int/out_value_long for token usage metrics
        /// </returns>
        /// <remarks>
        /// This method follows the same pattern as GetScalar and PostData:
        /// - Bearer token authentication
        /// - Windows Client: AES encryption for sensitive prompts
        /// - Other platforms: HTTPS only
        /// - No retry logic (handled by DAM if needed)
        /// 
        /// Supported @Case_ values (inside JsonPara):
        /// - DB_CMD.AI_COMPLETE_CHAT: Chat completion with Azure OpenAI
        /// </remarks>
        public async Task<ScalarModel> Ai(UserWebApi user)
        {
            var result = new ScalarModel();

            try
            {
                // Set 'user.DisplayError = API_CONST.TRUE_VALUE;' if error messages should be returned.
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_Ai);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize(content, JsonContext.Default.ScalarModel);

                if (data != null)
                {
                    if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
                    {
                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                        {
                            result.out_err = string.IsNullOrEmpty(data.out_err) ? "" : aes.Decrypt(data.out_err!);
                            result.out_value_str = string.IsNullOrEmpty(data.out_value_str) ? "" : aes.Decrypt(data.out_value_str!);
                        }

                        result.out_value_bool = data.out_value_bool;
                        result.out_value_int = data.out_value_int;
                        result.out_value_dbl = data.out_value_dbl;
                        result.out_value_long = data.out_value_long;
                    }
                    else
                    {
                        result.out_err = data.out_err ?? string.Empty;
                        result.out_value_str = data.out_value_str ?? string.Empty;

                        result.out_value_bool = data.out_value_bool;
                        result.out_value_int = data.out_value_int;
                        result.out_value_dbl = data.out_value_dbl;
                        result.out_value_long = data.out_value_long;
                    }
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
        /// Retrieves a list (table) of data from the WebAPI server.
        /// AOT/Trimming safe through explicit JsonTypeInfo parameter.
        /// </summary>
        /// <param name="user">Model object containing all client information (token, parameters, flags)</param>
        /// <param name="listTypeInfo">AOT-safe JsonTypeInfo for List<T?> from the JsonContext</param>
        /// <returns>Returns a ReaderModel object containing the deserialized list</returns>
        public async Task<ReaderModel<T>> GetRows<T>(UserWebApi user, JsonTypeInfo<List<T?>> listTypeInfo)
        {
            var result = new ReaderModel<T>();

            try
            {
                // Set 'user.DisplayError = API_CONST.TRUE_VALUE;' if error messages should be returned.
                using var request = new HttpRequestMessage(HttpMethod.Post, _globalState!.ConfigWebapi.url_GetRows);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);
                request.Content = JsonContent.Create(user, JsonContext.Default.UserWebApi);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result.out_err = "Error 401: Unauthorized request.";
                    return result;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                // AOT-safe deserialization of the dynamic response container
                var readerDynamicModel = JsonSerializer.Deserialize(content, JsonContext.Default.ReaderDynamicModel);

                if (readerDynamicModel == null)
                {
                    throw new InvalidOperationException("Deserialized response is null");
                }

                if (user.EncryptDecrypt == API_CONST.TRUE_VALUE)
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

                // AOT-safe deserialization of the actual list using the provided JsonTypeInfo
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
