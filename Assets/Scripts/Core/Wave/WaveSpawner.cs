using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.Mobile;

namespace FantasyDefense.Wave
{
    /// <summary>
    /// Mengelola kemunculan gelombang monster (Wave Spawning), komposisi musuh,
    /// dan pemantauan status eliminasi gelombang hingga tercapainya Kemenangan (Victory).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        public static WaveSpawner Instance { get; private set; }

        [Header("Wave Progression List")]
        [SerializeField] private List<WaveDataSO> waveList = new List<WaveDataSO>();

        [Header("Monster Fallback Prefab (Optional)")]
        [SerializeField] private GameObject defaultMonsterPrefab;

        [Header("Runtime State")]
        [SerializeField] private int currentWaveIndex = 0;
        [SerializeField] private bool isSpawningWave = false;
        [SerializeField] private bool isWaveCompleting = false;
        [SerializeField] private int monstersRemainingInWave = 0;

        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int TotalWaves => waveList != null ? waveList.Count : 5;
        public int TotalMonstersInWave { get; private set; }
        public int MonstersRemainingInWave => monstersRemainingInWave;
        public bool IsWaveInProgress => isSpawningWave || monstersRemainingInWave > 0;

        public static event Action<int, int> OnWaveStarted;                   // (currentWave, totalWaves)
        public static event Action<int> OnWaveCompleted;                       // (completedWave)
        public static event Action<int, int> OnWaveMonsterProgressChanged;     // (remaining, total)
        public static event Action OnAllWavesCleared;                          // Victory!

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            MonsterController.OnMonsterKilled += HandleMonsterKilled;
            MonsterController.OnMonsterReachedBarricade += HandleMonsterReachedBarricade;
        }

        private void OnDisable()
        {
            MonsterController.OnMonsterKilled -= HandleMonsterKilled;
            MonsterController.OnMonsterReachedBarricade -= HandleMonsterReachedBarricade;
        }

        public void StartBattleWaves()
        {
            currentWaveIndex = 0;

            // Periksa apakah Endless Abyssal Trial mode aktif
            if (Metagame.EndlessTrialManager.Instance != null && Metagame.EndlessTrialManager.Instance.IsEndlessModeActive)
            {
                StartCoroutine(SpawnEndlessFloorRoutine(Metagame.EndlessTrialManager.Instance.CurrentFloor));
                return;
            }

            // Periksa apakah Stage aktif menyediakan daftar gelombang khusus
            if (Metagame.StageManager.Instance != null)
            {
                var stage = Metagame.StageManager.Instance.GetSelectedStage();
                if (stage != null && stage.stageWaves != null && stage.stageWaves.Count > 0)
                {
                    waveList = stage.stageWaves;
                }
            }

            StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
        }

        private IEnumerator SpawnWaveRoutine(int waveIdx)
        {
            if (waveList == null || waveList.Count == 0)
            {
                Debug.LogWarning("[WaveSpawner] Daftar waveList kosong! Menunggu konfigurasi wave.");
                yield break;
            }

            if (waveIdx >= waveList.Count)
            {
                // Seluruh wave berhasil dipertahankan! (Victory)
                OnAllWavesCleared?.Invoke();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(BattleState.Victory);
                }
                yield break;
            }

            WaveDataSO currentWave = waveList[waveIdx];
            isSpawningWave = true;
            isWaveCompleting = false;

            // Hitung total monster pada wave ini
            int totalMonsters = 0;
            foreach (var group in currentWave.spawnGroups)
            {
                totalMonsters += group.count;
            }
            TotalMonstersInWave = totalMonsters;
            monstersRemainingInWave = totalMonsters;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(BattleState.WaveActive);
            }

            OnWaveStarted?.Invoke(currentWave.waveNumber, waveList.Count);
            OnWaveMonsterProgressChanged?.Invoke(monstersRemainingInWave, TotalMonstersInWave);
            Debug.Log($"[WaveSpawner] Memulai Wave {currentWave.waveNumber}: {currentWave.waveTitle} (Total {totalMonsters} monster)");

            Vector3[] waypoints = MonsterPathManager.Instance != null
                ? MonsterPathManager.Instance.GetWaypoints()
                : new Vector3[] { Vector3.up * 4f, Vector3.down * 4f };

            // Spawn masing-masing grup monster secara berurutan dengan formasi Skuad Rombongan (Point 2)
            int monsterCounter = 0;
            foreach (var group in currentWave.spawnGroups)
            {
                if (group.initialDelay > 0)
                    yield return new WaitForSeconds(group.initialDelay);

                // Jika grup adalah Creep dan berjumlah banyak, spawn dalam formasi skuad (2-3 monster per barisan)
                bool isSwarm = group.monsterData != null && group.monsterData.monsterType == MonsterType.Creep && group.count >= 2;
                int squadSize = isSwarm ? 2 : 1;

                int spawnedInGroup = 0;
                while (spawnedInGroup < group.count)
                {
                    int currentBatch = Mathf.Min(squadSize, group.count - spawnedInGroup);
                    for (int b = 0; b < currentBatch; b++)
                    {
                        // Sebar dinamis ke 5 variasi lajur horizontal: tengah (0m), sayap dalam (±0.65m), dan pinggir (±1.35m)
                        // Monster tidak selalu muncul di tengah, tetapi bervariasi secara taktis ke sisi pinggir arena
                        int lanePattern = (monsterCounter + b) % 5;
                        float laneOffset = lanePattern switch
                        {
                            0 => 0f,                                  // Tengah
                            1 => -0.65f,                              // Sayap Kiri Dalam
                            2 => 0.65f,                               // Sayap Kanan Dalam
                            3 => -1.35f,                              // Pinggir Kiri
                            4 => 1.35f,                               // Pinggir Kanan
                            _ => 0f
                        };
                        laneOffset += UnityEngine.Random.Range(-0.10f, 0.10f);

                        SpawnMonster(group.monsterData, waypoints, EliteAffixType.None, laneOffset);
                        if (currentBatch > 1) yield return new WaitForSeconds(0.18f); // Jeda stagger sangat singkat antar anggota skuad
                    }

                    spawnedInGroup += currentBatch;
                    monsterCounter += currentBatch;

                    float restInterval = isSwarm ? Mathf.Max(0.9f, group.spawnInterval * 1.35f) : group.spawnInterval;
                    yield return new WaitForSeconds(restInterval);
                }
            }

            isSpawningWave = false;
        }

        private void SpawnMonster(MonsterDataSO data, Vector3[] waypoints, EliteAffixType forcedAffix = EliteAffixType.None, float laneOffset = 0f)
        {
            if (data == null) return;

            Vector3 spawnPos = waypoints[0] + new Vector3(laneOffset, 0f, 0f);

            // Peringatan Ancaman Jalur (Lane Threat Warning) untuk Brute & Boss (Visual Clarity & Tactic Tension)
            if (data.monsterType == MonsterType.Boss)
            {
                Debug.Log($"[WaveSpawner] ⚠️ PERINGATAN ANCAMAN: BOSS {data.monsterName} memasuki medan tempur!");
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(waypoints[0] + Vector3.down * 0.45f, $"⚠️ BOSS: {data.monsterName.ToUpper()}!", new Color(1f, 0.2f, 0.2f), 1.6f, true);
                }
                if (MobileCameraAdapter.Instance != null)
                {
                    MobileCameraAdapter.Instance.TriggerScreenShake(0.35f, 0.25f);
                }
                if (DevicePerformanceManager.Instance != null)
                {
                    DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Heavy);
                }
            }
            else if (data.monsterType == MonsterType.Brute)
            {
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(spawnPos + Vector3.down * 0.35f, $"⚠️ ANCAMAN: {data.monsterName.ToUpper()}", new Color(1f, 0.6f, 0.15f), 1.1f, false);
                }
                if (DevicePerformanceManager.Instance != null)
                {
                    DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Medium);
                }
            }

            GameObject monsterObj;
            if (defaultMonsterPrefab != null)
            {
                monsterObj = Instantiate(defaultMonsterPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Dynamic clean fallback: membuat objek monster di runtime
                monsterObj = new GameObject($"Monster_{data.monsterType}");
                monsterObj.transform.position = spawnPos;

                // Child Visual terdedikasi untuk SpriteRenderer agar animasi procedural (squash, bob, tilt)
                // tidak merusak koordinat transform.position atau skala transform.localScale pada root objek
                var visualObj = new GameObject("Visual");
                visualObj.transform.SetParent(monsterObj.transform, false);

                var sr = visualObj.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Gem";
                sr.sortingOrder = 25;

                var col = monsterObj.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
                col.isTrigger = false;
            }

            MonsterController controller;
            if (data.monsterType == MonsterType.Boss)
            {
                controller = monsterObj.GetComponent<BossMonsterController>();
                if (controller == null)
                {
                    controller = monsterObj.AddComponent<BossMonsterController>();
                }
            }
            else
            {
                controller = monsterObj.GetComponent<MonsterController>();
                if (controller == null)
                {
                    controller = monsterObj.AddComponent<MonsterController>();
                }
            }

            controller.Initialize(data, waypoints, laneOffset);

            // Evaluasi Mutasi Monster Varian Elit (Sprint 13)
            if (data.monsterType != MonsterType.Boss)
            {
                if (forcedAffix != EliteAffixType.None)
                {
                    controller.SetEliteAffix(forcedAffix);
                }
                else
                {
                    bool isEndless = Metagame.EndlessTrialManager.Instance != null && Metagame.EndlessTrialManager.Instance.IsEndlessModeActive;
                    float eliteChance = data.monsterType == MonsterType.Brute ? (isEndless ? 0.35f : 0.20f) : (isEndless ? 0.15f : 0.08f);

                    if ((currentWaveIndex >= 2 || isEndless) && UnityEngine.Random.value < eliteChance)
                    {
                        EliteAffixType randomAffix = (EliteAffixType)UnityEngine.Random.Range(1, 4);
                        controller.SetEliteAffix(randomAffix);
                    }
                }
            }
        }

        private void HandleMonsterKilled(MonsterController monster)
        {
            monstersRemainingInWave = Mathf.Max(0, monstersRemainingInWave - 1);
            OnWaveMonsterProgressChanged?.Invoke(monstersRemainingInWave, TotalMonstersInWave);
            CheckWaveProgress();
        }

        private void HandleMonsterReachedBarricade(MonsterController monster, int dmg)
        {
            // Sesuai Instruksi Komandan: Monster kini terus menyerang benteng dan tetap hidup hingga dieliminasi pahlawan/turret
        }

        private void CheckWaveProgress()
        {
            if (isWaveCompleting) return;

            int livingMonsters = 0;
            for (int i = 0; i < MonsterController.ActiveMonsters.Count; i++)
            {
                var m = MonsterController.ActiveMonsters[i];
                if (m != null && m.IsAlive && m.CurrentHealth > 0)
                {
                    livingMonsters++;
                }
            }

            // Jika semua monster pada wave ini telah dieliminasi dan proses spawn telah selesai
            if (!isSpawningWave && monstersRemainingInWave <= 0 && livingMonsters == 0)
            {
                isWaveCompleting = true;
                if (Metagame.EndlessTrialManager.Instance != null && Metagame.EndlessTrialManager.Instance.IsEndlessModeActive)
                {
                    StartCoroutine(CompleteEndlessFloorRoutine());
                }
                else
                {
                    StartCoroutine(CompleteCurrentWaveRoutine());
                }
            }
        }

        private void Update()
        {
            if (isWaveCompleting || isSpawningWave) return;

            // Watchdog Guard: Menjamin wave tidak pernah macet saat seluruh musuh tereliminasi di arena
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == BattleState.WaveActive)
            {
                int livingMonsters = 0;
                for (int i = 0; i < MonsterController.ActiveMonsters.Count; i++)
                {
                    var m = MonsterController.ActiveMonsters[i];
                    if (m != null && m.IsAlive && m.CurrentHealth > 0) livingMonsters++;
                }

                if (livingMonsters == 0)
                {
                    monstersRemainingInWave = 0;
                    CheckWaveProgress();
                }
            }
        }

        private IEnumerator SpawnEndlessFloorRoutine(int floor)
        {
            var endlessMgr = Metagame.EndlessTrialManager.Instance;
            var config = endlessMgr != null ? endlessMgr.Config : null;

            int monsterCount = config != null ? config.GetMonsterCount(floor) : 10;
            float hpMul = config != null ? config.GetHealthMultiplier(floor) : 1.0f;
            float dmgMul = config != null ? config.GetDamageMultiplier(floor) : 1.0f;
            float spdMul = config != null ? config.GetSpeedMultiplier(floor) : 1.0f;
            bool isBossFloor = config != null && config.IsBossFloor(floor);

            isSpawningWave = true;
            isWaveCompleting = false;
            monstersRemainingInWave = monsterCount + (isBossFloor ? 1 : 0);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(BattleState.WaveActive);
            }

            OnWaveStarted?.Invoke(floor, 999);
            Debug.Log($"[WaveSpawner] Memulai Abyssal Floor {floor}! Total monster: {monstersRemainingInWave} (Boss: {isBossFloor})");

            Vector3[] waypoints = MonsterPathManager.Instance != null
                ? MonsterPathManager.Instance.GetWaypoints()
                : new Vector3[] { Vector3.up * 4f, Vector3.down * 4f };

            float interval = config != null ? config.baseSpawnInterval : 1.1f;

            for (int i = 0; i < monsterCount; i++)
            {
                var creepData = ScriptableObject.CreateInstance<MonsterDataSO>();
                creepData.monsterType = MonsterType.Creep;
                creepData.monsterName = $"Abyssal Creep (F{floor})";
                creepData.maxHealth = 45f * hpMul;
                creepData.moveSpeed = 1.6f * spdMul;
                creepData.castleDamage = Mathf.RoundToInt(5 * dmgMul);
                creepData.gemsReward = 4;
                creepData.themeColor = new Color(0.9f, 0.2f, 0.3f);
                creepData.monsterSprite = Resources.Load<Sprite>("Sprites/Monsters/Monster_Creep");

                float laneOffset = (((i % 3) - 1) * 0.72f) + UnityEngine.Random.Range(-0.12f, 0.12f);
                SpawnMonster(creepData, waypoints, EliteAffixType.None, laneOffset);
                yield return new WaitForSeconds(interval);
            }

            if (isBossFloor)
            {
                yield return new WaitForSeconds(1.5f);
                var bossData = ScriptableObject.CreateInstance<MonsterDataSO>();
                bossData.monsterType = MonsterType.Boss;
                bossData.monsterName = (floor % 10 == 0) ? $"Abyssal Drake Overlord (F{floor})" : $"Orc Berserker General (F{floor})";
                bossData.maxHealth = 650f * hpMul;
                bossData.moveSpeed = 1.25f * spdMul;
                bossData.castleDamage = Mathf.RoundToInt(25 * dmgMul);
                bossData.gemsReward = 25;
                bossData.themeColor = Color.red;
                bossData.monsterSprite = Resources.Load<Sprite>("Sprites/Monsters/Monster_Boss");

                SpawnMonster(bossData, waypoints);
            }

            isSpawningWave = false;
        }

        private IEnumerator CompleteEndlessFloorRoutine()
        {
            var endlessMgr = Metagame.EndlessTrialManager.Instance;
            int finishedFloor = endlessMgr != null ? endlessMgr.CurrentFloor : 1;

            if (endlessMgr != null)
            {
                endlessMgr.NotifyFloorCleared(finishedFloor);
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AwardWaveDividend();
            }

            OnWaveCompleted?.Invoke(finishedFloor);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(BattleState.WaveClear);
            }

            float rest = endlessMgr != null && endlessMgr.Config != null
                ? endlessMgr.Config.restTimeBetweenFloors
                : 3.5f;

            yield return new WaitForSeconds(rest);

            if (endlessMgr != null && endlessMgr.IsEndlessModeActive)
            {
                StartCoroutine(SpawnEndlessFloorRoutine(endlessMgr.CurrentFloor));
            }
        }

        public int CurrentWaveIndex => currentWaveIndex;

        /// <summary>
        /// Memulai gelombang monster berikutnya setelah fase persiapan taktis pahlawan selesai.
        /// </summary>
        public void StartNextWave()
        {
            if (isSpawningWave) return;
            StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
        }

        private IEnumerator CompleteCurrentWaveRoutine()
        {
            WaveDataSO finishedWave = (waveList != null && currentWaveIndex < waveList.Count) ? waveList[currentWaveIndex] : null;

            int bonusGems = finishedWave != null ? finishedWave.bonusGemsOnClear : 20;
            if (EconomyManager.Instance != null)
            {
                if (bonusGems > 0) EconomyManager.Instance.AddGems(bonusGems);
                EconomyManager.Instance.AwardWaveDividend();
            }

            Debug.Log($"[WaveSpawner] Wave {currentWaveIndex + 1} Berhasil Dibersihkan! (+{bonusGems} Gems)");
            OnWaveCompleted?.Invoke(currentWaveIndex + 1);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(BattleState.WaveClear);
            }

            // Jeda selebrasi singkat sebelum memasuki fase persiapan antar-gelombang
            yield return new WaitForSeconds(1.2f);

            currentWaveIndex++;

            // Jika seluruh wave dalam stage ini sudah selesai -> Kemenangan (Victory)
            if (waveList != null && currentWaveIndex >= waveList.Count)
            {
                OnAllWavesCleared?.Invoke();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(BattleState.Victory);
                }
                yield break;
            }

            // Sesuai instruksi Komandan: Untuk wave berikutnya JANGAN langsung musuh menyerang!
            // Berikan langkah taktis tambahan (bonus moves) agar pemain dapat memposisikan ulang
            // dan menggabungkan (merge) hero untuk menaikkan level sebelum gelombang musuh tiba.
            int bonusMoves = finishedWave != null && finishedWave.bonusMovesOnClear > 0
                ? finishedWave.bonusMovesOnClear
                : (GameManager.Instance != null && GameManager.Instance.ActiveLevelConfig != null ? GameManager.Instance.ActiveLevelConfig.interWaveMoves : 5);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnterInterWavePreparation(currentWaveIndex + 1, bonusMoves);
            }
            else
            {
                StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
            }
        }

        public void RegisterWave(WaveDataSO wave)
        {
            if (wave != null && !waveList.Contains(wave))
            {
                waveList.Add(wave);
            }
        }

        /// <summary>
        /// Mengembalikan teks ringkasan intelijen monster yang akan turun pada gelombang tertentu (Wave Monster Preview).
        /// </summary>
        public string GetUpcomingWavePreviewSummary(int waveNumber)
        {
            int waveIdx = waveNumber - 1;
            if (waveList == null || waveIdx < 0 || waveIdx >= waveList.Count)
            {
                return "";
            }

            WaveDataSO wave = waveList[waveIdx];
            if (wave == null || wave.spawnGroups == null || wave.spawnGroups.Count == 0)
            {
                return "";
            }

            Dictionary<string, int> countByMonster = new Dictionary<string, int>();
            bool hasBoss = false;

            for (int i = 0; i < wave.spawnGroups.Count; i++)
            {
                var group = wave.spawnGroups[i];
                if (group != null && group.monsterData != null)
                {
                    string mName = !string.IsNullOrEmpty(group.monsterData.monsterName)
                        ? group.monsterData.monsterName
                        : group.monsterData.monsterType.ToString();

                    if (!countByMonster.ContainsKey(mName))
                        countByMonster[mName] = 0;
                    countByMonster[mName] += group.count;

                    if (group.monsterData.monsterType == MonsterType.Boss)
                        hasBoss = true;
                }
            }

            List<string> parts = new List<string>();
            foreach (var kvp in countByMonster)
            {
                parts.Add($"{kvp.Value}x {kvp.Key}");
            }

            string threatList = string.Join("  •  ", parts);
            if (hasBoss)
            {
                return $"⚠️ <color=#FF4444>[BAHAYA BOSS]</color> {threatList}";
            }
            return $"👾 <color=#00FFFF>[INTEL MUSUH]</color> {threatList}";
        }

        public WaveDataSO GetWaveData(int waveIndex)
        {
            if (waveList != null && waveIndex >= 0 && waveIndex < waveList.Count)
                return waveList[waveIndex];
            return null;
        }

        public void StopAndClearAllWaves()
        {
            StopAllCoroutines();
            isSpawningWave = false;
            isWaveCompleting = false;
            monstersRemainingInWave = 0;

            var activeMonsters = UnityEngine.Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);
            if (activeMonsters != null)
            {
                for (int i = 0; i < activeMonsters.Length; i++)
                {
                    if (activeMonsters[i] != null)
                    {
                        Destroy(activeMonsters[i].gameObject);
                    }
                }
            }
        }
    }
}
