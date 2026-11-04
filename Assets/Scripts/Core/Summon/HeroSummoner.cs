using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.GridSystem;

namespace FantasyDefense.Summon
{
    public enum SummonFailureReason
    {
        NotEnoughGems,
        GridFull,
        ManagerNotReady
    }

    /// <summary>
    /// Mengelola mekanisme Summon Hero ke arena Grid secara dinamis.
    /// Mengintegrasikan sistem ekonomi gems (40 + (n-1)*10) dan deteksi petak kosong.
    /// Didesain khusus untuk interaksi layar mobile sentuh (tombol HUD besar & ramah jempol).
    /// </summary>
    public class HeroSummoner : MonoBehaviour
    {
        public static HeroSummoner Instance { get; private set; }

        [Header("Hero Roster Pool (Trio Pahlawan Aktif)")]
        [Tooltip("Daftar ScriptableObject Trio Hero yang dapat dipanggil: Cedric, Rose Tanker, Rabbit Mage")]
        [SerializeField] private List<UnitDataSO> activeHeroPool = new List<UnitDataSO>();

        [Header("Prefab References (Optional Fallback)")]
        [Tooltip("Prefab dasar unit dengan SpriteRenderer dan UnitController. Jika kosong, akan dibuat secara dinamis.")]
        [SerializeField] private GameObject defaultHeroPrefab;

        [Header("Spawn Animation")]
        [SerializeField] private bool playSpawnScaleBounce = true;

        public static event Action<UnitController, TileCell> OnHeroSummoned;
        public static event Action<SummonFailureReason> OnSummonFailed;

        public int ActiveHeroPoolCount => activeHeroPool != null ? activeHeroPool.Count : 0;
        public IReadOnlyList<UnitDataSO> ActiveHeroPool => activeHeroPool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureHeroPoolLoaded();
        }

        /// <summary>
        /// Menjamin roster pahlawan kuartet selalu terisi di memori runtime.
        /// Mencegah kegagalan spawn pahlawan / permata di seluruh build (Android, PC, Editor).
        /// </summary>
        public void EnsureHeroPoolLoaded()
        {
            if (activeHeroPool == null)
            {
                activeHeroPool = new List<UnitDataSO>();
            }

            // 1. Bersihkan elemen null jika ada
            activeHeroPool.RemoveAll(item => item == null);

            // Verifikasi bahwa seluruh hero di pool memiliki tier0GemSprite valid
            foreach (var hero in activeHeroPool)
            {
                if (hero != null && hero.tier0GemSprite == null)
                {
                    string gemName = hero.unitType switch
                    {
                        UnitType.Cedric => "Gem_T0_Cedric",
                        UnitType.RoseTanker => "Gem_T0_Rose",
                        UnitType.RabbitMage => "Gem_T0_Rabbit",
                        UnitType.WindRanger => "Gem_T0_Wind",
                        _ => $"Gem_T0_{hero.unitType}"
                    };
                    hero.tier0GemSprite = Resources.Load<Sprite>($"Sprites/Gems/{gemName}");
                }
            }

            if (activeHeroPool.Count > 0) return;

            // 2. Coba muat dari folder Resources/Units
            var resourcesUnits = Resources.LoadAll<UnitDataSO>("Units");
            if (resourcesUnits != null && resourcesUnits.Length > 0)
            {
                foreach (var u in resourcesUnits)
                {
                    if (u != null && !activeHeroPool.Contains(u))
                    {
                        activeHeroPool.Add(u);
                    }
                }
            }

            // 3. Fallback jika masih kosong: muat dari root Resources
            if (activeHeroPool.Count == 0)
            {
                var allResourcesUnits = Resources.LoadAll<UnitDataSO>("");
                if (allResourcesUnits != null && allResourcesUnits.Length > 0)
                {
                    foreach (var u in allResourcesUnits)
                    {
                        if (u != null && !activeHeroPool.Contains(u))
                        {
                            activeHeroPool.Add(u);
                        }
                    }
                }
            }

#if UNITY_EDITOR
            // 4. Editor Fallback via AssetDatabase jika di run di Editor
            if (activeHeroPool.Count == 0)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:UnitDataSO");
                foreach (var guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitDataSO>(path);
                    if (asset != null && !activeHeroPool.Contains(asset))
                    {
                        activeHeroPool.Add(asset);
                    }
                }
            }
#endif

            // 5. Hardening Runtime Fallback: Jika pool masih kosong sama sekali di perangkat fisik,
            // buat runtime instance darurat kuartet hero dengan warna dan stat standar agar game tidak pernah blank!
            if (activeHeroPool.Count == 0)
            {
                Debug.LogWarning("[HeroSummoner] Hero pool kosong di runtime! Mengaktifkan Self-Healing Emergency Quartet Roster...");
                CreateEmergencyQuartetRoster();
            }

            Debug.Log($"[HeroSummoner] EnsureHeroPoolLoaded selesai -> {activeHeroPool.Count} hero aktif terdaftar.");
        }

        private void CreateEmergencyQuartetRoster()
        {
            // Cedric
            var cedric = ScriptableObject.CreateInstance<UnitDataSO>();
            cedric.unitType = UnitType.Cedric;
            cedric.heroName = "Cedric";
            cedric.title = "The Red Knight";
            cedric.baseDamage = 30f;
            cedric.attackRate = 1.2f;
            cedric.attackRange = 1f;
            cedric.maxHealth = 120f;
            cedric.themeColor = new Color(0.9f, 0.2f, 0.2f, 1f);
            cedric.tier0GemSprite = Resources.Load<Sprite>("Sprites/Gems/Gem_T0_Cedric");
            cedric.unitIcon = cedric.tier0GemSprite;
            activeHeroPool.Add(cedric);

            // Rose Tanker
            var rose = ScriptableObject.CreateInstance<UnitDataSO>();
            rose.unitType = UnitType.RoseTanker;
            rose.heroName = "Rose Tanker";
            rose.title = "Ksatria Rambut Pink";
            rose.baseDamage = 22f;
            rose.attackRate = 0.9f;
            rose.attackRange = 1f;
            rose.maxHealth = 200f;
            rose.themeColor = new Color(0.95f, 0.4f, 0.65f, 1f);
            rose.tier0GemSprite = Resources.Load<Sprite>("Sprites/Gems/Gem_T0_Rose");
            rose.unitIcon = rose.tier0GemSprite;
            activeHeroPool.Add(rose);

            // Rabbit Mage
            var rabbit = ScriptableObject.CreateInstance<UnitDataSO>();
            rabbit.unitType = UnitType.RabbitMage;
            rabbit.heroName = "Rabbit Mage";
            rabbit.title = "Penyihir Kelinci";
            rabbit.baseDamage = 26f;
            rabbit.attackRate = 0.8f;
            rabbit.attackRange = 3.5f;
            rabbit.maxHealth = 85f;
            rabbit.themeColor = new Color(0.2f, 0.6f, 0.95f, 1f);
            rabbit.tier0GemSprite = Resources.Load<Sprite>("Sprites/Gems/Gem_T0_Rabbit");
            rabbit.unitIcon = rabbit.tier0GemSprite;
            activeHeroPool.Add(rabbit);

            // Wind Ranger
            var wind = ScriptableObject.CreateInstance<UnitDataSO>();
            wind.unitType = UnitType.WindRanger;
            wind.heroName = "Wind Ranger";
            wind.title = "Emerald Zephyr";
            wind.baseDamage = 24f;
            wind.attackRate = 1.5f;
            wind.attackRange = 3.8f;
            wind.maxHealth = 90f;
            wind.themeColor = new Color(0.2f, 0.85f, 0.45f, 1f);
            wind.tier0GemSprite = Resources.Load<Sprite>("Sprites/Gems/Gem_T0_Wind");
            wind.unitIcon = wind.tier0GemSprite;
            activeHeroPool.Add(wind);
        }

        /// <summary>
        /// Mencoba melakukan pemanggilan hero (Summon) ke arena grid.
        /// Beroperasi adaptif:
        /// - Pada Mode 1 (Swap & Merge): Berfungsi sebagai Katalis Papan Taktis (Transmutasi / Peningkatan Tier Bidak)
        /// - Pada Mode 2 (Real-Time Deploy): Memanggil unit baru ke petak kosong
        /// </summary>
        public bool TrySummonHero()
        {
            if (FantasyDefense.Core.GridMatch3Solver.Instance != null && FantasyDefense.Core.GridMatch3Solver.Instance.IsResolving)
            {
                Debug.LogWarning("[HeroSummoner] Aksi ditahan: Papan sedang menyelesaikan cascade Match-3.");
                return false;
            }

            // 1. Validasi keberadaan GridManager
            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
            {
                Debug.LogWarning("[HeroSummoner] GridManager belum terinisialisasi.");
                OnSummonFailed?.Invoke(SummonFailureReason.ManagerNotReady);
                return false;
            }

            // Adaptif: Mode 1 (Swap & Merge) -> Katalis Transmutasi Papan Taktis
            if (FantasyDefense.Core.GameManager.Instance != null &&
                FantasyDefense.Core.GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation)
            {
                return TryExecuteSwapMergeCatalyst();
            }

            // 2. Cek apakah seluruh petak arena sudah penuh (Mode 2 Deploy)
            if (GridManager.Instance.IsGridFull())
            {
                Debug.LogWarning("[HeroSummoner] Seluruh petak arena penuh! Tidak dapat memanggil hero baru.");
                OnSummonFailed?.Invoke(SummonFailureReason.GridFull);
                if (SkillVfxManager.Instance != null)
                {
                    Vector3 originPos = (FantasyDefense.UI.SummonButtonUI.Instance != null)
                        ? FantasyDefense.UI.SummonButtonUI.Instance.transform.position
                        : Vector3.zero;
                    SkillVfxManager.Instance.SpawnFloatingText(
                        originPos,
                        "⚠️ ARENA PENUH! GABUNGKAN UNIT! ⚠️",
                        new Color(1f, 0.35f, 0.35f),
                        1.8f
                    );
                }
                return false;
            }

            // 3. Cek kecukupan gems dari EconomyManager
            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanAffordSummon())
            {
                Debug.LogWarning($"[HeroSummoner] Gems tidak mencukupi untuk summon (Dibutuhkan: {EconomyManager.Instance.GetCurrentSummonCost()}).");
                OnSummonFailed?.Invoke(SummonFailureReason.NotEnoughGems);
                return false;
            }

            // 4. Potong biaya gems dinamis
            if (EconomyManager.Instance != null)
            {
                bool spendSuccess = EconomyManager.Instance.SpendGemsForSummon();
                if (!spendSuccess)
                {
                    OnSummonFailed?.Invoke(SummonFailureReason.NotEnoughGems);
                    return false;
                }
            }

            // 5. Pilih petak kosong acak di Grid
            TileCell emptyCell = GridManager.Instance.GetRandomEmptyCell();
            if (emptyCell == null)
            {
                OnSummonFailed?.Invoke(SummonFailureReason.GridFull);
                return false;
            }

            // 6. Pilih salah satu hero dari trio aktif secara acak (probabilitas seimbang)
            UnitDataSO chosenHero = GetRandomHeroFromPool();
            if (chosenHero == null)
            {
                Debug.LogError("[HeroSummoner] Roster Hero kosong! Pastikan activeHeroPool telah diisi di Inspector.");
                return false;
            }

            // 7. Spawn unit ke petak target
            UnitController spawnedUnit = SpawnUnitAtCell(chosenHero, emptyCell, UnitTier.Tier1);

            // Live-Ops Telemetry Tracking
            if (FantasyDefense.Analytics.CitadelTelemetryManager.Instance != null)
            {
                int cost = EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentSummonCost() : 40;
                int occupied = GridManager.Instance != null ? GridManager.Instance.GetOccupiedCount() : 1;
                FantasyDefense.Analytics.CitadelTelemetryManager.Instance.TrackHeroSummon(chosenHero.heroName, cost, occupied);
            }

            // 8. Siarkan event keberhasilan
            OnHeroSummoned?.Invoke(spawnedUnit, emptyCell);

            // Respons getaran taktil mobile berproteksi status pengaturan
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Light);
            }

            // 9. Cek apakah penempatan unit baru ini memicu Match-3
            if (GridMatch3Solver.Instance != null)
            {
                GridMatch3Solver.Instance.CheckAndResolveMatches();
            }

            return true;
        }

        /// <summary>
        /// Memanggil pahlawan awal secara gratis ke arena (Starter Free Heroes) tanpa memotong cadangan gems pemain,
        /// sehingga cadangan gems awal pemain (100G) tetap utuh untuk digunakan pada tombol pemanggilan.
        /// </summary>
        public bool TrySummonFreeHero()
        {
            if (FantasyDefense.Core.GridMatch3Solver.Instance != null && FantasyDefense.Core.GridMatch3Solver.Instance.IsResolving)
            {
                return false;
            }

            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
            {
                return false;
            }

            if (GridManager.Instance.IsGridFull())
            {
                return false;
            }

            TileCell emptyCell = GridManager.Instance.GetRandomEmptyCell();
            if (emptyCell == null) return false;

            UnitDataSO chosenHero = GetRandomHeroFromPool();
            if (chosenHero == null) return false;

            UnitController spawnedUnit = SpawnUnitAtCell(chosenHero, emptyCell, UnitTier.Tier1);
            OnHeroSummoned?.Invoke(spawnedUnit, emptyCell);
            return true;
        }

        /// <summary>
        /// Memanggil pahlawan dengan tipe spesifik ke petak arena kosong (Mode 2 Real-Time Deploy).
        /// Memenuhi spesifikasi kanonikal Master Game Spec di mana pemain dapat memilih pahlawan
        /// tertentu dari deck bawah untuk strategi formasi terarah.
        /// </summary>
        public bool TrySummonSpecificHero(UnitType targetType)
        {
            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
            {
                OnSummonFailed?.Invoke(SummonFailureReason.ManagerNotReady);
                return false;
            }

            // Pada Mode 1 (Swap & Merge), seluruh papan penuh, picu katalis transmutasi papan
            if (FantasyDefense.Core.GameManager.Instance != null &&
                FantasyDefense.Core.GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation)
            {
                return TryExecuteSwapMergeCatalyst();
            }

            if (GridManager.Instance.IsGridFull())
            {
                OnSummonFailed?.Invoke(SummonFailureReason.GridFull);
                if (SkillVfxManager.Instance != null)
                {
                    Vector3 originPos = (FantasyDefense.UI.SummonButtonUI.Instance != null)
                        ? FantasyDefense.UI.SummonButtonUI.Instance.transform.position
                        : Vector3.zero;
                    SkillVfxManager.Instance.SpawnFloatingText(
                        originPos,
                        "⚠️ ARENA PENUH! GABUNGKAN UNIT! ⚠️",
                        new Color(1f, 0.35f, 0.35f),
                        1.8f
                    );
                }
                return false;
            }

            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanAffordSummon())
            {
                OnSummonFailed?.Invoke(SummonFailureReason.NotEnoughGems);
                return false;
            }

            if (EconomyManager.Instance != null)
            {
                bool spendSuccess = EconomyManager.Instance.SpendGemsForSummon();
                if (!spendSuccess)
                {
                    OnSummonFailed?.Invoke(SummonFailureReason.NotEnoughGems);
                    return false;
                }
            }

            TileCell emptyCell = GridManager.Instance.GetRandomEmptyCell();
            if (emptyCell == null)
            {
                OnSummonFailed?.Invoke(SummonFailureReason.GridFull);
                return false;
            }

            // Temukan UnitDataSO yang sesuai dengan targetType
            EnsureHeroPoolLoaded();
            UnitDataSO chosenHero = null;
            foreach (var h in activeHeroPool)
            {
                if (h != null && h.unitType == targetType)
                {
                    chosenHero = h;
                    break;
                }
            }

            if (chosenHero == null)
            {
                chosenHero = GetRandomHeroFromPool();
            }

            if (chosenHero == null) return false;

            UnitController spawnedUnit = SpawnUnitAtCell(chosenHero, emptyCell, UnitTier.Tier1);

            if (FantasyDefense.Analytics.CitadelTelemetryManager.Instance != null)
            {
                int cost = EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentSummonCost() : 40;
                int occupied = GridManager.Instance != null ? GridManager.Instance.GetOccupiedCount() : 1;
                FantasyDefense.Analytics.CitadelTelemetryManager.Instance.TrackHeroSummon(chosenHero.heroName, cost, occupied);
            }

            OnHeroSummoned?.Invoke(spawnedUnit, emptyCell);

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Light);
            }

            if (GridMatch3Solver.Instance != null)
            {
                GridMatch3Solver.Instance.CheckAndResolveMatches();
            }

            return true;
        }

        /// <summary>
        /// Menjalankan intervensi taktis pada Mode 1 (Swap & Merge).
        /// Karena seluruh petak terisi penuh, penekanan tombol Summon memicu katalis peningkatan
        /// (Transcendence Catalyst): mempromosikan 1 unit acak ber-tier rendah ke tier berikutnya
        /// dan mengecek kombo berantai otomatis!
        /// </summary>
        public bool TryExecuteSwapMergeCatalyst()
        {
            if (FantasyDefense.Core.GridMatch3Solver.Instance != null && FantasyDefense.Core.GridMatch3Solver.Instance.IsResolving)
            {
                Debug.LogWarning("[HeroSummoner] Katalis ditahan: Papan sedang menyelesaikan cascade Match-3.");
                return false;
            }

            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanAffordSummon())
            {
                Debug.LogWarning("[HeroSummoner] Gems/Mana tidak cukup untuk memicu Katalis Papan.");
                OnSummonFailed?.Invoke(SummonFailureReason.NotEnoughGems);
                return false;
            }

            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized) return false;

            int w = GridManager.Instance.GridWidth;
            int h = GridManager.Instance.GridHeight;
            List<TileCell> candidateCells = new List<TileCell>();

            // Cari unit Tier-0 (Permata mentah) atau Tier-1
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = GridManager.Instance.GetCell(x, y);
                    if (cell != null && cell.IsOccupied && cell.CurrentUnit != null)
                    {
                        if (cell.CurrentUnit.CurrentTier <= UnitTier.Tier1)
                        {
                            candidateCells.Add(cell);
                        }
                    }
                }
            }

            // Jika semua sudah T2+, ambil sembarang unit yang belum T5
            if (candidateCells.Count == 0)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        var cell = GridManager.Instance.GetCell(x, y);
                        if (cell != null && cell.IsOccupied && cell.CurrentUnit != null && cell.CurrentUnit.CurrentTier < UnitTier.Tier5)
                        {
                            candidateCells.Add(cell);
                        }
                    }
                }
            }

            if (candidateCells.Count == 0)
            {
                Debug.LogWarning("[HeroSummoner] Seluruh token papan telah mencapai Tier 5! Tidak ada kandidat evolusi lagi.");
                OnSummonFailed?.Invoke(SummonFailureReason.GridFull);
                if (SkillVfxManager.Instance != null)
                {
                    Vector3 originPos = (FantasyDefense.UI.SummonButtonUI.Instance != null)
                        ? FantasyDefense.UI.SummonButtonUI.Instance.transform.position + Vector3.up * 0.8f
                        : Vector3.up * -1.5f;
                    SkillVfxManager.Instance.SpawnFloatingText(
                        originPos,
                        "⭐ SEMUA TOKEN TELAH TIER 5 (MAX)! ⭐",
                        new Color(1f, 0.85f, 0.2f),
                        1.4f,
                        true
                    );
                }
                return false;
            }

            if (EconomyManager.Instance != null)
            {
                bool spent = EconomyManager.Instance.SpendGemsForSummon();
                if (!spent)
                {
                    OnSummonFailed?.Invoke(SummonFailureReason.NotEnoughGems);
                    return false;
                }
            }

            var chosenCell = candidateCells[UnityEngine.Random.Range(0, candidateCells.Count)];
            var targetUnit = chosenCell.CurrentUnit;
            UnitTier nextTier = (targetUnit.CurrentTier == UnitTier.Tier0) ? UnitTier.Tier1 :
                               (targetUnit.CurrentTier == UnitTier.Tier1) ? UnitTier.Tier2 :
                               (targetUnit.CurrentTier == UnitTier.Tier2) ? UnitTier.Tier3 :
                               (targetUnit.CurrentTier == UnitTier.Tier3) ? UnitTier.Tier4 : UnitTier.Tier5;

            targetUnit.UpgradeTier(nextTier);

            // Audio & Partikel Spektakuler
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.CatalystThunderZap);
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.MergeSuccess, 1.40f);
            }

            // Respons Haptic Getaran Taktil (Hanya aktif jika pemain menyalakan getaran di pengaturan)
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Heavy);
            }

            if (SkillVfxManager.Instance != null)
            {
                // Sambaran petir berkilau dari dock kubus bawah ke petak unit target
                Vector3 originPos = (FantasyDefense.UI.SummonButtonUI.Instance != null)
                    ? FantasyDefense.UI.SummonButtonUI.Instance.transform.position
                    : new Vector3(2.2f, -4.2f, 0f);
                SkillVfxManager.Instance.SpawnCatalystLightningBeam(originPos, chosenCell.transform.position);

                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    chosenCell.transform.position,
                    new Color(1f, 0.85f, 0.2f),
                    Color.white,
                    24,
                    2.2f
                );
                SkillVfxManager.Instance.SpawnFloatingText(
                    chosenCell.transform.position,
                    $"✨ KATALIS EVOLUSI {nextTier}! ✨",
                    new Color(1f, 0.9f, 0.3f),
                    1.8f
                );
            }

            // Siarkan event summon/katalis sukses
            OnHeroSummoned?.Invoke(targetUnit, chosenCell);

            // Cek kecocokan baris setelah evolusi
            if (GridMatch3Solver.Instance != null)
            {
                GridMatch3Solver.Instance.CheckAndResolveMatches(chosenCell);
            }

            return true;
        }

        /// <summary>
        /// Mengecek apakah ada unit di papan catur yang masih dapat ditingkatkan via Katalis (Tier < Tier 5).
        /// </summary>
        public bool HasCatalystCandidate()
        {
            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized) return false;
            int w = GridManager.Instance.GridWidth;
            int h = GridManager.Instance.GridHeight;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = GridManager.Instance.GetCell(x, y);
                    if (cell != null && cell.IsOccupied && cell.CurrentUnit != null && cell.CurrentUnit.CurrentTier < UnitTier.Tier5)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Menyinkronkan hero pool pertempuran agar hanya memunculkan permata/token pahlawan yang aktif di formasi.
        /// </summary>
        public void SynchronizeWithFormation(System.Collections.Generic.List<FantasyDefense.Data.UnitType> formation)
        {
            if (formation == null || formation.Count == 0) return;

            EnsureHeroPoolLoaded();

            // Kumpulkan hero yang ada di formasi dan sudah terbuka
            var newPool = new System.Collections.Generic.List<FantasyDefense.Data.UnitDataSO>();
            var lobby = FantasyDefense.Metagame.CitadelLobbyManager.Instance;

            foreach (var heroType in formation)
            {
                var profile = lobby != null ? lobby.GetHeroProfile(heroType) : null;
                if (profile != null && !profile.isUnlocked) continue; // Jangan masukkan jika terkunci

                var matchingHeroSO = activeHeroPool.Find(h => h != null && h.unitType == heroType);
                if (matchingHeroSO != null && !newPool.Contains(matchingHeroSO))
                {
                    newPool.Add(matchingHeroSO);
                }
            }

            if (newPool.Count > 0)
            {
                activeHeroPool = newPool;
                Debug.Log($"[HeroSummoner] ActiveHeroPool disinkronkan dengan formasi: {activeHeroPool.Count} pahlawan aktif.");
            }
        }

        /// <summary>
        /// Mengambil data hero acak yang terikat pada 4 Slot Formasi Aktif Akun Pemain.
        /// Menghindari dilusi taktis dengan hanya memanggil pahlawan dari formasi terpilih.
        /// </summary>
        public UnitDataSO GetRandomHeroFromPool()
        {
            if (activeHeroPool == null || activeHeroPool.Count == 0)
            {
                EnsureHeroPoolLoaded();
            }

            if (activeHeroPool == null || activeHeroPool.Count == 0) return null;

            // Prioritaskan pahlawan yang terdaftar dalam formasi 4 slot lobi pemain
            var formationHeroes = GetFormationBoundHeroes();
            if (formationHeroes != null && formationHeroes.Count > 0)
            {
                int rIdx = UnityEngine.Random.Range(0, formationHeroes.Count);
                return formationHeroes[rIdx];
            }

            int randomIndex = UnityEngine.Random.Range(0, activeHeroPool.Count);
            return activeHeroPool[randomIndex];
        }

        /// <summary>
        /// Mengambil daftar ScriptableObject pahlawan yang sesuai dengan formasi 4 pahlawan aktif.
        /// </summary>
        public List<UnitDataSO> GetFormationBoundHeroes()
        {
            if (FantasyDefense.Metagame.CitadelLobbyManager.Instance != null &&
                FantasyDefense.Metagame.CitadelLobbyManager.Instance.AccountData != null &&
                FantasyDefense.Metagame.CitadelLobbyManager.Instance.AccountData.deployedFormation != null &&
                FantasyDefense.Metagame.CitadelLobbyManager.Instance.AccountData.deployedFormation.Count > 0)
            {
                var formationTypes = FantasyDefense.Metagame.CitadelLobbyManager.Instance.AccountData.deployedFormation;
                var list = new List<UnitDataSO>();
                foreach (var h in activeHeroPool)
                {
                    if (h != null && formationTypes.Contains(h.unitType))
                    {
                        list.Add(h);
                    }
                }
                if (list.Count > 0) return list;
            }
            return activeHeroPool;
        }

        /// <summary>
        /// Mengambil data hero acak dengan mengecualikan tipe-tipe tertentu.
        /// Digunakan untuk inisialisasi papan grid bebas formasi kembar (No Initial Matches).
        /// </summary>
        public UnitDataSO GetRandomHeroExcluding(System.Collections.Generic.HashSet<UnitType> excludedTypes)
        {
            if (activeHeroPool == null || activeHeroPool.Count == 0)
            {
                EnsureHeroPoolLoaded();
            }

            if (activeHeroPool == null || activeHeroPool.Count == 0) return null;

            var basePool = GetFormationBoundHeroes() ?? activeHeroPool;
            var candidates = new System.Collections.Generic.List<UnitDataSO>();
            foreach (var h in basePool)
            {
                if (h != null && (excludedTypes == null || !excludedTypes.Contains(h.unitType)))
                {
                    candidates.Add(h);
                }
            }

            if (candidates.Count == 0)
            {
                return GetRandomHeroFromPool();
            }

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// Melakukan instansiasi fisik GameObject hero pada koordinat petak grid.
        /// </summary>
        public UnitController SpawnUnitAtCell(UnitDataSO heroData, TileCell cell, UnitTier tier = UnitTier.Tier1, bool isPreparationToken = false)
        {
            if (cell == null || heroData == null) return null;

            GameObject unitObj = null;

            // Prioritas 1: Gunakan prefab spesifik dari UnitDataSO jika ada
            if (heroData.unitPrefab != null)
            {
                unitObj = Instantiate(heroData.unitPrefab, cell.transform.position, Quaternion.identity);
            }
            // Prioritas 2: Gunakan default prefab
            else if (defaultHeroPrefab != null)
            {
                unitObj = Instantiate(defaultHeroPrefab, cell.transform.position, Quaternion.identity);
            }
            // Prioritas 3: Buat GameObject dinamis secara runtime (Clean Fallback)
            else
            {
                unitObj = new GameObject($"Unit_{heroData.unitType}_T{(int)tier}");
                unitObj.transform.position = new Vector3(cell.transform.position.x, cell.transform.position.y, -0.1f);

                var sr = unitObj.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Gem";
                sr.sortingOrder = 20;

                float cellW = GridSystem.GridManager.Instance != null ? GridSystem.GridManager.Instance.CellSize : 1.18f;
                var col = unitObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(cellW * 0.85f, cellW * 0.85f);
                col.isTrigger = false;

                // Tambahkan script subclass spesifik sesuai tipe hero
                switch (heroData.unitType)
                {
                    case UnitType.Cedric:
                        unitObj.AddComponent<CedricUnit>();
                        break;
                    case UnitType.RoseTanker:
                        unitObj.AddComponent<RoseTankerUnit>();
                        break;
                    case UnitType.RabbitMage:
                        unitObj.AddComponent<RabbitMageUnit>();
                        break;
                    case UnitType.WindRanger:
                        unitObj.AddComponent<WindRangerUnit>();
                        break;
                    default:
                        unitObj.AddComponent<UnitController>();
                        break;
                }
            }

            UnitController controller = unitObj.GetComponent<UnitController>();
            if (controller == null)
            {
                controller = unitObj.AddComponent<UnitController>();
            }

            controller.Initialize(heroData, tier, cell);

            if (isPreparationToken)
            {
                controller.SetAsPreparationToken(true);
            }
            else
            {
                // Efek visual portal pemanggilan dan audio resonansi mistis
                Color portalCol = heroData.unitType switch
                {
                    UnitType.Cedric => new Color(1f, 0.40f, 0.25f),
                    UnitType.RoseTanker => new Color(1f, 0.45f, 0.75f),
                    UnitType.RabbitMage => new Color(0.35f, 0.65f, 1f),
                    UnitType.WindRanger => new Color(0.25f, 0.95f, 0.55f),
                    _ => new Color(1f, 0.85f, 0.35f)
                };

                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnSummonPortalVfx(cell.transform.position, portalCol);
                }

                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.SummonHero);
                }
            }

            // Animasi bounce kemunculan hanya berlaku untuk mode pemanggilan biasa (bukan token puzzle persiapan)
            if (playSpawnScaleBounce && !isPreparationToken)
            {
                StartCoroutine(AnimateSpawnBounce(unitObj.transform));
            }

            return controller;
        }

        /// <summary>
        /// Animasi pop-up / bounce sederhana saat unit muncul di layar smartphone/tablet.
        /// </summary>
        private System.Collections.IEnumerator AnimateSpawnBounce(Transform unitTransform)
        {
            if (unitTransform == null) yield break;

            Vector3 targetScale = unitTransform.localScale;
            if (targetScale.sqrMagnitude < 0.001f) targetScale = Vector3.one;

            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (unitTransform == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Kurva elastis dari skala kecil ke skala penuh
                float scaleFactor = Mathf.Lerp(0.35f, 1.0f, progress) * (1f + Mathf.Sin(progress * Mathf.PI) * 0.15f);
                unitTransform.localScale = targetScale * scaleFactor;
                yield return null;
            }

            if (unitTransform != null)
            {
                unitTransform.localScale = targetScale;
                var anim = unitTransform.GetComponent<Combat.HeroVisualAnimator>();
                if (anim != null) anim.SetBaseScale(targetScale);
            }
        }

        public void RegisterHeroData(UnitDataSO data)
        {
            if (data != null && !activeHeroPool.Contains(data))
            {
                activeHeroPool.Add(data);
            }
        }
    }
}
