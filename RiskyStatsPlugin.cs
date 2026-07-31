using BepInEx;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RiskyStats
{
    [BepInPlugin("com.shadowblade.riskystats", "Risky Stats", "1.0.0")]
    public class RiskyStatsPlugin : BaseUnityPlugin
    {
        private GameObject statsObject;
        private HorizontalOrVerticalLayoutGroup mainLayoutGroup;
        private GameObject armorCritContainer;

        private readonly Dictionary<string, TextMeshProUGUI> statTexts = new Dictionary<string, TextMeshProUGUI>();
        private readonly Dictionary<string, GameObject> statObjects = new Dictionary<string, GameObject>();

        private readonly string[] statKeys = new string[]
        {
            "AttackSpeed", "Armor", "Crit", "Healing", "Damage",
            "Streak", "DamageTaken", "DamageTakenStreak", "Speed"
        };

        private readonly string[] containerStats = new string[] { "AttackSpeed", "Armor", "Crit" };

        private float lastDamage;
        private float lastDamageTaken;
        private float damageStreak;
        private float damageTakenStreak;
        private float healingStreak;
        private bool lastDamageCrit;
        private float lastDamageTime;
        private float lastTakenTime;
        private float lastHealingTime;

        private void Awake()
        {
            Logger.LogInfo("Risky Stats loaded!");
            RSSettings.Init(Config);
            gameObject.AddComponent<RSSettingsUI>();

            GlobalEventManager.onServerDamageDealt += DamageDealt;
            On.RoR2.HealthComponent.TakeDamage += DamageTaken;
            On.RoR2.HealthComponent.Heal += Healing;

            RSSettings.OnSettingsChanged += RefreshUI;
        }

        private void OnDestroy()
        {
            RSSettings.OnSettingsChanged -= RefreshUI;
        }

        private void Update()
        {
            if (BepInEx.UnityInput.Current.GetKeyDown(RSSettings.ToggleKey))
            {
                if (statsObject != null)
                    statsObject.SetActive(!statsObject.activeSelf);
            }

            if (statsObject == null)
            {
                HUD hud = FindObjectOfType<HUD>();
                if (hud != null)
                    CreateUI(hud);
            }

            if (statsObject != null && statsObject.activeSelf)
                UpdateStats();
        }

        private void CreateUI(HUD hud)
        {
            statsObject = new GameObject("RiskyStatsPanel");
            statsObject.transform.SetParent(hud.mainContainer.transform, false);

            RectTransform rect = statsObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -160);
            rect.sizeDelta = new Vector2(1700, 130);

            BuildUI();
            ApplySettings();
        }

        private void BuildUI()
        {
            if (mainLayoutGroup != null)
            {
                DestroyImmediate(mainLayoutGroup);
                mainLayoutGroup = null;
            }

            List<Transform> existingChildren = new List<Transform>();
            foreach (Transform child in statsObject.transform)
                existingChildren.Add(child);

            foreach (Transform child in existingChildren)
                DestroyImmediate(child.gameObject);

            armorCritContainer = null;

            statTexts.Clear();
            statObjects.Clear();

            if (RSSettings.Alignment == StatAlignment.Horizontal)
                mainLayoutGroup = statsObject.AddComponent<HorizontalLayoutGroup>();
            else
                mainLayoutGroup = statsObject.AddComponent<VerticalLayoutGroup>();

            mainLayoutGroup.spacing = RSSettings.StatSpacing;
            mainLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            mainLayoutGroup.childControlWidth = true;
            mainLayoutGroup.childForceExpandWidth = false;
            mainLayoutGroup.childControlHeight = true;
            mainLayoutGroup.childForceExpandHeight = false;

            if (RSSettings.Alignment == StatAlignment.Horizontal)
            {
                armorCritContainer = new GameObject("ArmorCritContainer");
                armorCritContainer.transform.SetParent(statsObject.transform, false);
                VerticalLayoutGroup containerLayout = armorCritContainer.AddComponent<VerticalLayoutGroup>();
                containerLayout.spacing = 4;
                containerLayout.childAlignment = TextAnchor.MiddleCenter;
                containerLayout.childControlWidth = true;
                containerLayout.childControlHeight = true;
                containerLayout.childForceExpandWidth = false;
                containerLayout.childForceExpandHeight = false;

                LayoutElement containerLE = armorCritContainer.AddComponent<LayoutElement>();
                containerLE.minWidth = 0;
                containerLE.preferredWidth = -1;
            }

            foreach (string key in statKeys)
            {
                Transform parent;
                if (RSSettings.Alignment == StatAlignment.Horizontal && System.Array.Exists(containerStats, s => s == key))
                    parent = armorCritContainer.transform;
                else
                    parent = statsObject.transform;

                TextMeshProUGUI text = CreateText(GetStatLabel(key) + ": 0", parent);
                statTexts[key] = text;
                statObjects[key] = text.gameObject;
            }

            ContentSizeFitter fitter = statsObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = statsObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private TextMeshProUGUI CreateText(string initialText, Transform parent)
        {
            GameObject obj = new GameObject("StatText");
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.text = initialText;
            text.fontSize = RSSettings.StatFontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;

            LayoutElement layoutElement = obj.AddComponent<LayoutElement>();
            layoutElement.minWidth = 0;
            layoutElement.preferredWidth = -1;
            layoutElement.flexibleWidth = 1;

            return text;
        }

        private void RefreshUI()
        {
            if (statsObject == null) return;

            bool alignmentChanged = (RSSettings.Alignment == StatAlignment.Horizontal && mainLayoutGroup is VerticalLayoutGroup) ||
                                    (RSSettings.Alignment == StatAlignment.Vertical && mainLayoutGroup is HorizontalLayoutGroup);

            if (alignmentChanged)
                BuildUI();
            else
                mainLayoutGroup.spacing = RSSettings.StatSpacing;

            ApplySettings();
        }

        private void ApplySettings()
        {
            TextAlignmentOptions textAlignment = (RSSettings.Alignment == StatAlignment.Vertical && RSSettings.TextAlign == StatTextAlign.Left)
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.Center;

            foreach (var kv in statTexts)
            {
                kv.Value.fontSize = RSSettings.StatFontSize;
                kv.Value.alignment = textAlignment;
            }

            foreach (string key in statKeys)
            {
                bool visible = RSSettings.StatVisibility.ContainsKey(key) && RSSettings.StatVisibility[key];
                if (statObjects.TryGetValue(key, out GameObject obj))
                    obj.SetActive(visible);
            }
        }

        private void UpdateStats()
        {
            CharacterBody body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (body == null) return;

            if (Time.time - lastDamageTime > 5) damageStreak = 0;
            if (Time.time - lastTakenTime > 5) damageTakenStreak = 0;
            if (Time.time - lastHealingTime > 3) healingStreak = 0;

            if (statObjects["Armor"].activeSelf)
                statTexts["Armor"].text = $"Armor: <color=#4A90E2>{body.armor:0}</color>";

            if (statObjects["Healing"].activeSelf)
                statTexts["Healing"].text = $"Healing: <color=#90EE90>{FormatNumber(healingStreak)}</color>";

            if (statObjects["Damage"].activeSelf)
                statTexts["Damage"].text = $"Damage: <color=#{(lastDamageCrit ? "FF0000" : "FFFFFF")}>{FormatNumber(lastDamage)}</color>";

            if (statObjects["Streak"].activeSelf)
                statTexts["Streak"].text = $"Streak: <color=#FFFF00>{FormatNumber(damageStreak)}</color>";

            if (statObjects["DamageTaken"].activeSelf)
                statTexts["DamageTaken"].text = $"Damage Taken: <color=#FF0000>{FormatNumber(lastDamageTaken)}</color>";

            if (statObjects["DamageTakenStreak"].activeSelf)
                statTexts["DamageTakenStreak"].text = $"Damage Taken Streak: <color=#FF0000>{FormatNumber(damageTakenStreak)}</color>";

            if (statObjects["Speed"].activeSelf)
                statTexts["Speed"].text = $"Speed: <color=#00FFFF>{body.characterMotor.velocity.magnitude:0.0} m/s</color>";

            if (statObjects["AttackSpeed"].activeSelf)
                statTexts["AttackSpeed"].text = $"Attack Speed: <color=#90EE90>{body.attackSpeed:0.0}</color>";

            if (statObjects["Crit"].activeSelf)
                statTexts["Crit"].text = $"Crit: <color=#8B0000>{body.crit:0}%</color>";
        }

        private void DamageDealt(DamageReport report)
        {
            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (player == null || report.attackerBody != player) return;
            lastDamage = report.damageDealt;
            lastDamageCrit = report.damageInfo.crit;
            damageStreak += report.damageDealt;
            lastDamageTime = Time.time;
        }

        private void DamageTaken(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo info)
        {
            orig(self, info);
            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (player == null || self.body != player) return;
            lastDamageTaken = info.damage;
            damageTakenStreak += info.damage;
            lastTakenTime = Time.time;
        }

        private float Healing(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
        {
            float result = orig(self, amount, procChainMask, nonRegen);
            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (player != null && self.body == player)
            {
                healingStreak += result;
                lastHealingTime = Time.time;
            }
            return result;
        }

        private string FormatNumber(float number)
        {
            if (number >= 1e12f) return (number / 1e12f).ToString("0.0") + "t";
            if (number >= 1e9f) return (number / 1e9f).ToString("0.0") + "b";
            if (number >= 1e6f) return (number / 1e6f).ToString("0.0") + "m";
            if (number >= 1e3f) return (number / 1e3f).ToString("0.0") + "k";
            return number.ToString("0");
        }

        private string GetStatLabel(string key)
        {
            if (key == "AttackSpeed") return "Attack Speed";
            if (key == "DamageTaken") return "Damage Taken";
            if (key == "DamageTakenStreak") return "Damage Taken Streak";
            return key;
        }
    }
}