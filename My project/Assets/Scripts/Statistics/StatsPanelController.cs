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
    /// Czytelny, przewijany widok historii benchmarków. Komponent sam buduje swoją
    /// strukturę UI wewnątrz przekazanego ResultsPanel i nie wymaga ręcznego
    /// utrzymywania przykładowych wierszy w scenie.
    /// </summary>
    public sealed class StatsPanelController : MonoBehaviour
    {
        private const float HeaderHeight = 64f;
        private const float RowHeight = 72f;
        private const float SummaryHeight = 104f;
        private const float PanelPadding = 18f;

        private BenchmarkHistoryStore historyStore;
        private RectTransform rootPanel;
        private RectTransform rowsContent;
        private TMP_Text summaryText;
        private ScrollRect scrollRect;
        private bool viewBuilt;

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
            List<BenchmarkHistoryEntry> ranking = BuildRanking(history);

            UpdateSummary(history, ranking);
            ClearRows();
            DisplayHeader();

            if (ranking.Count == 0)
            {
                DisplayEmptyMessage();
            }
            else
            {
                for (int i = 0; i < ranking.Count; i++)
                {
                    DisplayEntry(ranking[i], i + 1);
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowsContent);
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
            DisableLegacyLayoutComponents();
            RemoveLegacyChildren();

            Image background = rootPanel.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.105f, 0.105f, 0.105f, 1f);
            }

            summaryText = CreateText(
                rootPanel,
                "RankingSummary",
                27f,
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

            GameObject scrollObject = CreateObject("RankingScroll", rootPanel, typeof(Image), typeof(ScrollRect));
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

            GameObject rowsObject = CreateObject("RowsContent", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            rowsContent = rowsObject.GetComponent<RectTransform>();
            rowsContent.anchorMin = new Vector2(0f, 1f);
            rowsContent.anchorMax = new Vector2(1f, 1f);
            rowsContent.pivot = new Vector2(0.5f, 1f);
            rowsContent.anchoredPosition = Vector2.zero;
            rowsContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup rowsLayout = rowsObject.GetComponent<VerticalLayoutGroup>();
            rowsLayout.padding = new RectOffset(2, 2, 2, 2);
            rowsLayout.spacing = 8f;
            rowsLayout.childAlignment = TextAnchor.UpperLeft;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = rowsObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = rowsContent;
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

        private static List<BenchmarkHistoryEntry> BuildRanking(IEnumerable<BenchmarkHistoryEntry> entries)
        {
            return entries
                .Where(entry => entry != null)
                .OrderByDescending(entry => entry.reachedGoal)
                .ThenByDescending(entry => entry.pathEfficiency)
                .ThenBy(entry => entry.pathLength <= 0 ? int.MaxValue : entry.pathLength)
                .ThenBy(entry => entry.totalRuntimeMs)
                .ThenByDescending(entry => entry.measurementUtcTicks)
                .ToList();
        }

        private void UpdateSummary(IReadOnlyList<BenchmarkHistoryEntry> history, IReadOnlyList<BenchmarkHistoryEntry> ranking)
        {
            if (summaryText == null)
            {
                return;
            }

            if (history.Count == 0)
            {
                summaryText.text = "RANKING POMIARÓW\nBrak zapisanych przebiegów.";
                return;
            }

            int successCount = history.Count(entry => entry.reachedGoal);
            BenchmarkHistoryEntry best = ranking.FirstOrDefault(entry => entry.reachedGoal);
            string bestText = best == null
                ? "Brak ukończonych tras"
                : $"Lider: {ShortAlgorithmName(best.algorithmName)} | {best.pathLength} kroków | {best.GetRuntimeFormatted()}";

            summaryText.text = $"RANKING POMIARÓW   •   Przebiegi: {history.Count}   •   Udane: {successCount}\n{bestText}";
        }

        private void ClearRows()
        {
            if (rowsContent == null)
            {
                return;
            }

            for (int i = rowsContent.childCount - 1; i >= 0; i--)
            {
                GameObject oldRow = rowsContent.GetChild(i).gameObject;
                oldRow.SetActive(false);
                Destroy(oldRow);
            }
        }

        private void DisplayEmptyMessage()
        {
            GameObject row = CreateRow("EmptyHistory", 84f, new Color(0.13f, 0.13f, 0.13f, 1f));
            AddTextCell(row, "Wykonaj pomiar, aby wynik pojawił się w rankingu.", 760f, false, TextAlignmentOptions.MidlineLeft);
        }

        private void DisplayHeader()
        {
            GameObject row = CreateRow("RankingHeader", HeaderHeight, new Color(0.22f, 0.22f, 0.22f, 1f));
            AddTextCell(row, "#", 52f, true, TextAlignmentOptions.Center);
            AddTextCell(row, "Algorytm", 182f, true, TextAlignmentOptions.MidlineLeft);
            AddTextCell(row, "Labirynt", 270f, true, TextAlignmentOptions.MidlineLeft);
            AddTextCell(row, "Droga", 112f, true, TextAlignmentOptions.Center);
            AddTextCell(row, "Czas", 165f, true, TextAlignmentOptions.MidlineLeft);
            AddTextCell(row, "Wynik", 130f, true, TextAlignmentOptions.Center);
        }

        private void DisplayEntry(BenchmarkHistoryEntry entry, int rank)
        {
            bool isLeader = rank == 1 && entry.reachedGoal;
            Color rowColor = isLeader
                ? new Color(0.16f, 0.25f, 0.19f, 1f)
                : rank % 2 == 0
                    ? new Color(0.12f, 0.12f, 0.12f, 1f)
                    : new Color(0.145f, 0.145f, 0.145f, 1f);

            GameObject row = CreateRow("Rank_" + rank, RowHeight, rowColor);
            string path = entry.reachedGoal ? entry.pathLength.ToString() : "—";
            string result = entry.reachedGoal ? "UKOŃCZONO" : "BRAK";

            AddTextCell(row, rank.ToString(), 52f, isLeader, TextAlignmentOptions.Center);
            AddTextCell(row, ShortAlgorithmName(entry.algorithmName), 182f, isLeader, TextAlignmentOptions.MidlineLeft);
            AddTextCell(row, $"{entry.mazeName}  {entry.mazeWidth}x{entry.mazeHeight}", 270f, false, TextAlignmentOptions.MidlineLeft);
            AddTextCell(row, path, 112f, isLeader, TextAlignmentOptions.Center);
            AddTextCell(row, entry.GetRuntimeFormatted(), 165f, false, TextAlignmentOptions.MidlineLeft);
            AddTextCell(row, result, 130f, isLeader, TextAlignmentOptions.Center);
        }

        private GameObject CreateRow(string objectName, float height, Color color)
        {
            GameObject row = CreateObject(objectName, rowsContent, typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.GetComponent<Image>().color = color;

            LayoutElement element = row.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = 1f;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            return row;
        }

        private static void AddTextCell(
            GameObject row,
            string value,
            float width,
            bool emphasis,
            TextAlignmentOptions alignment)
        {
            GameObject cell = CreateObject("Cell", row.transform as RectTransform, typeof(LayoutElement), typeof(TextMeshProUGUI));
            LayoutElement layout = cell.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.flexibleWidth = 0f;

            TMP_Text text = cell.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = emphasis ? 24f : 22f;
            text.fontStyle = emphasis ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.color = emphasis ? Color.white : new Color(0.90f, 0.90f, 0.90f, 1f);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string objectName,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject gameObject = CreateObject(objectName, parent, typeof(TextMeshProUGUI));
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
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

        private static string ShortAlgorithmName(string algorithmName)
        {
            if (string.IsNullOrWhiteSpace(algorithmName))
            {
                return "—";
            }

            if (algorithmName.IndexOf("Genetic", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Genetyczny";
            }

            if (algorithmName.IndexOf("Ant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                algorithmName.IndexOf("Colony", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Mrówkowy";
            }

            return algorithmName;
        }
    }
}
