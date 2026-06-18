using BlazorCore.Models;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using pFemmeExample.Shared.Services.Platform;

namespace pFemmeExample.Shared.Pages
{
    public partial class Landing : IDisposable
    {
        [Parameter]
        public bool IsShowingSpinner { get; set; } = false;

        [Inject]
        private BlazorCore.Services.GlobalState.IGlobalStateBase? _globalState { get; set; }

        [Inject]
        private BlazorCore.Services.Platform.IPlatformBase _platform { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.Platform.IPlatformStorageBase? _platformStorage { get; set; }

        [Inject]
        private BlazorCore.Services.EventAggregator.IEventAggregator _eventAggregator { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.Dam.IDamBase _dam { get; set; } = default!;

        [Inject]
        private p11.UI.Services.IMessageBoxService _messageBoxService { get; set; } = default!;

        [Inject] 
        IJSRuntime? _js { get; set; }


        private LandingModel _landing = new();
        private Func<Task> _deleteCookies = null!;

        protected override async Task OnInitializedAsync()
        {
            if (IsShowingSpinner)
                return;

            try
            {
                _eventAggregator.OnRefreshLandingpage += HandleStateChange;
                _eventAggregator.OnLanguageHasChanged += HandleStateChange;

                // URLs
                bool checkCookie = true;
                if (_appState.StorageLocation != STORAGE_LOCATION.LOCAL)
                {
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
                        checkCookie = false;
                    }
                }
                if(checkCookie)
                {
                    // Prüfen ob Cookie vorhanden (lokalprüfung geht nicht, da User nicht bekannt ist)
                    if (_appState.Catalog.LocalStorage != null && _appState.Catalog.DownloadApp != null && _platformStorage != null)
                    {
                        var resultPin = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.storeurls);
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Control the display of the reconnect modal based on the IsShowingSpinner parameter
                pFemmeExample.Shared.Global.Configuration.ConfigGeneral.IsShowingReconnectModal = !IsShowingSpinner;

                if(_js != null && !IsShowingSpinner)
                    await _js.InvokeVoidAsync("tpc_showApp");

                StateHasChanged();
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

        public void Dispose()
        {
            if (IsShowingSpinner)
                return;

            _eventAggregator.OnRefreshLandingpage -= HandleStateChange;
            _eventAggregator.OnLanguageHasChanged -= HandleStateChange;
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
        public bool IsOpenModalAccessibility { get; set; } = false;

        public Guid IdAbout = Guid.NewGuid();
        public Guid IdDonate = Guid.NewGuid();
        public Guid IdCookies = Guid.NewGuid();
        public Guid IdAccessibility = Guid.NewGuid();
    }


}
