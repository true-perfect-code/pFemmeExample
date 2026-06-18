//using BlazorCore.DbApp.Models;
using BlazorCore.Models;
using BlazorCore.Services.EventAggregator;
using Microsoft.AspNetCore.Components;
using p11.UI;
using pFemmeExample.Shared.Models;
using pFemmeExample.Shared.Services.EventAggregatorProject;
using pFemmeExample.Shared.Services.Platform;

namespace pFemmeExample.Shared.Pages
{
    partial class Home: IDisposable
    {
        [Parameter]
        public string? Para { get; set; }

        [Inject]
        private p11.UI.Services.IToastService? _toastService { get; set; }

        [Inject]
        private BlazorCore.Services.Dam.IDamBase _dam { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.GlobalState.IGlobalStateBase _globalState { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.EventAggregator.IEventAggregator _eventAggregator { get; set; } = default!;

        [Inject]
        private Services.EventAggregatorProject.IEventAggregatorProject _eventAggregatorProject { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.AppState.IAppStateBase _appState { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.Platform.IPlatformBase _platform { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.Otp.IOtpBase? _otp { get; set; }

        [Inject]
        private p11.UI.Services.IMessageBoxService _messageBoxService { get; set; } = default!;

        [Inject]
        private p11.UI.Services.IEventStateService? _eventState { get; set; } = default!;


        private HomeModel _home = new();

        private FuncModel _func = new();


        protected override void OnInitialized()
        {
            _appState.UpdateIsRootPageLoaded(true);

            _eventAggregator.OnLanguageHasChanged += HandleStateChange;
        }
        public void Dispose()
        {
            _appState.Log("[BLAZOR Home.razor.cs Dispose] START");

            _appState.UpdateIsRootPageLoaded(false);

            _eventAggregator.OnLanguageHasChanged -= HandleStateChange;

            _appState.Log("[BLAZOR Home.razor.cs Dispose] START");
        }

        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //    try
        //    {
        //        if (firstRender)
        //        {
        //            StateHasChanged();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _messageBoxService.ShowOkAsync(
        //            title: _appState!.T("Error"),
        //            message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
        //        );
        //    }
        //}




        #region Events
        protected void HandleStateChange()
        {
            // InvokeAsync erzwingt den Wechsel auf den UI-Thread,
            // bevor StateHasChanged ausgeführt wird.
            InvokeAsync(StateHasChanged);

            _appState!.Log($"[Blazor Home.razor] HandleStateChange, _appState.T(Add a new todo)", data: _appState.T("Add a new todo"));
        }

        protected void OnToggleUserExpand()
        {
            if(!_home.IsOpenDropdownAccount)
                _eventAggregator.ParametersSetAuthenticationExtend();

            _home.IsOpenDropdownAccount = !_home.IsOpenDropdownAccount;
        }
        #endregion


        #region Otp_2FA
        protected async Task OnClick_Open2FASetup()
        {
            try
            {
                _home.IsOpenModalAccount = false;
                StateHasChanged();

                // Benutzer fragen, ob er sicher 2FA Setup starten will
                MessageBoxResult result = await _messageBoxService.ShowYesNoCancelAsync(
                    title: _appState.T("2FA Setup"),
                    message: _appState.T("Have you completed all preparations for setting up 2FA (e.g., installed an authenticator app on your phone)?"),
                    yestext: _appState.T("Yes"),
                    notext: _appState.T("No"),
                    canceltext: _appState.T("More info")
                );

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        _appState.UpdateIs2FAActivated(true);
                        _home.IsOpenModal2FASetup = true;
                        break;

                    case MessageBoxResult.No:
                        _home.IsOpenModalAccount = true;
                        break;

                    case MessageBoxResult.Cancel:
                        await Task.Delay(700);
                        _home.IsOpenModalAccount = true;
                        _home.IsOpenHelp2FA = true;
                        break;
                }

            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
        }

        protected async Task OnClick_Open2FARevoke()
        {
            try
            {
                _home.IsOpenModalAccount = false;
                StateHasChanged();
                await Task.Delay(200);

                _home.IsOpenModal2FARevoke = true;
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
        }

        protected async Task OnClick_2FARevoke((string OtpUserDigitInput, string OtpBackupCode, string Account, string Password) result)
        {
            if (_otp == null) return;

            try
            {
                if(!string.IsNullOrEmpty(result.OtpUserDigitInput) || !string.IsNullOrEmpty(result.OtpBackupCode))
                {
                    BlazorCore.Services.Otp.OtpParametersModel _otpParameters = new();
                    _otpParameters.OtpUserDigitInput = result.OtpUserDigitInput;
                    _otpParameters.OtpBackupCode = result.OtpBackupCode;
                    _otpParameters.Account = result.Account;
                    _otpParameters.Password = result.Password;
                    _otpParameters.AuthUsers_UnixTS = _appState.UnixTS;
                    var resultDelete = await _otp.DeleteOtpKey(_otpParameters);
                    if (resultDelete != null && string.IsNullOrEmpty(resultDelete.out_err) && resultDelete.out_value_str != null)
                    {
                        if (resultDelete.out_value_str.Contains("updated:"))
                        {
                            await _toastService!.ShowSuccessAsync(_appState.T("Data has been successfully saved."), _appState.T("Success"), position: ToastPosition.BottomEnd, durationMs: 1000);

                            _appState.UpdateIs2FAActivated(false);
                            _home.IsOpenModal2FARevoke = false;
                            _home.IsOpenModalAccount = true;
                            StateHasChanged();
                            await Task.Delay(200);
                        }
                        else
                        {
                            _home.IsOpenModal2FARevoke = false;

                            await _messageBoxService.ShowOkAsync(
                                title: _appState.T("Warning"),
                                message: _appState.T("2FA could not be disabled. If you have any problems, please contact support.")
                            );
                        }
                    }
                }
                else
                {
                    _home.IsOpenModal2FARevoke = false;

                    await _messageBoxService.ShowOkAsync(
                        title: _appState.T("Warning"),
                        message: _appState.T("2FA cannot be disabled because neither the backup code nor the 6-digit OTP code has been entered.")
                    );
                }
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
        }
        #endregion
    }


    public class HomeModel
    {
        // Spezifisch
        public List<List<string>> ContainerExportimport = new List<List<string>>();
        public Common.Authentication.AuthenticationLogout? LogoutComponent;
        public double CurrentWindowWidth = 0;
        //public string Avatar = "_content/pE/img/avatar.jpg";

        // Modals & Drawers
        public bool IsOpenModalAdmin { get; set; } = false;
        public bool IsOpenModalSettings { get; set; } = false;
        public bool IsOpenDrawerMenu { get; set; } = false;
        public bool IsOpenModalAccount { get; set; } = false;
        public bool IsOpenModalSharing { get; set; } = false;
        public bool IsOpenModal2FASetup { get; set; } = false;
        public bool IsOpenModal2FARevoke { get; set; } = false;
        public bool IsOpenModalExportImport { get; set; } = false;
        public bool IsOpenModalImprint { get; set; } = false;
        public bool IsOpenModalPrivacy { get; set; } = false;
        public bool IsOpenModalTerms { get; set; } = false;
        public bool IsOpenModalAbout { get; set; } = false;
        public bool IsOpenModalDonate { get; set; } = false;
        public bool IsOpenModalCyclesCal { get; set; } = false;
        public bool IsOpenModalCycle { get; set; } = false;
        public bool IsOpenModalAskAI { get; set; } = false;
        public bool IsOpenModalCyclesInfo { get; set; } = false;
        public bool IsOpenModalSynchronization { get; set; } = false;
        public bool IsOpenModalCookies { get; set; } = false;

        // Help
        public bool IsOpenHelp { get; set; } = false;
        public bool IsOpenHelpSettings { get; set; } = false;
        public bool IsOpenHelpAccount { get; set; } = false;
        public bool IsOpenHelpSharing { get; set; } = false;
        public bool IsOpenHelpExportImport { get; set; } = false;
        public bool IsOpenHelp2FA { get; set; } = false;

        // Dropdown
        public bool IsOpenDropdownAccount { get; set; } = false;

        // IDs
        public Guid IdDrawerMenu { get; set; } = Guid.NewGuid();
        public Guid IdSettings { get; set; } = Guid.NewGuid();
        public Guid IdAccount { get; set; } = Guid.NewGuid();
        public Guid IdSharing { get; set; } = Guid.NewGuid();
        public Guid IdExportImport { get; set; } = Guid.NewGuid();
        public Guid Id2FASetup { get; set; } = Guid.NewGuid();
        public Guid Id2FARevoke { get; set; } = Guid.NewGuid();
        public Guid IdAdmin { get; set; } = Guid.NewGuid();
        public Guid IdHelp { get; set; } = Guid.NewGuid();
        public Guid IdHelpSettings { get; set; } = Guid.NewGuid();
        public Guid IdHelpAccount { get; set; } = Guid.NewGuid();
        public Guid IdHelpSharing { get; set; } = Guid.NewGuid();
        public Guid IdHelpExportImport { get; set; } = Guid.NewGuid();
        public Guid IdHelp2FA { get; set; } = Guid.NewGuid();
        public Guid IdAbout { get; set; } = Guid.NewGuid();
        public Guid IdImprint { get; set; } = Guid.NewGuid();
        public Guid IdPrivacy { get; set; } = Guid.NewGuid();
        public Guid IdTerms { get; set; } = Guid.NewGuid();
        public Guid IdDonate { get; set; } = Guid.NewGuid();
        public Guid IdCyclesCal { get; set; } = Guid.NewGuid();
        public Guid IdCycle { get; set; } = Guid.NewGuid();
        public Guid IdAskAI { get; set; } = Guid.NewGuid();
        public Guid IdCyclesInfo { get; set; } = Guid.NewGuid();
        public Guid IdSynchronization { get; set; } = Guid.NewGuid();
        public Guid IdCookies { get; set; } = Guid.NewGuid();

        // Allgemein
        public bool IsHeaderHide { get; set; } = false;
        public bool IsFooterHide { get; set; } = false;

        // Data
        public Models.CyclesModel? SelectedCycle { get; set; }

    }

    public class ImportItemModel
    {
        public string Todo { get; set; } = string.Empty;
        public List<string> Tasks { get; set; } = new();
    }

    public class FuncModel
    {
        // Settings
        public Func<Task>? SaveSettings;

        // Export(Import
        public Func<Task>? SaveExportImport;

        // Cycle
        public Func<Task>? SaveCycle;
        public Func<Task>? CancelCycle;
    }


}
