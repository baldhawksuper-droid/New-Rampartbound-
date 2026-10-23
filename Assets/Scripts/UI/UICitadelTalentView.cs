using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Antarmuka UI untuk Pohon Talenta Benteng (Citadel Talents & Blessings Tree).
    /// Menampilkan daftar talenta, status rank saat ini, biaya Citadel Crests,
    /// serta tombol upgrade interaktif dengan umpan balik pesan status.
    /// </summary>
    public class UICitadelTalentView : MonoBehaviour
    {
        public static UICitadelTalentView Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject talentPanel;
        [SerializeField] private Button openTalentButton;
        [SerializeField] private Button closeTalentButton;

        [Header("Header Info")]
        [SerializeField] private Text crestsCountText;
        [SerializeField] private Text totalTalentRankText;

        [Header("Feedback Toast")]
        [SerializeField] private Text feedbackText;

        [Header("Talent Item UI Template / Dynamic Container")]
        [SerializeField] private Transform talentItemsContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (openTalentButton != null)
                openTalentButton.onClick.AddListener(OpenPanel);

            if (closeTalentButton != null)
                closeTalentButton.onClick.AddListener(ClosePanel);

            if (talentPanel != null)
                talentPanel.SetActive(false);
        }

        private void OnEnable()
        {
            CitadelTalentManager.OnTalentUpgraded += HandleTalentUpgraded;
            CitadelTalentManager.OnCitadelCrestsChanged += HandleCrestsChanged;
        }

        private void OnDisable()
        {
            CitadelTalentManager.OnTalentUpgraded -= HandleTalentUpgraded;
            CitadelTalentManager.OnCitadelCrestsChanged -= HandleCrestsChanged;
        }

        private void Start()
        {
            RefreshUI();
        }

        public void OpenPanel()
        {
            transform.SetAsLastSibling();
            if (talentPanel != null)
            {
                talentPanel.transform.SetAsLastSibling();
                talentPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            RefreshUI();
        }

        public void ClosePanel()
        {
            if (talentPanel != null)
                talentPanel.SetActive(false);
            else
                gameObject.SetActive(false);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
            }

            if (FantasyDefense.Core.GameFlowManager.Instance != null)
            {
                FantasyDefense.Core.GameFlowManager.Instance.CloseCurrentPanel();
            }
        }

        public void OpenView() => OpenPanel();
        public void CloseView() => ClosePanel();

        private void HandleTalentUpgraded(string talentId, int newRank)
        {
            RefreshUI();
        }

        private void HandleCrestsChanged(int crests)
        {
            UpdateCrestsDisplay();
        }

        public void RefreshUI()
        {
            UpdateCrestsDisplay();
            UpdateTotalRankDisplay();
        }

        private void UpdateCrestsDisplay()
        {
            int crests = CitadelTalentManager.Instance != null ? CitadelTalentManager.Instance.AvailableCrests : 0;
            if (crestsCountText != null)
            {
                crestsCountText.text = $"Citadel Crests: {crests}";
            }
        }

        private void UpdateTotalRankDisplay()
        {
            if (CitadelTalentManager.Instance == null) return;

            int totalRank = 0;
            foreach (var talent in CitadelTalentManager.Instance.Talents)
            {
                if (talent != null)
                {
                    totalRank += CitadelTalentManager.Instance.GetTalentRank(talent.talentId);
                }
            }

            if (totalTalentRankText != null)
            {
                totalTalentRankText.text = $"Total Talenta Aktif: Lv. {totalRank}";
            }
        }

        public void OnClickUpgradeTalent(CitadelTalentDataSO talent)
        {
            if (CitadelTalentManager.Instance == null || talent == null) return;

            bool success = CitadelTalentManager.Instance.TryUpgradeTalent(talent, out string message);
            ShowFeedback(message, success ? Color.green : new Color(1f, 0.4f, 0.4f));
            RefreshUI();
        }

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                CancelInvoke(nameof(ClearFeedback));
                Invoke(nameof(ClearFeedback), 3.0f);
            }
            else
            {
                Debug.Log($"[UICitadelTalentView] {message}");
            }
        }

        private void ClearFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }
        }
    }
}
