//using BlazorCore.Services.Dam;
//using BlazorCore.Services.SqlClient;

//namespace BlazorCore.Services.Realm
//{
//    public interface IRealmBase
//    {
//        bool IsInitialized { get; set; }

//        Task EnsureInitializedAsync(string appDataDirectory);
//        //Task InitializeAsync();

//        // Hauptmethoden
//        Task<ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> dbParams);
//        //Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams);
//        Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : class, new();
//        Task<ScalarModel> Save(Dictionary<string, string> dbParams);
//        Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams);
//        Task<ScalarModel> Scalar(Dictionary<string, string> dbParams);

//        //// Account Management
//        //Task<ClientStorageModel> CreateLocalAccount(Dictionary<string, string> dbParams);

//        //// Utility Methods
//        Task<string> DeleteData();
//        Task<string> DeleteDB();
//    }
//}
