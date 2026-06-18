namespace BlazorCore.Services.Translation
{
    public interface ITranslation
    {
        /// <summary>
        /// Loads translations for the specified language.
        /// </summary>
        /// <param name="routesStateHasChanged">If true, triggers UI update after loading.</param>
        /// <param name="selectedLanguage">Language code (e.g., "EN", "DE"). If empty, uses current SelectedLanguage.</param>
        Task LoadTranslations(bool routesStateHasChanged, string selectedLanguage = "");

        /// <summary>
        /// Gets the translated text for the given English term.
        /// Returns the original term if no translation is found.
        /// </summary>
        /// <param name="englishTerm">The English term to translate.</param>
        /// <returns>Translated text or original term.</returns>
        string T(string englishTerm);

        /// <summary>
        /// Gets or sets the currently selected language (e.g., "EN", "DE").
        /// </summary>
        string SelectedLanguage { get; set; }
    }
}
