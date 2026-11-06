using System;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Antarmuka Citadel Season Pass (UI Battle Pass View):
    /// Menampilkan progres EXP musiman 30 Tiers, status jalur gratis (Free Pass) & elit (Elite Pass),
    /// serta tombol klaim hadiah instan (Claim All) dan pembukaan Elite Pass.
    /// </summary>
    public class UIBattlePassView : MonoBehaviour
    {
        [Header("Root & Panels")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button closeButton;

        [Header("Header & Season Status")]
        [SerializeField] private Text seasonTitleText;
        [SerializeField] private Text passLevelText;
        [SerializeField] private Slider passExpSlider;
        [SerializeField] private Text passExpText;
        [SerializeField] private Text seasonTimeRemainingText;

        [Header("Elite Pass Activation")]
        [SerializeField] private Button unlockEliteButton;
        [SerializeField] private Text eliteStatusBadgeText;

        [Header("Action Buttons & Feedback")]
        [SerializeField] private Button claimAllButton;
        [SerializeField] private Text feedbackText;

        [Header("Current Tier Spotlight")]
        [SerializeField] private Text currentTierTitleText;
        [SerializeField] private Text freeRewardDescText;
        [SerializeField] private Button claimFreeTierButton;
        [SerializeField] private Text eliteRewardDescText;
        [SerializeField] private Button claimEliteTierButton;

        private void OnEnable()
        {
            BattlePassManager.OnBattlePassUpdated += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            BattlePassManager.OnBattlePassUpdated -= RefreshUI;
        }

        private void Start()
        {
            InitializeButtons();
            RefreshUI();
        }

        private void InitializeButtons()
        {
            var btnClose = Resources.Load<Sprite>("Sprites/UI/Btn_Close_X");
            void StyleCloseBtn(Button btn)
            {
                if (btn == null || btnClose == null) return;
                var img = btn.GetComponent<Image>();
                if (img != null && (img.sprite == null || img.sprite.name.Contains("UISprite") || img.sprite.name.Contains("Background")))
                {
                    img.sprite = btnClose;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }
            }
            StyleCloseBtn(closeButton);

            if (closeButton != null) closeButton.onClick.AddListener(CloseView);
            if (unlockEliteButton != null) unlockEliteButton.onClick.AddListener(OnUnlockEliteClicked);
            if (claimAllButton != null) claimAllButton.onClick.AddListener(OnClaimAllClicked);

            if (claimFreeTierButton != null) claimFreeTierButton.onClick.AddListener(OnClaimCurrentFreeTierClicked);
            if (claimEliteTierButton != null) claimEliteTierButton.onClick.AddListener(OnClaimCurrentEliteTierClicked);
        }

        public void RefreshUI()
        {
            if (BattlePassManager.Instance == null || BattlePassManager.Instance.CurrentSeason == null) return;
            var season = BattlePassManager.Instance.CurrentSeason;

            if (seasonTitleText != null) seasonTitleText.text = season.seasonTitle;
            if (passLevelText != null) passLevelText.text = $"Tier {season.currentPassLevel} / 30";

            if (passExpSlider != null)
            {
                passExpSlider.value = (float)season.currentPassExp / season.ExpPerTier;
            }
            if (passExpText != null)
            {
                passExpText.text = $"{season.currentPassExp} / {season.ExpPerTier} EXP";
            }

            // Hitung sisa hari musim
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long diffSeconds = Math.Max(0L, season.seasonEndTimeUtc - now);
            int daysRemaining = Mathf.CeilToInt(diffSeconds / 86400f);
            if (seasonTimeRemainingText != null)
            {
                seasonTimeRemainingText.text = $"Sisa Waktu Musim: {daysRemaining} Hari";
            }

            // Status Elite Pass
            if (eliteStatusBadgeText != null)
            {
                eliteStatusBadgeText.text = season.isElitePassUnlocked ? "<color=#f1c40f>ELITE PASS AKTIF ★</color>" : "Jalur Standar (Gratis)";
            }

            if (unlockEliteButton != null)
            {
                unlockEliteButton.gameObject.SetActive(!season.isElitePassUnlocked);
            }

            // Update Current Tier Spotlight
            var tier = season.GetTier(season.currentPassLevel);
            if (tier != null)
            {
                if (currentTierTitleText != null) currentTierTitleText.text = $"Sorotan Hadiah Tier {tier.tierLevel}";

                if (freeRewardDescText != null)
                    freeRewardDescText.text = $"Gratis: +{tier.freeRewardAmount} {tier.freeRewardType}";

                if (claimFreeTierButton != null)
                {
                    claimFreeTierButton.interactable = !tier.isFreeClaimed;
                    var txt = claimFreeTierButton.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = tier.isFreeClaimed ? "TERKLAIM ✔" : "KLAIM GRATIS";
                }

                if (eliteRewardDescText != null)
                    eliteRewardDescText.text = $"Elite: +{tier.eliteRewardAmount} {tier.eliteRewardType}";

                if (claimEliteTierButton != null)
                {
                    bool canClaim = season.isElitePassUnlocked && !tier.isEliteClaimed;
                    claimEliteTierButton.interactable = canClaim;
                    var txt = claimEliteTierButton.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        if (tier.isEliteClaimed) txt.text = "TERKLAIM ✔";
                        else if (!season.isElitePassUnlocked) txt.text = "TERKUNCI 🔒";
                        else txt.text = "KLAIM ELITE";
                    }
                }
            }
        }

        private void OnUnlockEliteClicked()
        {
            if (BattlePassManager.Instance != null)
            {
                bool success = BattlePassManager.Instance.UnlockElitePass(spendGems: true, out string msg);
                if (feedbackText != null) feedbackText.text = msg;
            }
        }

        private void OnClaimAllClicked()
        {
            if (BattlePassManager.Instance != null)
            {
                int count = BattlePassManager.Instance.ClaimAllAvailableRewards();
                if (feedbackText != null)
                {
                    feedbackText.text = count > 0 ? $"Berhasil mengklaim {count} hadiah sekaligus!" : "Tidak ada hadiah baru yang siap diklaim.";
                }
            }
        }

        private void OnClaimCurrentFreeTierClicked()
        {
            if (BattlePassManager.Instance == null || BattlePassManager.Instance.CurrentSeason == null) return;
            int lvl = BattlePassManager.Instance.CurrentSeason.currentPassLevel;
            bool ok = BattlePassManager.Instance.ClaimFreeReward(lvl, out string msg);
            if (feedbackText != null) feedbackText.text = msg;
        }

        private void OnClaimCurrentEliteTierClicked()
        {
            if (BattlePassManager.Instance == null || BattlePassManager.Instance.CurrentSeason == null) return;
            int lvl = BattlePassManager.Instance.CurrentSeason.currentPassLevel;
            bool ok = BattlePassManager.Instance.ClaimEliteReward(lvl, out string msg);
            if (feedbackText != null) feedbackText.text = msg;
        }

        public void OpenView()
        {
            transform.SetAsLastSibling();
            if (rootPanel != null)
            {
                rootPanel.transform.SetAsLastSibling();
                rootPanel.SetActive(true);
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

        public void CloseView()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            else gameObject.SetActive(false);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
            }

            if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentActivePanel == GamePanelType.BattlePass)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }
    }
}
