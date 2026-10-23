using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyDefense.Analytics
{
    public enum TelemetryCategory
    {
        Session,
        Progression,
        EconomyMerge,
        Retention,
        Monetization,
        Tutorial
    }

    /// <summary>
    /// Model data representasi payload untuk 1 event telemetri analitik Live-Ops.
    /// Kompatibel dengan schema Google Play Games Services, Firebase Analytics, dan GameAnalytics.
    /// Dilengkapi HMAC signature untuk mencegah tamper payload data di sisi klien.
    /// </summary>
    [Serializable]
    public class TelemetryEvent
    {
        public string eventId;
        public string eventName;
        public TelemetryCategory category;
        public string timestampUtc;
        public string sessionId;
        public string playerId;
        public int clientVersionCode;
        public string deviceModel;
        public string osVersion;
        [NonSerialized] public Dictionary<string, string> parameters = new Dictionary<string, string>();
        public string payloadChecksum; // HMAC SHA-256 anti-tamper hash

        public TelemetryEvent() { }

        public TelemetryEvent(string name, TelemetryCategory cat, string session, string player, int versionCode)
        {
            eventId = Guid.NewGuid().ToString("N");
            eventName = name;
            category = cat;
            timestampUtc = DateTime.UtcNow.ToString("o");
            sessionId = session;
            playerId = player;
            clientVersionCode = versionCode;
            deviceModel = SystemInfo.deviceModel ?? "UnknownDevice";
            osVersion = SystemInfo.operatingSystem ?? "UnknownOS";
            parameters = new Dictionary<string, string>();
        }

        public void SetParameter(string key, object value)
        {
            if (string.IsNullOrEmpty(key) || value == null) return;
            parameters[key] = value.ToString();
        }

        public string GetParameter(string key, string defaultValue = "")
        {
            if (parameters != null && parameters.TryGetValue(key, out string val))
            {
                return val;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Model akumulasi metriks retensi & siklus hidup pemain (Player Retention & Lifecycle Metrics).
    /// Mengukur D1, D3, D7, D14, D30 retention, streak login beruntun, serta total LTV & aktivitas bermain.
    /// </summary>
    [Serializable]
    public class PlayerRetentionMetrics
    {
        public string firstInstallDateUtc;
        public string lastLoginDateUtc;
        public int consecutiveLoginStreak = 1;
        public int totalDaysActive = 1;
        public int totalSessionsCount = 0;
        public float totalPlaytimeSeconds = 0f;

        // Retention Milestone Flags
        public bool hasReportedD1 = false;
        public bool hasReportedD3 = false;
        public bool hasReportedD7 = false;
        public bool hasReportedD14 = false;
        public bool hasReportedD30 = false;

        // Player Activity Aggregations
        public int totalStagesAttempted = 0;
        public int totalStagesWon = 0;
        public int totalHeroesSummoned = 0;
        public int totalHeroMerges = 0;
        public int totalGemsSpent = 0;
        public double totalLifetimeRevenueUsd = 0.0;

        public PlayerRetentionMetrics()
        {
            firstInstallDateUtc = DateTime.UtcNow.ToString("o");
            lastLoginDateUtc = DateTime.UtcNow.ToString("o");
            consecutiveLoginStreak = 1;
            totalDaysActive = 1;
        }

        public int GetDaysSinceInstall()
        {
            if (DateTime.TryParse(firstInstallDateUtc, out DateTime installDate))
            {
                return Mathf.Max(0, (int)(DateTime.UtcNow - installDate).TotalDays);
            }
            return 0;
        }
    }

    /// <summary>
    /// Batch kontainer pengiriman offline event ke telemetry backend / remote buffer.
    /// </summary>
    [Serializable]
    public class TelemetryBatchPayload
    {
        public string batchId;
        public string sentAtUtc;
        public int eventCount;
        public List<TelemetryEvent> events = new List<TelemetryEvent>();
    }
}
