using System;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Antarmuka Aliansi Benteng (UI Guild View):
    /// Menampilkan Info Guild & Donasi Harian, Tab Ekspedisi Hero Otomatis,
    /// serta Medan Serbuan Titan Kolosal Mingguan (Co-op Titan Raid).
    /// </summary>
    public class UIGuildView : MonoBehaviour
    {
        [Header("Main Panels & Tabs")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button tabOverviewButton;
        [SerializeField] private Button tabExpeditionsButton;
        [SerializeField] private Button tabTitanRaidButton;
        [SerializeField] private GameObject overviewPanel;
        [SerializeField] private GameObject expeditionsPanel;
        [SerializeField] private GameObject titanRaidPanel;
        [SerializeField] private Button closeButton;

        [Header("Guild Overview & Donation")]
        [SerializeField] private Text guildNameText;
        [SerializeField] private Text guildLevelText;
        [SerializeField] private Slider guildExpSlider;
        [SerializeField] private Text guildExpText;
        [SerializeField] private Text guildTokensText;
        [SerializeField] private Text guildNoticeText;
        [SerializeField] private Button donateButton;
        [SerializeField] private Text donationFeedbackText;

        [Header("Expeditions UI")]
        [SerializeField] private Text exp1TitleText;
        [SerializeField] private Text exp1StatusText;
        [SerializeField] private Button exp1ActionButton;
        [SerializeField] private Text exp1ActionText;

        [SerializeField] private Text exp2TitleText;
        [SerializeField] private Text exp2StatusText;
        [SerializeField] private Button exp2ActionButton;
        [SerializeField] private Text exp2ActionText;

        [SerializeField] private Text exp3TitleText;
        [SerializeField] private Text exp3StatusText;
        [SerializeField] private Button exp3ActionButton;
        [SerializeField] private Text exp3ActionText;

        [Header("Titan Raid UI")]
        [SerializeField] private Text titanNameText;
        [SerializeField] private Slider titanHpSlider;
        [SerializeField] private Text titanHpText;
        [SerializeField] private Text guildTotalDamageText;
        [SerializeField] private Text personalDamageText;
        [SerializeField] private Button attackTitanButton;
        [SerializeField] private Text attackFeedbackText;

        [Header("Titan Milestone Chests")]
        [SerializeField] private Button chest20Button;
        [SerializeField] private Button chest40Button;
        [SerializeField] private Button chest60Button;
        [SerializeField] private Button chest80Button;
        [SerializeField] private Button chest100Button;

        private void OnEnable()
        {
            GuildManager.OnGuildUpdated += RefreshUI;
            GuildManager.OnTitanRaidDamageDealt += HandleTitanDamageDealt;
            RefreshUI();
        }

        private void OnDisable()
        {
            GuildManager.OnGuildUpdated -= RefreshUI;
            GuildManager.OnTitanRaidDamageDealt -= HandleTitanDamageDealt;
        }

        private void Start()
        {
            InitializeButtons();
            SwitchTab(0);
            RefreshUI();
        }

        private void Update()
        {
            // Update countdown timer ekspedisi saat panel aktif
            if (expeditionsPanel != null && expeditionsPanel.activeSelf)
            {
                UpdateExpeditionTimers();
            }
        }

        private void InitializeButtons()
        {
            if (tabOverviewButton != null) tabOverviewButton.onClick.AddListener(() => SwitchTab(0));
            if (tabExpeditionsButton != null) tabExpeditionsButton.onClick.AddListener(() => SwitchTab(1));
            if (tabTitanRaidButton != null) tabTitanRaidButton.onClick.AddListener(() => SwitchTab(2));

            if (closeButton != null) closeButton.onClick.AddListener(CloseView);
            if (donateButton != null) donateButton.onClick.AddListener(OnDonateClicked);

            if (exp1ActionButton != null) exp1ActionButton.onClick.AddListener(() => OnExpeditionActionClicked("exp_01"));
            if (exp2ActionButton != null) exp2ActionButton.onClick.AddListener(() => OnExpeditionActionClicked("exp_02"));
            if (exp3ActionButton != null) exp3ActionButton.onClick.AddListener(() => OnExpeditionActionClicked("exp_03"));

            if (attackTitanButton != null) attackTitanButton.onClick.AddListener(OnAttackTitanClicked);

            if (chest20Button != null) chest20Button.onClick.AddListener(() => OnClaimChestClicked(20));
            if (chest40Button != null) chest40Button.onClick.AddListener(() => OnClaimChestClicked(40));
            if (chest60Button != null) chest60Button.onClick.AddListener(() => OnClaimChestClicked(60));
            if (chest80Button != null) chest80Button.onClick.AddListener(() => OnClaimChestClicked(80));
            if (chest100Button != null) chest100Button.onClick.AddListener(() => OnClaimChestClicked(100));
        }

        public void SwitchTab(int tabIndex)
        {
            if (overviewPanel != null) overviewPanel.SetActive(tabIndex == 0);
            if (expeditionsPanel != null) expeditionsPanel.SetActive(tabIndex == 1);
            if (titanRaidPanel != null) titanRaidPanel.SetActive(tabIndex == 2);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshUI()
        {
            if (GuildManager.Instance == null || GuildManager.Instance.CurrentGuild == null) return;
            var guild = GuildManager.Instance.CurrentGuild;

            // 1. Overview Info
            if (guildNameText != null) guildNameText.text = guild.guildName;
            if (guildLevelText != null) guildLevelText.text = $"Lv. {guild.guildLevel}";
            int reqExp = guild.GetRequiredExpForNextLevel();
            if (guildExpSlider != null) guildExpSlider.value = (float)guild.guildExp / Mathf.Max(1, reqExp);
            if (guildExpText != null) guildExpText.text = $"{guild.guildExp} / {reqExp} EXP";
            if (guildTokensText != null) guildTokensText.text = $"{guild.guildTokens} Tokens";
            if (guildNoticeText != null) guildNoticeText.text = guild.guildNotice;

            // 2. Expeditions
            UpdateExpeditionCards();

            // 3. Titan Raid
            if (guild.titanRaid != null)
            {
                var titan = guild.titanRaid;
                if (titanNameText != null) titanNameText.text = $"{titan.titanName} (Lv.{titan.titanLevel})";
                if (titanHpSlider != null) titanHpSlider.value = titan.GetHpPercent();
                if (titanHpText != null) titanHpText.text = $"{titan.titanCurrentHp:N0} / {titan.titanMaxHp:N0} HP ({titan.GetHpPercent() * 100f:F1}%)";
                if (guildTotalDamageText != null) guildTotalDamageText.text = $"Total DMG Aliansi: {titan.guildTotalDamage:N0}";
                if (personalDamageText != null) personalDamageText.text = $"Kontribusi Kamu: {titan.personalDamageDealt:N0}";

                // Update Chest Buttons interactability
                UpdateChestButton(chest20Button, titan.IsMilestoneClaimable(20), titan.claimedMilestones.Contains(20));
                UpdateChestButton(chest40Button, titan.IsMilestoneClaimable(40), titan.claimedMilestones.Contains(40));
                UpdateChestButton(chest60Button, titan.IsMilestoneClaimable(60), titan.claimedMilestones.Contains(60));
                UpdateChestButton(chest80Button, titan.IsMilestoneClaimable(80), titan.claimedMilestones.Contains(80));
                UpdateChestButton(chest100Button, titan.IsMilestoneClaimable(100), titan.claimedMilestones.Contains(100));
            }
        }

        private void UpdateChestButton(Button btn, bool claimable, bool alreadyClaimed)
        {
            if (btn == null) return;
            btn.interactable = claimable;
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                if (alreadyClaimed) txt.text = "TERKLAIM ✔";
                else if (claimable) txt.text = "KLAIM!";
                else txt.text = "TERKUNCI 🔒";
            }
        }

        private void UpdateExpeditionCards()
        {
            if (GuildManager.Instance == null || GuildManager.Instance.CurrentGuild == null) return;
            var exps = GuildManager.Instance.CurrentGuild.expeditions;
            if (exps == null) return;

            if (exps.Count > 0) SetupExpCard(exps[0], exp1TitleText, exp1StatusText, exp1ActionButton, exp1ActionText);
            if (exps.Count > 1) SetupExpCard(exps[1], exp2TitleText, exp2StatusText, exp2ActionButton, exp2ActionText);
            if (exps.Count > 2) SetupExpCard(exps[2], exp3TitleText, exp3StatusText, exp3ActionButton, exp3ActionText);
        }

        private void SetupExpCard(GuildExpedition exp, Text title, Text status, Button btn, Text btnText)
        {
            if (title != null) title.text = exp.title;
            if (status != null)
            {
                if (!exp.isDispatched) status.text = $"Hadiah: +{exp.rewardGold} Gold, +{exp.rewardTokens} Tokens, +{exp.rewardForgeDust} Dust";
                else if (exp.IsFinished()) status.text = "<color=#4cd137>Selesai! Siap Diklaim!</color>";
                else status.text = $"Patroli... Sisa: {Mathf.CeilToInt(exp.GetRemainingSeconds())}s";
            }

            if (btn != null && btnText != null)
            {
                if (!exp.isDispatched)
                {
                    btnText.text = "Berangkatkan (Deploy)";
                    btn.interactable = true;
                }
                else if (exp.IsFinished())
                {
                    btnText.text = "Klaim Hadiah!";
                    btn.interactable = true;
                }
                else
                {
                    btnText.text = "Sedang Patroli...";
                    btn.interactable = false;
                }
            }
        }

        private void UpdateExpeditionTimers()
        {
            if (GuildManager.Instance == null || GuildManager.Instance.CurrentGuild == null) return;
            var exps = GuildManager.Instance.CurrentGuild.expeditions;
            if (exps == null) return;

            if (exps.Count > 0 && exps[0].isDispatched && !exps[0].IsFinished() && exp1StatusText != null)
                exp1StatusText.text = $"Patroli... Sisa: {Mathf.CeilToInt(exps[0].GetRemainingSeconds())}s";
            if (exps.Count > 1 && exps[1].isDispatched && !exps[1].IsFinished() && exp2StatusText != null)
                exp2StatusText.text = $"Patroli... Sisa: {Mathf.CeilToInt(exps[1].GetRemainingSeconds())}s";
            if (exps.Count > 2 && exps[2].isDispatched && !exps[2].IsFinished() && exp3StatusText != null)
                exp3StatusText.text = $"Patroli... Sisa: {Mathf.CeilToInt(exps[2].GetRemainingSeconds())}s";
        }

        private void OnDonateClicked()
        {
            if (GuildManager.Instance != null)
            {
                bool success = GuildManager.Instance.DonateToGuild(500, out string msg);
                if (donationFeedbackText != null) donationFeedbackText.text = msg;
            }
        }

        private void OnExpeditionActionClicked(string expId)
        {
            if (GuildManager.Instance == null) return;
            var exp = GuildManager.Instance.CurrentGuild.expeditions.Find(e => e.expeditionId == expId);
            if (exp == null) return;

            if (!exp.isDispatched)
            {
                GuildManager.Instance.DispatchExpedition(expId, UnitType.Cedric, out string _);
            }
            else if (exp.IsFinished())
            {
                GuildManager.Instance.ClaimExpeditionReward(expId, out string _);
            }
            RefreshUI();
        }

        private void OnAttackTitanClicked()
        {
            if (GuildManager.Instance != null)
            {
                bool success = GuildManager.Instance.AttackTitanRaid(out long dmg, out string feedback);
                if (attackFeedbackText != null) attackFeedbackText.text = feedback;
            }
        }

        private void OnClaimChestClicked(int milestone)
        {
            if (GuildManager.Instance != null)
            {
                bool success = GuildManager.Instance.ClaimTitanChest(milestone, out string feedback);
                if (attackFeedbackText != null) attackFeedbackText.text = feedback;
            }
        }

        private void HandleTitanDamageDealt(long dmg, long remHp)
        {
            RefreshUI();
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

            if (GameFlowManager.Instance != null)
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
