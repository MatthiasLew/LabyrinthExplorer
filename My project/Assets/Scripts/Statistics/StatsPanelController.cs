using System;
using System.Collections.Generic;
using System.Linq;
using Algorytm.Dane;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Statistics
{
    /// <summary>
    /// Widok historii benchmarków. Historia nadal przechowuje surowe próby
    /// potrzebne do analizy, ale ekran pokazuje jeden kafel na jeden pomiar
    /// porównawczy zamiast osobnego wiersza dla każdego przebiegu algorytmu.
    /// </summary>
    public sealed class StatsPanelController : MonoBehaviour
    {
        private const float SummaryHeight = 106f;
        private const float CardHeight = 210f;
        private const float PanelPadding = 18f;

        private BenchmarkHistoryStore historyStore;
        private RectTransform rootPanel;
        private RectTransform cardsContent;
        private TMP_Text summaryText;
        private ScrollRect scrollRect;
        private bool viewBuilt;

        // Teksty są tworzone dynamicznie, dlatego muszą otrzymać jawnie
        // istniejący font TMP oraz jego materiał. Bez tego layout próbuje
        // obliczyć rozmiar tekstu z pustym fontAsset i powoduje NullReferenceException.
        private TMP_FontAsset resolvedFontAsset;
        private Material resolvedFontMaterial;

        public void Initialize(RectTransform contentArea)
        {
            if (contentArea == null)
            {
                return;
            }

            historyStore ??= new BenchmarkHistoryStore();

            if (rootPanel != contentArea)
            {
                rootPanel = contentArea;
                viewBuilt = false;
            }

            if (!viewBuilt)
            {
                BuildView();
            }
        }

        public void RefreshDisplay()
        {
            if (rootPanel == null)
            {
                return;
            }

            if (!viewBuilt)
            {
                BuildView();
            }

            historyStore ??= new BenchmarkHistoryStore();
            List<BenchmarkHistoryEntry> history = historyStore.LoadHistory();
            List<BenchmarkMeasurementSummary> measurements = BuildMeasurementSummaries(history);

            UpdateSummary(history, measurements);
            ClearCards();

            if (measurements.Count == 0)
            {
                DisplayEmptyMessage();
            }
            else
            {
                for (int i = 0; i < measurements.Count; i++)
                {
                    DisplayMeasurementCard(measurements[i], i + 1);
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContent);
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void AppendBenchmarkResults(string testId, IReadOnlyList<AlgorithmMetrics> allMetrics)
        {
            historyStore ??= new BenchmarkHistoryStore();
            historyStore.AppendResults(testId, allMetrics);
            RefreshDisplay();
        }

        private void BuildView()
        {
            ResolveFontResources();
            DisableLegacyLayoutComponents();
            RemoveLegacyChildren();

            Image background = rootPanel.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.105f, 0.105f, 0.105f, 1f);
            }

            summaryText = CreateText(
                rootPanel,
                "MeasurementSummary",
                25f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                Color.white);

            RectTransform summaryRect = summaryText.rectTransform;
            summaryRect.anchorMin = new Vector2(0f, 1f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.pivot = new Vector2(0.5f, 1f);
            summaryRect.offsetMin = new Vector2(PanelPadding + 12f, -SummaryHeight);
            summaryRect.offsetMax = new Vector2(-PanelPadding - 12f, -12f);
            summaryText.enableWordWrapping = true;
            summaryText.overflowMode = TextOverflowModes.Overflow;

            GameObject scrollObject = CreateObject("MeasurementsScroll", rootPanel, typeof(Image), typeof(ScrollRect));
            RectTransform scrollTransform = scrollObject.GetComponent<RectTransform>();
            scrollTransform.anchorMin = Vector2.zero;
            scrollTransform.anchorMax = Vector2.one;
            scrollTransform.offsetMin = new Vector2(PanelPadding, PanelPadding);
            scrollTransform.offsetMax = new Vector2(-PanelPadding, -SummaryHeight - 12f);
            scrollObject.GetComponent<Image>().color = new Color(0.075f, 0.075f, 0.075f, 1f);

            GameObject viewportObject = CreateObject("Viewport", scrollTransform, typeof(Image), typeof(RectMask2D));
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -8f);
            viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            GameObject contentObject = CreateObject("MeasurementCards", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cardsContent = contentObject.GetComponent<RectTransform>();
            cardsContent.anchorMin = new Vector2(0f, 1f);
            cardsContent.anchorMax = new Vector2(1f, 1f);
            cardsContent.pivot = new Vector2(0.5f, 1f);
            cardsContent.anchoredPosition = Vector2.zero;
            cardsContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = cardsContent;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;

            viewBuilt = true;
        }

        private void DisableLegacyLayoutComponents()
        {
            VerticalLayoutGroup legacyLayout = rootPanel.GetComponent<VerticalLayoutGroup>();
            if (legacyLayout != null)
            {
                legacyLayout.enabled = false;
            }

            ContentSizeFitter legacyFitter = rootPanel.GetComponent<ContentSizeFitter>();
            if (legacyFitter != null)
            {
                legacyFitter.enabled = false;
            }
        }

        private void RemoveLegacyChildren()
        {
            for (int i = rootPanel.childCount - 1; i >= 0; i--)
            {
                GameObject child = rootPanel.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private static List<BenchmarkMeasurementSummary> BuildMeasurementSummaries(
            IEnumerable<BenchmarkHistoryEntry> entries)
        {
            return entries
                .Where(entry => entry != null)
                .GroupBy(entry => string.IsNullOrWhiteSpace(entry.testId)
                    ? $"legacy_{entry.measurementUtcTicks}_{entry.algorithmName}_{entry.runIndex}"
                    : entry.testId)
                .Select(group => BenchmarkMeasurementSummary.Create(group.Key, group))
                .OrderByDescending(summary => summary.latestMeasurementUtcTicks)
                .ToList();
        }

        private void UpdateSummary(
            IReadOnlyList<BenchmarkHistoryEntry> history,
            IReadOnlyList<BenchmarkMeasurementSummary> measurements)
        {
            if (summaryText == null)
            {
                return;
            }

            if (measurements.Count == 0)
            {
                summaryText.text = "WYNIKI POMIARÓW\nBrak zapisanych pomiarów.";
                return;
            }

            int successfulRuns = history.Count(entry => entry != null && entry.reachedGoal);
            summaryText.text =
                $"WYNIKI POMIARÓW   •   Pomiarów: {measurements.Count}\n" +
                $"Zapisane próby algorytmów: {history.Count}   •   Udane: {successfulRuns}";
        }

        private void ClearCards()
        {
            if (cardsContent == null)
            {
                return;
            }

            for (int i = cardsContent.childCount - 1; i >= 0; i--)
            {
                GameObject oldCard = cardsContent.GetChild(i).gameObject;
                oldCard.SetActive(false);
                Destroy(oldCard);
            }
        }

        private void DisplayEmptyMessage()
        {
            GameObject card = CreateCard("EmptyHistory", 94f, new Color(0.13f, 0.13f, 0.13f, 1f));
            AddLine(card, "Wykonaj pomiar, aby wynik pojawił się tutaj.", 21f, FontStyles.Normal, Color.white, 64f);
        }

        private void DisplayMeasurementCard(BenchmarkMeasurementSummary measurement, int position)
        {
            AlgorithmRunSummary genetic = measurement.GetAlgorithm("Genetic");
            AlgorithmRunSummary ant = measurement.GetAlgorithm("Ant");

            string maze = string.IsNullOrWhiteSpace(measurement.mazeName) ? "Labirynt" : measurement.mazeName;
            string pathWinner = DeterminePathWinner(genetic, ant);
            string timeWinner = DetermineTimeWinner(genetic, ant);

            GameObject card = CreateCard(
                "Measurement_" + position,
                CardHeight,
                position == 1
                    ? new Color(0.15f, 0.21f, 0.18f, 1f)
                    : new Color(0.13f, 0.13f, 0.13f, 1f));

            AddLine(
                card,
                $"#{position}   {maze} ({measurement.mazeWidth}x{measurement.mazeHeight})   •   Próby: {measurement.runCountPerAlgorithm}",
                21f,
                FontStyles.Bold,
                Color.white,
                38f);

            AddLine(
                card,
                $"Lepsza surowa trasa: {pathWinner}   •   Szybsza logika: {timeWinner}",
                18f,
                FontStyles.Bold,
                new Color(0.90f, 0.94f, 0.88f, 1f),
                38f);

            AddLine(
                card,
                BuildAlgorithmLine("Genetyczny", genetic),
                18f,
                FontStyles.Normal,
                new Color(0.91f, 0.91f, 0.91f, 1f),
                46f);

            AddLine(
                card,
                BuildAlgorithmLine("Mrówkowy", ant),
                18f,
                FontStyles.Normal,
                new Color(0.91f, 0.91f, 0.91f, 1f),
                46f);
        }

        private GameObject CreateCard(string objectName, float height, Color color)
        {
            GameObject card = CreateObject(
                objectName,
                cardsContent,
                typeof(Image),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));

            card.GetComponent<Image>().color = color;

            LayoutElement element = card.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = 1f;

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return card;
        }

        private void AddLine(
            GameObject card,
            string value,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            float height)
        {
            GameObject line = CreateObject("Line", card.transform as RectTransform, typeof(LayoutElement), typeof(TextMeshProUGUI));

            LayoutElement element = line.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = 1f;

            TMP_Text text = line.GetComponent<TMP_Text>();
            ApplyResolvedFont(text);
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = color;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
        }

        private static string BuildAlgorithmLine(string displayName, AlgorithmRunSummary algorithm)
        {
            if (algorithm == null || algorithm.totalRuns == 0)
            {
                return $"{displayName}: brak danych";
            }

            string path = algorithm.successfulRuns > 0
                ? $"{algorithm.averageSuccessfulRawPathLength:F1} kroków"
                : "brak trasy";

            return $"{displayName}: sukces {algorithm.successfulRuns}/{algorithm.totalRuns}   •   śr. surowa trasa {path}   •   logika {algorithm.averageLogicTimeMs:F2} ms";
        }

        private static string DeterminePathWinner(AlgorithmRunSummary genetic, AlgorithmRunSummary ant)
        {
            if (genetic == null || genetic.successfulRuns == 0)
            {
                return ant != null && ant.successfulRuns > 0 ? "Mrówkowy" : "brak";
            }

            if (ant == null || ant.successfulRuns == 0)
            {
                return "Genetyczny";
            }

            if (Mathf.Approximately(
                (float)genetic.averageSuccessfulRawPathLength,
                (float)ant.averageSuccessfulRawPathLength))
            {
                return "remis";
            }

            return genetic.averageSuccessfulRawPathLength < ant.averageSuccessfulRawPathLength
                ? "Genetyczny"
                : "Mrówkowy";
        }

        private static string DetermineTimeWinner(AlgorithmRunSummary genetic, AlgorithmRunSummary ant)
        {
            if (genetic == null || genetic.totalRuns == 0)
            {
                return ant != null && ant.totalRuns > 0 ? "Mrówkowy" : "brak";
            }

            if (ant == null || ant.totalRuns == 0)
            {
                return "Genetyczny";
            }

            if (Mathf.Approximately((float)genetic.averageLogicTimeMs, (float)ant.averageLogicTimeMs))
            {
                return "remis";
            }

            return genetic.averageLogicTimeMs < ant.averageLogicTimeMs
                ? "Genetyczny"
                : "Mrówkowy";
        }

        private void ResolveFontResources()
        {
            if (resolvedFontAsset != null)
            {
                return;
            }

            // Najpierw bierzemy font z istniejącego tekstu panelu/sceny.
            // To zachowuje styl istniejącego UI i działa także po zmianie fontów w Inspectorze.
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text candidate = texts[i];
                if (candidate == null || candidate.font == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    candidate.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                resolvedFontAsset = candidate.font;
                resolvedFontMaterial = candidate.fontSharedMaterial;
                break;
            }

            // Awaryjnie korzystamy z fontu ustawionego globalnie przez TextMesh Pro.
            if (resolvedFontAsset == null)
            {
                resolvedFontAsset = TMP_Settings.defaultFontAsset;
                if (resolvedFontAsset != null)
                {
                    resolvedFontMaterial = resolvedFontAsset.material;
                }
            }

            // Ostatni fallback dla projektu z zaimportowanymi TMP Essentials.
            if (resolvedFontAsset == null)
            {
                resolvedFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (resolvedFontAsset != null)
                {
                    resolvedFontMaterial = resolvedFontAsset.material;
                }
            }

            if (resolvedFontAsset == null)
            {
                Debug.LogError(
                    "StatsPanelController: Nie znaleziono fontu TextMesh Pro. " +
                    "Zaimportuj TMP Essential Resources albo przypisz font do tekstu w scenie.");
            }
        }

        private void ApplyResolvedFont(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            ResolveFontResources();

            if (resolvedFontAsset == null)
            {
                return;
            }

            text.font = resolvedFontAsset;
            text.fontSharedMaterial = resolvedFontMaterial != null
                ? resolvedFontMaterial
                : resolvedFontAsset.material;
        }

        private TextMeshProUGUI CreateText(
            RectTransform parent,
            string objectName,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject gameObject = CreateObject(objectName, parent, typeof(TextMeshProUGUI));
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            ApplyResolvedFont(text);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(string objectName, RectTransform parent, params Type[] components)
        {
            var types = new List<Type> { typeof(RectTransform) };
            types.AddRange(components);
            GameObject gameObject = new GameObject(objectName, types.ToArray());
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private sealed class BenchmarkMeasurementSummary
        {
            public string testId;
            public string mazeName;
            public int mazeWidth;
            public int mazeHeight;
            public int runCountPerAlgorithm;
            public long latestMeasurementUtcTicks;
            public List<AlgorithmRunSummary> algorithms = new();

            public AlgorithmRunSummary GetAlgorithm(string namePart)
            {
                return algorithms.FirstOrDefault(item =>
                    item.algorithmName != null &&
                    item.algorithmName.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            public static BenchmarkMeasurementSummary Create(
                string testId,
                IEnumerable<BenchmarkHistoryEntry> sourceEntries)
            {
                List<BenchmarkHistoryEntry> entries = sourceEntries
                    .Where(entry => entry != null)
                    .ToList();

                BenchmarkHistoryEntry newest = entries
                    .OrderByDescending(entry => entry.measurementUtcTicks)
                    .First();

                var result = new BenchmarkMeasurementSummary
                {
                    testId = testId,
                    mazeName = newest.mazeName,
                    mazeWidth = newest.mazeWidth,
                    mazeHeight = newest.mazeHeight,
                    latestMeasurementUtcTicks = newest.measurementUtcTicks
                };

                result.algorithms = entries
                    .GroupBy(entry => entry.algorithmName ?? string.Empty)
                    .Select(group => AlgorithmRunSummary.Create(group.Key, group))
                    .ToList();

                result.runCountPerAlgorithm = result.algorithms.Count == 0
                    ? 0
                    : result.algorithms.Max(item => item.totalRuns);

                return result;
            }
        }

        private sealed class AlgorithmRunSummary
        {
            public string algorithmName;
            public int totalRuns;
            public int successfulRuns;
            public double averageSuccessfulRawPathLength;
            public double averageLogicTimeMs;

            public static AlgorithmRunSummary Create(
                string algorithmName,
                IEnumerable<BenchmarkHistoryEntry> sourceEntries)
            {
                List<BenchmarkHistoryEntry> entries = sourceEntries.ToList();
                List<BenchmarkHistoryEntry> successful = entries
                    .Where(entry => entry.reachedGoal)
                    .ToList();

                return new AlgorithmRunSummary
                {
                    algorithmName = algorithmName,
                    totalRuns = entries.Count,
                    successfulRuns = successful.Count,
                    averageSuccessfulRawPathLength = successful.Count > 0
                        ? successful.Average(entry => entry.pathLength)
                        : 0d,
                    averageLogicTimeMs = entries.Count > 0
                        ? entries.Average(entry => entry.GetComparableLogicTimeMs())
                        : 0d
                };
            }
        }
    }
}
