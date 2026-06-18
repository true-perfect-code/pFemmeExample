using BlazorCore.Services.EventAggregator;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Logging;

namespace BlazorCore.Services.Translation
{
    public class Translation : ITranslation
    {
        private readonly IGlobalStateBase _globalState;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogging _logging; // Falls du Logging schon integriert hast
        private Dictionary<string, string>? _langRef;

        public Translation(IGlobalStateBase globalState, IEventAggregator eventAggregator, ILogging logging)
        {
            _globalState = globalState;
            _eventAggregator = eventAggregator;
            _logging = logging;

            SelectedLanguage = _globalState.ConfigGeneral.DefaultLanguage;
        }

        public string SelectedLanguage { get; set; } = "EN";

        private Dictionary<string, string> LangRef
        {
            get
            {
                if (_langRef == null)
                {
                    _langRef = _globalState.Translations?.GetLanguageMap(SelectedLanguage)
                              ?? _globalState.Translations?.GetLanguageMap(_globalState.ConfigGeneral.DefaultLanguage)
                              ?? new Dictionary<string, string>();
                }
                return _langRef;
            }
        }

        public async Task LoadTranslations(bool routesStateHasChanged, string selectedLanguage = "")
        {
            await _logging.Log("[Blazor Translation] START LoadTranslations");

            try
            {
                await _logging.Log($"[Blazor Translation] routesStateHasChanged: {routesStateHasChanged} , SelectedLanguage: {SelectedLanguage} , selectedLanguage: {selectedLanguage}");

                if (SelectedLanguage != selectedLanguage && !string.IsNullOrEmpty(selectedLanguage))
                {
                    SelectedLanguage = selectedLanguage.ToUpper();
                    await _logging.Log($"[Blazor Translation] SelectedLanguage: {SelectedLanguage}");

                    var languageMap = _globalState.Translations?.GetLanguageMap(SelectedLanguage);

                    if (languageMap == null && SelectedLanguage != _globalState.ConfigGeneral.DefaultLanguage)
                    {
                        languageMap = _globalState.Translations?.GetLanguageMap(_globalState.ConfigGeneral.DefaultLanguage);
                    }

                    if (languageMap != null)
                    {
                        _langRef = languageMap;
                    }

                    if (routesStateHasChanged)
                    {
                        // Update loading status – du hast hier APP_LOADING_STATUS, ggf. anpassen
                        _eventAggregator.LanguageHasChanged();
                        await Task.Delay(30);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logging.Error($"[Blazor Translation] ERROR LoadTranslations: {ex.Message}");
                throw;
            }

            await _logging.Log("[Blazor Translation] END LoadTranslations");
        }

        public string T(string englishTerm)
        {
            return LangRef.TryGetValue(englishTerm, out var translation)
                ? translation
                : englishTerm;
        }
    }
}
