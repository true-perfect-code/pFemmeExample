using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Authentication
{
    public interface IAuthenticationBase
    {
        Task<ScalarModel?> Login(string token);
        Task Logout();
        Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync();
        //Microsoft.AspNetCore.Components.Authorization.AuthenticationState GetAuthenticationState();

        // Passwort in Blazor Server ändern (MAUI Methode ist hier leer)
        Task<SqlClient.ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user);
    }
}
