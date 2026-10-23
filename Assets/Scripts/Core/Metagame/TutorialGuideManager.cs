using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.GridSystem;
using FantasyDefense.Summon;

namespace FantasyDefense.Metagame
{
    public enum TutorialStep
    {
        WelcomeGreeting = 0,
        SummonFirstHero = 1,
        SummonSecondHero = 2,
        MergeTutorial = 3,
        BarricadeDefense = 4,
        TriggerHeroSkill = 5,
        VictoryStage = 6,
        LobbyNavigation = 7,
        UpgradeHero = 8,
        Completed = 9
    }

    [Serializable]
    public class TutorialStepData
    {
        public TutorialStep step;
        public string speakerName;
        public string dialogueText;
        public string targetUiElementName; // Nama elemen UI atau tag untuk spotlight highlight
        public bool requireSpecificAction; // Jika true, pemain harus klik tombol target (bukan sembarang klik)

        public TutorialStepData(TutorialStep s, string speaker, string dialogue, string targetUi = "", bool reqAction = false)
        {
            step = s;
            speakerName = speaker;
            dialogueText = dialogue;
            targetUiElementName = targetUi;
            requireSpecificAction = reqAction;
        }
    }

    /// <summary>
    /// Master Manager untuk First-Time User Experience (FTUE) Interactive Tutorial di Rampartbound.
    /// Membimbing pemain baru melalui alur summon, merge drag-and-drop, pertahanan benteng, skill aktif, dan navigasi lobby.
    /// </summary>
    public class TutorialGuideManager : MonoBehaviour
    {
        public static TutorialGuideManager Instance { get; private set; }

        private const string PREF_TUTORIAL_COMPLETED = "Rampartbound_Tutorial_Completed";

        [Header("State")]
        [SerializeField] private bool disableTutorial = true;
        [SerializeField] private TutorialStep currentStep = TutorialStep.WelcomeGreeting;
        [SerializeField] private bool isTutorialActive = false;
        [SerializeField] private List<TutorialStepData> stepDefinitions = new List<TutorialStepData>();

        public TutorialStep CurrentStep => currentStep;
        public bool IsTutorialActive => isTutorialActive;
        public bool IsCompleted => PlayerPrefs.GetInt(PREF_TUTORIAL_COMPLETED, 0) == 1;

        public static event Action<TutorialStepData> OnTutorialStepStarted;
        public static event Action<TutorialStep> OnTutorialStepCompleted;
        public static event Action OnTutorialCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeStepDefinitions();
            CheckTutorialStatus();
        }

        private void InitializeStepDefinitions()
        {
            if (stepDefinitions == null || stepDefinitions.Count == 0)
            {
                stepDefinitions = new List<TutorialStepData>
                {
                    new TutorialStepData(
                        TutorialStep.WelcomeGreeting,
                        "BaldHawk (Mentor)",
                        "Selamat datang di Citadel, Panglima! Monster dari jurang Abyssal semakin dekat. Kita harus segera bersiap!",
                        "",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.SummonFirstHero,
                        "BaldHawk (Mentor)",
                        "Tekan tombol 'Summon Hero' untuk memanggil pahlawan pertama kita ke medan perang!",
                        "SummonButton",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.SummonSecondHero,
                        "BaldHawk (Mentor)",
                        "Luar biasa! Sekarang panggil satu pahlawan lagi agar kita bisa menggabungkan kekuatan mereka.",
                        "SummonButton",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.MergeTutorial,
                        "BaldHawk (Mentor)",
                        "Tarik (Drag) pahlawan yang bertipe sama ke atas pahlawan lainnya untuk melakukan Merge ke Tier 2!",
                        "GridHeroUnit",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.BarricadeDefense,
                        "BaldHawk (Mentor)",
                        "Hebat! Pahlawan Tier 2 memiliki serangan lebih dahsyat. Pertahankan barikade benteng dari gelombang monster!",
                        "CastleBarricade",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.TriggerHeroSkill,
                        "BaldHawk (Mentor)",
                        "Perhatikan bilah energi skill pahlawan! Pahlawan Tier 2 & 3 otomatis memicu jurus pamungkas saat penuh.",
                        "SkillIndicator",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.VictoryStage,
                        "BaldHawk (Mentor)",
                        "Pertahanan sempurna! Pasukan musuh dipukul mundur. Mari kita periksa markas benteng kita.",
                        "VictoryContinueBtn",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.LobbyNavigation,
                        "BaldHawk (Mentor)",
                        "Ini adalah Citadel Lobby. Buka menu 'Hero Roster' untuk memperkuat atribut dan perlengkapan pahlawanmu.",
                        "LobbyHeroMenuBtn",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.UpgradeHero,
                        "BaldHawk (Mentor)",
                        "Tingkatkan level Sir Cedric untuk meningkatkan Attack & Defense benteng kita!",
                        "HeroUpgradeBtn",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.Completed,
                        "BaldHawk (Mentor)",
                        "Pelatihan selesai! Sekarang benteng berada di bawah komandomu. Pertahankan Citadel dengan segenap jiwamu!",
                        "",
                        false
                    )
                };
            }
        }

        private void CheckTutorialStatus()
        {
            if (disableTutorial)
            {
                isTutorialActive = false;
                currentStep = TutorialStep.Completed;
                PlayerPrefs.SetInt(PREF_TUTORIAL_COMPLETED, 1);
                return;
            }

            if (!IsCompleted)
            {
                isTutorialActive = true;
                currentStep = TutorialStep.WelcomeGreeting;
            }
            else
            {
                isTutorialActive = false;
                currentStep = TutorialStep.Completed;
            }
        }

        private void OnEnable()
        {
            HeroSummoner.OnHeroSummoned += HandleHeroSummoned;
            UnitController.OnUnitMerged += HandleUnitMerged;
            CitadelLobbyManager.OnHeroUpgraded += HandleHeroUpgraded;
        }

        private void OnDisable()
        {
            HeroSummoner.OnHeroSummoned -= HandleHeroSummoned;
            UnitController.OnUnitMerged -= HandleUnitMerged;
            CitadelLobbyManager.OnHeroUpgraded -= HandleHeroUpgraded;
        }

        private void HandleHeroSummoned(UnitController unit, GridSystem.TileCell cell)
        {
            if (!isTutorialActive) return;
            if (currentStep == TutorialStep.SummonFirstHero || currentStep == TutorialStep.SummonSecondHero)
            {
                AdvanceStep();
            }
        }

        private void HandleUnitMerged(UnitController u1, UnitController u2, UnitTier tier)
        {
            if (!isTutorialActive) return;
            if (currentStep == TutorialStep.MergeTutorial)
            {
                AdvanceStep();
            }
        }

        private void HandleHeroUpgraded(HeroLobbyProfile profile)
        {
            if (!isTutorialActive) return;
            if (currentStep == TutorialStep.UpgradeHero)
            {
                AdvanceStep();
            }
        }

        private void Start()
        {
            // PENTING: Jangan memicu tutorial otomatis pada Start!
            // Hal ini mencegah TutorialOverlayPanel memblokir raycast tombol Title Screen, Lobby, dan Stage Map saat peluncuran game.
        }

        /// <summary>
        /// Memulai alur tutorial pertarungan secara aman hanya saat pemain memasuki mode battle arena pertama kali.
        /// </summary>
        public void StartBattleTutorialIfNeeded()
        {
            if (IsCompleted) return;

            // Jika dalam mode PuzzlePreparation, selesaikan tutorial agar tidak memblokir raycast antarmuka dan grid
            if (GameManager.Instance != null && GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation)
            {
                CompleteTutorial();
                return;
            }

            isTutorialActive = true;
            currentStep = TutorialStep.WelcomeGreeting;
            TriggerStep(currentStep);
        }

        public TutorialStepData GetStepData(TutorialStep step)
        {
            return stepDefinitions.Find(s => s.step == step);
        }

        public void TriggerStep(TutorialStep step)
        {
            currentStep = step;
            var data = GetStepData(step);
            if (data != null)
            {
                // Live-Ops FTUE Funnel Telemetry Tracking
                if (FantasyDefense.Analytics.CitadelTelemetryManager.Instance != null)
                {
                    FantasyDefense.Analytics.CitadelTelemetryManager.Instance.TrackTutorialStep((int)step, step.ToString());
                }

                Debug.Log($"[TutorialGuideManager] Memulai Tutorial Step: {step}");
                OnTutorialStepStarted?.Invoke(data);
            }
        }

        /// <summary>
        /// Melangkah ke langkah tutorial berikutnya.
        /// </summary>
        public void AdvanceStep()
        {
            if (!isTutorialActive) return;

            OnTutorialStepCompleted?.Invoke(currentStep);

            int next = (int)currentStep + 1;
            if (next >= (int)TutorialStep.Completed)
            {
                CompleteTutorial();
            }
            else
            {
                TriggerStep((TutorialStep)next);
            }
        }

        /// <summary>
        /// Dipanggil ketika aksi spesifik pemain berhasil dilakukan (misal klik summon, merge hero, klik upgrade).
        /// </summary>
        public void NotifyActionTriggered(string actionName)
        {
            if (!isTutorialActive) return;

            var data = GetStepData(currentStep);
            if (data != null && data.requireSpecificAction)
            {
                if (string.Equals(data.targetUiElementName, actionName, StringComparison.OrdinalIgnoreCase))
                {
                    AdvanceStep();
                }
            }
        }

        public void CompleteTutorial()
        {
            currentStep = TutorialStep.Completed;
            isTutorialActive = false;
            PlayerPrefs.SetInt(PREF_TUTORIAL_COMPLETED, 1);
            PlayerPrefs.Save();

            AudioManager.Instance?.PlaySfx(AudioSfxType.VictoryTrumpet);
            OnTutorialCompleted?.Invoke();
            Debug.Log("[TutorialGuideManager] FTUE Interactive Tutorial Selesai 100%!");
        }

        public void ResetTutorial()
        {
            PlayerPrefs.SetInt(PREF_TUTORIAL_COMPLETED, 0);
            PlayerPrefs.Save();
            isTutorialActive = true;
            TriggerStep(TutorialStep.WelcomeGreeting);
            Debug.Log("[TutorialGuideManager] Tutorial di-reset untuk sesi pengujian.");
        }
    }
}
