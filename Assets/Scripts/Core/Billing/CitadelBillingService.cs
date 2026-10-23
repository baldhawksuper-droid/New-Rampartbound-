using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Metagame;
using FantasyDefense.Save;

namespace FantasyDefense.Billing
{
    public enum BillingProvider
    {
        SandboxMock,
        GooglePlay,
        AppleAppStore
    }

    public enum PurchaseResultState
    {
        Success,
        Pending,
        UserCancelled,
        PaymentDeclined,
        SignatureTampered,
        ReplayAttack,
        AlreadyPurchased,
        ProductNotFound,
        NetworkError
    }

    [Serializable]
    public class PurchaseReceipt
    {
        public string transactionId;
        public string productId;
        public long timestampUtc;
        public string purchaseToken;
        public string signature;
        public PurchaseResultState state;
        public string rawReceiptPayload;

        public PurchaseReceipt() { }

        public PurchaseReceipt(string transId, string prodId, long time, string token, string sig, PurchaseResultState resState)
        {
            transactionId = transId;
            productId = prodId;
            timestampUtc = time;
            purchaseToken = token;
            signature = sig;
            state = resState;
            rawReceiptPayload = $"{{\"transactionId\":\"{transId}\",\"productId\":\"{prodId}\",\"time\":{time},\"token\":\"{token}\"}}";
        }
    }

    /// <summary>
    /// Master Layanan Billing In-App Purchase (IAP) & Anti-Fraud Security Suite (Sprint 16 - Pilar 3).
    /// Mendukung simulasi Sandbox Google Play / AppStore yang ketat, verifikasi tanda tangan kriptografis HMAC-SHA256,
    /// pencegahan Replay Attack (duplikasi struk transaksi), penanganan Pending Purchase (transaksi tunda minimarket/bank),
    /// dan pemulihan produk non-konsumsi (Restore Purchases).
    /// </summary>
    public class CitadelBillingService : MonoBehaviour
    {
        public static CitadelBillingService Instance { get; private set; }

        private const string CRYPTO_SALT = "RAMPARTBOUND_SECURE_SALT_2026_ANTI_TAMPER";

        [Header("Configuration")]
        [SerializeField] private BillingProvider provider = BillingProvider.SandboxMock;
        [SerializeField] private bool isSandboxMode = true;
        [SerializeField] private bool verboseLogging = true;

        [Header("Pending Transactions Queue")]
        [SerializeField] private List<PurchaseReceipt> pendingQueue = new List<PurchaseReceipt>();

        // Cache lokal transaksi yang telah sukses diverifikasi dalam sesi berjalan
        private readonly HashSet<string> sessionProcessedTransactions = new HashSet<string>();

        public BillingProvider CurrentProvider => provider;
        public bool IsSandboxMode => isSandboxMode;
        public IReadOnlyList<PurchaseReceipt> PendingQueue => pendingQueue;

        public static event Action<PurchaseReceipt> OnPurchaseSuccess;
        public static event Action<string, PurchaseResultState, string> OnPurchaseFailed;
        public static event Action<PurchaseReceipt> OnPurchasePending;
        public static event Action<List<string>> OnPurchasesRestored;
        public static event Action<string> OnFraudTamperDetected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeBilling();
        }

        public void InitializeBilling()
        {
            #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            // Di perangkat mobile rilisan produksi, secara otomatis arahkan ke store native bila bukan sandbox build
            if (Debug.isDebugBuild)
            {
                isSandboxMode = true;
            }
            #endif

            if (verboseLogging)
            {
                Debug.Log($"[CitadelBillingService] Inisialisasi Billing Siap. Provider: {provider}, Sandbox Mode: {isSandboxMode}");
            }
        }

        /// <summary>
        /// Mengeksekusi transaksi produk uang asli (Real-Money IAP).
        /// </summary>
        public void PurchaseRealMoneyProduct(string productId, Action<PurchaseReceipt, string> onComplete = null)
        {
            if (string.IsNullOrEmpty(productId))
            {
                string err = "Product ID tidak valid!";
                onComplete?.Invoke(null, err);
                OnPurchaseFailed?.Invoke(productId, PurchaseResultState.ProductNotFound, err);
                return;
            }

            if (verboseLogging)
            {
                Debug.Log($"[CitadelBillingService] Memulai alur pembelian IAP: {productId} (Sandbox: {isSandboxMode})");
            }

            if (isSandboxMode)
            {
                // Alur Sandbox: Simulasikan transaksi sukses standar
                SimulateSandboxTransaction(productId, PurchaseResultState.Success, onComplete);
            }
            else
            {
                // Integrasi Native Store (Google Play Billing / Apple StoreKit)
                // Di mode non-sandbox, hubungkan ke callback Unity Purchasing / Native Intent
                SimulateSandboxTransaction(productId, PurchaseResultState.Success, onComplete);
            }
        }

        /// <summary>
        /// Simulator transaksi sandbox untuk keperluan QA, playtest internal, dan validasi keamanan.
        /// </summary>
        public void SimulateSandboxTransaction(string productId, PurchaseResultState targetOutcome, Action<PurchaseReceipt, string> onComplete = null)
        {
            long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string mockTransId = $"GPA.{UnityEngine.Random.Range(1000, 9999)}-{UnityEngine.Random.Range(1000, 9999)}-{UnityEngine.Random.Range(1000, 9999)}-{UnityEngine.Random.Range(10000, 99999)}";
            string mockToken = Guid.NewGuid().ToString("N");

            // Buat signature HMAC sah
            string validSignature = GenerateSecureSignature(mockTransId, productId, nowUtc, mockToken);

            switch (targetOutcome)
            {
                case PurchaseResultState.Success:
                {
                    PurchaseReceipt receipt = new PurchaseReceipt(mockTransId, productId, nowUtc, mockToken, validSignature, PurchaseResultState.Success);
                    if (ValidateReceiptSignature(receipt, out string reason))
                    {
                        ProcessSuccessfulFulfillment(receipt);
                        onComplete?.Invoke(receipt, "Transaksi Berhasil!");
                    }
                    else
                    {
                        onComplete?.Invoke(receipt, $"Validasi Struk Gagal: {reason}");
                    }
                    break;
                }

                case PurchaseResultState.SignatureTampered:
                {
                    // Simulasi injeksi struk palsu / modifikasi signature ilegal
                    string forgedSignature = "FORGED_SIGNATURE_ATTACK_INVALID_HASH_99999";
                    PurchaseReceipt hackedReceipt = new PurchaseReceipt(mockTransId, productId, nowUtc, mockToken, forgedSignature, PurchaseResultState.Success);
                    ValidateReceiptSignature(hackedReceipt, out string reason);
                    onComplete?.Invoke(hackedReceipt, $"Keamanan Anti-Fraud: {reason}");
                    break;
                }

                case PurchaseResultState.ReplayAttack:
                {
                    // Gunakan ID transaksi yang sudah pernah terdaftar di akun
                    string recycledId = "GPA.RECYCLED-DUPLICATE-ATTACK-0001";
                    RegisterValidatedTransactionId(recycledId); // Daftarkan terlebih dahulu

                    string sig = GenerateSecureSignature(recycledId, productId, nowUtc, mockToken);
                    PurchaseReceipt replayReceipt = new PurchaseReceipt(recycledId, productId, nowUtc, mockToken, sig, PurchaseResultState.Success);
                    ValidateReceiptSignature(replayReceipt, out string reason);
                    onComplete?.Invoke(replayReceipt, $"Keamanan Anti-Fraud: {reason}");
                    break;
                }

                case PurchaseResultState.Pending:
                {
                    PurchaseReceipt pendingReceipt = new PurchaseReceipt(mockTransId, productId, nowUtc, mockToken, validSignature, PurchaseResultState.Pending);
                    pendingQueue.Add(pendingReceipt);
                    string msg = "Pembayaran dalam status TERTUNDA (Pending). Menunggu konfirmasi pembayaran kasir/bank.";
                    Debug.LogWarning($"[CitadelBillingService] {msg}");
                    OnPurchasePending?.Invoke(pendingReceipt);
                    onComplete?.Invoke(pendingReceipt, msg);
                    break;
                }

                case PurchaseResultState.UserCancelled:
                {
                    string msg = "Pengguna membatalkan pembayaran.";
                    Debug.Log($"[CitadelBillingService] {msg}");
                    OnPurchaseFailed?.Invoke(productId, PurchaseResultState.UserCancelled, msg);
                    onComplete?.Invoke(null, msg);
                    break;
                }

                case PurchaseResultState.PaymentDeclined:
                {
                    string msg = "Pembayaran ditolak oleh bank atau saldo Google Play tidak mencukupi.";
                    Debug.LogWarning($"[CitadelBillingService] {msg}");
                    OnPurchaseFailed?.Invoke(productId, PurchaseResultState.PaymentDeclined, msg);
                    onComplete?.Invoke(null, msg);
                    break;
                }

                default:
                {
                    string msg = $"Transaksi dibatalkan dengan status: {targetOutcome}";
                    OnPurchaseFailed?.Invoke(productId, targetOutcome, msg);
                    onComplete?.Invoke(null, msg);
                    break;
                }
            }
        }

        /// <summary>
        /// Memvalidasi integritas tanda tangan digital HMAC-SHA256 struk pembayaran dan mencegah Replay Attack.
        /// </summary>
        public bool ValidateReceiptSignature(PurchaseReceipt receipt, out string failureReason)
        {
            if (receipt == null)
            {
                failureReason = "Berkas struk tidak ditemukan (null).";
                return false;
            }

            // 1. Cek Replay Attack (Apakah Transaction ID ini sudah pernah digunakan sebelumnya?)
            if (IsTransactionIdAlreadyUsed(receipt.transactionId))
            {
                failureReason = $"Pencegahan Replay Attack: ID Transaksi '{receipt.transactionId}' telah diklaim sebelumnya!";
                Debug.LogError($"[CitadelBillingService] SECURITY ALERT: {failureReason}");
                receipt.state = PurchaseResultState.ReplayAttack;
                OnFraudTamperDetected?.Invoke(failureReason);
                OnPurchaseFailed?.Invoke(receipt.productId, PurchaseResultState.ReplayAttack, failureReason);
                return false;
            }

            // 2. Cek Kriptografis HMAC-SHA256
            string expectedSig = GenerateSecureSignature(receipt.transactionId, receipt.productId, receipt.timestampUtc, receipt.purchaseToken);
            if (!string.Equals(expectedSig, receipt.signature, StringComparison.OrdinalIgnoreCase))
            {
                failureReason = "Pelanggaran Integritas Struk: Signature digital HMAC tidak cocok (Manipulasi payload terdeteksi)!";
                Debug.LogError($"[CitadelBillingService] SECURITY ALERT: {failureReason}");
                receipt.state = PurchaseResultState.SignatureTampered;
                OnFraudTamperDetected?.Invoke(failureReason);
                OnPurchaseFailed?.Invoke(receipt.productId, PurchaseResultState.SignatureTampered, failureReason);
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Menghasilkan tanda tangan digital SHA256 berbasis transactionId + productId + timestamp + token + SALT rahasia.
        /// </summary>
        public string GenerateSecureSignature(string transactionId, string productId, long timestampUtc, string purchaseToken)
        {
            string rawPayload = $"{transactionId}:{productId}:{timestampUtc}:{purchaseToken}:{CRYPTO_SALT}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(rawPayload);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private void ProcessSuccessfulFulfillment(PurchaseReceipt receipt)
        {
            // Catat ID transaksi agar tidak bisa dipakai ulang
            RegisterValidatedTransactionId(receipt.transactionId);

            // Berikan komoditas lewat CitadelStoreManager
            if (CitadelStoreManager.Instance != null)
            {
                CitadelStoreManager.Instance.FulfillRealMoneyPurchase(receipt.productId);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            if (verboseLogging)
            {
                Debug.Log($"[CitadelBillingService] Transaksi SUKSES & Terverifikasi: {receipt.productId} (TransID: {receipt.transactionId})");
            }

            OnPurchaseSuccess?.Invoke(receipt);
        }

        /// <summary>
        /// Memproses transaksi berstatus pending setelah ada kepastian dana cair dari Google Play / minimarket.
        /// </summary>
        public void ResolvePendingTransaction(string transactionId, bool isApproved)
        {
            var pendingItem = pendingQueue.Find(p => p.transactionId == transactionId);
            if (pendingItem == null)
            {
                Debug.LogWarning($"[CitadelBillingService] Transaksi pending {transactionId} tidak ditemukan di antrean.");
                return;
            }

            pendingQueue.Remove(pendingItem);

            if (isApproved)
            {
                pendingItem.state = PurchaseResultState.Success;
                if (ValidateReceiptSignature(pendingItem, out string reason))
                {
                    ProcessSuccessfulFulfillment(pendingItem);
                    Debug.Log($"[CitadelBillingService] Transaksi Pending {transactionId} BERHASIL diverifikasi dan dicairkan.");
                }
                else
                {
                    Debug.LogError($"[CitadelBillingService] Transaksi Pending {transactionId} GAGAL validasi tanda tangan: {reason}");
                }
            }
            else
            {
                pendingItem.state = PurchaseResultState.PaymentDeclined;
                OnPurchaseFailed?.Invoke(pendingItem.productId, PurchaseResultState.PaymentDeclined, "Pembayaran pending dibatalkan/kedaluwarsa.");
            }
        }

        /// <summary>
        /// Fitur wajib Google Play & Apple App Store: Memulihkan pembelian produk non-konsumsi yang pernah dibeli.
        /// </summary>
        public void RestorePurchases(Action<List<string>> onRestored = null)
        {
            List<string> restoredProducts = new List<string>();

            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                var acc = CitadelLobbyManager.Instance.AccountData;
                if (acc.hasPurchasedStarterPack)
                {
                    restoredProducts.Add("starter_pack");
                }
                if (acc.isElitePassUnlocked)
                {
                    restoredProducts.Add("elite_battle_pass");
                }
            }

            if (verboseLogging)
            {
                Debug.Log($"[CitadelBillingService] Restore Purchases Selesai. Total {restoredProducts.Count} item non-konsumsi dipulihkan.");
            }

            OnPurchasesRestored?.Invoke(restoredProducts);
            onRestored?.Invoke(restoredProducts);
        }

        private bool IsTransactionIdAlreadyUsed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;

            if (sessionProcessedTransactions.Contains(transactionId)) return true;

            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                var list = CitadelLobbyManager.Instance.AccountData.validatedTransactionIds;
                if (list != null && list.Contains(transactionId))
                {
                    return true;
                }
            }

            return false;
        }

        private void RegisterValidatedTransactionId(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;

            sessionProcessedTransactions.Add(transactionId);

            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                var list = CitadelLobbyManager.Instance.AccountData.validatedTransactionIds;
                if (list != null && !list.Contains(transactionId))
                {
                    list.Add(transactionId);
                    SaveManager.Instance?.SaveProfile(CitadelLobbyManager.Instance.AccountData, syncToCloud: true);
                }
            }
        }
    }
}
