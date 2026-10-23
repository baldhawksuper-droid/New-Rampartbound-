#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FantasyDefense.Billing;
using FantasyDefense.Metagame;

namespace Rampartbound.Editor
{
    /// <summary>
    /// Editor Window Interaktif untuk Simulasi In-App Purchase (IAP) Sandbox Billing & Anti-Fraud Suite (Sprint 16 - Pilar 3).
    /// Mengizinkan tim developer & QA menguji skenario Google Play Billing secara komprehensif tanpa kartu kredit:
    /// Transaksi Sukses, Pembatalan User, Saldo Ditolak, Transaksi Tertunda (Pending),
    /// serta simulasi serangan manipulasi struk (Tampered Signature) dan serangan Replay Attack.
    /// </summary>
    public class CitadelBillingSandboxWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private string selectedProductId = "starter_pack";
        private string lastStatusMessage = "Siap melakukan simulasi transaksi sandbox.";
        private MessageType lastMessageType = MessageType.Info;
        private List<string> simulationLogs = new List<string>();

        private readonly string[] productIds = new[]
        {
            "starter_pack",
            "pack_gems_120",
            "pack_gems_650",
            "pack_gems_3500"
        };

        private readonly string[] productLabels = new[]
        {
            "Welcome Starter Pack ($1.99 - 1x Only)",
            "Pouch of Gems ($0.99 - +120 Gems)",
            "Chest of Gems ($4.99 - +650 Gems)",
            "Vault of Gems ($19.99 - +3500 Gems)"
        };

        [MenuItem("Rampartbound/IAP Billing Sandbox & Security Test", false, 4)]
        public static void OpenSandboxWindow()
        {
            var window = GetWindow<CitadelBillingSandboxWindow>("IAP Sandbox & Security");
            window.minSize = new Vector2(580, 680);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 1. Header Banner
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Citadel IAP Sandbox Billing & Anti-Fraud Suite", headerStyle);
            EditorGUILayout.LabelField("Modul Pengujian Google Play / AppStore Sandbox & Validasi Kriptografis HMAC-SHA256", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 2. Status Akun & Billing
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Live Account & Billing Status", EditorStyles.boldLabel);
            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                var acc = CitadelLobbyManager.Instance.AccountData;
                EditorGUILayout.LabelField($"Citadel Level: {acc.citadelLevel} | Gems: {acc.accountGems:N0} | Gold: {acc.goldCoins:N0}");
                EditorGUILayout.LabelField($"Starter Pack Terbeli: {(acc.hasPurchasedStarterPack ? "YA (Owned)" : "BELUM")}");
                EditorGUILayout.LabelField($"Total Transaksi Tervalidasi: {acc.validatedTransactionIds.Count} transaksi");
            }
            else
            {
                EditorGUILayout.HelpBox("Scene game sedang tidak berjalan atau CitadelLobbyManager belum aktif. Mode simulasi standalone siap digunakan.", MessageType.Info);
            }

            var billing = CitadelBillingService.Instance;
            if (billing != null)
            {
                EditorGUILayout.LabelField($"Billing Service: AKTIF (Provider: {billing.CurrentProvider}, Sandbox: {billing.IsSandboxMode})");
                EditorGUILayout.LabelField($"Antrean Pending: {billing.PendingQueue.Count} transaksi menunggu penyelesaian");
            }
            else
            {
                EditorGUILayout.HelpBox("CitadelBillingService belum terpasang di Scene berjalan. Simulasi akan menginstansiasi objek runtime secara otomatis jika tombol diklik.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 3. Pilihan Produk
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pilih Produk Toko Real-Money (IAP)", EditorStyles.boldLabel);
            int selectedIndex = Array.IndexOf(productIds, selectedProductId);
            if (selectedIndex < 0) selectedIndex = 0;
            selectedIndex = EditorGUILayout.Popup("Target Product", selectedIndex, productLabels);
            selectedProductId = productIds[selectedIndex];
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 4. Tombol Skenario Simulasi
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Eksekusi Skenario Pengujian Transaksi", EditorStyles.boldLabel);

            // Skenario 1: Transaksi Normal Berhasil
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
            if (GUILayout.Button("1. Simulasi Pembelian Sukses (Google Play Standard Test Card)", GUILayout.Height(32)))
            {
                RunSimulation(PurchaseResultState.Success);
            }

            GUILayout.Space(4);

            // Skenario 2: Transaksi Pending
            GUI.backgroundColor = new Color(1.0f, 0.85f, 0.3f);
            if (GUILayout.Button("2. Simulasi Pembayaran Tertunda (Pending Minimarket/ATM Transfer)", GUILayout.Height(30)))
            {
                RunSimulation(PurchaseResultState.Pending);
            }

            GUILayout.Space(4);

            // Skenario 3: Transaksi Dibatalkan User
            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
            if (GUILayout.Button("3. Simulasi User Membatalkan Pembelian (Back Button/Cancel)", GUILayout.Height(28)))
            {
                RunSimulation(PurchaseResultState.UserCancelled);
            }

            GUILayout.Space(4);

            // Skenario 4: Pembayaran Ditolak Bank
            GUI.backgroundColor = new Color(0.95f, 0.6f, 0.6f);
            if (GUILayout.Button("4. Simulasi Kartu Ditolak / Saldo Kurang (Payment Declined)", GUILayout.Height(28)))
            {
                RunSimulation(PurchaseResultState.PaymentDeclined);
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Pengujian Keamanan Kriptografis Anti-Fraud", EditorStyles.boldLabel);

            // Skenario 5: Tampered Receipt Attack
            GUI.backgroundColor = new Color(0.95f, 0.4f, 0.4f);
            if (GUILayout.Button("5. Uji Coba Hacking: Struk Palsu (Tampered Signature Attack)", GUILayout.Height(32)))
            {
                RunSimulation(PurchaseResultState.SignatureTampered);
            }

            GUILayout.Space(4);

            // Skenario 6: Replay Attack
            GUI.backgroundColor = new Color(0.95f, 0.3f, 0.3f);
            if (GUILayout.Button("6. Uji Coba Hacking: Replay Attack (Struk Bekas Pakai)", GUILayout.Height(32)))
            {
                RunSimulation(PurchaseResultState.ReplayAttack);
            }

            GUILayout.Space(8);
            GUI.backgroundColor = Color.white;

            // Skenario 7: Restore Purchases & Selesaikan Pending
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore Purchases (Non-Consumable)", GUILayout.Height(28)))
            {
                EnsureBillingService();
                CitadelBillingService.Instance.RestorePurchases((restored) =>
                {
                    LogMessage($"[RESTORE] {restored.Count} produk dipulihkan: {string.Join(", ", restored)}", MessageType.Info);
                });
            }

            if (GUILayout.Button("Cairkan 1 Transaksi Pending", GUILayout.Height(28)))
            {
                EnsureBillingService();
                if (CitadelBillingService.Instance.PendingQueue.Count > 0)
                {
                    var item = CitadelBillingService.Instance.PendingQueue[0];
                    CitadelBillingService.Instance.ResolvePendingTransaction(item.transactionId, true);
                    LogMessage($"[RESOLVE PENDING] Transaksi {item.transactionId} telah dicairkan & diverifikasi!", MessageType.Info);
                }
                else
                {
                    LogMessage("Tidak ada transaksi dalam antrean pending.", MessageType.Warning);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 5. Pesan Hasil Terakhir
            EditorGUILayout.HelpBox(lastStatusMessage, lastMessageType);

            GUILayout.Space(10);

            // 6. Konsol Riwayat Audit Simulasi
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Log Riwayat Transaksi Sandbox", EditorStyles.boldLabel);
            if (GUILayout.Button("Bersihkan Log", GUILayout.Width(100)))
            {
                simulationLogs.Clear();
            }
            EditorGUILayout.EndHorizontal();

            if (simulationLogs.Count == 0)
            {
                EditorGUILayout.LabelField("Belum ada transaksi yang disimulasikan.", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = simulationLogs.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.SelectableLabel(simulationLogs[i], EditorStyles.miniLabel, GUILayout.Height(18));
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void RunSimulation(PurchaseResultState targetOutcome)
        {
            EnsureBillingService();

            CitadelBillingService.Instance.SimulateSandboxTransaction(selectedProductId, targetOutcome, (receipt, msg) =>
            {
                MessageType type = MessageType.Info;
                if (targetOutcome == PurchaseResultState.Success) type = MessageType.Info;
                else if (targetOutcome == PurchaseResultState.Pending) type = MessageType.Warning;
                else if (targetOutcome == PurchaseResultState.SignatureTampered || targetOutcome == PurchaseResultState.ReplayAttack) type = MessageType.Error;
                else type = MessageType.Warning;

                string time = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{time}] [{targetOutcome}] {selectedProductId} -> {msg}";
                LogMessage(logEntry, type);
            });
        }

        private void LogMessage(string text, MessageType type)
        {
            lastStatusMessage = text;
            lastMessageType = type;
            simulationLogs.Add(text);
            Repaint();
        }

        private void EnsureBillingService()
        {
            if (CitadelBillingService.Instance == null)
            {
                var existing = FindAnyObjectByType<CitadelBillingService>();
                if (existing == null)
                {
                    var holder = new GameObject("CitadelBillingService_TestMock");
                    holder.AddComponent<CitadelBillingService>();
                }
            }
        }
    }
}
#endif
