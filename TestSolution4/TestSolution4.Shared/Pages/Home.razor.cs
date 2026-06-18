using Microsoft.AspNetCore.Components;
using p11.UI;
//using BlazorCore.DbApp.Models;
using BlazorCore.Models;

namespace TestSolution4.Shared.Pages
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
        private BlazorCore.Services.AppState.IAppStateBase _appState { get; set; } = default!;

        [Inject]
        private BlazorCore.Services.Platform.IPlatformBase _platform { get; set; } = default!;

        [Inject]
        private p11.UI.Services.IMessageBoxService _messageBoxService { get; set; } = default!;

        [Inject]
        private p11.UI.Services.IEventStateService? _eventState { get; set; } = default!;


        private HomeModel _home = new();

        private Func<Task>? _saveSettings;
        private Func<Task>? _saveExportImport;


        protected override void OnInitialized()
        {
            _appState.UpdateIsRootPageLoaded(true);

            _appState.OnOpenAboutHome += OnClick_OpenAbout;
            _appState.OnToggleSettingsHome += OnToggleSettings;
            _appState.OnLanguageHasChanged += HandleStateChange;
            _appState.OnHideModalNativeHeaderFooter += HandleHideModalNativeHeaderFooter;
        }
        public void Dispose()
        {
            _appState.Log("[BLAZOR Home.razor.cs Dispose] START");

            _appState.UpdateIsRootPageLoaded(false);

            _appState.OnOpenAboutHome -= OnClick_OpenAbout;
            _appState.OnToggleSettingsHome -= OnToggleSettings;
            _appState.OnLanguageHasChanged -= HandleStateChange;
            _appState.OnHideModalNativeHeaderFooter -= HandleHideModalNativeHeaderFooter;

            _appState.Log("[BLAZOR Home.razor.cs Dispose] START");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                     // Current windows width
                    _home.CurrentWindowWidth = await _platform.GetWindowWidth();

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
        }



        #region OnEvents
        protected void HandleStateChange()
        {
            // InvokeAsync erzwingt den Wechsel auf den UI-Thread,
            // bevor StateHasChanged ausgeführt wird.
            InvokeAsync(StateHasChanged);

            _appState!.Log($"[Blazor Home.razor] HandleStateChange, _appState.T(Add a new todo)", data: _appState.T("Add a new todo"));
        }

        protected void HandleHideModalNativeHeaderFooter(bool isHide)
        {
            _home.IsHeaderHide = isHide;
            _home.IsFooterHide = isHide;
            StateHasChanged();
        }

        protected void OnToggleUserExpand()
        {
            if(!_home.IsOpenDropdownAccount)
                _appState.ParametersSetAuthenticationExtend();

            _home.IsOpenDropdownAccount = !_home.IsOpenDropdownAccount;
        }

        protected void OnToggleSettings()
        {
            _home.IsOpenModalSettings = !_home.IsOpenModalSettings;
            StateHasChanged();
        }

        protected void OnOpenModalSharing()
        {
            _home.IsOpenModalSharing = true;
            StateHasChanged();
        }

        protected void OnClick_OpenAbout()
        {
            _home.IsOpenDrawerMenu = false;
            _home.IsOpenModalAbout = true;
            StateHasChanged();
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
                    var resultDelete = await _platform!.DeleteOtpKey(_otpParameters);
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


        #region ExportImport
        protected async Task ExportData()
        {
            try
            {
                _home.IsOpenDrawerMenu = false;

                List<List<string>> dataContainer = new List<List<string>>();

                _home.ContainerExportimport.Clear();

                // BEISPIELE ///////////////////////////////////////////////////////////
                //_home.ContainerExportimport.AddRange(await GetDataContainer_Todo());
                //_home.ContainerExportimport.AddRange(await GetDataContainer_Tasks());
                // BEISPIELE ///////////////////////////////////////////////////////////

                // Sharing ist nur auf dem Cloud verfügbar
                if (_appState.IsInternetConnected)
                    _home.ContainerExportimport.AddRange(await GetDataContainer_SharingUsers());

                _home.ContainerExportimport.AddRange(await GetDataContainer_Parameter());
                _home.ContainerExportimport.AddRange(await GetDataContainer_Userdata());

                _home.IsOpenModalExportImport = true;
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
        }

        // BEISPIELE ///////////////////////////////////////////////////////////
        protected async Task OnImportData(string data)
        {
            //try
            //{
            //    var import = new List<ImportItemModel>();
            //    TodoModel todo = new();
            //    import = ParseImportData(data);

            //    foreach (var item in import)
            //    {
            //        // Todo speichern
            //        todo.ID = 0;
            //        todo.UnixTS = _appState.GenerateUniqueId();
            //        await Task.Delay(5);
            //        string UnixTS2 = _appState.GenerateUniqueId();
            //        todo.DisplayName = item.Todo;
            //        todo.IsChecked = false;
            //        todo.Tasks = string.Join(", ", item.Tasks); ;
            //        todo.IsNotifyActivated = false;
            //        todo.RecordDateTimeUnix = 0;
            //        todo.AuthUsers_UnixTS = _appState.UnixTS;

            //        BlazorCore.Services.SqlClient.ScalarModel result = await _todoService!.Save(todo, "", _appState.StorageLocation);
            //        if (result != null && string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
            //        {
            //            string[] tasks = todo.Tasks.Split(',', StringSplitOptions.RemoveEmptyEntries); // Tasks spliten
            //            foreach (var task in tasks)  // Tasks durchlaufen und erstellen
            //            {
            //                // Objekt setzen
            //                TasksModel taskItem = new();
            //                taskItem.UnixTS = string.IsNullOrEmpty(taskItem.UnixTS) ? _appState.GenerateUniqueId() : taskItem.UnixTS;
            //                taskItem.DisplayName = task.Trim();
            //                taskItem.Todo_UnixTS = todo.UnixTS;

            //                result = await _tasksService!.Save(taskItem, _appState.StorageLocation)!; // Abfrage ausführen
            //                if (result != null && !string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
            //                {
            //                    string err = result != null && result.out_err != null ? result.out_err : "_tasksService!.Save";
            //                    await _messageBoxService.ShowOkAsync(
            //                        title: _appState!.T("Error"),
            //                        message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {err}"
            //                    );
            //                }
            //            }
            //        }
            //        else
            //        {
            //            string err = result != null && result.out_err != null ? result.out_err : "_todoService!.Save";
            //            await _messageBoxService.ShowOkAsync(
            //                title: _appState!.T("Error"),
            //                message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {err}"
            //            );
            //        }
            //    }
            //    await _appState.RefreshTodoUnixTSAsync("");
            //    _home.IsOpenModalExportImport = false;
            //}
            //catch (Exception ex)
            //{
            //    await _messageBoxService.ShowOkAsync(
            //        title: _appState!.T("Error"),
            //        message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
            //    );
            //}
        }

        private List<ImportItemModel> ParseImportData(string importData)
        {
            var result = new List<ImportItemModel>();

            try
            {
                if (string.IsNullOrWhiteSpace(importData))
                    return result;

                // Zeilenweise splitten (unterstützt Windows und Unix Zeilenumbrüche)
                var lines = importData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;

                    // Prüfen, ob ein Doppelpunkt existiert
                    int colonIndex = trimmedLine.IndexOf(':');

                    string todo;
                    List<string> tasks = new();

                    if (colonIndex > 0)
                    {
                        // Format: Kategorie: Task1, Task2, ...
                        todo = trimmedLine[..colonIndex].Trim();
                        string tasksPart = trimmedLine[(colonIndex + 1)..].Trim();

                        if (!string.IsNullOrEmpty(tasksPart))
                        {
                            tasks = tasksPart
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .ToList();
                        }
                    }
                    else
                    {
                        // Kein Doppelpunkt -> nur Kategorie
                        todo = trimmedLine;
                    }

                    if (!string.IsNullOrEmpty(todo))
                    {
                        result.Add(new ImportItemModel
                        {
                            Todo = todo,
                            Tasks = tasks
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }

            return result;
        }

        // BEISPIEL ///////////////////////////////////////////////////////////
        //private async Task<List<List<string>>> GetDataContainer_Todo()
        //{
        //    List<List<string>> dataContainer = new List<List<string>>();

        //    try
        //    {
        //        Dictionary<string, string> db_para = new()
        //        {
        //            { "@Case_", "Select>>Todo" },
        //            { "@AuthUsers_UnixTS", _appState.UnixTS },
        //        };

        //        BlazorCore.Services.SqlClient.ReadModel<TodoModel?> result = await _dam.ReadCompare<TodoModel>(db_para)!; // Abfrage ausführen

        //        if (result != null && result.out_list != null) // Resultat prüfen
        //        {
        //            // 1. ADD TABLE-NAME ROW 
        //            var TablenameRow = new List<string>
        //            {
        //                "[TABLE-NAME]:Todo",
        //            };
        //            dataContainer.Add(TablenameRow);

        //            // 2. ADD HEADER ROW (column titles)
        //            var headerRow = new List<string>
        //            {
        //                "Name",                 // DisplayName
        //                "Is shared",            // Int__IsSharingFrom
        //                "Single completed",     // IsChecked
        //                "Notification Active",  // IsNotifyActivated
        //                "Notification Date",    // RecordDateTime
        //                "Category Color"        // CategoryColor
        //            };
        //            dataContainer.Add(headerRow);


        //            // 3. ADD DATA ROWS
        //            foreach (var item in result.out_list)
        //            {
        //                var dataRow = new List<string>
        //                {
        //                    // DisplayName
        //                    item!.DisplayName,
            
        //                    // IsChecked (Formatted as readable string)
        //                    item.Int__IsSharingFrom ? "Yes" : "",

        //                    // IsChecked (Formatted as readable string)
        //                    item.IsChecked ? "Yes" : "",
            
        //                    // IsNotifyActivated (Formatted as readable string)
        //                    item.IsNotifyActivated ? "Yes" : "No",
            
        //                    // RecordDateTime (Date/Timestamp formatting)
        //                    // Uses an empty string if the value is null.
        //                    item.RecordDateTimeUI.HasValue
        //                        ? item.RecordDateTimeUI.Value.ToString("yyyy-MM-dd HH:mm:ss")
        //                        : string.Empty,
            
        //                    // CategoryColor
        //                    item.CategoryColor
        //                };

        //                dataContainer.Add(dataRow);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _messageBoxService.ShowOkAsync(
        //            title: _appState!.T("Error"),
        //            message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
        //        );
        //    }

        //    return dataContainer;
        //}
        // BEISPIEL ///////////////////////////////////////////////////////////

        // BEISPIEL ///////////////////////////////////////////////////////////
        //private async Task<List<List<string>>> GetDataContainer_Tasks()
        //{
        //    List<List<string>> dataContainer = new List<List<string>>();

        //    try
        //    {
        //        Dictionary<string, string> db_para = new()
        //        {
        //            { "@Case_", "SelectContainerByAuthUsers_UnixTS>>Tasks" },
        //            { "@AuthUsers_UnixTS", _appState.UnixTS },
        //        };

        //        BlazorCore.Services.SqlClient.ReadModel<TasksModel?> result = await _dam.ReadCompare<TasksModel>(db_para)!; // Abfrage ausführen

        //        if (result != null && result.out_list != null) // Resultat prüfen
        //        {
        //            // 1. ADD TABLE-NAME ROW 
        //            var TablenameRow = new List<string>
        //            {
        //                "[TABLE-NAME]:Tasks",
        //            };
        //            dataContainer.Add(TablenameRow);

        //            // 2. ADD HEADER ROW (column titles)
        //            var headerRow = new List<string>
        //            {
        //                "Name",             // DisplayName
        //                "Is completed",     // IsChecked
        //                "Img thumbnail",    // imgJpegThumbnail
        //            };
        //            dataContainer.Add(headerRow);


        //            // 3. ADD DATA ROWS
        //            foreach (var item in result.out_list)
        //            {
        //                var dataRow = new List<string>
        //                {
        //                    // DisplayName
        //                    item!.DisplayName,

        //                    // IsChecked (Formatted as readable string)
        //                    item.IsChecked ? "Yes" : "No",
            
        //                    // Image
        //                    item.imgJpegThumbnail
        //                };

        //                dataContainer.Add(dataRow);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _messageBoxService.ShowOkAsync(
        //            title: _appState!.T("Error"),
        //            message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
        //        );
        //    }

        //    return dataContainer;
        //}
        // BEISPIEL ///////////////////////////////////////////////////////////

        private async Task<List<List<string>>> GetDataContainer_SharingUsers()
        {
            List<List<string>> dataContainer = new List<List<string>>();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "Select>>SharingUsers" },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                    { BlazorCore.Services.Dam.DB_CMD.NO_LOCAL, string.Empty } // Kein Sharing wenn kein Internet
                };

                BlazorCore.Services.SqlClient.ReadModel<SharingUsersModel?> result = await _dam.ReadData<SharingUsersModel>(db_para)!; // Abfrage ausführen
                
                if (result != null && result.out_list != null) // Resultat prüfen
                {
                    // 1. ADD TABLE-NAME ROW 
                    var TablenameRow = new List<string>
                    {
                        "[TABLE-NAME]:Sharingusers",
                    };
                    dataContainer.Add(TablenameRow);

                    // 2. ADD HEADER ROW (column titles)
                    var headerRow = new List<string>
                    {
                        "Alias",            // Int__Alias
                        "Status",           // SharingStatus
                    };
                    dataContainer.Add(headerRow);


                    // 3. ADD DATA ROWS
                    foreach (var item in result.out_list)
                    {
                        var dataRow = new List<string>
                        {
                            // DisplayName
                            item!.Int__Alias,

                            item.SharingStatus.ToString()
                        };

                        dataContainer.Add(dataRow);
                    }
                }
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }

            return dataContainer;
        }

        private async Task<List<List<string>>> GetDataContainer_Parameter()
        {
            List<List<string>> dataContainer = new List<List<string>>();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "Select>>AppParameter" },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                    { "@Scope", "set" },
                    { "cmd_nocomparetion", "no_comparetion_cloudlocal_data" }
                };

                BlazorCore.Services.SqlClient.ReadModel<AppParameterModel?> result = await _dam.ReadData<AppParameterModel>(db_para)!; // Abfrage ausführen

                if (result != null && result.out_list != null) // Resultat prüfen
                {
                    // 1. ADD TABLE-NAME ROW 
                    var TablenameRow = new List<string>
                    {
                        "[TABLE-NAME]:Userparameter",
                    };
                    dataContainer.Add(TablenameRow);

                    // 2. ADD HEADER ROW (column titles)
                    var headerRow = new List<string>
                    {
                        "Parametername",            // ParameterName
                        "Parametervalue",           // ParameterValue
                    };
                    dataContainer.Add(headerRow);


                    // 3. ADD DATA ROWS
                    foreach (var item in result.out_list)
                    {
                        var dataRow = new List<string>
                        {
                            item!.ParameterName,

                            item.ParameterValue
                        };

                        dataContainer.Add(dataRow);
                    }
                }
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }

            return dataContainer;
        }

        private async Task<List<List<string>>> GetDataContainer_Userdata()
        {
            List<List<string>> dataContainer = new List<List<string>>();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SelectAuthUsersData>>AuthUsersExtend" },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                    { "cmd_nocomparetion", "no_comparetion_cloudlocal_data" }
                };

                BlazorCore.Services.SqlClient.ReadModel<AuthUsersAuthUsersExtendModel?> result = await _dam.ReadData<AuthUsersAuthUsersExtendModel>(db_para)!; // Abfrage ausführen

                if (result != null && result.out_list != null) // Resultat prüfen
                {
                    // 1. ADD TABLE-NAME ROW 
                    var TablenameRow = new List<string>
                    {
                        "[TABLE-NAME]:Userdata",
                    };
                    dataContainer.Add(TablenameRow);

                    // 2. ADD HEADER ROW (column titles)
                    var headerRow = new List<string>
                    {
                        "Account",              // EmailHash
                        "Password",             // PasswordHash
                        "Is active",            // active
                        "Is terms accepted",    // TermsAccepted
                        "Account type",         // IdP
                        "Alias",                // DisplayName
                        "Img thumbnail",        // imgJpegThumbnail
                    };
                    dataContainer.Add(headerRow);


                    // 3. ADD DATA ROWS
                    foreach (var item in result.out_list)
                    {
                        var dataRow = new List<string>
                        {
                            item!.EmailHash,
                            item.PasswordHash,
                            item.active ? "Yes" : "No",
                            item.TermsAccepted ? "Yes" : "No",
                            item.IdP,
                            item.DisplayName,
                            item.imgJpegThumbnail
                        };

                        dataContainer.Add(dataRow);
                    }
                }
            }
            catch (Exception ex)
            {
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!" : "We’re sorry, but an error occurred!")} {BlazorCore.ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }

            return dataContainer;
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

        // Allgemein
        public bool IsHeaderHide { get; set; } = false;
        public bool IsFooterHide { get; set; } = false;
    }

    public class ImportItemModel
    {
        public string Todo { get; set; } = string.Empty;
        public List<string> Tasks { get; set; } = new();
    }


}
