using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// View Controller untuk UI Layar Live Event Arena & Limited-Time Challenge.
    /// Menampilkan waktu tersisa event, batasan modifier pertempuran, tangga skor milestone, dan papan peringkat.
    /// </summary>
    public class UIEventArenaView : MonoBehaviour
    {
        [Header("Root & Panels")]
        [SerializeField] private GameObject arenaRootPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Event Header Info")]
        [SerializeField] private Text eventTitleText;
        [SerializeField] private Text eventDescriptionText;
        [SerializeField] private Text countdownTimerText;
        [SerializeField] private Text activeModifiersText;

        [Header("Player Status & Action")]
        [SerializeField] private Text attemptsLeftText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text currentRankText;
        [SerializeField] private Button challengeNowButton;
        [SerializeField] private Button buyAttemptButton;

        [Header("Milestones & Rewards")]
        [SerializeField] private Slider milestoneProgressBar;
        [SerializeField] private Text milestoneProgressLabel;
        [SerializeField] private Transform milestoneContainer;
        [SerializeField] private GameObject milestoneItemPrefab;

        [Header("Leaderboard Panel")]
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject leaderboardRowPrefab;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseView);

            if (challengeNowButton != null)
                challengeNowButton.onClick.AddListener(OnChallengeClicked);

            if (buyAttemptButton != null)
                buyAttemptButton.onClick.AddListener(OnBuyAttemptClicked);
        }

        private void OnEnable()
        {
            EventChallengeManager.OnEventChallengeUpdated += RefreshView;
            EventChallengeManager.OnScoreUpdated += (s) => RefreshView();
            EventChallengeManager.OnRewardClaimed += (m) => RefreshView();
            RefreshView();
        }

        private void OnDisable()
        {
            EventChallengeManager.OnEventChallengeUpdated -= RefreshView;
            EventChallengeManager.OnScoreUpdated -= (s) => RefreshView();
            EventChallengeManager.OnRewardClaimed -= (m) => RefreshView();
        }

        public void OpenView()
        {
            transform.SetAsLastSibling();
            if (arenaRootPanel != null)
            {
                arenaRootPanel.transform.SetAsLastSibling();
                arenaRootPanel.SetActive(true);
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            AudioManager.Instance?.PlaySfx(AudioSfxType.ButtonClick);
            RefreshView();
        }

        public void CloseView()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (arenaRootPanel != null) arenaRootPanel.SetActive(false);

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            AudioManager.Instance?.PlaySfx(AudioSfxType.ButtonClick);
        }

        public void RefreshView()
        {
            if (EventChallengeManager.Instance == null) return;
            var evt = EventChallengeManager.Instance.CurrentEvent;
            if (evt == null) return;

            if (eventTitleText != null) eventTitleText.text = evt.eventTitle;
            if (eventDescriptionText != null) eventDescriptionText.text = evt.description;

            // Format Timer: Jam & Menit
            TimeSpan timeSpan = TimeSpan.FromSeconds(evt.remainingSeconds);
            if (countdownTimerText != null)
            {
                countdownTimerText.text = $"Sisa Waktu: {timeSpan.Days}h {timeSpan.Hours}j {timeSpan.Minutes}m";
            }

            // Format Modifiers
            if (activeModifiersText != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var mod in evt.activeModifiers)
                {
                    sb.AppendLine($"• <b>{mod.modifierName}</b>: {mod.description}");
                }
                activeModifiersText.text = sb.ToString().TrimEnd();
            }

            // Format Skor & Tiket
            if (attemptsLeftText != null)
                attemptsLeftText.text = $"Tiket Hari Ini: {evt.dailyAttemptsLeft}/{evt.maxDailyAttempts}";

            if (highScoreText != null)
                highScoreText.text = $"Rekor Tertinggi: {evt.currentHighScore:N0} Pts";

            if (currentRankText != null)
                currentRankText.text = $"Peringkat Anda: #{evt.playerRank}";

            // Milestone Progress
            if (evt.milestones != null && evt.milestones.Count > 0)
            {
                int maxTarget = evt.milestones[evt.milestones.Count - 1].requiredScore;
                if (milestoneProgressBar != null)
                {
                    milestoneProgressBar.maxValue = maxTarget;
                    milestoneProgressBar.value = Mathf.Clamp(evt.currentHighScore, 0, maxTarget);
                }
                if (milestoneProgressLabel != null)
                {
                    milestoneProgressLabel.text = $"{evt.currentHighScore:N0} / {maxTarget:N0} Pts";
                }
            }

            // Tombol Validasi
            if (challengeNowButton != null)
            {
                challengeNowButton.interactable = evt.dailyAttemptsLeft > 0;
            }
        }

        private void OnChallengeClicked()
        {
            if (EventChallengeManager.Instance == null) return;

            if (EventChallengeManager.Instance.ConsumeAttempt())
            {
                Debug.Log("[UIEventArenaView] Memulai pertempuran arena event tantangan!");
                AudioManager.Instance?.PlaySfx(AudioSfxType.ButtonClick);
                // Menutup UI modal dan memicu flow pertarungan wave event di GameManager
                CloseView();
            }
        }

        private void OnBuyAttemptClicked()
        {
            if (EventChallengeManager.Instance == null) return;
            EventChallengeManager.Instance.PurchaseAdditionalAttempt(50);
        }
    }
}
