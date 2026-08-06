using BepInEx.Configuration;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace RiskyStats
{
    public enum StatAlignment
    {
        Horizontal,
        Vertical
    }

    public enum StatTextAlign
    {
        Center,
        Left
    }

    public static class RSSettings
    {
        public static event Action OnSettingsChanged;

        public static Dictionary<string, bool> StatVisibility = new Dictionary<string, bool>
        {
            { "AttackSpeed", true },
            { "Armor", true },
            { "Crit", true },
            { "Healing", true },
            { "Damage", true },
            { "Streak", true },
            { "DamageTaken", true },
            { "DamageTakenStreak", true },
            { "Speed", true }
        };

        public static float StatFontSize = 27f;
        public static float StatSpacing = 40f;
        public static StatAlignment Alignment = StatAlignment.Horizontal;
        public static StatTextAlign TextAlign = StatTextAlign.Center;
        private static ConfigEntry<StatTextAlign> textAlignEntry;
        public static bool PanelVisible = true;
        public static KeyCode ToggleKey = KeyCode.V;
        public static bool ShowThankYouMessage = true;

        private static ConfigFile config;
        private static Dictionary<string, ConfigEntry<bool>> visibilityEntries = new Dictionary<string, ConfigEntry<bool>>();
        private static ConfigEntry<float> fontSizeEntry;
        private static ConfigEntry<float> spacingEntry;
        private static ConfigEntry<StatAlignment> alignmentEntry;
        private static ConfigEntry<KeyCode> toggleKeyEntry;
        private static ConfigEntry<bool> showThankYouMessageEntry;

        public static void Init(ConfigFile cfg)
        {
            config = cfg;

            foreach (string key in new List<string>(StatVisibility.Keys))
            {
                ConfigEntry<bool> entry = config.Bind(
                    "Stats Visibility",
                    key,
                    true,
                    "Show or hide the " + key + " stat"
                );

                visibilityEntries[key] = entry;
                StatVisibility[key] = entry.Value;
            }

            fontSizeEntry = config.Bind("Appearance", "Font Size", 27f, "Text size of each stat");
            spacingEntry = config.Bind("Appearance", "Spacing", 40f, "Spacing between stats");
            alignmentEntry = config.Bind("Appearance", "Alignment", StatAlignment.Horizontal, "Horizontal or vertical layout");
            textAlignEntry = config.Bind("Appearance", "Text Alignment", StatTextAlign.Center, "Text alignment when using vertical layout");
            toggleKeyEntry = config.Bind("General", "Toggle Key", KeyCode.V, "Key used to show/hide the stats panel");
            showThankYouMessageEntry = config.Bind("General", "Show Thank You Message", true, "Whether to show the developer message in the settings panel");

            StatFontSize = fontSizeEntry.Value;
            StatSpacing = spacingEntry.Value;
            Alignment = alignmentEntry.Value;
            TextAlign = textAlignEntry.Value;
            ToggleKey = toggleKeyEntry.Value;
            ShowThankYouMessage = showThankYouMessageEntry.Value;
        }

        public static void SetVisibility(string key, bool value)
        {
            StatVisibility[key] = value;

            if (visibilityEntries.ContainsKey(key))
                visibilityEntries[key].Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void SetFontSize(float value)
        {
            StatFontSize = value;

            if (fontSizeEntry != null)
                fontSizeEntry.Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void SetSpacing(float value)
        {
            StatSpacing = value;

            if (spacingEntry != null)
                spacingEntry.Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void SetAlignment(StatAlignment value)
        {
            Alignment = value;

            if (alignmentEntry != null)
                alignmentEntry.Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void SetTextAlign(StatTextAlign value)
        {
            TextAlign = value;

            if (textAlignEntry != null)
                textAlignEntry.Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void TogglePanelVisible()
        {
            PanelVisible = !PanelVisible;
            OnSettingsChanged?.Invoke();
        }

        public static void DismissThankYouMessage()
        {
            ShowThankYouMessage = false;

            if (showThankYouMessageEntry != null)
                showThankYouMessageEntry.Value = false;
        }

        public static void ResetToDefault()
        {
            foreach (string key in new List<string>(StatVisibility.Keys))
            {
                StatVisibility[key] = true;
                if (visibilityEntries.ContainsKey(key))
                    visibilityEntries[key].Value = true;
            }

            StatFontSize = 27f;
            if (fontSizeEntry != null)
                fontSizeEntry.Value = 27f;

            StatSpacing = 40f;
            if (spacingEntry != null)
                spacingEntry.Value = 40f;

            Alignment = StatAlignment.Horizontal;
            if (alignmentEntry != null)
                alignmentEntry.Value = StatAlignment.Horizontal;

            Alignment = StatAlignment.Horizontal;
            if (alignmentEntry != null)
                alignmentEntry.Value = StatAlignment.Horizontal;

            TextAlign = StatTextAlign.Center;
            if (textAlignEntry != null)
                textAlignEntry.Value = StatTextAlign.Center;

            RSPlusSettings.ResetToDefault();

            OnSettingsChanged?.Invoke();
        }
    }

    public static class RSPlusSettings
    {
        public static event Action OnSettingsChanged;

        public static Dictionary<string, bool> StatVisibility = new Dictionary<string, bool>
        {
            { "Jumps", false },
            { "MountainShrines", false },
            { "Drones", false },
            { "Luck", false },
            { "Kills", false }
        };

        public static float StatFontSize = 27f;
        public static float StatSpacing = 40f;

        private static ConfigFile config;
        private static Dictionary<string, ConfigEntry<bool>> visibilityEntries = new Dictionary<string, ConfigEntry<bool>>();
        private static ConfigEntry<float> fontSizeEntry;
        private static ConfigEntry<float> spacingEntry;

        public static void Init(ConfigFile cfg)
        {
            config = cfg;

            foreach (string key in new List<string>(StatVisibility.Keys))
            {
                ConfigEntry<bool> entry = config.Bind(
                    "Plus Stats Visibility",
                    key,
                    false,
                    "Show or hide the " + key + " stat (Risky Stats+)"
                );

                visibilityEntries[key] = entry;
                StatVisibility[key] = entry.Value;
            }

            fontSizeEntry = config.Bind("Plus Appearance", "Font Size", 27f, "Text size of each Risky Stats+ stat");
            spacingEntry = config.Bind("Plus Appearance", "Spacing", 40f, "Spacing between Risky Stats+ stats");

            StatFontSize = fontSizeEntry.Value;
            StatSpacing = spacingEntry.Value;
        }

        public static void SetVisibility(string key, bool value)
        {
            StatVisibility[key] = value;

            if (visibilityEntries.ContainsKey(key))
                visibilityEntries[key].Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void SetFontSize(float value)
        {
            StatFontSize = value;

            if (fontSizeEntry != null)
                fontSizeEntry.Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void SetSpacing(float value)
        {
            StatSpacing = value;

            if (spacingEntry != null)
                spacingEntry.Value = value;

            OnSettingsChanged?.Invoke();
        }

        public static void ResetToDefault()
        {
            foreach (string key in new List<string>(StatVisibility.Keys))
            {
                StatVisibility[key] = false;
                if (visibilityEntries.ContainsKey(key))
                    visibilityEntries[key].Value = false;
            }

            StatFontSize = 27f;
            if (fontSizeEntry != null)
                fontSizeEntry.Value = 27f;

            StatSpacing = 40f;
            if (spacingEntry != null)
                spacingEntry.Value = 40f;

            OnSettingsChanged?.Invoke();
        }
    }

    public class RSSettingsUI : MonoBehaviour
    {
        private static GameObject panelObject;
        private static GameObject plusPanelObject;
        private static GameObject navButtonObject;
        private static GameObject backdropObject;
        private static GameObject thankYouMessageObject;
        private static Transform rootCanvasTransform;

        private static readonly Color BackgroundColor = new Color(0.05f, 0.07f, 0.18f, 0.97f);
        private static readonly Color BorderColor = new Color(1f, 0.82f, 0.2f, 1f);
        private static readonly Color RowColor = new Color(0.09f, 0.11f, 0.24f, 1f);
        private static readonly Color RowColorAlt = new Color(0.11f, 0.13f, 0.27f, 1f);
        private static readonly Color AccentColor = new Color(1f, 0.82f, 0.2f, 1f);
        private static readonly Color OffColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color DarkTextColor = new Color(0.05f, 0.07f, 0.18f, 1f);
        private static readonly Color SubTextColor = new Color(0.65f, 0.68f, 0.8f, 1f);
        private static readonly Color ThankYouBackgroundColor = new Color(0f, 0.1019f, 0.5098f, 0.97f);
        private static readonly Color ThankYouOutlineColor = new Color(0.53f, 0.81f, 1f, 1f);
        private static readonly Color ThankYouDismissColor = new Color(0.8f, 0.15f, 0.15f, 1f);

        private const float PanelWidth = 560f;
        private const float RowHeight = 32f;
        private const float RowSpacing = 6f;

        private static readonly string[] StatOrder = new string[]
        {
            "AttackSpeed", "Armor", "Crit", "Healing", "Damage",
            "Streak", "DamageTaken", "DamageTakenStreak", "Speed"
        };

        private static readonly Dictionary<string, string> StatLabels = new Dictionary<string, string>
        {
            { "AttackSpeed", "Attack Speed" },
            { "Armor", "Armor" },
            { "Crit", "Crit" },
            { "Healing", "Healing" },
            { "Damage", "Damage" },
            { "Streak", "Streak" },
            { "DamageTaken", "Damage Taken" },
            { "DamageTakenStreak", "Damage Taken Streak" },
            { "Speed", "Speed" }
        };

        private static readonly string[] PlusStatOrder = new string[]
        {
            "Jumps", "MountainShrines", "Drones", "Luck", "Kills"
        };

        private static readonly Dictionary<string, string> PlusStatLabels = new Dictionary<string, string>
        {
            { "Jumps", "Jumps" },
            { "MountainShrines", "Mountain Shrines" },
            { "Drones", "Drones" },
            { "Luck", "Luck" },
            { "Kills", "Kills" }
        };

        private void Awake()
        {
            On.RoR2.UI.PauseScreenController.Awake += PauseScreenController_Awake;
        }

        private void OnDestroy()
        {
            On.RoR2.UI.PauseScreenController.Awake -= PauseScreenController_Awake;
        }

        private void PauseScreenController_Awake(On.RoR2.UI.PauseScreenController.orig_Awake orig, PauseScreenController self)
        {
            orig(self);

            Debug.Log("[RiskyStats] PauseScreenController_Awake fired");

            CreateSettingsButton(self);
        }

        private void CreateSettingsButton(PauseScreenController self)
        {
            HGButton[] allButtons = self.GetComponentsInChildren<HGButton>(true);

            Debug.Log($"[RiskyStats] Found {allButtons.Length} buttons");

            foreach (HGButton b in allButtons)
            {
                Debug.Log(
                    $"[RiskyStats] Button: {b.gameObject.name} | " +
                    $"Parent: {b.transform.parent?.name} | " +
                    $"Sibling: {b.transform.GetSiblingIndex()} | " +
                    $"Active: {b.gameObject.activeInHierarchy}");
            }

            HGButton template = allButtons.FirstOrDefault(b => b.gameObject.activeInHierarchy);

            if (template == null)
                return;

            Transform parent = template.transform.parent;

            GameObject buttonObj = Instantiate(template.gameObject, parent);
            buttonObj.name = "RiskyStatsSettingsButton";

            HGButton button = buttonObj.GetComponent<HGButton>();
            button.onClick = new Button.ButtonClickedEvent();

            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "Risky Stats";

                RoR2.UI.LanguageTextMeshController langCtrl = label.GetComponent<RoR2.UI.LanguageTextMeshController>();
                if (langCtrl != null)
                    UnityEngine.Object.Destroy(langCtrl);

                RoR2.UI.LanguageTextMeshController buttonLangCtrl = buttonObj.GetComponent<RoR2.UI.LanguageTextMeshController>();
                if (buttonLangCtrl != null)
                    UnityEngine.Object.Destroy(buttonLangCtrl);
            }

            Canvas rootCanvas = self.GetComponentInParent<Canvas>();
            Transform canvasTransform = rootCanvas != null ? rootCanvas.transform : self.transform;

            button.onClick.AddListener(() =>
            {
                TogglePanel(canvasTransform);
                EventSystem.current.SetSelectedGameObject(null);
            });

            Debug.Log("========== CHILD ORDER ==========");

            for (int i = 0; i < parent.childCount; i++)
            {
                Debug.Log($"{i}: {parent.GetChild(i).name}");
            }

            Transform quitButton = parent.Cast<Transform>()
                .FirstOrDefault(t =>
                    t.name.IndexOf("quit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.name.IndexOf("exit", StringComparison.OrdinalIgnoreCase) >= 0);

            if (quitButton != null)
            {
                Debug.Log($"[RiskyStats] Found quit button: {quitButton.name}");

                buttonObj.transform.SetSiblingIndex(quitButton.GetSiblingIndex());

                Debug.Log($"[RiskyStats] Inserted at index {buttonObj.transform.GetSiblingIndex()}");
            }
            else
            {
                Debug.LogWarning("[RiskyStats] Could not find Quit/Exit button, putting at end.");
                buttonObj.transform.SetAsLastSibling();
            }
        }

        private static void TogglePanel(Transform canvasTransform)
        {
            rootCanvasTransform = canvasTransform;

            if (panelObject == null)
            {
                BuildBackdrop(canvasTransform);
                BuildPanel(canvasTransform);
                BuildNavButton(canvasTransform);
                return;
            }

            bool opening = !panelObject.activeSelf && (plusPanelObject == null || !plusPanelObject.activeSelf);

            if (opening)
            {
                if (backdropObject != null)
                    backdropObject.SetActive(true);
                panelObject.SetActive(true);
                if (plusPanelObject != null)
                    plusPanelObject.SetActive(false);
                if (navButtonObject != null)
                    navButtonObject.SetActive(true);
                UpdateNavButtonLabel();
                SyncThankYouMessageVisibility();
            }
            else
            {
                if (backdropObject != null)
                    backdropObject.SetActive(false);
                panelObject.SetActive(false);
                if (plusPanelObject != null)
                    plusPanelObject.SetActive(false);
                if (navButtonObject != null)
                    navButtonObject.SetActive(false);
                SyncThankYouMessageVisibility();
            }
        }

        private static void BuildBackdrop(Transform parent)
        {
            backdropObject = new GameObject("RiskyStatsBackdrop");
            backdropObject.transform.SetParent(parent, false);

            RectTransform rect = backdropObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = backdropObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.75f);
            img.raycastTarget = true;
        }

        private static void BuildNavButton(Transform parent)
        {
            navButtonObject = new GameObject("RiskyStatsNavButton");
            navButtonObject.transform.SetParent(parent, false);

            RectTransform rect = navButtonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 40f);
            rect.sizeDelta = new Vector2(140f, 36f);

            Image bg = navButtonObject.AddComponent<Image>();
            bg.color = AccentColor;

            Button button = navButtonObject.AddComponent<Button>();
            button.targetGraphic = bg;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(navButtonObject.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Next";
            label.alignment = TextAlignmentOptions.Center;
            label.color = DarkTextColor;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 16;

            button.onClick.AddListener(() =>
            {
                NavigateToggle();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        private static void NavigateToggle()
        {
            if (plusPanelObject == null)
                BuildPlusPanel(rootCanvasTransform);

            bool goingToPlus = panelObject.activeSelf;

            panelObject.SetActive(!goingToPlus);
            plusPanelObject.SetActive(goingToPlus);

            UpdateNavButtonLabel();
            SyncThankYouMessageVisibility();
        }

        private static void UpdateNavButtonLabel()
        {
            if (navButtonObject == null) return;

            TextMeshProUGUI label = navButtonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) return;

            bool onPlusPanel = plusPanelObject != null && plusPanelObject.activeSelf;
            label.text = onPlusPanel ? "Back" : "Next";
        }

        private static void SyncThankYouMessageVisibility()
        {
            if (thankYouMessageObject != null)
                thankYouMessageObject.SetActive(panelObject != null && panelObject.activeSelf);
        }

        private static void BuildPanel(Transform parent)
        {
            panelObject = new GameObject("RiskyStatsSettingsPanel");
            panelObject.transform.SetParent(parent, false);

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, 100);

            Image bg = panelObject.AddComponent<Image>();
            bg.color = BackgroundColor;

            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2, -2);

            VerticalLayoutGroup rootLayout = panelObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(18, 18, 18, 18);
            rootLayout.spacing = RowSpacing;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateTitle(panelObject.transform, "RISKY STATS SETTINGS");
            CreateDivider(panelObject.transform);

            bool alt = false;
            foreach (string key in StatOrder)
            {
                bool startValue = RSSettings.StatVisibility.ContainsKey(key) && RSSettings.StatVisibility[key];
                CreateToggleRow(panelObject.transform, key, StatLabels[key], alt, startValue,
                    value => RSSettings.SetVisibility(key, value));
                alt = !alt;
            }

            CreateDivider(panelObject.transform);

            CreateSliderRow(panelObject.transform, "Size", 14f, 48f, RSSettings.StatFontSize, RSSettings.SetFontSize);
            CreateSliderRow(panelObject.transform, "Spacing", 5f, 100f, RSSettings.StatSpacing, RSSettings.SetSpacing);

            CreateAlignmentRow(panelObject.transform);

            if (RSSettings.Alignment == StatAlignment.Vertical)
                CreateTextAlignRow(panelObject.transform);

            CreateBottomButtonsRow(panelObject.transform);

            if (RSSettings.ShowThankYouMessage)
                BuildThankYouMessage(parent);
        }

        private static void BuildThankYouMessage(Transform parent)
        {
            thankYouMessageObject = new GameObject("RiskyStatsThankYouMessage");
            thankYouMessageObject.transform.SetParent(parent, false);

            RectTransform rect = thankYouMessageObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(60f, 0f);
            rect.sizeDelta = new Vector2(380f, 10f);

            Image bg = thankYouMessageObject.AddComponent<Image>();
            bg.color = ThankYouBackgroundColor;

            Outline outline = thankYouMessageObject.AddComponent<Outline>();
            outline.effectColor = ThankYouOutlineColor;
            outline.effectDistance = new Vector2(2, -2);

            VerticalLayoutGroup layout = thankYouMessageObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = thankYouMessageObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textObj = new GameObject("MessageText");
            textObj.transform.SetParent(thankYouMessageObject.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Dear User,\n\nThis mod was originally a random personal project made just for fun. When I decided to publish it, I never expected it to reach more than a few downloads, let alone 1,000+ downloads.\nI am honored that you took the time to download and use this passion project. Thank you for being part of the journey.\n\n- A Software Engineer";
            text.fontSize = 15;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;

            LayoutElement textLayoutElement = textObj.AddComponent<LayoutElement>();
            textLayoutElement.preferredWidth = 348;
            textLayoutElement.flexibleWidth = 0;

            GameObject dismissObj = new GameObject("DismissButton");
            dismissObj.transform.SetParent(thankYouMessageObject.transform, false);

            LayoutElement dismissLayoutElement = dismissObj.AddComponent<LayoutElement>();
            dismissLayoutElement.preferredHeight = 30;
            dismissLayoutElement.minHeight = 30;
            dismissLayoutElement.preferredWidth = 100;
            dismissLayoutElement.minWidth = 100;
            dismissLayoutElement.flexibleWidth = 0;

            Image dismissBg = dismissObj.AddComponent<Image>();
            dismissBg.color = ThankYouDismissColor;

            Button dismissButton = dismissObj.AddComponent<Button>();
            dismissButton.targetGraphic = dismissBg;

            GameObject dismissLabelObj = new GameObject("Label");
            dismissLabelObj.transform.SetParent(dismissObj.transform, false);
            RectTransform dismissLabelRect = dismissLabelObj.AddComponent<RectTransform>();
            dismissLabelRect.anchorMin = Vector2.zero;
            dismissLabelRect.anchorMax = Vector2.one;
            dismissLabelRect.offsetMin = Vector2.zero;
            dismissLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI dismissLabel = dismissLabelObj.AddComponent<TextMeshProUGUI>();
            dismissLabel.text = "Dismiss";
            dismissLabel.alignment = TextAlignmentOptions.Center;
            dismissLabel.color = Color.white;
            dismissLabel.fontStyle = FontStyles.Bold;
            dismissLabel.fontSize = 15;

            dismissButton.onClick.AddListener(() =>
            {
                RSSettings.DismissThankYouMessage();

                if (thankYouMessageObject != null)
                {
                    DestroyImmediate(thankYouMessageObject);
                    thankYouMessageObject = null;
                }

                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        private static void BuildPlusPanel(Transform parent)
        {
            plusPanelObject = new GameObject("RiskyStatsPlusSettingsPanel");
            plusPanelObject.transform.SetParent(parent, false);

            RectTransform panelRect = plusPanelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, 100);

            Image bg = plusPanelObject.AddComponent<Image>();
            bg.color = BackgroundColor;

            Outline outline = plusPanelObject.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2, -2);

            VerticalLayoutGroup rootLayout = plusPanelObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(18, 18, 18, 18);
            rootLayout.spacing = RowSpacing;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = plusPanelObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateTitle(plusPanelObject.transform, "RISKY STATS+ SETTINGS");
            CreateDivider(plusPanelObject.transform);

            bool alt = false;
            foreach (string key in PlusStatOrder)
            {
                bool startValue = RSPlusSettings.StatVisibility.ContainsKey(key) && RSPlusSettings.StatVisibility[key];
                CreateToggleRow(plusPanelObject.transform, key, PlusStatLabels[key], alt, startValue,
                    value => RSPlusSettings.SetVisibility(key, value));
                alt = !alt;
            }

            CreateDivider(plusPanelObject.transform);

            CreateSliderRow(plusPanelObject.transform, "Size", 14f, 48f, RSPlusSettings.StatFontSize, RSPlusSettings.SetFontSize);
            CreateSliderRow(plusPanelObject.transform, "Spacing", 5f, 100f, RSPlusSettings.StatSpacing, RSPlusSettings.SetSpacing);

            GameObject bottomSpacer = new GameObject("BottomSpacer");
            bottomSpacer.transform.SetParent(plusPanelObject.transform, false);
            LayoutElement bottomSpacerLE = bottomSpacer.AddComponent<LayoutElement>();
            bottomSpacerLE.minHeight = 70;
            bottomSpacerLE.preferredHeight = 70;
            bottomSpacerLE.flexibleWidth = 1;

            CreatePlusBottomButtonsRow(plusPanelObject.transform);

            plusPanelObject.SetActive(false);
        }

        private static void CreateTitle(Transform parent, string titleText)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = titleText;
            title.fontSize = 24;
            title.color = AccentColor;
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            LayoutElement le = titleObj.AddComponent<LayoutElement>();
            le.minHeight = 30;
            le.preferredHeight = 30;
            le.flexibleWidth = 1;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject line = new GameObject("Divider");
            line.transform.SetParent(parent, false);

            Image img = line.AddComponent<Image>();
            img.color = new Color(BorderColor.r, BorderColor.g, BorderColor.b, 0.35f);

            LayoutElement le = line.AddComponent<LayoutElement>();
            le.minHeight = 2;
            le.preferredHeight = 2;
            le.flexibleWidth = 1;
        }

        private static void CreateToggleRow(Transform parent, string key, string labelText, bool alt, bool startValue, Action<bool> onChanged)
        {
            GameObject row = new GameObject("Row_" + key);
            row.transform.SetParent(parent, false);

            LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = RowHeight;
            rowLayoutElement.preferredHeight = RowHeight;
            rowLayoutElement.flexibleWidth = 1;

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 4, 4);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.spacing = 10;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            Image rowBg = row.AddComponent<Image>();
            rowBg.color = alt ? RowColorAlt : RowColor;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 17;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement labelLayoutElement = labelObj.AddComponent<LayoutElement>();
            labelLayoutElement.flexibleWidth = 1;

            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(row.transform, false);

            toggleObj.AddComponent<RectTransform>();

            LayoutElement toggleLayoutElement = toggleObj.AddComponent<LayoutElement>();
            toggleLayoutElement.preferredWidth = 46;
            toggleLayoutElement.minWidth = 46;
            toggleLayoutElement.preferredHeight = 22;
            toggleLayoutElement.minHeight = 22;
            toggleLayoutElement.flexibleWidth = 0;

            Image toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = startValue ? AccentColor : OffColor;

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = toggleBg;
            toggle.isOn = startValue;
            toggle.onValueChanged.AddListener(value =>
            {
                toggleBg.color = value ? AccentColor : OffColor;
                onChanged(value);
            });
        }

        private static void CreateSliderRow(Transform parent, string labelText, float min, float max, float startValue, Action<float> onChanged)
        {
            GameObject row = new GameObject("SliderRow_" + labelText);
            row.transform.SetParent(parent, false);

            LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = 44;
            rowLayoutElement.preferredHeight = 44;
            rowLayoutElement.flexibleWidth = 1;

            VerticalLayoutGroup rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 4;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText + ": " + startValue.ToString("0");
            label.fontSize = 15;
            label.color = SubTextColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement labelLayoutElement = labelObj.AddComponent<LayoutElement>();
            labelLayoutElement.minHeight = 18;
            labelLayoutElement.preferredHeight = 18;

            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);

            LayoutElement sliderLayoutElement = sliderObj.AddComponent<LayoutElement>();
            sliderLayoutElement.minHeight = 20;
            sliderLayoutElement.preferredHeight = 20;

            sliderObj.AddComponent<RectTransform>();

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.direction = Slider.Direction.LeftToRight;

            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = RowColor;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.sizeDelta = new Vector2(10, 0);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = AccentColor;

            slider.fillRect = fillRect;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14, 22);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            slider.value = startValue;

            slider.onValueChanged.AddListener(value =>
            {
                label.text = labelText + ": " + value.ToString("0");
                onChanged(value);
            });
        }

        private static void CreateAlignmentRow(Transform parent)
        {
            GameObject row = new GameObject("AlignmentRow");
            row.transform.SetParent(parent, false);

            LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = 36;
            rowLayoutElement.preferredHeight = 36;
            rowLayoutElement.flexibleWidth = 1;

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 4, 4);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.spacing = 10;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            Image rowBg = row.AddComponent<Image>();
            rowBg.color = RowColor;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Alignment";
            label.fontSize = 17;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement labelLayoutElement = labelObj.AddComponent<LayoutElement>();
            labelLayoutElement.flexibleWidth = 1;

            GameObject buttonObj = new GameObject("AlignmentButton");
            buttonObj.transform.SetParent(row.transform, false);

            buttonObj.AddComponent<RectTransform>();

            LayoutElement buttonLayoutElement = buttonObj.AddComponent<LayoutElement>();
            buttonLayoutElement.preferredWidth = 130;
            buttonLayoutElement.minWidth = 130;
            buttonLayoutElement.preferredHeight = 26;
            buttonLayoutElement.minHeight = 26;
            buttonLayoutElement.flexibleWidth = 0;

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = AccentColor;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBg;

            GameObject buttonLabelObj = new GameObject("Label");
            buttonLabelObj.transform.SetParent(buttonObj.transform, false);
            RectTransform buttonLabelRect = buttonLabelObj.AddComponent<RectTransform>();
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = Vector2.zero;
            buttonLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonLabel = buttonLabelObj.AddComponent<TextMeshProUGUI>();
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = DarkTextColor;
            buttonLabel.fontSize = 15;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.text = RSSettings.Alignment == StatAlignment.Horizontal ? "Horizontal" : "Vertical";

            button.onClick.AddListener(() =>
            {
                StatAlignment newAlignment = RSSettings.Alignment == StatAlignment.Horizontal
                    ? StatAlignment.Vertical
                    : StatAlignment.Horizontal;

                RSSettings.SetAlignment(newAlignment);

                if (thankYouMessageObject != null)
                {
                    DestroyImmediate(thankYouMessageObject);
                    thankYouMessageObject = null;
                }

                Transform panelParent = panelObject.transform.parent;
                DestroyImmediate(panelObject);
                BuildPanel(panelParent);

                SyncThankYouMessageVisibility();
            });
        }

        private static void CreateTextAlignRow(Transform parent)
        {
            GameObject row = new GameObject("TextAlignRow");
            row.transform.SetParent(parent, false);

            LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = 36;
            rowLayoutElement.preferredHeight = 36;
            rowLayoutElement.flexibleWidth = 1;

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 4, 4);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.spacing = 10;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            Image rowBg = row.AddComponent<Image>();
            rowBg.color = RowColor;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Text Align";
            label.fontSize = 17;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement labelLayoutElement = labelObj.AddComponent<LayoutElement>();
            labelLayoutElement.flexibleWidth = 1;

            GameObject buttonObj = new GameObject("TextAlignButton");
            buttonObj.transform.SetParent(row.transform, false);

            buttonObj.AddComponent<RectTransform>();

            LayoutElement buttonLayoutElement = buttonObj.AddComponent<LayoutElement>();
            buttonLayoutElement.preferredWidth = 130;
            buttonLayoutElement.minWidth = 130;
            buttonLayoutElement.preferredHeight = 26;
            buttonLayoutElement.minHeight = 26;
            buttonLayoutElement.flexibleWidth = 0;

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = AccentColor;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBg;

            GameObject buttonLabelObj = new GameObject("Label");
            buttonLabelObj.transform.SetParent(buttonObj.transform, false);
            RectTransform buttonLabelRect = buttonLabelObj.AddComponent<RectTransform>();
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = Vector2.zero;
            buttonLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonLabel = buttonLabelObj.AddComponent<TextMeshProUGUI>();
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = DarkTextColor;
            buttonLabel.fontSize = 15;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.text = RSSettings.TextAlign == StatTextAlign.Center ? "Center" : "Left";

            button.onClick.AddListener(() =>
            {
                StatTextAlign newTextAlign = RSSettings.TextAlign == StatTextAlign.Center
                    ? StatTextAlign.Left
                    : StatTextAlign.Center;

                RSSettings.SetTextAlign(newTextAlign);
                buttonLabel.text = newTextAlign == StatTextAlign.Center ? "Center" : "Left";
            });
        }

        private static readonly Color ResetColor = new Color(0.4f, 0.4f, 0.45f, 1f);

        private static void CreateBottomButtonsRow(Transform parent)
        {
            GameObject wrapper = new GameObject("BottomButtonsWrapper");
            wrapper.transform.SetParent(parent, false);

            LayoutElement wrapperLayoutElement = wrapper.AddComponent<LayoutElement>();
            wrapperLayoutElement.minHeight = 40;
            wrapperLayoutElement.preferredHeight = 40;
            wrapperLayoutElement.flexibleWidth = 1;

            HorizontalLayoutGroup wrapperLayout = wrapper.AddComponent<HorizontalLayoutGroup>();
            wrapperLayout.spacing = 12;
            wrapperLayout.childAlignment = TextAnchor.MiddleCenter;
            wrapperLayout.childControlWidth = true;
            wrapperLayout.childControlHeight = true;
            wrapperLayout.childForceExpandWidth = false;
            wrapperLayout.childForceExpandHeight = false;

            CreateBottomButton(wrapper.transform, "Reset to Default", ResetColor, Color.white, () =>
            {
                RSSettings.ResetToDefault();

                if (thankYouMessageObject != null)
                {
                    DestroyImmediate(thankYouMessageObject);
                    thankYouMessageObject = null;
                }

                Transform panelParent = panelObject.transform.parent;
                DestroyImmediate(panelObject);
                BuildPanel(panelParent);

                if (plusPanelObject != null)
                {
                    DestroyImmediate(plusPanelObject);
                    plusPanelObject = null;
                }

                UpdateNavButtonLabel();
                SyncThankYouMessageVisibility();
            });

            CreateBottomButton(wrapper.transform, "Close", AccentColor, DarkTextColor, () =>
            {
                panelObject.SetActive(false);
                if (navButtonObject != null)
                    navButtonObject.SetActive(false);
                if (backdropObject != null)
                    backdropObject.SetActive(false);
                SyncThankYouMessageVisibility();
            });
        }

        private static void CreatePlusBottomButtonsRow(Transform parent)
        {
            GameObject wrapper = new GameObject("BottomButtonsWrapper");
            wrapper.transform.SetParent(parent, false);

            LayoutElement wrapperLayoutElement = wrapper.AddComponent<LayoutElement>();
            wrapperLayoutElement.minHeight = 40;
            wrapperLayoutElement.preferredHeight = 40;
            wrapperLayoutElement.flexibleWidth = 1;

            HorizontalLayoutGroup wrapperLayout = wrapper.AddComponent<HorizontalLayoutGroup>();
            wrapperLayout.spacing = 12;
            wrapperLayout.childAlignment = TextAnchor.MiddleCenter;
            wrapperLayout.childControlWidth = true;
            wrapperLayout.childControlHeight = true;
            wrapperLayout.childForceExpandWidth = false;
            wrapperLayout.childForceExpandHeight = false;

            CreateBottomButton(wrapper.transform, "Reset to Default", ResetColor, Color.white, () =>
            {
                RSSettings.ResetToDefault();

                if (thankYouMessageObject != null)
                {
                    DestroyImmediate(thankYouMessageObject);
                    thankYouMessageObject = null;
                }

                Transform panelParent = panelObject.transform.parent;
                DestroyImmediate(panelObject);
                BuildPanel(panelParent);

                DestroyImmediate(plusPanelObject);
                plusPanelObject = null;

                UpdateNavButtonLabel();
                SyncThankYouMessageVisibility();
            });

            CreateBottomButton(wrapper.transform, "Close", AccentColor, DarkTextColor, () =>
            {
                plusPanelObject.SetActive(false);
                if (navButtonObject != null)
                    navButtonObject.SetActive(false);
                if (backdropObject != null)
                    backdropObject.SetActive(false);
                SyncThankYouMessageVisibility();
            });
        }

        private static void CreateBottomButton(Transform parent, string text, Color bgColor, Color textColor, Action onClick)
        {
            GameObject buttonObj = new GameObject(text.Replace(" ", "") + "Button");
            buttonObj.transform.SetParent(parent, false);

            LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 34;
            layoutElement.minHeight = 34;
            layoutElement.preferredWidth = 140;
            layoutElement.minWidth = 140;
            layoutElement.flexibleWidth = 0;

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = bgColor;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBg;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(buttonObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 15;

            button.onClick.AddListener(() =>
            {
                onClick();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }
    }
}