using TMPro;
using UnityEngine;

/// <summary>
/// Lokalizacja statycznych tekstów interfejsu oraz wspólne pomocnicze metody językowe.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private string TextByLanguage(string polish, string english)
    {
        return currentLanguage == AppLanguage.Polski ? polish : english;
    }

    private void ApplyLanguageToAllTexts()
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || !text.gameObject.scene.IsValid())
            {
                continue;
            }

            if (text == resolutionButtonText || text == languageButtonText || text == fullscreenButtonText ||
                text == resolutionLabelText || text == fullscreenLabelText || text == languageLabelText ||
                text == measurementHeaderText)
            {
                continue;
            }

            text.text = TranslateStaticText(text.text);
        }

        UpdateSaveDialogLanguage();
        UpdateLoadMazeDialogLanguage();

        if (lastComparisonResult != null)
        {
            DisplayBenchmarkResults(lastComparisonResult);
        }
        else
        {
            ResetResultsText();
        }

        UpdateSettingsControlsText();
        UpdateMeasurementsHeaderText();
    }

    private string TranslateStaticText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        bool toEnglish = currentLanguage == AppLanguage.English;

        if (toEnglish)
        {
            switch (input)
            {
                case "Wyniki Pomiarów": return "Measurement Results";
                case "Edytor Labiryntów": return "Maze Editor";
                case "Ustawienia": return "Settings";
                case "Informacje Pomiaru": return "Measurement Info";
                case "Rozpocznij Pomiar": return "Start Measurement";
                case "Dodaj Labirynt": return "Add Maze";
                case "Rysowanie": return "Draw";
                case "Usuwanie": return "Erase";
                case "Dodaj Start/Mete":
                case "Dodaj Start/Metę":
                case "Ustaw start/metę": return "Set Start/Finish";
                case "Generuj Losowo": return "Random Maze";
                case "Zapisz Labirynt": return "Save Maze";
                case "Usuń Labirynt": return "Delete Maze";
                case "Wróć": return "Back";
                case "Algorytm A": return "Algorithm A";
                case "Algorytm B": return "Algorithm B";
                case "Pomiary dla Labiryntu - Nazwa Labiryntu": return "Measurements for Maze - Maze Name";
                case "Identyfikator | Nazwa Algorytmu | Nazwa Labiryntu | Czas Pomiaru": return "ID | Algorithm Name | Maze Name | Measurement Time";
                case "Wybierz rozmiar": return "Choose size";
            }

            return input
                .Replace("Nazwa Algorytmu", "Algorithm Name")
                .Replace("Nazwa Labiryntu", "Maze Name")
                .Replace("Czas Pomiaru", "Measurement Time")
                .Replace("Algorytm", "Algorithm")
                .Replace("Labirynt", "Maze");
        }

        switch (input)
        {
            case "Measurement Results": return "Wyniki Pomiarów";
            case "Maze Editor": return "Edytor Labiryntów";
            case "Settings": return "Ustawienia";
            case "Measurement Info": return "Informacje Pomiaru";
            case "Start Measurement": return "Rozpocznij Pomiar";
            case "Add Maze": return "Dodaj Labirynt";
            case "Draw": return "Rysowanie";
            case "Erase": return "Usuwanie";
            case "Set Start/Finish": return "Ustaw start/metę";
            case "Dodaj Start/Mete":
            case "Dodaj Start/Metę": return "Ustaw start/metę";
            case "Random Maze": return "Generuj Losowo";
            case "Save Maze": return "Zapisz Labirynt";
            case "Delete Maze": return "Usuń Labirynt";
            case "Back": return "Wróć";
            case "Algorithm A": return "Algorytm A";
            case "Algorithm B": return "Algorytm B";
            case "Measurements for Maze - Maze Name": return "Pomiary dla Labiryntu - Nazwa Labiryntu";
            case "ID | Algorithm Name | Maze Name | Measurement Time": return "Identyfikator | Nazwa Algorytmu | Nazwa Labiryntu | Czas Pomiaru";
            case "Choose size": return "Wybierz rozmiar";
        }

        return input
            .Replace("Algorithm Name", "Nazwa Algorytmu")
            .Replace("Maze Name", "Nazwa Labiryntu")
            .Replace("Measurement Time", "Czas Pomiaru")
            .Replace("Algorithm", "Algorytm")
            .Replace("Maze", "Labirynt");
    }
}
