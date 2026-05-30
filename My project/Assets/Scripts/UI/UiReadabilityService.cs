using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Presentation
{
    /// <summary>
    /// Centralizuje minimalne wymagania czytelności interfejsu oraz zabezpiecza
    /// nawigację powrotną na panelach aplikacji.
    /// </summary>
    public sealed class UiReadabilityService
    {
        private const float MinimumButtonFontSize = 27f;

        public void ApplyButtonTypography(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                {
                    continue;
                }

                label.fontSize = Mathf.Max(label.fontSize, MinimumButtonFontSize);
                label.fontStyle = FontStyles.Normal;
                label.color = Color.white;
                label.enableWordWrapping = false;
            }
        }

        /// <summary>
        /// Zabezpiecza przycisk powrotu z rankingu: umieszcza go nad dynamiczną listą,
        /// nadaje duży obszar kliknięcia oraz dopina akcję powrotu do menu.
        /// </summary>
        public void PinStatisticsBackButton(GameObject statsPanel, UnityAction goBack)
        {
            if (statsPanel == null || goBack == null)
            {
                return;
            }

            Button backButton = FindBackButton(statsPanel.transform);
            if (backButton == null)
            {
                return;
            }

            backButton.onClick.RemoveListener(goBack);
            backButton.onClick.AddListener(goBack);

            RectTransform rect = backButton.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.SetParent(statsPanel.transform, false);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(430f, 88f);
            rect.SetAsLastSibling();

            TMP_Text label = backButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 29f;
                label.fontStyle = FontStyles.Bold;
                label.text = LooksEnglish(label.text) ? "Back" : "Wróć";
            }
        }

        private static Button FindBackButton(Transform root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button candidate = buttons[i];
                if (candidate.name.Equals("BtnBack", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }

                TMP_Text text = candidate.GetComponentInChildren<TMP_Text>(true);
                string label = text != null ? text.text : string.Empty;
                if (label.IndexOf("Wróć", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    label.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool LooksEnglish(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
