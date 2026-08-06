using BepInEx.Configuration;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskyStats
{
    public static class ProgressSettings
    {
        public static KeyCode ToggleKey = KeyCode.B;
        private static ConfigEntry<KeyCode> toggleKeyEntry;

        public static void Init(ConfigFile config)
        {
            toggleKeyEntry = config.Bind(
                "General",
                "Progress Toggle Key",
                KeyCode.B,
                "Key to hold to show the run progress panel"
            );

            ToggleKey = toggleKeyEntry.Value;
        }
    }

    public static class RunProgressStats
    {
        public static float TotalDamage;
        public static float TotalDamageTaken;
        public static float TotalHealing;
        public static float MaxSpeed;
        public static float BiggestHit;
        public static bool BiggestHitCrit;
        public static float TotalDistance;

        public static int ChestsOpened;
        public static float GoldCollected;
        public static int DronesUsed;

        private static bool hooked;
        private static Vector3 lastPosition;
        private static bool hasLastPosition;

        public static void Init()
        {
            if (hooked) return;
            hooked = true;

            GlobalEventManager.onServerDamageDealt += OnDamageDealt;
            On.RoR2.HealthComponent.TakeDamage += OnDamageTaken;
            On.RoR2.HealthComponent.Heal += OnHealing;
            Run.onRunStartGlobal += OnRunStart;

            On.RoR2.CharacterMaster.GiveMoney += OnGiveMoney;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += OnPurchaseInteractionBegin;
        }

        private static void OnRunStart(Run run)
        {
            ResetStats();
        }

        public static void ResetStats()
        {
            TotalDamage = 0f;
            TotalDamageTaken = 0f;
            TotalHealing = 0f;
            MaxSpeed = 0f;
            BiggestHit = 0f;
            BiggestHitCrit = false;
            TotalDistance = 0f;
            hasLastPosition = false;

            ChestsOpened = 0;
            GoldCollected = 0f;
            DronesUsed = 0;
        }

        private static bool IsLocalPlayerBody(CharacterBody body)
        {
            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            return player != null && body == player;
        }

        private static bool IsLocalPlayerMaster(CharacterMaster master)
        {
            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            return player != null && master != null && master == player.master;
        }

        private static void OnDamageDealt(DamageReport report)
        {
            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (player == null || report.attackerBody != player) return;

            TotalDamage += report.damageDealt;

            if (report.damageDealt > BiggestHit)
            {
                BiggestHit = report.damageDealt;
                BiggestHitCrit = report.damageInfo.crit;
            }
        }

        private static void OnDamageTaken(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo info)
        {
            orig(self, info);

            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (player == null || self.body != player) return;

            TotalDamageTaken += info.damage;
        }

        private static float OnHealing(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
        {
            float result = orig(self, amount, procChainMask, nonRegen);

            CharacterBody player = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (player != null && self.body == player)
                TotalHealing += result;

            return result;
        }

        private static void OnGiveMoney(On.RoR2.CharacterMaster.orig_GiveMoney orig, CharacterMaster self, uint amount)
        {
            orig(self, amount);

            if (IsLocalPlayerMaster(self))
                GoldCollected += amount;
        }

        private static void OnPurchaseInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            bool wasAvailable = self.available;

            orig(self, activator);

            if (activator == null) return;
            if (!wasAvailable) return;

            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (!IsLocalPlayerBody(activatorBody)) return;

            if (self.GetComponent<ChestBehavior>() != null)
            {
                ChestsOpened++;
                return;
            }

            SummonMasterBehavior summonBehavior = self.GetComponent<SummonMasterBehavior>();
            if (summonBehavior == null) return;

            if (self.gameObject.name.IndexOf("Drone", System.StringComparison.OrdinalIgnoreCase) >= 0)
                DronesUsed++;
        }

        public static void TrackSpeed(CharacterBody body)
        {
            if (body == null || body.characterMotor == null) return;

            float speed = body.characterMotor.velocity.magnitude;
            if (speed > MaxSpeed)
                MaxSpeed = speed;
        }

        public static void TrackDistance(CharacterBody body)
        {
            if (body == null)
            {
                hasLastPosition = false;
                return;
            }

            Vector3 currentPosition = body.transform.position;

            if (hasLastPosition)
            {
                float delta = Vector3.Distance(lastPosition, currentPosition);
                if (delta < 50f)
                    TotalDistance += delta;
            }

            lastPosition = currentPosition;
            hasLastPosition = true;
        }

        public static string FormatNumber(float number)
        {
            if (number >= 1e12f) return (number / 1e12f).ToString("0.0") + "t";
            if (number >= 1e9f) return (number / 1e9f).ToString("0.0") + "b";
            if (number >= 1e6f) return (number / 1e6f).ToString("0.0") + "m";
            if (number >= 1e3f) return (number / 1e3f).ToString("0.0") + "k";
            return number.ToString("0");
        }

        public static string FormatDistance(float meters)
        {
            if (meters >= 1000f)
                return (meters / 1000f).ToString("0.00") + " km";
            return meters.ToString("0") + " m";
        }
    }

    public class RunProgressUI : MonoBehaviour
    {
        private GameObject panelObject;
        private HUD cachedHud;

        private TextMeshProUGUI damageText;
        private TextMeshProUGUI damageTakenText;
        private TextMeshProUGUI healingText;
        private TextMeshProUGUI maxSpeedText;
        private TextMeshProUGUI biggestHitText;
        private TextMeshProUGUI chestsOpenedText;
        private TextMeshProUGUI goldCollectedText;
        private TextMeshProUGUI dronesUsedText;
        private TextMeshProUGUI distanceRanText;

        private static readonly Color BackgroundColor = new Color(0.05f, 0.07f, 0.18f, 0.97f);
        private static readonly Color BorderColor = new Color(1f, 0.82f, 0.2f, 1f);
        private static readonly Color RowColor = new Color(0.09f, 0.11f, 0.24f, 1f);
        private static readonly Color RowColorAlt = new Color(0.11f, 0.13f, 0.27f, 1f);
        private static readonly Color AccentColor = new Color(1f, 0.82f, 0.2f, 1f);
        private static readonly Color SubTextColor = new Color(0.65f, 0.68f, 0.8f, 1f);

        private const float PanelWidth = 420f;
        private const float RowHeight = 34f;
        private const float RowSpacing = 6f;

        private float refreshTimer;
        private const float RefreshInterval = 0.05f;

        private void Awake()
        {
            RunProgressStats.Init();
        }

        private void Update()
        {
            if (panelObject == null)
            {
                if (cachedHud == null)
                    cachedHud = FindObjectOfType<HUD>();

                if (cachedHud == null)
                    return;

                CreatePanel(cachedHud);
            }

            bool held = BepInEx.UnityInput.Current.GetKey(ProgressSettings.ToggleKey);

            if (held != panelObject.activeSelf)
                panelObject.SetActive(held);

            CharacterBody localBody = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            RunProgressStats.TrackDistance(localBody);

            CharacterBody body = held ? localBody : null;

            if (held)
                RunProgressStats.TrackSpeed(body);

            if (held)
            {
                refreshTimer += Time.deltaTime;
                if (refreshTimer >= RefreshInterval)
                {
                    refreshTimer = 0f;
                    RefreshValues();
                }
            }
        }

        private void CreatePanel(HUD hud)
        {
            panelObject = new GameObject("RiskyStatsProgressPanel");
            panelObject.transform.SetParent(hud.mainContainer.transform, false);

            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(PanelWidth, 100);

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

            CreateTitle(panelObject.transform);
            CreateDivider(panelObject.transform);

            damageText = CreateStatRow(panelObject.transform, "Total Damage", false);
            damageTakenText = CreateStatRow(panelObject.transform, "Total Damage Taken", true);
            healingText = CreateStatRow(panelObject.transform, "Total Healing", false);
            maxSpeedText = CreateStatRow(panelObject.transform, "Max Speed Achieved", true);
            biggestHitText = CreateStatRow(panelObject.transform, "Biggest Hit", false);
            chestsOpenedText = CreateStatRow(panelObject.transform, "Chests Opened", true);
            goldCollectedText = CreateStatRow(panelObject.transform, "Gold Collected", false);
            dronesUsedText = CreateStatRow(panelObject.transform, "Drones Used", true);
            distanceRanText = CreateStatRow(panelObject.transform, "Distance Ran", false);

            panelObject.SetActive(false);
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "RUN PROGRESS";
            title.fontSize = 24;
            title.color = AccentColor;
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            LayoutElement le = titleObj.AddComponent<LayoutElement>();
            le.minHeight = 30;
            le.preferredHeight = 30;
            le.flexibleWidth = 1;
        }

        private void CreateDivider(Transform parent)
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

        private TextMeshProUGUI CreateStatRow(Transform parent, string labelText, bool alt)
        {
            GameObject row = new GameObject("Row_" + labelText.Replace(" ", ""));
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
            label.color = SubTextColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement labelLayoutElement = labelObj.AddComponent<LayoutElement>();
            labelLayoutElement.flexibleWidth = 1;

            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(row.transform, false);

            TextMeshProUGUI value = valueObj.AddComponent<TextMeshProUGUI>();
            value.text = "0";
            value.fontSize = 17;
            value.color = Color.white;
            value.alignment = TextAlignmentOptions.MidlineRight;
            value.fontStyle = FontStyles.Bold;

            LayoutElement valueLayoutElement = valueObj.AddComponent<LayoutElement>();
            valueLayoutElement.preferredWidth = 120;
            valueLayoutElement.minWidth = 120;
            valueLayoutElement.flexibleWidth = 0;

            return value;
        }

        private static void SetTextIfChanged(TextMeshProUGUI text, string value)
        {
            if (text.text != value)
                text.text = value;
        }

        private void RefreshValues()
        {
            SetTextIfChanged(damageText, $"<color=#FFFFFF>{RunProgressStats.FormatNumber(RunProgressStats.TotalDamage)}</color>");
            SetTextIfChanged(damageTakenText, $"<color=#8B0000>{RunProgressStats.FormatNumber(RunProgressStats.TotalDamageTaken)}</color>");
            SetTextIfChanged(healingText, $"<color=#90EE90>{RunProgressStats.FormatNumber(RunProgressStats.TotalHealing)}</color>");
            SetTextIfChanged(maxSpeedText, $"<color=#00FFFF>{RunProgressStats.MaxSpeed:0.0} m/s</color>");

            string hitColor = RunProgressStats.BiggestHitCrit ? "FF0000" : "FFFFFF";
            SetTextIfChanged(biggestHitText, $"<color=#{hitColor}>{RunProgressStats.FormatNumber(RunProgressStats.BiggestHit)}</color>");

            SetTextIfChanged(chestsOpenedText, $"<color=#4A90E2>{RunProgressStats.ChestsOpened}</color>");
            SetTextIfChanged(goldCollectedText, $"<color=#FFD700>{RunProgressStats.FormatNumber(RunProgressStats.GoldCollected)}</color>");
            SetTextIfChanged(dronesUsedText, $"<color=#006400>{RunProgressStats.DronesUsed}</color>");
            SetTextIfChanged(distanceRanText, $"<color=#00FFFF>{RunProgressStats.FormatDistance(RunProgressStats.TotalDistance)}</color>");
        }
    }
}