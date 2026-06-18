using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using System.Text.Json.Serialization.Metadata;

namespace BlazorCore.Services.Apis
{
    public interface IApiBase
    {
        Task<ClientStorageModel> GetTokenDataTPC(UserWebApi user);
        Task<ClientStorageModel> GetTokenDataIDP(UserWebApi user);
        Task<ScalarModel> ChangePassword(UserWebApi user);
        //Task<ScalarModel> ManageOtp(MANAGE_OTP otpmana, UserWebApi user);
        Task<ScalarModel> GetScalar(UserWebApi user);
        Task<ScalarModel> PostData(UserWebApi user);
        Task<ScalarModel> AnonymousQuery(UserWebApi user);
        //Task<ReaderModel<T>> GetRows<T>(UserWebApi user);
        // AOT/Trimming friendly version
        //Task<ReaderModel<T>> GetRows<T>(UserWebApi user, JsonTypeInfo<List<T?>> listTypeInfo) where T : class;
        Task<ReaderModel<T>> GetRows<T>(UserWebApi user, JsonTypeInfo<List<T?>> listTypeInfo);
        Task<bool> CheckToken(UserWebApi user);
    }
}
