using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk Mode Endless Abyssal Trial.
    /// Menampilkan rekor lantai tertinggi, countdown reset mingguan,
    /// daftar hadiah milestone peti harta, serta tombol mulai ekspedisi survival.
    /// </summary>
    public class UIEndlessTrialView : MonoBehaviour
    {
        public static UIEndlessTrialView Instance { get; private set; }

        [Header("Root Panels")]
        [SerializeField] private GameObject trialLobbyPanel;
        [SerializeField] private GameObject inBattleFloorBanner;

        [Header("Lobby Summary Displays")]
        [SerializeField] private Text highestFloorText;
        [SerializeField] private Text weeklyResetCountdownText;
        [SerializeField] private Text energyCostText;
        [SerializeField] private Text trialDescriptionText;

        [Header("Live In-Battle Display")]
        [SerializeField] private Text liveFloorCounterText;

        [Header("Action Buttons")]
        [SerializeField] private Button startTrialButton;
        [SerializeField] private Button closeButton;

        [Header("Milestone Item Displays")]
        [SerializeField] private Transform milestoneContainer;
        [SerializeField] private Text feedbackMessageText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
        }

        private void OnEnable()
        {
            EndlessTrialManager.OnEndlessTrialStarted += HandleTrialStarted;
            EndlessTrialManager.OnEndlessFloorStarted += HandleFloorStarted;
            EndlessTrialManager.OnEndlessTrialEnded += HandleTrialEnded;
            EndlessTrialManager.OnMilestoneRewardClaimed += HandleMilestoneClaimed;
            CitadelLobbyManager.OnAbyssalFloorProgressed += HandleHighestFloorChanged;
        }

        private void OnDisable()
        {
            EndlessTrialManager.OnEndlessTrialStarted -= HandleTrialStarted;
            EndlessTrialManager.OnEndlessFloorStarted -= HandleFloorStarted;
            EndlessTrialManager.OnEndlessTrialEnded -= HandleTrialEnded;
            EndlessTrialManager.OnMilestoneRewardClaimed -= HandleMilestoneClaimed;
            CitadelLobbyManager.OnAbyssalFloorProgressed -= HandleHighestFloorChanged;
        }

        private void Start()
        {
            RefreshLobbyDisplay();
        }

        private void InitializeButtons()
        {
            if (startTrialButton != null)
                startTrialButton.onClick.AddListener(OnStartTrialClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseView);
        }

        public void OpenView()
        {
            transform.SetAsLastSibling();
            if (trialLobbyPanel != null)
            {
                trialLobbyPanel.transform.SetAsLastSibling();
                trialLobbyPanel.SetActive(true);
            }
            else
                gameObject.SetActive(true);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            RefreshLobbyDisplay();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void CloseView()
        {
            if (trialLobbyPanel != null)
                trialLobbyPanel.SetActive(false);
            else
                gameObject.SetActive(false);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
            }

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshLobbyDisplay()
        {
            if (CitadelLobbyManager.Instance == null) return;

            int highest = CitadelLobbyManager.Instance.HighestAbyssalFloor;
            if (highestFloorText != null)
            {
                highestFloorText.text = highest > 0 ? $"Rekor Tertinggi: Lantai {highest}" : "Rekor Tertinggi: Belum Ada";
            }

            if (weeklyResetCountdownText != null && EndlessTrialManager.Instance != null)
            {
                weeklyResetCountdownText.text = $"Reset Hadiah: {EndlessTrialManager.Instance.GetWeeklyResetTimeRemaining()}";
            }

            if (energyCostText != null)
            {
                energyCostText.text = "Biaya Masuk: 5 Energi";
            }

            if (startTrialButton != null)
            {
                startTrialButton.interactable = CitadelLobbyManager.Instance.CanStartBattle();
            }
        }

        private void OnStartTrialClicked()
        {
            if (EndlessTrialManager.Instance == null) return;

            CloseView();
            EndlessTrialManager.Instance.StartEndlessTrial();

            // Pemicu inisialisasi arena jika GameManager ada
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InitializeBattle();
            }
        }

        private void HandleTrialStarted()
        {
            if (inBattleFloorBanner != null)
                inBattleFloorBanner.SetActive(true);

            UpdateLiveFloor(1);
        }

        private void HandleFloorStarted(int floor)
        {
            UpdateLiveFloor(floor);
        }

        private void UpdateLiveFloor(int floor)
        {
            if (liveFloorCounterText != null)
            {
                liveFloorCounterText.text = $"★ ABYSSAL TRIAL: LANTAI {floor} ★";
            }
        }

        private void HandleTrialEnded(int finalFloor, int goldEarned, int gemsEarned)
        {
            if (inBattleFloorBanner != null)
                inBattleFloorBanner.SetActive(false);

            ShowFeedback($"Tantangan Abyssal Selesai! Capaian: Lantai {finalFloor}. (+{goldEarned} Gold, +{gemsEarned} Gems)");
            RefreshLobbyDisplay();
        }

        private void HandleMilestoneClaimed(int milestoneFloor)
        {
            ShowFeedback($"Hadiah Peti Lantai {milestoneFloor} berhasil diklaim!");
            RefreshLobbyDisplay();
        }

        private void HandleHighestFloorChanged(int newRecord)
        {
            RefreshLobbyDisplay();
        }

        public void TryClaimMilestone(int milestone)
        {
            if (EndlessTrialManager.Instance != null)
            {
                EndlessTrialManager.Instance.TryClaimMilestone(milestone);
            }
        }

        private void ShowFeedback(string msg)
        {
            if (feedbackMessageText != null)
            {
                feedbackMessageText.text = msg;
            }
        }
    }
}
