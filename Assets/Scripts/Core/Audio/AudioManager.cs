using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;

namespace FantasyDefense.Audio
{
    public enum SoundType
    {
        Slash,
        DualSlash,
        BladeTempest,
        ShieldSlam,
        Earthshaker,
        MagicSpark,
        MysticBurst,
        MeteorImpact,
        MergeSuccess,
        SummonHero,
        HeroSummon = SummonHero,
        CastleHit,
        CastleHitCreep,
        CastleHitBrute,
        CastleHitBoss,
        VictoryFanfare,
        DefeatChime,
        ButtonClick,
        ArrowShot,
        PiercingGale,
        TornadoImpact,
        GachaPull,
        GachaLegendary,
        BossRoar,
        CannonFire,
        BallistaShoot = ArrowShot,
        Surrender = DefeatChime,
        WaveIncoming,
        BarricadeRepair,
        CatalystThunderZap,
        GoldCoinSpend,
        ModeSwitchWhoosh,
        ArchetypeMaxChime,
        TileSlideSwish,
        SlimeSquishImpact,
        GoblinBucklerClank,
        MonsterHitSlime = SlimeSquishImpact,
        MonsterHitGoblin = GoblinBucklerClank,
        HeroRecycleChime,
        AltarWhooshOpen,
        AltarWhooshClose,
        UndoRestoreChime,
        DialogueBlipChime,
        GolemFistSmash,
        FrostWyrmBlizzard,
        PermafrostCrystalShatter,
        TwinFocusChime,
        CriticalHitChime,
        TileReboundThud,
        HeroUpgrade = MergeSuccess,
        SummonCard = GachaPull
    }

    public enum AudioSfxType
    {
        ButtonClick,
        UnitMergeTier3,
        VictoryTrumpet
    }

    public enum SkillAudioType
    {
        Lightning,
        Fire,
        Ice,
        Wind
    }

    public enum BgmTrack
    {
        None,
        LobbyTheme,
        BattleTheme,
        CitadelLobby = LobbyTheme,
        BattlefieldTheme = BattleTheme,
        TitleTheme,
        MiniBossTheme,
        BossTheme,
        BattlefieldForest,
        BattlefieldMountain,
        BattlefieldVolcanic,
        BattlefieldBeach
    }

    /// <summary>
    /// Master Audio System untuk Fantasy Citadel Defense.
    /// Mendukung pemutaran SFX prosedural dan musik latar BGM (synthesized audio tone & melodies)
    /// secara langsung tanpa perlu file audio eksternal,
    /// serta mendukung penggantian dengan AudioClip asli melalui Inspector jika diinginkan.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources Pool")]
        [SerializeField] private int audioSourcePoolSize = 8;
        private List<AudioSource> sfxSources = new List<AudioSource>();
        private AudioSource bgmSource;
        private int currentSourceIndex = 0;

        [Header("Volume Controls")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [SerializeField] private bool isMasterMuted = false;
        [SerializeField] private bool isBgmMuted = false;
        [SerializeField] private bool isSfxMuted = false;

        [Header("Current BGM State")]
        [SerializeField] private BgmTrack currentBgmTrack = BgmTrack.None;

        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public bool IsMasterMuted => isMasterMuted;
        public bool IsBgmMuted => isBgmMuted;
        public bool IsSfxMuted => isSfxMuted;
        public BgmTrack CurrentBgmTrack => currentBgmTrack;

        public static event System.Action<float, float, float> OnVolumeChanged;

        [Header("Custom Audio Drop-In Overrides (Optional)")]
        [Tooltip("BGM khusus Lobby Citadel jika ingin mengganti musik prosedural bawaan")]
        [SerializeField] private AudioClip customLobbyBgm;
        [Tooltip("BGM khusus Pertempuran jika ingin mengganti musik prosedural bawaan")]
        [SerializeField] private AudioClip customBattleBgm;

        // Cache untuk real audio drop-in dan procedural clips
        private readonly Dictionary<string, AudioClip> externalClipCache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<SoundType, AudioClip> proceduralClipCache = new Dictionary<SoundType, AudioClip>();
        private readonly Dictionary<BgmTrack, AudioClip> proceduralBgmCache = new Dictionary<BgmTrack, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeAudioSources();
            GenerateProceduralSoundClips();
            GenerateProceduralBgmClips();
            LoadSavedVolumes();
        }

        private void OnEnable()
        {
            UnitController.OnUnitSkillTriggered += HandleUnitSkillTriggered;
            UnitController.OnUnitMerged += HandleUnitMerged;
            Summon.HeroSummoner.OnHeroSummoned += HandleHeroSummoned;
            GameManager.OnBattleStateChanged += HandleBattleStateChanged;
            CastleBarricade.OnCastleHealthChanged += HandleCastleHealthChanged;
        }

        private void OnDisable()
        {
            UnitController.OnUnitSkillTriggered -= HandleUnitSkillTriggered;
            UnitController.OnUnitMerged -= HandleUnitMerged;
            Summon.HeroSummoner.OnHeroSummoned -= HandleHeroSummoned;
            GameManager.OnBattleStateChanged -= HandleBattleStateChanged;
            CastleBarricade.OnCastleHealthChanged -= HandleCastleHealthChanged;
        }

        private void InitializeAudioSources()
        {
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D Sound untuk mobile
                sfxSources.Add(src);
            }

            // Inisialisasi AudioSource khusus BGM musik latar
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.loop = true;
        }

        private void LoadSavedVolumes()
        {
            if (Save.SaveManager.Instance != null && Save.SaveManager.Instance.CurrentData != null)
            {
                var data = Save.SaveManager.Instance.CurrentData;
                masterVolume = data.masterVolume > 0f ? Mathf.Clamp01(data.masterVolume) : 1.0f;
                bgmVolume = Mathf.Clamp01(data.bgmVolume);
                sfxVolume = Mathf.Clamp01(data.sfxVolume);
                isMasterMuted = data.isMasterMuted;
                isBgmMuted = data.isBgmMuted;
                isSfxMuted = data.isSfxMuted;
            }
            UpdateVolumes();
        }

        private int sfxPlayCounter = 0;
        private readonly Dictionary<SoundType, float> lastPlayTimeBySound = new Dictionary<SoundType, float>();
        private Coroutine duckBgmCoroutine;
        private Coroutine bgmCrossfadeCoroutine;

        /// <summary>
        /// Meredam volume BGM sementara saat ledakan besar/fanfare terjadi (Sidechain Ducking),
        /// memberikan kejelasan dan kepuasan impak tanpa menenggelamkan melodi pertempuran.
        /// </summary>
        public void DuckBgm(float duckMultiplier = 0.78f, float duration = 0.40f)
        {
            if (bgmSource == null || !bgmSource.isPlaying || isBgmMuted) return;
            if (duckBgmCoroutine != null) StopCoroutine(duckBgmCoroutine);
            duckBgmCoroutine = StartCoroutine(DuckBgmRoutine(duckMultiplier, duration));
        }

        private System.Collections.IEnumerator DuckBgmRoutine(float duckMultiplier, float duration)
        {
            float normalVol = bgmVolume * masterVolume;
            float targetDuckedVol = normalVol * duckMultiplier;
            float attackDur = duration * 0.25f;
            float elapsed = 0f;

            while (elapsed < attackDur)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(normalVol, targetDuckedVol, elapsed / attackDur);
                yield return null;
            }

            elapsed = 0f;
            float releaseDur = duration - attackDur;
            while (elapsed < releaseDur)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(targetDuckedVol, normalVol, elapsed / releaseDur);
                yield return null;
            }

            bgmSource.volume = normalVol;
            duckBgmCoroutine = null;
        }

        public void PlaySound(SoundType type, float pitchOverride = 1.0f)
        {
            if (isMasterMuted || isSfxMuted || sfxSources.Count == 0) return;

            float speedFactor = Time.timeScale;

            // Anti-clipping & concurrency throttling adaptif terhadap kecepatan game (1x, 2x, 3x)
            float minInterval = 0.032f;
            if (speedFactor >= 1.8f)
            {
                // Pada mode cepat (2x/3x), proyektil dan benturan frekuensi tinggi datang lebih rapat.
                // Tingkatkan interval throttling khusus audio impak cepat agar tidak terjadi penumpukan suara (audio mud/clipping).
                if (type == SoundType.ArrowShot || type == SoundType.MagicSpark || 
                    type == SoundType.SlimeSquishImpact || type == SoundType.GoblinBucklerClank ||
                    type == SoundType.Slash || type == SoundType.DualSlash)
                {
                    minInterval = speedFactor >= 2.5f ? 0.065f : 0.050f;
                }
            }

            float now = Time.unscaledTime;
            if (lastPlayTimeBySound.TryGetValue(type, out float lastTime))
            {
                if (now - lastTime < minInterval) return;
            }
            lastPlayTimeBySound[type] = now;

            AudioClip clip = GetAudioClip(type);
            if (clip == null) return;

            AudioSource source = sfxSources[currentSourceIndex];
            currentSourceIndex = (currentSourceIndex + 1) % sfxSources.Count;
            sfxPlayCounter++;

            // Kalibrasi Volume Balancing adaptif terhadap kecepatan game (1x, 2x, 3x)
            float speedVolumeDamping = 1.0f;
            if (speedFactor >= 2.5f)
            {
                // Kecepatan 3x: redam 30% agar serangan beruntun tidak menimbulkan audio mud / ear fatigue
                speedVolumeDamping = 0.70f;
            }
            else if (speedFactor >= 1.8f)
            {
                // Kecepatan 2x: redam 18% untuk keseimbangan jernih dengan musik pertempuran
                speedVolumeDamping = 0.82f;
            }

            source.volume = sfxVolume * masterVolume * speedVolumeDamping;

            // Variasi nada alami & anti-monotoni saat pertempuran berjalan di kecepatan 2x atau 3x
            float pitchJitter;
            if (type == SoundType.ButtonClick)
            {
                // Micro-pitch jitter alami untuk tap UI (0.96x - 1.04x) agar tap beruntun terdengar renyah & organik
                pitchJitter = Random.Range(0.96f, 1.04f);
            }
            else if (speedFactor >= 2.0f)
            {
                // Batasi deviasi nada agar suara impak tetap musikal dan tidak cempreng/distorsi
                float rhythmicStep = (sfxPlayCounter % 3 - 1) * 0.025f;
                pitchJitter = Random.Range(0.95f, 1.06f) + rhythmicStep;
            }
            else
            {
                pitchJitter = Random.Range(0.96f, 1.04f);
            }

            source.pitch = Mathf.Clamp(pitchOverride * pitchJitter, 0.75f, 1.45f);
            source.PlayOneShot(clip);

            // BGM sidechain ducking otomatis pada tembakan meriam benteng dan ledakan berat
            if (type == SoundType.CannonFire || type == SoundType.MeteorImpact || type == SoundType.CastleHitBoss)
            {
                DuckBgm(0.78f, 0.40f);
            }
        }

        public void PlaySfx(AudioSfxType sfxType)
        {
            switch (sfxType)
            {
                case AudioSfxType.ButtonClick:
                    PlaySound(SoundType.ButtonClick);
                    break;
                case AudioSfxType.UnitMergeTier3:
                    PlaySound(SoundType.MergeSuccess);
                    break;
                case AudioSfxType.VictoryTrumpet:
                    PlaySound(SoundType.VictoryFanfare);
                    break;
                default:
                    PlaySound(SoundType.ButtonClick);
                    break;
            }
        }

        public void PlaySkillSfx(SkillAudioType skillAudio)
        {
            switch (skillAudio)
            {
                case SkillAudioType.Lightning:
                    PlaySound(SoundType.MeteorImpact, 1.3f);
                    break;
                default:
                    PlaySound(SoundType.MysticBurst);
                    break;
            }
        }

        /// <summary>
        /// Memainkan variasi SFX hantaman gerbang benteng dan getaran haptic ponsel sesuai jenis monster pengepung (Point 4).
        /// Creep: gesit tajam (cakar), Brute: dentuman godam berat, Boss: gempa menggelegar.
        /// </summary>
        public void PlayCastleHit(MonsterType monsterType)
        {
            SoundType sound = monsterType switch
            {
                MonsterType.Creep => SoundType.CastleHitCreep,
                MonsterType.Brute => SoundType.CastleHitBrute,
                MonsterType.Boss => SoundType.CastleHitBoss,
                _ => SoundType.CastleHit
            };

            float pitch = monsterType switch
            {
                MonsterType.Creep => Random.Range(0.95f, 1.15f),
                MonsterType.Brute => Random.Range(0.85f, 1.0f),
                MonsterType.Boss => Random.Range(0.75f, 0.90f),
                _ => 1.0f
            };

            // Volume balance: creep 0.72x, brute 0.82x, boss 0.95x agar tidak menutupi BGM & dialog
            float volumeScale = monsterType switch
            {
                MonsterType.Creep => 0.72f,
                MonsterType.Brute => 0.82f,
                MonsterType.Boss => 0.95f,
                _ => 0.75f
            };

            PlaySound(sound, pitch * volumeScale);

            // Memicu haptic feedback ponsel yang proporsional dengan bobot musuh
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null)
            {
                var hapticType = monsterType switch
                {
                    MonsterType.Creep => FantasyDefense.Mobile.HapticFeedbackType.Light,
                    MonsterType.Brute => FantasyDefense.Mobile.HapticFeedbackType.Medium,
                    MonsterType.Boss => FantasyDefense.Mobile.HapticFeedbackType.Heavy,
                    _ => FantasyDefense.Mobile.HapticFeedbackType.Light
                };
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(hapticType);
            }
        }

        public void PlayBGM(BgmTrack track, float fadeDuration = 0.45f)
        {
            if (bgmSource == null) return;
            if (currentBgmTrack == track && bgmSource.isPlaying) return;

            currentBgmTrack = track;
            if (track == BgmTrack.None)
            {
                StopBGM(fadeDuration);
                return;
            }

            // 1. Cek Inspector override
            AudioClip realBgm = null;
            if (track == BgmTrack.LobbyTheme && customLobbyBgm != null) realBgm = customLobbyBgm;
            else if (track == BgmTrack.BattleTheme && customBattleBgm != null) realBgm = customBattleBgm;

            // 2. Cek Resources drop-in (Assets/Resources/Audio/Music/)
            if (realBgm == null)
            {
                string resPath = track switch
                {
                    BgmTrack.TitleTheme => "Audio/Music/BGM_Title_Screen",
                    BgmTrack.MiniBossTheme => "Audio/Music/BGM_MiniBoss_Theme",
                    BgmTrack.BossTheme => "Audio/Music/BGM_Boss_Theme",
                    BgmTrack.LobbyTheme => "Audio/Music/BGM_Citadel_Lobby",
                    BgmTrack.BattleTheme => "Audio/Music/BGM_Battle_Theme",
                    BgmTrack.BattlefieldForest => "Audio/Music/BGM_Battlefield_Forest",
                    BgmTrack.BattlefieldMountain => "Audio/Music/BGM_Battlefield_Mountain",
                    BgmTrack.BattlefieldVolcanic => "Audio/Music/BGM_Battlefield_Volcanic",
                    BgmTrack.BattlefieldBeach => "Audio/Music/BGM_Battlefield_Beach",
                    _ => null
                };
                if (!string.IsNullOrEmpty(resPath))
                {
                    realBgm = Resources.Load<AudioClip>(resPath);
                }
            }

            AudioClip targetClip = realBgm;
            if (targetClip == null)
            {
                proceduralBgmCache.TryGetValue(track, out targetClip);
            }

            if (targetClip != null)
            {
                if (bgmCrossfadeCoroutine != null) StopCoroutine(bgmCrossfadeCoroutine);
                if (gameObject.activeInHierarchy && bgmSource.isPlaying && fadeDuration > 0.05f)
                {
                    bgmCrossfadeCoroutine = StartCoroutine(CrossfadeBgmRoutine(targetClip, fadeDuration));
                }
                else
                {
                    bgmSource.clip = targetClip;
                    UpdateVolumes();
                    bgmSource.Play();
                }
            }
        }

        private System.Collections.IEnumerator CrossfadeBgmRoutine(AudioClip newClip, float duration)
        {
            float targetVol = isBgmMuted ? 0f : (bgmVolume * masterVolume);
            float startVol = bgmSource.volume;
            float halfDuration = duration * 0.5f;

            // Fade out musik lama
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.volume = 0f;
            bgmSource.Play();

            // Fade in musik baru
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                bgmSource.volume = Mathf.Lerp(0f, targetVol, t);
                yield return null;
            }

            bgmSource.volume = targetVol;
            bgmCrossfadeCoroutine = null;
        }

        public void PlayBgm(BgmTrack track) => PlayBGM(track, 0.45f);

        public void StopBGM(float fadeDuration = 0.35f)
        {
            currentBgmTrack = BgmTrack.None;
            if (bgmSource == null || !bgmSource.isPlaying) return;

            if (bgmCrossfadeCoroutine != null) StopCoroutine(bgmCrossfadeCoroutine);
            if (gameObject.activeInHierarchy && fadeDuration > 0.05f)
            {
                bgmCrossfadeCoroutine = StartCoroutine(FadeOutBgmRoutine(fadeDuration));
            }
            else
            {
                bgmSource.Stop();
            }
        }

        private System.Collections.IEnumerator FadeOutBgmRoutine(float duration)
        {
            float startVol = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }
            bgmSource.Stop();
            UpdateVolumes();
            bgmCrossfadeCoroutine = null;
        }

        public void SetMasterVolume(float val)
        {
            masterVolume = Mathf.Clamp01(val);
            UpdateVolumes();
            SyncWithSaveData();
            OnVolumeChanged?.Invoke(masterVolume, bgmVolume, sfxVolume);
        }

        public void SetBgmVolume(float val)
        {
            bgmVolume = Mathf.Clamp01(val);
            UpdateVolumes();
            SyncWithSaveData();
            OnVolumeChanged?.Invoke(masterVolume, bgmVolume, sfxVolume);
        }

        public void SetSfxVolume(float val)
        {
            sfxVolume = Mathf.Clamp01(val);
            UpdateVolumes();
            SyncWithSaveData();
            OnVolumeChanged?.Invoke(masterVolume, bgmVolume, sfxVolume);
        }

        public void ToggleMuteMaster()
        {
            isMasterMuted = !isMasterMuted;
            UpdateVolumes();
            SyncWithSaveData();
            OnVolumeChanged?.Invoke(masterVolume, bgmVolume, sfxVolume);
        }

        public void ToggleMuteBgm()
        {
            isBgmMuted = !isBgmMuted;
            UpdateVolumes();
            SyncWithSaveData();
            OnVolumeChanged?.Invoke(masterVolume, bgmVolume, sfxVolume);
        }

        public void ToggleMuteSfx()
        {
            isSfxMuted = !isSfxMuted;
            UpdateVolumes();
            SyncWithSaveData();
            OnVolumeChanged?.Invoke(masterVolume, bgmVolume, sfxVolume);
        }

        private void UpdateVolumes()
        {
            float effectiveMaster = isMasterMuted ? 0f : masterVolume;
            if (bgmSource != null)
            {
                bgmSource.volume = (isBgmMuted || isMasterMuted) ? 0f : (bgmVolume * effectiveMaster);
            }
            if (isMasterMuted || isSfxMuted)
            {
                foreach (var s in sfxSources)
                {
                    if (s != null && s.isPlaying) s.volume = 0f;
                }
            }
        }

        private void SyncWithSaveData()
        {
            if (Save.SaveManager.Instance != null && Save.SaveManager.Instance.CurrentData != null)
            {
                Save.SaveManager.Instance.CurrentData.masterVolume = masterVolume;
                Save.SaveManager.Instance.CurrentData.bgmVolume = bgmVolume;
                Save.SaveManager.Instance.CurrentData.sfxVolume = sfxVolume;
                Save.SaveManager.Instance.CurrentData.isMasterMuted = isMasterMuted;
                Save.SaveManager.Instance.CurrentData.isBgmMuted = isBgmMuted;
                Save.SaveManager.Instance.CurrentData.isSfxMuted = isSfxMuted;
            }
        }

        private AudioClip GetAudioClip(SoundType type)
        {
            string key = type.ToString();

            // 1. Cek cache drop-in eksternal
            if (externalClipCache.TryGetValue(key, out AudioClip cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            // 2. Cek Resources drop-in (Assets/Resources/Audio/SFX/[SoundType])
            try
            {
                AudioClip resClip = Resources.Load<AudioClip>($"Audio/SFX/{key}");
                if (resClip != null)
                {
                    externalClipCache[key] = resClip;
                    return resClip;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AudioManager] Exception loading Resources audio '{key}': {ex.Message}");
            }

            // 3. Fallback mulus ke procedural synthesis
            if (proceduralClipCache.TryGetValue(type, out AudioClip clip) && clip != null)
            {
                return clip;
            }
            return null;
        }

        /// <summary>
        /// Memainkan seruan vokal heroik khas pahlawan (Hero Battle Grunt / Vocal Shouts)
        /// saat mengeksekusi skill aktif Tier 2 ke atas.
        /// </summary>
        public void PlayHeroSkillVoice(UnitType heroType)
        {
            if (isSfxMuted || sfxSources.Count == 0) return;

            string voiceKey = heroType switch
            {
                UnitType.Cedric => "Voice_Cedric_Skill",
                UnitType.RoseTanker => "Voice_Rose_Skill",
                UnitType.RabbitMage => "Voice_Rabbit_Skill",
                UnitType.WindRanger => "Voice_Wind_Skill",
                _ => "Voice_Cedric_Skill"
            };

            AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{voiceKey}");
            if (clip != null)
            {
                AudioSource source = sfxSources[currentSourceIndex];
                currentSourceIndex = (currentSourceIndex + 1) % sfxSources.Count;
                source.volume = sfxVolume * masterVolume * 0.95f;
                source.pitch = Random.Range(0.96f, 1.04f);
                source.PlayOneShot(clip);
            }
        }

        #region Event Handlers

        private void HandleUnitSkillTriggered(UnitController unit, string skillName)
        {
            if (unit == null) return;

            if (unit.CurrentTier >= UnitTier.Tier2)
            {
                PlayHeroSkillVoice(unit.UnitType);
            }

            // Kalibrasi pitch dinamis berbasis Tier pahlawan (T1 -> T5) agar progres merge terasa bertenaga
            float tierPitchMultiplier = unit.CurrentTier switch
            {
                UnitTier.Tier5 => 1.25f,
                UnitTier.Tier4 => 1.15f,
                UnitTier.Tier3 => 1.08f,
                UnitTier.Tier2 => 1.02f,
                _ => 1.0f
            };

            switch (unit.UnitType)
            {
                case UnitType.Cedric:
                    if (unit.CurrentTier >= UnitTier.Tier3) PlaySound(SoundType.BladeTempest, tierPitchMultiplier);
                    else if (unit.CurrentTier == UnitTier.Tier2) PlaySound(SoundType.DualSlash, tierPitchMultiplier);
                    else PlaySound(SoundType.Slash, tierPitchMultiplier);
                    break;

                case UnitType.RoseTanker:
                    if (unit.CurrentTier >= UnitTier.Tier3) PlaySound(SoundType.Earthshaker, tierPitchMultiplier);
                    else PlaySound(SoundType.ShieldSlam, tierPitchMultiplier);
                    break;

                case UnitType.RabbitMage:
                    if (unit.CurrentTier >= UnitTier.Tier3) PlaySound(SoundType.MeteorImpact, tierPitchMultiplier);
                    else if (unit.CurrentTier == UnitTier.Tier2) PlaySound(SoundType.MysticBurst, tierPitchMultiplier);
                    else PlaySound(SoundType.MagicSpark, tierPitchMultiplier);
                    break;

                case UnitType.WindRanger:
                    float arrowRandomPitch = tierPitchMultiplier * Random.Range(0.92f, 1.08f);
                    if (unit.CurrentTier >= UnitTier.Tier3) PlaySound(SoundType.TornadoImpact, arrowRandomPitch);
                    else if (unit.CurrentTier == UnitTier.Tier2) PlaySound(SoundType.PiercingGale, arrowRandomPitch);
                    else PlaySound(SoundType.ArrowShot, arrowRandomPitch);
                    break;
            }
        }

        private void HandleUnitMerged(UnitController source, UnitController target, UnitTier newTier)
        {
            float pitch = newTier == UnitTier.Tier5 ? 1.70f : (newTier == UnitTier.Tier4 ? 1.55f : (newTier == UnitTier.Tier3 ? 1.45f : 1.20f));
            PlaySound(SoundType.MergeSuccess, pitch);

            // Berikan fanfare perayaan jika berhasil menciptakan Hero Tier 3+ (Ultimate / Mythic / Celestial)
            if (newTier >= UnitTier.Tier3)
            {
                PlaySound(SoundType.GachaLegendary, newTier == UnitTier.Tier5 ? 1.25f : 1.1f);
            }
        }

        private void HandleHeroSummoned(UnitController unit, GridSystem.TileCell cell)
        {
            PlaySound(SoundType.SummonHero);
        }

        private int lastCastleHealth = -1;
        private void HandleCastleHealthChanged(int current, int max)
        {
            if (lastCastleHealth > current)
            {
                PlaySound(SoundType.CastleHit);
            }
            lastCastleHealth = current;
        }

        private void HandleBattleStateChanged(BattleState state)
        {
            if (state == BattleState.WaveActive)
            {
                PlayBGM(BgmTrack.BattleTheme);
            }
            else if (state == BattleState.Preparation)
            {
                PlayBGM(BgmTrack.LobbyTheme);
            }
            else if (state == BattleState.Victory)
            {
                StopBGM();
                PlaySound(SoundType.VictoryFanfare);
            }
            else if (state == BattleState.Defeat)
            {
                StopBGM();
                PlaySound(SoundType.DefeatChime);
            }
        }

        #endregion

        #region Procedural Audio Synthesis

        private void GenerateProceduralSoundClips()
        {
            proceduralClipCache[SoundType.Slash] = CreateToneClip("Slash", 0.08f, 440f, 180f);
            proceduralClipCache[SoundType.DualSlash] = CreateToneClip("DualSlash", 0.14f, 520f, 320f);
            proceduralClipCache[SoundType.BladeTempest] = CreateToneClip("BladeTempest", 0.28f, 650f, 220f);

            proceduralClipCache[SoundType.ShieldSlam] = CreateToneClip("ShieldSlam", 0.15f, 200f, 60f);
            proceduralClipCache[SoundType.Earthshaker] = CreateToneClip("Earthshaker", 0.35f, 150f, 40f);

            proceduralClipCache[SoundType.MagicSpark] = CreateToneClip("MagicSpark", 0.12f, 880f, 580f);
            proceduralClipCache[SoundType.MysticBurst] = CreateToneClip("MysticBurst", 0.22f, 780f, 350f);
            proceduralClipCache[SoundType.MeteorImpact] = CreateToneClip("MeteorImpact", 0.45f, 240f, 45f);

            proceduralClipCache[SoundType.MergeSuccess] = CreateArpeggioClip("MergeSuccess", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.06f);
            proceduralClipCache[SoundType.SummonHero] = CreateArpeggioClip("SummonHero", new float[] { 587.33f, 880f, 1174.66f, 1760f }, 0.055f);
            proceduralClipCache[SoundType.CastleHit] = CreateToneClip("CastleHit", 0.25f, 120f, 50f);
            proceduralClipCache[SoundType.CastleHitCreep] = CreateToneClip("CastleHitCreep", 0.12f, 220f, 130f); // Pukulan gesit cakar goblin
            proceduralClipCache[SoundType.CastleHitBrute] = CreateToneClip("CastleHitBrute", 0.30f, 110f, 40f); // Dentuman berat godam orc
            proceduralClipCache[SoundType.CastleHitBoss] = CreateToneClip("CastleHitBoss", 0.48f, 75f, 25f); // Gempa subwoofer destruktif boss

            proceduralClipCache[SoundType.VictoryFanfare] = CreateArpeggioClip("VictoryFanfare", new float[] { 440f, 554.37f, 659.25f, 880f, 1108.7f }, 0.12f);
            proceduralClipCache[SoundType.DefeatChime] = CreateArpeggioClip("DefeatChime", new float[] { 659.25f, 587.33f, 523.25f, 392f }, 0.15f);
            proceduralClipCache[SoundType.ButtonClick] = CreateToneClip("ButtonClick", 0.04f, 800f, 400f);

            // Audio Sintesis Tambahan Sprint 8 (Wind Ranger, Boss & Gacha)
            proceduralClipCache[SoundType.ArrowShot] = CreateToneClip("ArrowShot", 0.09f, 620f, 260f);
            proceduralClipCache[SoundType.PiercingGale] = CreateToneClip("PiercingGale", 0.18f, 750f, 320f);
            proceduralClipCache[SoundType.TornadoImpact] = CreateToneClip("TornadoImpact", 0.38f, 380f, 120f);
            proceduralClipCache[SoundType.GachaPull] = CreateArpeggioClip("GachaPull", new float[] { 330f, 392f, 493.88f, 659.25f }, 0.08f);
            proceduralClipCache[SoundType.GachaLegendary] = CreateArpeggioClip("GachaLegendary", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.5f }, 0.14f);
            proceduralClipCache[SoundType.BossRoar] = CreateToneClip("BossRoar", 0.5f, 120f, 55f);
            proceduralClipCache[SoundType.CannonFire] = CreateToneClip("CannonFire", 0.32f, 180f, 40f);
            proceduralClipCache[SoundType.WaveIncoming] = CreateArpeggioClip("WaveIncoming", new float[] { 349.23f, 440f, 523.25f, 698.46f }, 0.09f);
            proceduralClipCache[SoundType.BarricadeRepair] = CreateArpeggioClip("BarricadeRepair", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.5f }, 0.05f);
            proceduralClipCache[SoundType.CatalystThunderZap] = CreateToneClip("CatalystThunderZap", 0.36f, 1250f, 85f);
            proceduralClipCache[SoundType.GoldCoinSpend] = CreateArpeggioClip("GoldCoinSpend", new float[] { 1760f, 2637f, 3520f }, 0.045f);
            proceduralClipCache[SoundType.HeroRecycleChime] = CreateArpeggioClip("HeroRecycleChime", new float[] { 659.25f, 987.77f, 1318.51f, 1975.53f }, 0.05f);
            proceduralClipCache[SoundType.AltarWhooshOpen] = CreateToneClip("AltarWhooshOpen", 0.18f, 220f, 540f);
            proceduralClipCache[SoundType.AltarWhooshClose] = CreateToneClip("AltarWhooshClose", 0.15f, 480f, 180f);
            proceduralClipCache[SoundType.UndoRestoreChime] = CreateArpeggioClip("UndoRestoreChime", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.045f);
            proceduralClipCache[SoundType.DialogueBlipChime] = CreateToneClip("DialogueBlipChime", 0.05f, 660f, 880f);
            proceduralClipCache[SoundType.ModeSwitchWhoosh] = CreateToneClip("ModeSwitchWhoosh", 0.42f, 280f, 620f);
            proceduralClipCache[SoundType.ArchetypeMaxChime] = CreateArpeggioClip("ArchetypeMaxChime", new float[] { 1046.50f, 1318.51f, 1567.98f, 2093.00f, 2637.02f }, 0.055f);
            proceduralClipCache[SoundType.TileSlideSwish] = CreateToneClip("TileSlideSwish", 0.16f, 480f, 210f);

            // Audio Khusus Monster Canonical Mockup (Chibi Slime & Chibi Goblin)
            proceduralClipCache[SoundType.SlimeSquishImpact] = CreateToneClip("SlimeSquishImpact", 0.16f, 360f, 160f); // Kenyal gelatinous bubble squish slime
            proceduralClipCache[SoundType.GoblinBucklerClank] = CreateToneClip("GoblinBucklerClank", 0.22f, 540f, 150f); // Denting tameng kayu & bilah goblin

            // Audio Khusus Mini-Boss Archetype Attacks
            proceduralClipCache[SoundType.GolemFistSmash] = CreateToneClip("GolemFistSmash", 0.38f, 160f, 35f); // Hentakan tinju batu & kayu purba berbobot gempa
            proceduralClipCache[SoundType.FrostWyrmBlizzard] = CreateToneClip("FrostWyrmBlizzard", 0.45f, 680f, 180f); // Desisan hembusan badai salju kutub
            proceduralClipCache[SoundType.PermafrostCrystalShatter] = CreateArpeggioClip("PermafrostCrystalShatter", new float[] { 880f, 1318.5f, 1760f, 2637f }, 0.04f); // Pecahan kristal es tajam permafrost

            // Audio Khusus Tactile Twin Gem Focus (Resonansi Lonceng Kristal Harmonis Emas)
            proceduralClipCache[SoundType.TwinFocusChime] = CreateArpeggioClip("TwinFocusChime", new float[] { 783.99f, 1046.50f, 1318.51f, 1567.98f }, 0.038f);

            // Audio Khusus Critical Hit Chime (Resonansi Logam Emas Tajam Berkekuatan Tinggi)
            proceduralClipCache[SoundType.CriticalHitChime] = CreateArpeggioClip("CriticalHitChime", new float[] { 1046.50f, 1567.98f, 2093.00f, 3135.96f }, 0.024f);

            // Audio Khusus Tactile Tile Rebound Thud (Benturan elastis lembut saat geseran batal/terlarang)
            proceduralClipCache[SoundType.TileReboundThud] = CreateToneClip("TileReboundThud", 0.14f, 220f, 95f);
        }

        private AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            bool isClick = name == "ButtonClick";
            bool isBlade = name.Contains("Slash") || name.Contains("Arrow");
            bool isSlime = name.Contains("Slime") || name == "CastleHitCreep";
            bool isGoblinBuckler = name.Contains("Goblin") || name == "CastleHitBrute";
            bool isImpact = name.Contains("Hit") || name.Contains("Cannon") || name.Contains("Earthshaker") || name.Contains("Shield") || name.Contains("Meteor");
            bool isZap = name == "CatalystThunderZap";
            bool isWhoosh = name == "ModeSwitchWhoosh";
            bool isTileSwish = name == "TileSlideSwish";
            bool isTileRebound = name == "TileReboundThud";

            float filterNoise = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / totalSamples;

                if (isClick)
                {
                    // Tactile wooden button tap: rapid exponential decay with warm body
                    float env = Mathf.Exp(-75f * progress);
                    float tone = Mathf.Sin(2 * Mathf.PI * 820f * t) * 0.65f + Mathf.Sin(2 * Mathf.PI * 260f * t) * 0.35f;
                    samples[i] = tone * env * 0.5f;
                }
                else if (isWhoosh)
                {
                    // Aerodynamic phase whoosh: parabolic bell envelope with smooth low-pass wind resonance
                    float env = Mathf.Sin(progress * Mathf.PI);
                    filterNoise += 0.20f * (Random.Range(-1f, 1f) - filterNoise);
                    float windFreq = Mathf.Lerp(startFreq, endFreq, progress);
                    float wind = Mathf.Sin(2 * Mathf.PI * windFreq * t) * 0.40f;
                    samples[i] = (filterNoise * 0.55f + wind) * env * 0.45f;
                }
                else if (isTileSwish)
                {
                    // Tactile stone tile sliding on board: textured friction noise with low ceramic/stone body
                    float env = Mathf.Sin(progress * Mathf.PI);
                    filterNoise += 0.42f * (Random.Range(-1f, 1f) - filterNoise);
                    float frictionFreq = Mathf.Lerp(startFreq, endFreq, Mathf.SmoothStep(0f, 1f, progress));
                    float stoneBody = Mathf.Sin(2 * Mathf.PI * frictionFreq * t) * 0.35f +
                                      Mathf.Sin(4 * Mathf.PI * frictionFreq * t) * 0.15f;
                    samples[i] = (filterNoise * 0.50f + stoneBody) * env * 0.38f;
                }
                else if (isTileRebound)
                {
                    // Tactile rubber/wood token rebound thud with spring-damped non-linear pitch decay
                    float env = Mathf.Exp(-22f * progress);
                    filterNoise += 0.10f * (Random.Range(-1f, 1f) - filterNoise);
                    float nonLinearProgress = Mathf.Pow(progress, 0.72f);
                    float curFreq = Mathf.Lerp(startFreq, endFreq, nonLinearProgress);
                    float primaryThud = Mathf.Sin(2 * Mathf.PI * curFreq * t) * 0.72f;
                    float subResonance = Mathf.Sin(2 * Mathf.PI * 75f * t) * 0.28f;
                    samples[i] = (primaryThud + subResonance + filterNoise * 0.08f) * env * 0.65f;
                }
                else if (isZap)
                {
                    // Thunder zap: high frequency electrical sizzle decaying rapidly into deep sub-bass resonance
                    float env = Mathf.Exp(-9.5f * progress);
                    filterNoise += 0.35f * (Random.Range(-1f, 1f) - filterNoise);
                    float freqSweep = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(progress, 0.45f));
                    float electricalTone = Mathf.Sin(2 * Mathf.PI * freqSweep * t) * 0.45f +
                                           Mathf.Sin(6 * Mathf.PI * freqSweep * t) * 0.20f;
                    float subBass = Mathf.Sin(2 * Mathf.PI * 65f * t) * Mathf.Clamp01(1f - progress * 1.5f) * 0.35f;
                    samples[i] = (electricalTone + filterNoise * 0.35f + subBass) * env * 0.55f;
                }
                else if (isBlade)
                {
                    // Aerodynamic blade swoosh: shaped filtered noise + steel overtone
                    float env = Mathf.Sin(progress * Mathf.PI);
                    filterNoise += 0.25f * (Random.Range(-1f, 1f) - filterNoise);
                    float steel = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(startFreq, endFreq, progress) * t) * 0.35f;
                    samples[i] = (filterNoise * 0.65f + steel) * env * 0.45f;
                }
                else if (isSlime)
                {
                    // Viscous gelatinous squish & wet bubble cavitation (Chibi Slime Gemetite Ooze)
                    float env = Mathf.Sin(progress * Mathf.PI);
                    filterNoise += 0.30f * (Random.Range(-1f, 1f) - filterNoise);
                    float bubbleWobble = Mathf.Sin(2 * Mathf.PI * 22f * t) * 35f;
                    float jellyFreq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(progress, 0.7f)) + bubbleWobble;
                    float squishTone = Mathf.Sin(2 * Mathf.PI * jellyFreq * t) * 0.60f +
                                       Mathf.Sin(4 * Mathf.PI * jellyFreq * t) * 0.25f;
                    samples[i] = (squishTone + filterNoise * 0.20f) * env * 0.50f;
                }
                else if (isGoblinBuckler)
                {
                    // Heavy wooden buckler clank with sharp metallic transient (Chibi Goblin Mire Scrapper)
                    float envWood = Mathf.Exp(-14f * progress);
                    float envMetal = Mathf.Exp(-28f * progress);
                    filterNoise += 0.38f * (Random.Range(-1f, 1f) - filterNoise);
                    float woodThud = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(startFreq, endFreq, progress) * t) * 0.65f;
                    float metalClang = Mathf.Sin(2 * Mathf.PI * 1320f * t) * 0.35f;
                    samples[i] = (woodThud * envWood + metalClang * envMetal + filterNoise * 0.25f * envWood) * 0.55f;
                }
                else if (isImpact)
                {
                    // Physical siege impact: low frequency punch + sub-bass body + dampening
                    float env = Mathf.Exp(-8f * progress);
                    float currentFreq = Mathf.Lerp(startFreq, endFreq, progress);
                    filterNoise += 0.15f * (Random.Range(-1f, 1f) - filterNoise);
                    float punch = Mathf.Sin(2 * Mathf.PI * currentFreq * t) * 0.7f + filterNoise * 0.3f;
                    samples[i] = punch * env * 0.6f;
                }
                else
                {
                    // Harmonic overtone synthesis (fundamental + 2nd + 3rd harmonics + sub-octave)
                    float env = Mathf.Exp(-4.5f * progress);
                    float currentFreq = Mathf.Lerp(startFreq, endFreq, progress);
                    float tone = Mathf.Sin(2 * Mathf.PI * currentFreq * t) * 0.50f +
                                 Mathf.Sin(4 * Mathf.PI * currentFreq * t) * 0.25f +
                                 Mathf.Sin(6 * Mathf.PI * currentFreq * t) * 0.12f +
                                 Mathf.Sin(Mathf.PI * currentFreq * t) * 0.13f;
                    samples[i] = tone * env * 0.4f;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(string name, float[] freqs, float noteDuration)
        {
            int sampleRate = 44100;
            int samplesPerNote = Mathf.FloorToInt(sampleRate * noteDuration);
            int tailSamples = Mathf.FloorToInt(sampleRate * 0.4f);
            int totalSamples = samplesPerNote * freqs.Length + tailSamples;
            float[] samples = new float[totalSamples];

            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                int offset = n * samplesPerNote;
                int remainingSamples = totalSamples - offset;

                for (int i = 0; i < remainingSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-5.5f * t);
                    if (env < 0.001f) break;

                    // Bell acoustic partials (1.0, 2.76, 5.40)
                    float tone = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.28f +
                                 Mathf.Sin(2 * Mathf.PI * (freq * 2.76f) * t) * 0.10f +
                                 Mathf.Sin(2 * Mathf.PI * (freq * 5.40f) * t) * 0.04f;

                    samples[offset + i] += tone * env;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void GenerateProceduralBgmClips()
        {
            // BGM 1: Tema Citadel Lobby - Melodi fantasi hangat, damai, dan megah (C Major Arpeggio Progression)
            float[] lobbyMelody = new float[] { 
                261.63f, 329.63f, 392.00f, 523.25f, 
                392.00f, 329.63f, 261.63f, 349.23f, 
                440.00f, 523.25f, 440.00f, 349.23f,
                293.66f, 369.99f, 440.00f, 587.33f
            };
            proceduralBgmCache[BgmTrack.LobbyTheme] = CreateHarmonicBgmClip("BGM_LobbyTheme", lobbyMelody, 0.40f);

            // BGM 2: Tema Arena Pertempuran - Ritme dinamis, cepat, dan heroic (A Minor / D Minor War Cadence)
            float[] battleMelody = new float[] { 
                220.00f, 261.63f, 293.66f, 329.63f,
                440.00f, 392.00f, 329.63f, 293.66f,
                261.63f, 220.00f, 196.00f, 220.00f,
                293.66f, 329.63f, 440.00f, 523.25f
            };
            proceduralBgmCache[BgmTrack.BattleTheme] = CreateRhythmicBgmClip("BGM_BattleTheme", battleMelody, 0.22f);

            // BGM 3: Bioma Hutan (Forest Outskirts) - Melodi pastoral petualangan orkestra ringan (F Major / C Major)
            float[] forestMelody = new float[] {
                349.23f, 392.00f, 440.00f, 523.25f,
                440.00f, 392.00f, 349.23f, 293.66f,
                329.63f, 349.23f, 392.00f, 523.25f,
                587.33f, 523.25f, 440.00f, 392.00f
            };
            proceduralBgmCache[BgmTrack.BattlefieldForest] = CreateHarmonicBgmClip("BGM_BattlefieldForest", forestMelody, 0.28f);

            // BGM 4: Bioma Gunung (Mountain Pass) - Suasana angin dingin hening dengan resonansi kristal alpine (D Dorian)
            float[] mountainMelody = new float[] {
                293.66f, 349.23f, 440.00f, 587.33f,
                440.00f, 349.23f, 329.63f, 392.00f,
                493.88f, 659.25f, 493.88f, 392.00f,
                293.66f, 440.00f, 587.33f, 880.00f
            };
            proceduralBgmCache[BgmTrack.BattlefieldMountain] = CreateHarmonicBgmClip("BGM_BattlefieldMountain", mountainMelody, 0.34f);

            // BGM 5: Bioma Vulkanik (Abyssal Volcanic) - Tensi dramatis cepat & hentakan ritme perang magma (C Minor War March)
            float[] volcanicMelody = new float[] {
                261.63f, 311.13f, 392.00f, 311.13f,
                246.94f, 293.66f, 369.99f, 293.66f,
                220.00f, 261.63f, 329.63f, 261.63f,
                196.00f, 246.94f, 293.66f, 392.00f
            };
            proceduralBgmCache[BgmTrack.BattlefieldVolcanic] = CreateRhythmicBgmClip("BGM_BattlefieldVolcanic", volcanicMelody, 0.18f);

            // BGM 6: Bioma Pantai Pesisir (Coastal Beach Shore) - Melodi angin laut tropis & nada riak ombak tenang (F Major Pentatonic)
            float[] beachMelody = new float[] {
                349.23f, 392.00f, 440.00f, 523.25f, 587.33f, 523.25f, 440.00f, 392.00f,
                349.23f, 440.00f, 523.25f, 659.25f, 587.33f, 523.25f, 440.00f, 349.23f
            };
            proceduralBgmCache[BgmTrack.BattlefieldBeach] = CreateHarmonicBgmClip("BGM_BattlefieldBeach", beachMelody, 0.32f);
        }

        private AudioClip CreateHarmonicBgmClip(string name, float[] freqs, float noteDuration)
        {
            int sampleRate = 44100;
            int samplesPerNote = Mathf.FloorToInt(sampleRate * noteDuration);
            int totalSamples = samplesPerNote * freqs.Length;
            float[] samples = new float[totalSamples];

            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                int offset = n * samplesPerNote;

                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesPerNote;
                    // Envelope lembut tanpa hentakan klik saat transisi nada
                    float env = Mathf.Sin(progress * Mathf.PI);

                    // Lapisan nada dasar + overtone harmonik hangat (1st & 2nd harmonic)
                    float tone = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.22f +
                                 Mathf.Sin(4 * Mathf.PI * freq * t) * 0.08f +
                                 Mathf.Sin(6 * Mathf.PI * freq * t) * 0.03f;

                    samples[offset + i] = tone * env;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateRhythmicBgmClip(string name, float[] freqs, float noteDuration)
        {
            int sampleRate = 44100;
            int samplesPerNote = Mathf.FloorToInt(sampleRate * noteDuration);
            int totalSamples = samplesPerNote * freqs.Length;
            float[] samples = new float[totalSamples];

            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                int offset = n * samplesPerNote;

                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesPerNote;
                    // Envelope perkusi cepat
                    float env = Mathf.Pow(1f - progress, 1.5f);

                    // Gelombang saw-triangle sintetis untuk nuansa ritmis heroic
                    float tone = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.26f +
                                 Mathf.Sin(2 * Mathf.PI * (freq * 1.5f) * t) * 0.09f;

                    samples[offset + i] = tone * env;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
