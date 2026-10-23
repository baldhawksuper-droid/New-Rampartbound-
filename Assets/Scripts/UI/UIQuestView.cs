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
                closeButton.onClick.AddListener(CloseQuestView);
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
                    if (claimBtnText != null) claimBtnText.text = "SUDAH DIKLAIM";
                }
                else if (quest.IsCompleted)
                {
                    claimBtn.interactable = true;
                    if (claimBtnText != null) claimBtnText.text = "KLAIM!";
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
            GameObject card = new GameObject($"QuestCard_{quest.questId}", typeof(RectTransform));
            card.transform.SetParent(questCardContainer, false);
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
