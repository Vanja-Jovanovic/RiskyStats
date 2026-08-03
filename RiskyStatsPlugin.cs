using BepInEx;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RiskyStats
{
    [BepInPlugin("com.shadowblade.riskystats", "Risky Stats", "1.2.0")]
    public class RiskyStatsPlugin : BaseUnityPlugin
    {
        private const string CurrentConfigVersion = "1.2.0";

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

        // ---------------- Risky Stats+ ----------------

        private GameObject statsPlusObject;
        private VerticalLayoutGroup plusLayoutGroup;

        private readonly Dictionary<string, TextMeshProUGUI> plusStatTexts = new Dictionary<string, TextMeshProUGUI>();
        private readonly Dictionary<string, GameObject> plusStatObjects = new Dictionary<string, GameObject>();

        private readonly string[] plusStatKeys = new string[]
        {
            "Jumps", "MountainShrines", "Drones", "Luck", "Kills"
        };

        private int mountainShrinesActivated;
        private int monstersKilled;

        // ------------------------------------------------

        private void Awake()
        {
            Logger.LogInfo("Risky Stats loaded!");

            MigrateConfigIfNeeded();

            RSSettings.Init(Config);
            gameObject.AddComponent<RSSettingsUI>();

            RSPlusSettings.Init(Config);

            ProgressSettings.Init(Config);
            gameObject.AddComponent<RunProgressUI>();

            GlobalEventManager.onServerDamageDealt += DamageDealt;
            On.RoR2.HealthComponent.TakeDamage += DamageTaken;
            On.RoR2.HealthComponent.Heal += Healing;

            On.RoR2.PurchaseInteraction.OnInteractionBegin += ShrineActivated;
            GlobalEventManager.onCharacterDeathGlobal += MonsterKilled;
            Run.onRunStartGlobal += OnRunStart;

            RSSettings.OnSettingsChanged += RefreshUI;
            RSPlusSettings.OnSettingsChanged += RefreshPlusUI;
        }

        private void MigrateConfigIfNeeded()
        {
            BepInEx.Configuration.ConfigEntry<string> configVersion = Config.Bind(
                "Internal",
                "Config Version",
                "0.0.0",
                "Do not edit. Used internally to migrate settings between updates."
            );

            if (configVersion.Value == CurrentConfigVersion)
                return;

            Logger.LogInfo($"[RiskyStats] Migrating config from {configVersion.Value} to {CurrentConfigVersion}.");
            BepInEx.Configuration.ConfigEntry<KeyCode> toggleKeyEntry = Config.Bind(
                "General", "Toggle Key", KeyCode.V, "Key used to show/hide the stats panel");
            BepInEx.Configuration.ConfigEntry<KeyCode> progressKeyEntry = Config.Bind(
                "General", "Progress Toggle Key", KeyCode.B, "Key to hold to show the run progress panel");

            if (toggleKeyEntry.Value == KeyCode.None)
                toggleKeyEntry.Value = KeyCode.V;

            if (progressKeyEntry.Value == KeyCode.None)
                progressKeyEntry.Value = KeyCode.B;

            configVersion.Value = CurrentConfigVersion;

            Config.Save();

            Logger.LogInfo("[RiskyStats] Config migration complete.");
        }

        private void OnDestroy()
        {
            RSSettings.OnSettingsChanged -= RefreshUI;
            RSPlusSettings.OnSettingsChanged -= RefreshPlusUI;

            On.RoR2.PurchaseInteraction.OnInteractionBegin -= ShrineActivated;
            GlobalEventManager.onCharacterDeathGlobal -= MonsterKilled;
            Run.onRunStartGlobal -= OnRunStart;
        }

        private void Update()
        {
            if (BepInEx.UnityInput.Current.GetKeyDown(RSSettings.ToggleKey))
            {
                if (statsObject != null)
                {
                    bool newState = !statsObject.activeSelf;
                    statsObject.SetActive(newState);

                    if (statsPlusObject != null)
                        statsPlusObject.SetActive(newState);
                }
            }

            HUD hud = null;
            if (statsObject == null || statsPlusObject == null)
                hud = FindObjectOfType<HUD>();

            if (statsObject == null && hud != null)
                CreateUI(hud);

            if (statsPlusObject == null && hud != null)
                CreatePlusUI(hud);

            if (statsObject != null && statsObject.activeSelf)
                UpdateStats();

            if (statsPlusObject != null)
                UpdatePlusStats();
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

        // ---------------- Risky Stats+ ----------------

        private void CreatePlusUI(HUD hud)
        {
            statsPlusObject = new GameObject("RiskyStatsPlusPanel");
            statsPlusObject.transform.SetParent(hud.mainContainer.transform, false);

            RectTransform rect = statsPlusObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);

            rect.anchoredPosition = new Vector2(-30, 230);
            rect.sizeDelta = new Vector2(400, 130);

            BuildPlusUI();
            ApplyPlusSettings();

            if (statsObject != null)
                statsPlusObject.SetActive(statsObject.activeSelf);
        }

        private void BuildPlusUI()
        {
            if (plusLayoutGroup != null)
            {
                DestroyImmediate(plusLayoutGroup);
                plusLayoutGroup = null;
            }

            List<Transform> existingChildren = new List<Transform>();
            foreach (Transform child in statsPlusObject.transform)
                existingChildren.Add(child);

            foreach (Transform child in existingChildren)
                DestroyImmediate(child.gameObject);

            plusStatTexts.Clear();
            plusStatObjects.Clear();

            plusLayoutGroup = statsPlusObject.AddComponent<VerticalLayoutGroup>();
            plusLayoutGroup.spacing = RSPlusSettings.StatSpacing;
            plusLayoutGroup.childAlignment = TextAnchor.LowerRight;
            plusLayoutGroup.childControlWidth = true;
            plusLayoutGroup.childForceExpandWidth = false;
            plusLayoutGroup.childControlHeight = true;
            plusLayoutGroup.childForceExpandHeight = false;

            foreach (string key in plusStatKeys)
            {
                TextMeshProUGUI text = CreateText(GetPlusStatLabel(key) + ": 0", statsPlusObject.transform);
                text.alignment = TextAlignmentOptions.MidlineRight;
                plusStatTexts[key] = text;
                plusStatObjects[key] = text.gameObject;
            }

            ContentSizeFitter fitter = statsPlusObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = statsPlusObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RefreshPlusUI()
        {
            if (statsPlusObject == null) return;

            plusLayoutGroup.spacing = RSPlusSettings.StatSpacing;
            ApplyPlusSettings();
        }

        private void ApplyPlusSettings()
        {
            foreach (var kv in plusStatTexts)
                kv.Value.fontSize = RSPlusSettings.StatFontSize;

            foreach (string key in plusStatKeys)
            {
                bool visible = RSPlusSettings.StatVisibility.ContainsKey(key) && RSPlusSettings.StatVisibility[key];
                if (plusStatObjects.TryGetValue(key, out GameObject obj))
                    obj.SetActive(visible);
            }
        }

        private void UpdatePlusStats()
        {
            CharacterBody body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (body == null) return;

            if (plusStatObjects["Jumps"].activeSelf)
            {
                int usedJumps = body.characterMotor != null ? body.characterMotor.jumpCount : 0;
                plusStatTexts["Jumps"].text = $"Jumps: <color=#90EE90>{body.maxJumpCount}/{usedJumps}</color>";
            }

            if (plusStatObjects["MountainShrines"].activeSelf)
                plusStatTexts["MountainShrines"].text = $"Mountain Shrines: <color=#E0FFFF>{mountainShrinesActivated}</color>";

            if (plusStatObjects["Drones"].activeSelf)
                plusStatTexts["Drones"].text = $"Drones: <color=#00FF00>{CountAliveDrones(body)}</color>";

            if (plusStatObjects["Luck"].activeSelf)
            {
                int luck = body.inventory != null ? body.inventory.GetItemCount(RoR2Content.Items.Clover) : 0;
                plusStatTexts["Luck"].text = $"Luck: <color=#FF69B4>{luck}</color>";
            }

            if (plusStatObjects["Kills"].activeSelf)
                plusStatTexts["Kills"].text = $"Kills: <color=#8B0000>{monstersKilled}</color>";
        }

        private int CountAliveDrones(CharacterBody player)
        {
            if (player == null || player.master == null) return 0;

            int count = 0;
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body == null || body.master == null) continue;
                if (body.master.minionOwnership == null) continue;
                if (body.master.minionOwnership.ownerMaster != player.master) continue;
                if (body.master.name.IndexOf("Drone", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (body.healthComponent == null || !body.healthComponent.alive) continue;

                count++;
            }

            return count;
        }

        private string GetPlusStatLabel(string key)
        {
            if (key == "MountainShrines") return "Mountain Shrines";
            return key;
        }

        private void ShrineActivated(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            bool wasAvailable = self.available;

            orig(self, activator);

            if (activator == null || !wasAvailable) return;

            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (player == null || activatorBody != player) return;

            if (self.GetComponent<ShrineBossBehavior>() != null)
                mountainShrinesActivated++;
        }

        private void MonsterKilled(DamageReport report)
        {
            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (player == null || report.attackerBody != player) return;
            if (report.victimBody == null) return;

            TeamComponent victimTeam = report.victimBody.teamComponent;
            if (victimTeam != null && victimTeam.teamIndex == TeamIndex.Monster)
                monstersKilled++;
        }

        private void OnRunStart(Run run)
        {
            mountainShrinesActivated = 0;
            monstersKilled = 0;
        }
    }
}