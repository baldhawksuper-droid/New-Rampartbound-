using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    public enum StoreProductType
    {
        GemsPack,
        GoldBundle,
        EnergyFlask,
        WelcomePack
    }

    [Serializable]
    public class StoreProductItem
    {
        public string productId;
        public string title;
        public string description;
        public StoreProductType productType;
        public string priceString; // e.g. "$0.99", "$4.99", "50 Gems"
        public bool isRealMoney;
        public int gemCost;
        public int rewardGems;
        public int rewardGold;
        public int rewardEnergy;
        public int rewardHeroShards;
        public bool isOneTimeOnly;
        public bool isPurchased;

        public StoreProductItem() { }

        public StoreProductItem(string id, string title, string desc, StoreProductType type, string price, bool realMoney, int gemsCost, int rGems, int rGold, int rEnergy, int rShards = 0, bool oneTime = false)
        {
            productId = id;
            this.title = title;
            description = desc;
            productType = type;
            priceString = price;
            isRealMoney = realMoney;
            gemCost = gemsCost;
            rewardGems = rGems;
            rewardGold = rGold;
            rewardEnergy = rEnergy;
            rewardHeroShards = rShards;
            isOneTimeOnly = oneTime;
            isPurchased = false;
        }
    }

    /// <summary>
    /// Master Manager untuk Katalog Toko Game (Citadel In-App Purchase Store).
    /// Mengelola produk Gems, Gold, Energy Flask, dan Paket Pemula (Starter Pack).
    /// Arsitektur siap dihubungkan langsung ke modul Unity IAP (Google Play Billing / Apple StoreKit).
    /// </summary>
    public class CitadelStoreManager : MonoBehaviour
    {
        public static CitadelStoreManager Instance { get; private set; }

        [Header("Catalog Products")]
        [SerializeField] private List<StoreProductItem> catalog = new List<StoreProductItem>();

        public List<StoreProductItem> Catalog => catalog;

        public static event Action<StoreProductItem> OnProductPurchased;
        public static event Action OnStoreCatalogUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultCatalog();
        }

        private void InitializeDefaultCatalog()
        {
            if (catalog == null || catalog.Count == 0)
            {
                catalog = new List<StoreProductItem>
                {
                    new StoreProductItem("starter_pack", "Welcome Starter Pack", "300 Gems, 10,000 Gold, 50 Cedric Shards, +30 Stamina", StoreProductType.WelcomePack, "$1.99", true, 0, 300, 10000, 30, 50, true),
                    new StoreProductItem("pack_gems_120", "Pouch of Gems", "+120 Citadel Gems untuk gacha & item.", StoreProductType.GemsPack, "$0.99", true, 0, 120, 0, 0),
                    new StoreProductItem("pack_gems_650", "Chest of Gems", "+650 Citadel Gems (+10% Ekstra Bonus).", StoreProductType.GemsPack, "$4.99", true, 0, 650, 0, 0),
                    new StoreProductItem("pack_gems_3500", "Vault of Gems", "+3,500 Citadel Gems (+25% Nilai Terbaik).", StoreProductType.GemsPack, "$19.99", true, 0, 3500, 0, 0),
                    new StoreProductItem("bundle_gold_5k", "Pundi Emas Kerajaan", "+5,000 Gold untuk upgrade hero & donasi guild.", StoreProductType.GoldBundle, "50 Gems", false, 50, 0, 5000, 0),
                    new StoreProductItem("flask_energy_50", "Stamina Mana Flask", "Memulihkan +50 Poin Stamina Benteng.", StoreProductType.EnergyFlask, "30 Gems", false, 30, 0, 0, 50)
                };
            }
        }

        private void Start()
        {
            SyncCatalogWithAccountData();
        }

        public void SyncCatalogWithAccountData()
        {
            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                var starter = catalog.Find(p => p.productId == "starter_pack");
                if (starter != null && CitadelLobbyManager.Instance.AccountData.hasPurchasedStarterPack)
                {
                    starter.isPurchased = true;
                }
            }
            OnStoreCatalogUpdated?.Invoke();
        }

        /// <summary>
        /// Mengeksekusi transaksi pembelian item toko.
        /// Untuk produk real-money (IAP), diarahkan melalui CitadelBillingService.
        /// Untuk produk gem-cost (Soft-Currency), diproses langsung dari saldo akun.
        /// </summary>
        public bool PurchaseProduct(string productId, out string resultMessage)
        {
            var product = catalog.Find(p => p.productId == productId);
            if (product == null)
            {
                resultMessage = "Produk tidak ditemukan di katalog!";
                return false;
            }

            if (product.isOneTimeOnly && product.isPurchased)
            {
                resultMessage = "Paket ini hanya dapat dibeli satu kali!";
                return false;
            }

            // Alur Real-Money IAP (Google Play Billing / Sandbox)
            if (product.isRealMoney)
            {
                if (FantasyDefense.Billing.CitadelBillingService.Instance != null)
                {
                    FantasyDefense.Billing.CitadelBillingService.Instance.PurchaseRealMoneyProduct(productId, (receipt, billingMsg) =>
                    {
                        Debug.Log($"[CitadelStoreManager] Respons Billing untuk {productId}: {billingMsg}");
                    });
                    resultMessage = $"Memproses pembelian aman '{product.title}' ({product.priceString})...";
                    return true;
                }
                else
                {
                    // Fallback bila billing service offline
                    return FulfillRealMoneyPurchase(productId, out resultMessage);
                }
            }

            // Validasi biaya Gems jika produk berbasis Gems
            if (product.gemCost > 0)
            {
                if (CitadelLobbyManager.Instance != null && !CitadelLobbyManager.Instance.SpendGems(product.gemCost))
                {
                    resultMessage = $"Gems tidak mencukupi ({product.gemCost} Gems diperlukan)!";
                    return false;
                }
            }

            // Berikan komoditas ke pemain
            GrantProductRewards(product);

            if (product.isOneTimeOnly)
            {
                product.isPurchased = true;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            resultMessage = $"Pembelian '{product.title}' Berhasil! Hadiah telah masuk ke akun bentengmu.";
            Debug.Log($"[CitadelStoreManager] Transaksi Sukses: {product.productId} ({product.priceString})");

            // Live-Ops KPI Telemetry Tracking
            if (FantasyDefense.Analytics.CitadelTelemetryManager.Instance != null)
            {
                FantasyDefense.Analytics.CitadelTelemetryManager.Instance.TrackStorePurchase(
                    product.productId,
                    product.priceString,
                    product.isRealMoney,
                    true
                );
            }

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnProductPurchased?.Invoke(product);
            OnStoreCatalogUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Memberikan hadiah produk IAP yang telah tuntas diverifikasi oleh CitadelBillingService.
        /// </summary>
        public bool FulfillRealMoneyPurchase(string productId, out string resultMessage)
        {
            var product = catalog.Find(p => p.productId == productId);
            if (product == null)
            {
                resultMessage = "Produk tidak ditemukan!";
                return false;
            }

            GrantProductRewards(product);

            if (product.isOneTimeOnly || product.productId == "starter_pack")
            {
                product.isPurchased = true;
                if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
                {
                    CitadelLobbyManager.Instance.AccountData.hasPurchasedStarterPack = true;
                }
            }

            resultMessage = $"Pembelian '{product.title}' Berhasil! Hadiah telah masuk ke akun bentengmu.";
            Debug.Log($"[CitadelStoreManager] IAP Fulfillment Sukses: {product.productId} ({product.priceString})");

            // Live-Ops KPI Telemetry Tracking
            if (FantasyDefense.Analytics.CitadelTelemetryManager.Instance != null)
            {
                FantasyDefense.Analytics.CitadelTelemetryManager.Instance.TrackStorePurchase(
                    product.productId,
                    product.priceString,
                    true,
                    true
                );
            }

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnProductPurchased?.Invoke(product);
            OnStoreCatalogUpdated?.Invoke();
            return true;
        }

        public bool FulfillRealMoneyPurchase(string productId)
        {
            return FulfillRealMoneyPurchase(productId, out _);
        }

        private void GrantProductRewards(StoreProductItem product)
        {
            if (CitadelLobbyManager.Instance != null)
            {
                if (product.rewardGems > 0) CitadelLobbyManager.Instance.AddGems(product.rewardGems);
                if (product.rewardGold > 0) CitadelLobbyManager.Instance.AddGold(product.rewardGold);
                if (product.rewardEnergy > 0) CitadelLobbyManager.Instance.RestoreEnergy(product.rewardEnergy);

                if (product.rewardHeroShards > 0 && CitadelLobbyManager.Instance.AccountData != null)
                {
                    var cedric = CitadelLobbyManager.Instance.AccountData.activeRoster.Find(h => h.unitType == Data.UnitType.Cedric);
                    if (cedric != null) cedric.heroShards += product.rewardHeroShards;
                }
            }
        }
    }
}
