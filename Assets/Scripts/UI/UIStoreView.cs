using System;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Antarmuka Toko Citadel (UI Store View):
    /// Menampilkan katalog paket Gems, Bundle Gold, Energy Flask, dan Welcome Starter Pack.
    /// Dilengkapi saldo mata uang live dan respons cepat feedback pembelian.
    /// </summary>
    public class UIStoreView : MonoBehaviour
    {
        [Header("Root & Panels")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button closeButton;

        [Header("Live Currencies Display")]
        [SerializeField] private Text accountGemsText;
        [SerializeField] private Text accountGoldText;
        [SerializeField] private Text feedbackText;

        [Header("Product Buttons")]
        [SerializeField] private Button buyStarterPackButton;
        [SerializeField] private Text starterPackStatusText;

        [SerializeField] private Button buyGems120Button;
        [SerializeField] private Button buyGems650Button;
        [SerializeField] private Button buyGems3500Button;

        [SerializeField] private Button buyGoldBundleButton;
        [SerializeField] private Button buyEnergyFlaskButton;
        [SerializeField] private Button restorePurchasesButton;

        private void OnEnable()
        {
            CitadelStoreManager.OnStoreCatalogUpdated += RefreshUI;
            CitadelLobbyManager.OnGoldChanged += HandleGoldChanged;
            CitadelLobbyManager.OnAccountGemsChanged += HandleGemsChanged;
            FantasyDefense.Billing.CitadelBillingService.OnPurchaseSuccess += HandleBillingSuccess;
            FantasyDefense.Billing.CitadelBillingService.OnPurchaseFailed += HandleBillingFailed;
            FantasyDefense.Billing.CitadelBillingService.OnPurchasePending += HandleBillingPending;
            FantasyDefense.Billing.CitadelBillingService.OnPurchasesRestored += HandlePurchasesRestored;
            RefreshUI();
        }

        private void OnDisable()
        {
            CitadelStoreManager.OnStoreCatalogUpdated -= RefreshUI;
            CitadelLobbyManager.OnGoldChanged -= HandleGoldChanged;
            CitadelLobbyManager.OnAccountGemsChanged -= HandleGemsChanged;
            FantasyDefense.Billing.CitadelBillingService.OnPurchaseSuccess -= HandleBillingSuccess;
            FantasyDefense.Billing.CitadelBillingService.OnPurchaseFailed -= HandleBillingFailed;
            FantasyDefense.Billing.CitadelBillingService.OnPurchasePending -= HandleBillingPending;
            FantasyDefense.Billing.CitadelBillingService.OnPurchasesRestored -= HandlePurchasesRestored;
        }

        private void Start()
        {
            InitializeButtons();
            RefreshUI();
        }

        private void InitializeButtons()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseView);
            if (restorePurchasesButton != null) restorePurchasesButton.onClick.AddListener(OnRestorePurchasesClicked);

            if (buyStarterPackButton != null) buyStarterPackButton.onClick.AddListener(() => OnBuyClicked("starter_pack"));
            if (buyGems120Button != null) buyGems120Button.onClick.AddListener(() => OnBuyClicked("pack_gems_120"));
            if (buyGems650Button != null) buyGems650Button.onClick.AddListener(() => OnBuyClicked("pack_gems_650"));
            if (buyGems3500Button != null) buyGems3500Button.onClick.AddListener(() => OnBuyClicked("pack_gems_3500"));
            if (buyGoldBundleButton != null) buyGoldBundleButton.onClick.AddListener(() => OnBuyClicked("bundle_gold_5k"));
            if (buyEnergyFlaskButton != null) buyEnergyFlaskButton.onClick.AddListener(() => OnBuyClicked("flask_energy_50"));
        }

        public void RefreshUI()
        {
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (lilitaFont != null)
            {
                if (accountGemsText != null) accountGemsText.font = lilitaFont;
                if (accountGoldText != null) accountGoldText.font = lilitaFont;
                if (starterPackStatusText != null) starterPackStatusText.font = lilitaFont;
                if (feedbackText != null) feedbackText.font = lilitaFont;

                Button[] storeBtns = { buyStarterPackButton, buyGems120Button, buyGems650Button, buyGems3500Button, buyGoldBundleButton, buyEnergyFlaskButton, restorePurchasesButton };
                foreach (var b in storeBtns)
                {
                    if (b != null)
                    {
                        var t = b.GetComponentInChildren<Text>();
                        if (t != null) t.font = lilitaFont;
                    }
                }
            }

            // Update currency balances
            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                var acc = CitadelLobbyManager.Instance.AccountData;
                if (accountGemsText != null) accountGemsText.text = $"💎 {acc.accountGems:N0} Gems";
                if (accountGoldText != null) accountGoldText.text = $"🪙 {acc.goldCoins:N0} Gold";
            }

            // Update starter pack status
            if (CitadelStoreManager.Instance != null)
            {
                var starter = CitadelStoreManager.Instance.Catalog.Find(p => p.productId == "starter_pack");
                if (starter != null && buyStarterPackButton != null)
                {
                    buyStarterPackButton.interactable = !starter.isPurchased;
                    if (starterPackStatusText != null)
                    {
                        starterPackStatusText.text = starter.isPurchased 
                            ? FantasyDefense.Localization.CitadelLocalizationManager.Text("TERBELI (1x ONLY) ✔", "PURCHASED (1x ONLY) ✔")
                            : FantasyDefense.Localization.CitadelLocalizationManager.Text("BELI Rp 15.000", "BUY $1.99");
                    }
                }
            }
        }

        private void OnBuyClicked(string productId)
        {
            if (CitadelStoreManager.Instance != null)
            {
                bool success = CitadelStoreManager.Instance.PurchaseProduct(productId, out string msg);
                if (feedbackText != null)
                {
                    feedbackText.text = msg;
                }
            }
        }

        private void HandleGoldChanged(int g) => RefreshUI();
        private void HandleGemsChanged(int gems) => RefreshUI();

        private void OnRestorePurchasesClicked()
        {
            if (feedbackText != null) feedbackText.text = "Memeriksa riwayat pembelian akun...";
            if (FantasyDefense.Billing.CitadelBillingService.Instance != null)
            {
                FantasyDefense.Billing.CitadelBillingService.Instance.RestorePurchases();
            }
        }

        private void HandleBillingSuccess(FantasyDefense.Billing.PurchaseReceipt receipt)
        {
            if (feedbackText != null)
            {
                feedbackText.text = $"Pembayaran '{receipt.productId}' Berhasil!";
            }
            RefreshUI();
        }

        private void HandleBillingFailed(string productId, FantasyDefense.Billing.PurchaseResultState errorState, string errorMsg)
        {
            if (feedbackText != null)
            {
                feedbackText.text = $"Gagal ({errorState}): {errorMsg}";
            }
        }

        private void HandleBillingPending(FantasyDefense.Billing.PurchaseReceipt receipt)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "Transaksi Tertunda: Menunggu konfirmasi pembayaran kasir/bank.";
            }
        }

        private void HandlePurchasesRestored(System.Collections.Generic.List<string> restored)
        {
            if (feedbackText != null)
            {
                feedbackText.text = restored.Count > 0
                    ? $"Berhasil memulihkan {restored.Count} produk non-konsumsi!"
                    : "Tidak ada produk non-konsumsi yang perlu dipulihkan.";
            }
            RefreshUI();
        }

        public void OpenView()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            else gameObject.SetActive(true);

            transform.SetAsLastSibling();

            var cg = GetComponent<CanvasGroup>() ?? rootPanel?.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            RefreshUI();
        }

        public void CloseView()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            else gameObject.SetActive(false);

            if (FantasyDefense.Core.GameFlowManager.Instance != null)
            {
                FantasyDefense.Core.GameFlowManager.Instance.CloseCurrentPanel();
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(Audio.SoundType.ButtonClick);
            }
        }
    }
}
