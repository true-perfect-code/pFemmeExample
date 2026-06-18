using BlazorCore.Services.Apis;
using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;

using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Base interface for thin API communication with fat database (MSSQL stored procedures).
/// All business logic resides in the database, the web API acts as a pure transporter.
/// </summary>
public interface IApiBase
{
    /// <summary>
    /// Retrieves token data from TPC (Third Party / internal system).
    /// </summary>
    /// <param name="user">User context containing authentication and case parameters</param>
    /// <returns>Client storage model with token information</returns>
    Task<ClientStorageModel> GetTokenDataTPC(UserWebApi user);

    /// <summary>
    /// Retrieves token data from IDP (Identity Provider, e.g., Google, Microsoft).
    /// </summary>
    /// <param name="user">User context containing authentication and case parameters</param>
    /// <returns>Client storage model with token information</returns>
    Task<ClientStorageModel> GetTokenDataIDP(UserWebApi user);

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    /// <param name="user">User context containing old and new password</param>
    /// <returns>Scalar model indicating success/failure</returns>
    Task<ScalarModel> ChangePassword(UserWebApi user);

    /// <summary>
    /// Retrieves a single scalar value from the database.
    /// </summary>
    /// <param name="user">User context containing query parameters</param>
    /// <returns>Scalar model with single value</returns>
    Task<ScalarModel> GetScalar(UserWebApi user);

    /// <summary>
    /// Sends data to the database (INSERT/UPDATE operations).
    /// </summary>
    /// <param name="user">User context containing data and case parameter</param>
    /// <returns>Scalar model with operation result</returns>
    Task<ScalarModel> PostData(UserWebApi user);

    /// <summary>
    /// Executes an anonymous query (no authentication required).
    /// Used for public data like landing page content.
    /// </summary>
    /// <param name="user">User context with minimal/anonymous information</param>
    /// <returns>Scalar model with query result</returns>
    Task<ScalarModel> AnonymousQuery(UserWebApi user);

    /// <summary>
    /// Executes an AI operation (Chat, Embedding, etc.) on the WebAPI server.
    /// This is an authenticated endpoint (requires valid token).
    /// </summary>
    /// <param name="user">User context containing AI parameters (JsonPara with @Case_, prompts, etc.)</param>
    /// <returns>ScalarModel with AI response in out_value_str</returns>
    Task<ScalarModel> Ai(UserWebApi user);

    /// <summary>
    /// Retrieves a list of rows of type T from the database.
    /// AOT/Trimming friendly with explicit JsonTypeInfo.
    /// </summary>
    /// <typeparam name="T">The type of objects to deserialize</typeparam>
    /// <param name="user">User context containing query and case parameters</param>
    /// <param name="listTypeInfo">JSON type info for AOT/trimming support</param>
    /// <returns>Reader model containing the list of objects</returns>
    Task<ReaderModel<T>> GetRows<T>(UserWebApi user, JsonTypeInfo<List<T?>> listTypeInfo);

    /// <summary>
    /// Validates if the current authentication token is still valid.
    /// </summary>
    /// <param name="user">User context with token to validate</param>
    /// <returns>True if token is valid, otherwise false</returns>
    Task<bool> CheckToken(UserWebApi user);
}