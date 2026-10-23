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
        WaveIncoming
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
        BattlefieldTheme = BattleTheme
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
        [SerializeField] private bool isBgmMuted = false;
        [SerializeField] private bool isSfxMuted = false;

        [Header("Current BGM State")]
        [SerializeField] private BgmTrack currentBgmTrack = BgmTrack.None;

        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
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
                bgmVolume = Mathf.Clamp01(data.bgmVolume);
                sfxVolume = Mathf.Clamp01(data.sfxVolume);
            }
            UpdateVolumes();
        }

        private int sfxPlayCounter = 0;

        public void PlaySound(SoundType type, float pitchOverride = 1.0f)
        {
            if (isSfxMuted || sfxSources.Count == 0) return;

            AudioClip clip = GetAudioClip(type);
            if (clip == null) return;

            AudioSource source = sfxSources[currentSourceIndex];
            currentSourceIndex = (currentSourceIndex + 1) % sfxSources.Count;
            sfxPlayCounter++;

            source.volume = sfxVolume * masterVolume;

            // Variasi nada alami & anti-monotoni saat pertempuran berjalan di kecepatan 2x atau 3x
            float speedFactor = Time.timeScale;
            float pitchJitter;
            if (speedFactor >= 2.0f)
            {
                // Variasi pitch diperluas dan diiringi rhythmic micro-offset 3-ketukan agar tidak monoton
                float rhythmicStep = (sfxPlayCounter % 3 - 1) * 0.04f;
                pitchJitter = Random.Range(0.92f, 1.10f) + rhythmicStep;
            }
            else
            {
                pitchJitter = Random.Range(0.96f, 1.04f);
            }

            source.pitch = Mathf.Clamp(pitchOverride * pitchJitter, 0.75f, 1.45f);
            source.PlayOneShot(clip);
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

        public void PlayBGM(BgmTrack track)
        {
            if (bgmSource == null) return;
            if (currentBgmTrack == track && bgmSource.isPlaying) return;

            currentBgmTrack = track;
            if (track == BgmTrack.None)
            {
                bgmSource.Stop();
                return;
            }

            // 1. Cek Inspector override
            AudioClip realBgm = null;
            if (track == BgmTrack.LobbyTheme && customLobbyBgm != null) realBgm = customLobbyBgm;
            else if (track == BgmTrack.BattleTheme && customBattleBgm != null) realBgm = customBattleBgm;

            // 2. Cek Resources drop-in (Assets/Resources/Audio/Music/)
            if (realBgm == null)
            {
                string resPath = track == BgmTrack.LobbyTheme ? "Audio/Music/BGM_Citadel_Lobby" : "Audio/Music/BGM_Battle_Theme";
                realBgm = Resources.Load<AudioClip>(resPath);
            }

            if (realBgm != null)
            {
                bgmSource.clip = realBgm;
                UpdateVolumes();
                bgmSource.Play();
                return;
            }

            // 3. Fallback mulus ke procedural synthesized melody
            if (proceduralBgmCache.TryGetValue(track, out AudioClip clip))
            {
                bgmSource.clip = clip;
                UpdateVolumes();
                bgmSource.Play();
            }
        }

        public void PlayBgm(BgmTrack track) => PlayBGM(track);

        public void StopBGM()
        {
            currentBgmTrack = BgmTrack.None;
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void SetMasterVolume(float val)
        {
            masterVolume = Mathf.Clamp01(val);
            UpdateVolumes();
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

        public void ToggleMuteBgm()
        {
            isBgmMuted = !isBgmMuted;
            UpdateVolumes();
        }

        public void ToggleMuteSfx()
        {
            isSfxMuted = !isSfxMuted;
        }

        private void UpdateVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = isBgmMuted ? 0f : (bgmVolume * masterVolume);
            }
        }

        private void SyncWithSaveData()
        {
            if (Save.SaveManager.Instance != null && Save.SaveManager.Instance.CurrentData != null)
            {
                Save.SaveManager.Instance.CurrentData.bgmVolume = bgmVolume;
                Save.SaveManager.Instance.CurrentData.sfxVolume = sfxVolume;
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
            AudioClip resClip = Resources.Load<AudioClip>($"Audio/SFX/{key}");
            if (resClip != null)
            {
                externalClipCache[key] = resClip;
                return resClip;
            }

            // 3. Fallback mulus ke procedural synthesis
            if (proceduralClipCache.TryGetValue(type, out AudioClip clip))
            {
                return clip;
            }
            return null;
        }

        #region Event Handlers

        private void HandleUnitSkillTriggered(UnitController unit, string skillName)
        {
            if (unit == null) return;

            switch (unit.UnitType)
            {
                case UnitType.Cedric:
                    if (unit.CurrentTier == UnitTier.Tier3) PlaySound(SoundType.BladeTempest);
                    else if (unit.CurrentTier == UnitTier.Tier2) PlaySound(SoundType.DualSlash);
                    else PlaySound(SoundType.Slash);
                    break;

                case UnitType.RoseTanker:
                    if (unit.CurrentTier == UnitTier.Tier3) PlaySound(SoundType.Earthshaker);
                    else PlaySound(SoundType.ShieldSlam);
                    break;

                case UnitType.RabbitMage:
                    if (unit.CurrentTier == UnitTier.Tier3) PlaySound(SoundType.MeteorImpact);
                    else if (unit.CurrentTier == UnitTier.Tier2) PlaySound(SoundType.MysticBurst);
                    else PlaySound(SoundType.MagicSpark);
                    break;

                case UnitType.WindRanger:
                    if (unit.CurrentTier == UnitTier.Tier3) PlaySound(SoundType.TornadoImpact);
                    else if (unit.CurrentTier == UnitTier.Tier2) PlaySound(SoundType.PiercingGale);
                    else PlaySound(SoundType.ArrowShot);
                    break;
            }
        }

        private void HandleUnitMerged(UnitController source, UnitController target, UnitTier newTier)
        {
            float pitch = newTier == UnitTier.Tier3 ? 1.45f : 1.20f;
            PlaySound(SoundType.MergeSuccess, pitch);

            // Berikan fanfare perayaan jika berhasil menciptakan Hero Tier 3 Ultimate
            if (newTier == UnitTier.Tier3)
            {
                PlaySound(SoundType.GachaLegendary, 1.1f);
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
            proceduralClipCache[SoundType.SummonHero] = CreateToneClip("SummonHero", 0.18f, 440f, 660f);
            proceduralClipCache[SoundType.CastleHit] = CreateToneClip("CastleHit", 0.25f, 120f, 50f);

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
        }

        private AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = 1f - progress; // Linear decay

                samples[i] = Mathf.Sin(2 * Mathf.PI * currentFreq * t) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(string name, float[] freqs, float noteDuration)
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
                    float envelope = 1f - progress;

                    samples[offset + i] = Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * 0.35f;
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
