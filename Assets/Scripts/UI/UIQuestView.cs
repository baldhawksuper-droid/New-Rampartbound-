using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Antarmuka Tampilan Misi Harian (Daily Quests) & Pencapaian Kerajaan (Achievements).
    /// Menampilkan tabulasi kategori, indikator progres interaktif, waktu countdown reset harian,
    /// dan tombol klaim reward yang terhubung langsung ke saldo Gold, Gems, & Stamina pemain.
    /// </summary>
    public class UIQuestView : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject questPanel;

        [Header("Category Tab Buttons")]
        [SerializeField] private Button dailyTabButton;
        [SerializeField] private Button achievementTabButton;
        [SerializeField] private Text dailyTabBadgeText;
        [SerializeField] private Text achievementTabBadgeText;

        [Header("Header Info")]
        [SerializeField] private Text resetCountdownText;
        [SerializeField] private Button closeButton;

        [Header("Scroll Container")]
        [SerializeField] private Transform questCardContainer;
        [SerializeField] private GameObject questCardTemplatePrefab; // Optional prefab reference

        [Header("Current Category State")]
        [SerializeField] private QuestCategory activeCategory = QuestCategory.Daily;

        private float timerUpdateInterval = 1.0f;
        private float timerElapsed = 0f;

        private void Awake()
        {
            if (dailyTabButton != null)
                dailyTabButton.onClick.AddListener(() => SwitchCategory(QuestCategory.Daily));

            if (achievementTabButton != null)
                achievementTabButton.onClick.AddListener(() => SwitchCategory(QuestCategory.Achievement));

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseQuestView);

                var btnClose = Resources.Load<Sprite>("Sprites/UI/Btn_Close_X");
                if (btnClose != null)
                {
                    var img = closeButton.GetComponent<Image>();
                    if (img != null && (img.sprite == null || img.sprite.name.Contains("UISprite") || img.sprite.name.Contains("Background")))
                    {
                        img.sprite = btnClose;
                        img.type = Image.Type.Simple;
                        img.preserveAspect = true;
                        img.color = Color.white;
                    }
                }
            }
        }

        private void OnEnable()
        {
            QuestManager.OnQuestProgressUpdated += HandleQuestUpdated;
            QuestManager.OnQuestClaimed += HandleQuestClaimed;
            QuestManager.OnDailyQuestsReset += HandleDailyReset;

            RefreshView();
        }

        private void OnDisable()
        {
            QuestManager.OnQuestProgressUpdated -= HandleQuestUpdated;
            QuestManager.OnQuestClaimed -= HandleQuestClaimed;
            QuestManager.OnDailyQuestsReset -= HandleDailyReset;
        }

        private void Update()
        {
            if (activeCategory == QuestCategory.Daily && questPanel != null && questPanel.activeSelf)
            {
                timerElapsed += Time.deltaTime;
                if (timerElapsed >= timerUpdateInterval)
                {
                    timerElapsed = 0f;
                    UpdateCountdownDisplay();
                }
            }
        }

        public void OpenQuestView()
        {
            transform.SetAsLastSibling();
            if (questPanel != null)
            {
                questPanel.transform.SetAsLastSibling();
                questPanel.SetActive(true);
            }
            else
                gameObject.SetActive(true);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CheckDailyReset();
            }

            RefreshView();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void CloseQuestView()
        {
            if (questPanel != null)
                questPanel.SetActive(false);
            else
                gameObject.SetActive(false);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
            }

            if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentActivePanel == GamePanelType.Quests)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void OpenView() => OpenQuestView();
        public void CloseView() => CloseQuestView();

        public void SwitchCategory(QuestCategory category)
        {
            activeCategory = category;
            RefreshView();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshView()
        {
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (lilitaFont != null)
            {
                if (resetCountdownText != null) resetCountdownText.font = lilitaFont;
                if (dailyTabBadgeText != null) dailyTabBadgeText.font = lilitaFont;
                if (achievementTabBadgeText != null) achievementTabBadgeText.font = lilitaFont;

                if (dailyTabButton != null)
                {
                    var t = dailyTabButton.GetComponentInChildren<Text>();
                    if (t != null) t.font = lilitaFont;
                }
                if (achievementTabButton != null)
                {
                    var t = achievementTabButton.GetComponentInChildren<Text>();
                    if (t != null) t.font = lilitaFont;
                }
            }

            UpdateCountdownDisplay();
            UpdateTabBadges();
            PopulateQuestCards();
        }

        private void UpdateCountdownDisplay()
        {
            if (resetCountdownText == null) return;

            if (activeCategory == QuestCategory.Daily && QuestManager.Instance != null)
            {
                TimeSpan remaining = QuestManager.Instance.GetTimeUntilDailyReset();
                resetCountdownText.text = $"Reset Misi Dalam: {remaining.Hours:D2}j {remaining.Minutes:D2}m {remaining.Seconds:D2}d";
                resetCountdownText.gameObject.SetActive(true);
            }
            else
            {
                resetCountdownText.gameObject.SetActive(false);
            }
        }

        private void UpdateTabBadges()
        {
            if (QuestManager.Instance == null) return;

            int unclaimedDaily = QuestManager.Instance.GetUnclaimedCount(QuestCategory.Daily);
            int unclaimedAch = QuestManager.Instance.GetUnclaimedCount(QuestCategory.Achievement);

            if (dailyTabBadgeText != null)
            {
                dailyTabBadgeText.gameObject.SetActive(unclaimedDaily > 0);
                dailyTabBadgeText.text = unclaimedDaily.ToString();
            }

            if (achievementTabBadgeText != null)
            {
                achievementTabBadgeText.gameObject.SetActive(unclaimedAch > 0);
                achievementTabBadgeText.text = unclaimedAch.ToString();
            }
        }

        private void PopulateQuestCards()
        {
            if (questCardContainer == null || QuestManager.Instance == null) return;

            // Bersihkan item card sebelumnya
            foreach (Transform child in questCardContainer)
            {
                if (questCardTemplatePrefab == null || child.gameObject != questCardTemplatePrefab)
                {
                    Destroy(child.gameObject);
                }
            }

            List<QuestItem> quests = QuestManager.Instance.GetQuestsByCategory(activeCategory);

            foreach (var quest in quests)
            {
                CreateQuestCard(quest);
            }
        }

        private void CreateQuestCard(QuestItem quest)
        {
            GameObject cardObj;
            if (questCardTemplatePrefab != null)
            {
                cardObj = Instantiate(questCardTemplatePrefab, questCardContainer);
                cardObj.SetActive(true);
            }
            else
            {
                cardObj = CreateProceduralCardObj(quest);
            }

            BindQuestCardData(cardObj, quest);
        }

        private void BindQuestCardData(GameObject cardObj, QuestItem quest)
        {
            // Binding teks judul & deskripsi
            Text titleText = cardObj.transform.Find("TitleText")?.GetComponent<Text>();
            if (titleText != null) titleText.text = quest.title;

            Text descText = cardObj.transform.Find("DescText")?.GetComponent<Text>();
            if (descText != null) descText.text = quest.description;

            // Slider & Label Progres
            Slider progressSlider = cardObj.transform.Find("ProgressBar")?.GetComponent<Slider>();
            if (progressSlider != null) progressSlider.value = quest.ProgressNormalized;

            Text progressText = cardObj.transform.Find("ProgressText")?.GetComponent<Text>();
            if (progressText != null) progressText.text = $"{quest.currentAmount} / {quest.targetAmount}";

            // Label Hadiah
            Text rewardText = cardObj.transform.Find("RewardText")?.GetComponent<Text>();
            if (rewardText != null)
            {
                string rewardStr = $"🪙 +{quest.rewardGold}   💎 +{quest.rewardGems}";
                if (quest.rewardEnergy > 0) rewardStr += $"   ⚡ +{quest.rewardEnergy}";
                rewardText.text = rewardStr;
            }

            // Tombol Klaim
            Button claimBtn = cardObj.transform.Find("ClaimButton")?.GetComponent<Button>();
            Text claimBtnText = claimBtn != null ? claimBtn.GetComponentInChildren<Text>() : null;

            if (claimBtn != null)
            {
                if (quest.isClaimed)
                {
                    claimBtn.interactable = false;
                    if (claimBtnText != null) claimBtnText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("SUDAH KLAIM", "CLAIMED");
                }
                else if (quest.IsCompleted)
                {
                    claimBtn.interactable = true;
                    if (claimBtnText != null) claimBtnText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("KLAIM!", "CLAIM!");
                    claimBtn.onClick.RemoveAllListeners();
                    claimBtn.onClick.AddListener(() =>
                    {
                        if (QuestManager.Instance != null && QuestManager.Instance.TryClaimReward(quest.questId, out string msg))
                        {
                            RefreshView();
                        }
                    });
                }
                else
                {
                    claimBtn.interactable = false;
                    if (claimBtnText != null) claimBtnText.text = $"{Mathf.RoundToInt(quest.ProgressNormalized * 100f)}%";
                }
            }
        }

        private GameObject CreateProceduralCardObj(QuestItem quest)
        {
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var frameStone = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;
            var sliderTrack = Resources.Load<Sprite>("Sprites/UI/UI_Slider_Track_Stone")
#if UNITY_EDITOR
                           ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Slider_Track_Stone.png")
#endif
                           ;
            var sliderFill = Resources.Load<Sprite>("Sprites/UI/UI_Slider_Fill_Cyan")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Slider_Fill_Cyan.png")
#endif
                          ;
            var btnGold = Resources.Load<Sprite>("Sprites/UI/Btn_Primary_Gold")
#if UNITY_EDITOR
                       ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Primary_Gold.png")
#endif
                       ;
            var btnStone = Resources.Load<Sprite>("Sprites/UI/Btn_Secondary_Stone")
#if UNITY_EDITOR
                        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Secondary_Stone.png")
#endif
                        ;

            GameObject card = new GameObject($"QuestCard_{quest.questId}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(questCardContainer, false);

            var le = card.GetComponent<LayoutElement>();
            le.minHeight = 110;
            le.preferredHeight = 110;

            var cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            if (frameStone != null)
            {
                cardImg.sprite = frameStone;
                cardImg.type = Image.Type.Sliced;
                cardImg.color = Color.white;
            }

            // 1. Judul Misi
            var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(card.transform, false);
            var titleTxt = titleObj.GetComponent<Text>();
            if (lilitaFont != null) titleTxt.font = lilitaFont;
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = new Color(1f, 0.85f, 0.35f);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.04f, 0.65f);
            tRt.anchorMax = new Vector2(0.68f, 0.95f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            // 2. Deskripsi Misi
            var descObj = new GameObject("DescText", typeof(RectTransform), typeof(Text));
            descObj.transform.SetParent(card.transform, false);
            var descTxt = descObj.GetComponent<Text>();
            if (lilitaFont != null) descTxt.font = lilitaFont;
            descTxt.fontSize = 16;
            descTxt.fontStyle = FontStyle.Normal;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.85f, 0.9f, 0.95f);
            var dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.04f, 0.36f);
            dRt.anchorMax = new Vector2(0.68f, 0.65f);
            dRt.offsetMin = Vector2.zero;
            dRt.offsetMax = Vector2.zero;

            // 3. Bilah Progres Energi Sian
            var barObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Slider));
            barObj.transform.SetParent(card.transform, false);
            var barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.04f, 0.10f);
            barRt.anchorMax = new Vector2(0.48f, 0.32f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            var slider = barObj.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(barObj.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            if (sliderTrack != null)
            {
                bgImg.sprite = sliderTrack;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barObj.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.15f);
            faRt.anchorMax = new Vector2(1f, 0.85f);
            faRt.offsetMin = Vector2.zero;
            faRt.offsetMax = Vector2.zero;

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillArea.transform, false);
            var fRt = fillObj.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            var fImg = fillObj.GetComponent<Image>();
            fImg.color = new Color(0.2f, 0.85f, 0.95f);
            if (sliderFill != null)
            {
                fImg.sprite = sliderFill;
                fImg.type = Image.Type.Sliced;
                fImg.color = Color.white;
            }
            slider.fillRect = fRt;

            // Teks Progres Angka di atas Bar
            var progTxtObj = new GameObject("ProgressText", typeof(RectTransform), typeof(Text));
            progTxtObj.transform.SetParent(barObj.transform, false);
            var pTxt = progTxtObj.GetComponent<Text>();
            if (lilitaFont != null) pTxt.font = lilitaFont;
            pTxt.fontSize = 14;
            pTxt.fontStyle = FontStyle.Bold;
            pTxt.alignment = TextAnchor.MiddleCenter;
            pTxt.color = Color.white;
            var ptRt = progTxtObj.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.offsetMin = Vector2.zero;
            ptRt.offsetMax = Vector2.zero;

            // 4. Label Hadiah
            var rewObj = new GameObject("RewardText", typeof(RectTransform), typeof(Text));
            rewObj.transform.SetParent(card.transform, false);
            var rewTxt = rewObj.GetComponent<Text>();
            if (lilitaFont != null) rewTxt.font = lilitaFont;
            rewTxt.fontSize = 15;
            rewTxt.fontStyle = FontStyle.Bold;
            rewTxt.alignment = TextAnchor.MiddleLeft;
            rewTxt.color = new Color(0.35f, 0.95f, 0.85f);
            var rRt = rewObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.50f, 0.08f);
            rRt.anchorMax = new Vector2(0.70f, 0.34f);
            rRt.offsetMin = Vector2.zero;
            rRt.offsetMax = Vector2.zero;

            // 5. Tombol Klaim Bertekstur Emas / Batu
            var btnObj = new GameObject("ClaimButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(card.transform, false);
            var bRt = btnObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.72f, 0.20f);
            bRt.anchorMax = new Vector2(0.96f, 0.80f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;

            var bImg = btnObj.GetComponent<Image>();
            if (quest.IsCompleted && !quest.isClaimed && btnGold != null)
            {
                bImg.sprite = btnGold;
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
            }
            else if (btnStone != null)
            {
                bImg.sprite = btnStone;
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
            }

            var btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(Text));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var bTxt = btnTxtObj.GetComponent<Text>();
            if (lilitaFont != null) bTxt.font = lilitaFont;
            bTxt.fontSize = 18;
            bTxt.fontStyle = FontStyle.Bold;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.color = Color.white;
            var btRt = btnTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;

            return card;
        }

        private void HandleQuestUpdated(QuestItem quest)
        {
            if (quest.category == activeCategory)
            {
                RefreshView();
            }
            else
            {
                UpdateTabBadges();
            }
        }

        private void HandleQuestClaimed(QuestItem quest)
        {
            RefreshView();
        }

        private void HandleDailyReset()
        {
            RefreshView();
        }
    }
}
