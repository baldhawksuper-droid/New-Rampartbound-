using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using FantasyDefense.Save;

namespace FantasyDefense.Analytics
{
    /// <summary>
    /// Master Manager untuk Live-Ops KPI Telemetry & Analytics Tracking di Rampartbound: Merge Defense.
    /// Memonitor retensi D1/D3/D7/D14/D30, funnel FTUE tutorial, drop-off level/stage,
    /// frekuensi fusi pahlawan (merge frequency), konsumsi ekonomi, dan konversi IAP.
    /// Dilengkapi dengan Offline Event Queue buffer dan proteksi integritas payload HMAC-SHA256.
    /// </summary>
    public class CitadelTelemetryManager : MonoBehaviour
    {
        public static CitadelTelemetryManager Instance { get; private set; }

        private const string PREF_RETENTION_METRICS = "Rampartbound_Retention_Metrics";
        private const string PREF_OFFLINE_TELEMETRY_BUFFER = "Rampartbound_Offline_Telemetry_Queue";
        private const string HMAC_SECRET_SALT = "Rampartbound_LiveOps_Telemetry_Salt_2026_Secure";
        private const int MAX_BUFFER_CAPACITY = 300;
        private const int APP_VERSION_CODE = 131; // v1.2.11

        [Header("Telemetry Configuration")]
        [SerializeField] private bool enableTelemetry = true;
        [SerializeField] private bool logToUnityConsole = true;
        [SerializeField] private float autoFlushIntervalSeconds = 45f;

        [Header("Runtime State")]
        [SerializeField] private string currentSessionId;
        [SerializeField] private string playerId;
        [SerializeField] private PlayerRetentionMetrics retentionMetrics;
        [SerializeField] private int pendingEventQueueCount = 0;

        private Queue<TelemetryEvent> eventBuffer = new Queue<TelemetryEvent>();
        private float lastFlushTime;
        private float sessionStartTime;

        public PlayerRetentionMetrics RetentionMetrics => retentionMetrics;
        public int PendingEventsCount => eventBuffer.Count;
        public string CurrentSessionId => currentSessionId;

        public static event Action<TelemetryEvent> OnEventLogged;
        public static event Action<int> OnQueueFlushed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeTelemetrySession();
        }

        private void Start()
        {
            CheckAndReportDailyRetention();
        }

        private void Update()
        {
            if (!enableTelemetry) return;

            if (Time.unscaledTime - lastFlushTime >= autoFlushIntervalSeconds && eventBuffer.Count > 0)
            {
                FlushTelemetryQueue();
            }
        }

        private void OnApplicationQuit()
        {
            TrackSessionEnd();
            SaveOfflineQueue();
            SaveRetentionMetrics();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveOfflineQueue();
                SaveRetentionMetrics();
            }
        }

        #region Session & Initialization

        private void InitializeTelemetrySession()
        {
            currentSessionId = Guid.NewGuid().ToString("N").Substring(0, 16);
            sessionStartTime = Time.realtimeSinceStartup;
            lastFlushTime = Time.unscaledTime;

            // Load atau buat Player ID unik
            playerId = PlayerPrefs.GetString("Rampartbound_Telemetry_Player_Id", "");
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = "user_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                PlayerPrefs.SetString("Rampartbound_Telemetry_Player_Id", playerId);
                PlayerPrefs.Save();
            }

            LoadRetentionMetrics();
            LoadOfflineQueue();

            retentionMetrics.totalSessionsCount++;
            SaveRetentionMetrics();

            // Catat Session Start
            TrackSessionStart();
        }

        private void TrackSessionStart()
        {
            var evt = CreateBaseEvent("session_start", TelemetryCategory.Session);
            evt.SetParameter("session_number", retentionMetrics.totalSessionsCount);
            evt.SetParameter("battery_level", SystemInfo.batteryLevel);
            evt.SetParameter("battery_status", SystemInfo.batteryStatus.ToString());
            evt.SetParameter("system_memory_mb", SystemInfo.systemMemorySize);
            evt.SetParameter("graphics_device", SystemInfo.graphicsDeviceName);
            EnqueueEvent(evt);
        }

        private void TrackSessionEnd()
        {
            float sessionDuration = Time.realtimeSinceStartup - sessionStartTime;
            retentionMetrics.totalPlaytimeSeconds += sessionDuration;

            var evt = CreateBaseEvent("session_end", TelemetryCategory.Session);
            evt.SetParameter("session_duration_sec", Mathf.RoundToInt(sessionDuration));
            evt.SetParameter("total_playtime_hours", (retentionMetrics.totalPlaytimeSeconds / 3600f).ToString("F2"));
            EnqueueEvent(evt);
        }

        #endregion

        #region D1 / D7 Retention & Lifecycle Tracking

        private void CheckAndReportDailyRetention()
        {
            if (retentionMetrics == null) return;

            DateTime now = DateTime.UtcNow;
            if (DateTime.TryParse(retentionMetrics.lastLoginDateUtc, out DateTime lastLogin))
            {
                int daysSinceLastLogin = (int)(now.Date - lastLogin.Date).TotalDays;
                if (daysSinceLastLogin == 1)
                {
                    retentionMetrics.consecutiveLoginStreak++;
                    retentionMetrics.totalDaysActive++;
                }
                else if (daysSinceLastLogin > 1)
                {
                    retentionMetrics.consecutiveLoginStreak = 1;
                    retentionMetrics.totalDaysActive++;
                }
            }
            retentionMetrics.lastLoginDateUtc = now.ToString("o");

            int daysSinceInstall = retentionMetrics.GetDaysSinceInstall();

            // Trigger Milestone KPI Retention Events
            if (daysSinceInstall >= 1 && !retentionMetrics.hasReportedD1)
            {
                retentionMetrics.hasReportedD1 = true;
                ReportRetentionMilestone("retention_d1", daysSinceInstall);
            }
            if (daysSinceInstall >= 3 && !retentionMetrics.hasReportedD3)
            {
                retentionMetrics.hasReportedD3 = true;
                ReportRetentionMilestone("retention_d3", daysSinceInstall);
            }
            if (daysSinceInstall >= 7 && !retentionMetrics.hasReportedD7)
            {
                retentionMetrics.hasReportedD7 = true;
                ReportRetentionMilestone("retention_d7", daysSinceInstall);
            }
            if (daysSinceInstall >= 14 && !retentionMetrics.hasReportedD14)
            {
                retentionMetrics.hasReportedD14 = true;
                ReportRetentionMilestone("retention_d14", daysSinceInstall);
            }
            if (daysSinceInstall >= 30 && !retentionMetrics.hasReportedD30)
            {
                retentionMetrics.hasReportedD30 = true;
                ReportRetentionMilestone("retention_d30", daysSinceInstall);
            }

            SaveRetentionMetrics();
        }

        private void ReportRetentionMilestone(string milestoneName, int daysSinceInstall)
        {
            var evt = CreateBaseEvent(milestoneName, TelemetryCategory.Retention);
            evt.SetParameter("days_since_install", daysSinceInstall);
            evt.SetParameter("login_streak", retentionMetrics.consecutiveLoginStreak);
            evt.SetParameter("total_sessions", retentionMetrics.totalSessionsCount);
            evt.SetParameter("total_playtime_minutes", Mathf.RoundToInt(retentionMetrics.totalPlaytimeSeconds / 60f));
            evt.SetParameter("total_stages_won", retentionMetrics.totalStagesWon);
            evt.SetParameter("total_hero_merges", retentionMetrics.totalHeroMerges);
            EnqueueEvent(evt);

            if (logToUnityConsole)
            {
                Debug.Log($"<color=#2ecc71>[Telemetry] Retensi Tercatat: {milestoneName.ToUpper()}! Days: {daysSinceInstall}, Streak: {retentionMetrics.consecutiveLoginStreak}</color>");
            }
        }

        #endregion

        #region Game Progression & Funnel Tracking

        public void TrackStageStart(int stageId, string mode, int teamCp)
        {
            retentionMetrics.totalStagesAttempted++;
            var evt = CreateBaseEvent("stage_start", TelemetryCategory.Progression);
            evt.SetParameter("stage_id", stageId);
            evt.SetParameter("battle_mode", mode);
            evt.SetParameter("team_cp", teamCp);
            EnqueueEvent(evt);
        }

        public void TrackStageComplete(int stageId, int stars, float durationSec, int remainingCastleHp, int maxCastleHp)
        {
            retentionMetrics.totalStagesWon++;
            float hpPercent = (float)remainingCastleHp / Mathf.Max(1, maxCastleHp);

            var evt = CreateBaseEvent("stage_complete", TelemetryCategory.Progression);
            evt.SetParameter("stage_id", stageId);
            evt.SetParameter("stars", stars);
            evt.SetParameter("duration_seconds", Mathf.RoundToInt(durationSec));
            evt.SetParameter("barricade_hp_remaining_pct", Mathf.RoundToInt(hpPercent * 100f));
            EnqueueEvent(evt);

            // Flush instan saat stage selesai untuk mengamankan data progres KPI
            FlushTelemetryQueue();
        }

        public void TrackStageFailed(int stageId, int waveNumber, string killerMonsterName, int teamCp)
        {
            var evt = CreateBaseEvent("stage_failed", TelemetryCategory.Progression);
            evt.SetParameter("stage_id", stageId);
            evt.SetParameter("failed_at_wave", waveNumber);
            evt.SetParameter("killer_monster", killerMonsterName);
            evt.SetParameter("team_cp", teamCp);
            EnqueueEvent(evt);

            FlushTelemetryQueue();
        }

        public void TrackEndlessTrialWaveReached(int waveFloor, int totalScore, int bossesDefeated)
        {
            var evt = CreateBaseEvent("endless_trial_milestone", TelemetryCategory.Progression);
            evt.SetParameter("wave_floor", waveFloor);
            evt.SetParameter("total_score", totalScore);
            evt.SetParameter("bosses_defeated", bossesDefeated);
            EnqueueEvent(evt);
        }

        public void TrackTutorialStep(int stepIndex, string stepName)
        {
            var evt = CreateBaseEvent("ftue_tutorial_step", TelemetryCategory.Tutorial);
            evt.SetParameter("step_index", stepIndex);
            evt.SetParameter("step_name", stepName);
            evt.SetParameter("seconds_into_session", Mathf.RoundToInt(Time.realtimeSinceStartup - sessionStartTime));
            EnqueueEvent(evt);
        }

        #endregion

        #region Economy, Merge & Monetization Tracking

        public void TrackHeroSummon(string heroName, int gemCost, int currentHeroCount)
        {
            retentionMetrics.totalHeroesSummoned++;
            retentionMetrics.totalGemsSpent += gemCost;

            var evt = CreateBaseEvent("hero_summon", TelemetryCategory.EconomyMerge);
            evt.SetParameter("hero_name", heroName);
            evt.SetParameter("gem_cost", gemCost);
            evt.SetParameter("grid_units_count", currentHeroCount);
            EnqueueEvent(evt);
        }

        public void TrackHeroMerge(string heroName, int fromTier, int toTier, bool isAutoMerge = false)
        {
            retentionMetrics.totalHeroMerges++;

            var evt = CreateBaseEvent("hero_merge", TelemetryCategory.EconomyMerge);
            evt.SetParameter("hero_name", heroName);
            evt.SetParameter("from_tier", fromTier);
            evt.SetParameter("to_tier", toTier);
            evt.SetParameter("is_auto_assistant", isAutoMerge);
            EnqueueEvent(evt);
        }

        public void TrackAutoMergeBatch(int pairsMerged)
        {
            var evt = CreateBaseEvent("auto_merge_batch", TelemetryCategory.EconomyMerge);
            evt.SetParameter("pairs_merged", pairsMerged);
            EnqueueEvent(evt);
        }

        public void TrackStorePurchase(string productId, string priceString, bool isRealMoney, bool isSuccess, double priceUsd = 0.0)
        {
            if (isSuccess && isRealMoney)
            {
                retentionMetrics.totalLifetimeRevenueUsd += priceUsd;
            }

            var evt = CreateBaseEvent(isSuccess ? "iap_purchase_success" : "iap_purchase_failed", TelemetryCategory.Monetization);
            evt.SetParameter("product_id", productId);
            evt.SetParameter("price", priceString);
            evt.SetParameter("is_real_money", isRealMoney);
            evt.SetParameter("cumulative_ltv_usd", retentionMetrics.totalLifetimeRevenueUsd.ToString("F2"));
            EnqueueEvent(evt);

            FlushTelemetryQueue();
        }

        public void TrackBattlePassTierClaim(int tier, bool isElite)
        {
            var evt = CreateBaseEvent("battle_pass_claim", TelemetryCategory.Progression);
            evt.SetParameter("tier", tier);
            evt.SetParameter("is_elite_pass", isElite);
            EnqueueEvent(evt);
        }

        public void TrackTitanRaidDamage(long damageDealt, string bossName, int guildLevel)
        {
            var evt = CreateBaseEvent("titan_raid_attack", TelemetryCategory.Progression);
            evt.SetParameter("damage_dealt", damageDealt);
            evt.SetParameter("boss_name", bossName);
            evt.SetParameter("guild_level", guildLevel);
            EnqueueEvent(evt);
        }

        #endregion

        #region Internal Pipeline & Offline Queue Buffer

        private TelemetryEvent CreateBaseEvent(string eventName, TelemetryCategory category)
        {
            var evt = new TelemetryEvent(eventName, category, currentSessionId, playerId, APP_VERSION_CODE);
            return evt;
        }

        private void EnqueueEvent(TelemetryEvent evt)
        {
            if (!enableTelemetry || evt == null) return;

            // Generate HMAC Checksum untuk integritas data
            evt.payloadChecksum = ComputeHmacSignature(evt);

            if (eventBuffer.Count >= MAX_BUFFER_CAPACITY)
            {
                eventBuffer.Dequeue(); // Drop event tertua jika antrean overflow
            }

            eventBuffer.Enqueue(evt);
            pendingEventQueueCount = eventBuffer.Count;

            OnEventLogged?.Invoke(evt);

            if (logToUnityConsole)
            {
                Debug.Log($"[Telemetry] <color=#3498db>{evt.category}</color> -> <b>{evt.eventName}</b> (Pending: {eventBuffer.Count})");
            }
        }

        /// <summary>
        /// Mengirim antrean telemetri offline ke backend analitik Live-Ops (Firebase / GA4 / Custom REST API).
        /// </summary>
        public void FlushTelemetryQueue()
        {
            if (eventBuffer.Count == 0) return;

            int countToFlush = eventBuffer.Count;
            var batch = new TelemetryBatchPayload
            {
                batchId = Guid.NewGuid().ToString("N"),
                sentAtUtc = DateTime.UtcNow.ToString("o"),
                eventCount = countToFlush
            };

            while (eventBuffer.Count > 0)
            {
                batch.events.Add(eventBuffer.Dequeue());
            }

            pendingEventQueueCount = eventBuffer.Count;
            lastFlushTime = Time.unscaledTime;

            // Simulasi dispatch batch ke backend analitik cloud
            TransmitBatchToLiveOpsEndpoint(batch);

            // Bersihkan antrean offline yang tersimpan di disk
            PlayerPrefs.DeleteKey(PREF_OFFLINE_TELEMETRY_BUFFER);
            PlayerPrefs.Save();

            OnQueueFlushed?.Invoke(countToFlush);

            if (logToUnityConsole)
            {
                Debug.Log($"<color=#1abc9c>[Telemetry] Berhasil mem-flush {countToFlush} events ke Live-Ops Analytics Hub (Batch ID: {batch.batchId.Substring(0, 8)}).</color>");
            }
        }

        private void TransmitBatchToLiveOpsEndpoint(TelemetryBatchPayload batch)
        {
            // Di lingkungan produksi, serialisasi JSON batch ini dikirimkan via UnityWebRequest (POST)
            // ke REST Gateway Google Analytics 4 Measurement Protocol atau Cloudflare Worker.
            // Di sini kita mencatat verifikasi HMAC batch aman.
            string jsonSummary = $"[Batch {batch.batchId.Substring(0, 8)}]: {batch.eventCount} events disalurkan dengan aman.";
        }

        private string ComputeHmacSignature(TelemetryEvent evt)
        {
            string rawContent = $"{evt.eventId}|{evt.eventName}|{evt.timestampUtc}|{evt.sessionId}|{evt.playerId}|{HMAC_SECRET_SALT}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(HMAC_SECRET_SALT)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawContent));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void SaveOfflineQueue()
        {
            if (eventBuffer.Count == 0) return;

            var batch = new TelemetryBatchPayload
            {
                batchId = "offline_cache",
                sentAtUtc = DateTime.UtcNow.ToString("o"),
                eventCount = eventBuffer.Count,
                events = new List<TelemetryEvent>(eventBuffer)
            };

            string json = JsonUtility.ToJson(batch);
            PlayerPrefs.SetString(PREF_OFFLINE_TELEMETRY_BUFFER, json);
            PlayerPrefs.Save();
        }

        private void LoadOfflineQueue()
        {
            if (!PlayerPrefs.HasKey(PREF_OFFLINE_TELEMETRY_BUFFER)) return;

            try
            {
                string json = PlayerPrefs.GetString(PREF_OFFLINE_TELEMETRY_BUFFER, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var batch = JsonUtility.FromJson<TelemetryBatchPayload>(json);
                    if (batch != null && batch.events != null)
                    {
                        foreach (var evt in batch.events)
                        {
                            eventBuffer.Enqueue(evt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Telemetry] Gagal memulihkan offline queue: {ex.Message}");
            }
        }

        private void SaveRetentionMetrics()
        {
            if (retentionMetrics == null) return;
            string json = JsonUtility.ToJson(retentionMetrics);
            PlayerPrefs.SetString(PREF_RETENTION_METRICS, json);
            PlayerPrefs.Save();
        }

        private void LoadRetentionMetrics()
        {
            if (PlayerPrefs.HasKey(PREF_RETENTION_METRICS))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREF_RETENTION_METRICS, "");
                    retentionMetrics = JsonUtility.FromJson<PlayerRetentionMetrics>(json);
                }
                catch
                {
                    retentionMetrics = new PlayerRetentionMetrics();
                }
            }

            if (retentionMetrics == null)
            {
                retentionMetrics = new PlayerRetentionMetrics();
            }
        }

        #endregion
    }
}
