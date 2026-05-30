using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Statistics
{
    /// <summary>
    /// Wypełnia panel "Wyniki Pomiarów" prawdziwymi wpisami z zapisanej historii benchmarków.
    /// </summary>
    public class StatsPanelController : MonoBehaviour
    {
        private BenchmarkHistoryStore historyStore;
        private RectTransform contentPanel;
        private ScrollRect scrollRect;

        public void Initialize(RectTransform contentArea)
        {
            contentPanel = contentArea;
            historyStore ??= new BenchmarkHistoryStore();

            if (contentPanel == null)
            {
                return;
            }

            EnsureContentLayout(contentPanel);
            scrollRect = contentPanel.GetComponentInParent<ScrollRect>();
        }

        public void RefreshDisplay()
        {
            if (contentPanel == null)
            {
                return;
            }

            historyStore ??= new BenchmarkHistoryStore();
            List<BenchmarkHistoryEntry> history = historyStore.LoadHistory();

            ClearRows();

            if (history.Count == 0)
            {
                DisplayEmptyMessage();
                return;
            }

            DisplayHeader();

            for (int i = history.Count - 1; i >= 0; i--)
            {
                DisplayEntry(history[i], history.Count - i);
            }

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void AppendBenchmarkResults(
            string testId,
            IReadOnlyList<Algorytm.Dane.AlgorithmMetrics> allMetrics)
        {
            historyStore ??= new BenchmarkHistoryStore();
            historyStore.AppendResults(testId, allMetrics);
            RefreshDisplay();
        }

        private static void EnsureContentLayout(RectTransform content)
        {
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private void ClearRows()
        {
            for (int i = contentPanel.childCount - 1; i >= 0; i--)
            {
                Destroy(contentPanel.GetChild(i).gameObject);
            }
        }

        private void DisplayEmptyMessage()
        {
            GameObject row = CreateRowObject("EmptyHistory", 60f, new Color(0.12f, 0.12f, 0.12f, 1f));
            AddTextCell(row, "Brak wykonanych pomiarów.", 1f, true);
        }

        private void DisplayHeader()
        {
            GameObject row = CreateRowObject("HeaderRow", 48f, new Color(0.20f, 0.20f, 0.20f, 1f));

            AddTextCell(row, "ID", 0.20f, true);
            AddTextCell(row, "Algorytm", 0.25f, true);
            AddTextCell(row, "Labirynt", 0.27f, true);
            AddTextCell(row, "Czas", 0.16f, true);
            AddTextCell(row, "Sukces", 0.12f, true);
        }

        private void DisplayEntry(BenchmarkHistoryEntry entry, int index)
        {
            Color color = index % 2 == 0
                ? new Color(0.12f, 0.12f, 0.12f, 1f)
                : new Color(0.15f, 0.15f, 0.15f, 1f);

            GameObject row = CreateRowObject("HistoryRow_" + index, 46f, color);
            string displayId = $"{entry.testId}-{entry.runIndex}";

            AddTextCell(row, displayId, 0.20f, false);
            AddTextCell(row, ShortAlgorithmName(entry.algorithmName), 0.25f, false);
            AddTextCell(row, $"{entry.mazeName} ({entry.mazeWidth}x{entry.mazeHeight})", 0.27f, false);
            AddTextCell(row, entry.GetRuntimeFormatted(), 0.16f, false);
            AddTextCell(row, entry.reachedGoal ? "Tak" : "Nie", 0.12f, false);
        }

        private GameObject CreateRowObject(string name, float height, Color background)
        {
            GameObject row = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));

            row.transform.SetParent(contentPanel, false);

            Image image = row.GetComponent<Image>();
            image.color = background;

            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
            layoutElement.flexibleWidth = 1f;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            return row;
        }

        private static void AddTextCell(GameObject row, string value, float flexibleWidth, bool header)
        {
            GameObject cell = new GameObject(
                "Cell",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(TextMeshProUGUI));

            cell.transform.SetParent(row.transform, false);

            LayoutElement layout = cell.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.preferredWidth = flexibleWidth * 1000f;

            TextMeshProUGUI text = cell.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = header ? 18f : 16f;
            text.fontStyle = header ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = header ? Color.white : new Color(0.84f, 0.84f, 0.84f, 1f);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        private static string ShortAlgorithmName(string algorithmName)
        {
            if (string.IsNullOrWhiteSpace(algorithmName))
            {
                return "-";
            }

            if (algorithmName.Contains("Genetic"))
            {
                return "Genetyczny";
            }

            if (algorithmName.Contains("Ant"))
            {
                return "Mrówkowy";
            }

            return algorithmName;
        }
    }
}
