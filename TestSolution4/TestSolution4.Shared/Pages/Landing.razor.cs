using Microsoft.AspNetCore.Components;
using BlazorCore.Services.SqlClient;
using BlazorCore.Models;

namespace TestSolution4.Shared.Pages
{
    public partial class Landing
    {
        [Inject]
        private BlazorCore.Services.GlobalState.IGlobalStateBase? _globalState { get; set; }

        [Inject]
        private BlazorCore.Services.Platform.IPlatformBase _platform { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.Dam.IDamBase _dam { get; set; } = default!;

        [Inject]
        private p11.UI.Services.IMessageBoxService _messageBoxService { get; set; } = default!;

        private LandingModel _landing = new();


        public void Dispose()
        {
            _appState.OnRefreshLandingpage -= StateHasChanged;
            _appState.OnLanguageHasChanged -= HandleStateChange;
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _appState.OnRefreshLandingpage += StateHasChanged;
                _appState.OnLanguageHasChanged += HandleStateChange;

                // Store-Urls auslesen
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SelectStoreUrl>>AppParameter" },
                };

                ScalarModel result = await _dam.AnonymousQuery(db_para)!; // Abfrage ausführen
                if (result != null && !string.IsNullOrEmpty(result.out_value_str)) // Resultat prüfen
                {
                    List<AppParameterModel>? list = System.Text.Json.JsonSerializer.Deserialize(
                        result.out_value_str,
                        BlazorCore.JsonContext.Default.ListAppParameterModel);

                    if (list != null && _appState.Catalog.DownloadApp != null)
                    {
                        foreach (var item in list)
                        {
                            // Urls aktualisieren (für Cookie)
                            if (_appState.Catalog.DownloadApp.Any(x => x.Id == item!.ParameterName!))
                            {
                                _appState.Catalog.DownloadApp.Where(x => x.Id == item!.ParameterName!).FirstOrDefault()!.Url = item.ParameterValue!;
                                _appState.Catalog.DownloadApp.Where(x => x.Id == item!.ParameterName!).FirstOrDefault()!.Icon = item.Details!;
                            }
                        }
                    }
                }
                else
                {
                    // Prüfen ob Cookie vorhanden
                    if(_appState.Catalog.LocalStorage != null && _appState.Catalog.DownloadApp != null)
                    {
                        var resultPin = await _platform!.GetAsync(_appState.Catalog.LocalStorage.storeurls);
                        if (resultPin != null && string.IsNullOrEmpty(resultPin.out_err) && !string.IsNullOrEmpty(resultPin.out_value_str))
                        {
                            List<BlazorCore.Services.GlobalState.DownloadAppModel>? list = System.Text.Json.JsonSerializer.Deserialize(
                                resultPin.out_value_str,
                                BlazorCore.JsonContext.Default.ListDownloadAppModel);
                            if (list != null)
                            {
                                _appState.Catalog.DownloadApp.Clear();
                                foreach (var item in list)
                                {
                                    // Urls aktualisieren (für Cookie)
                                    _appState.Catalog.DownloadApp.Add(item);
                                }
                            }
                        }
                    }
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
        }

        protected void HandleStateChange()
        {
            // InvokeAsync erzwingt den Wechsel auf den UI-Thread,
            // bevor StateHasChanged ausgeführt wird.
            //_appState.Log("[Blazor Home.razor] HandleStateChange");
            InvokeAsync(StateHasChanged);

            _appState!.Log($"[Blazor Home.razor] HandleStateChange, _appState.T(Add a new todo)", data: _appState.T("Add a new todo"));
        }

    }

    public class LandingModel
    {
        // Modals
        public bool IsVisibleLogin { get; set; } = false;
        public bool IsOpenModalAbout { get; set; } = false;
        public bool IsOpenModalDonate { get; set; } = false;
        public bool IsOpenModalCookies { get; set; } = false;
        public bool IsOpenDropdownDownloadApp { get; set; } = false;

        public Guid IdAbout = Guid.NewGuid();
        public Guid IdDonate = Guid.NewGuid();
        public Guid IdCookies = Guid.NewGuid();
    }


}
