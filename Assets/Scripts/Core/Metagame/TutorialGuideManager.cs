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
        public string speakerNameEn;
        public string dialogueTextEn;
        public string targetUiElementName; // Nama elemen UI atau tag untuk spotlight highlight
        public bool requireSpecificAction; // Jika true, pemain harus klik tombol target (bukan sembarang klik)

        public TutorialStepData(TutorialStep s, string speakerId, string dialogueId, string speakerEn = "", string dialogueEn = "", string targetUi = "", bool reqAction = false)
        {
            step = s;
            speakerName = speakerId;
            dialogueText = dialogueId;
            speakerNameEn = !string.IsNullOrEmpty(speakerEn) ? speakerEn : speakerId;
            dialogueTextEn = !string.IsNullOrEmpty(dialogueEn) ? dialogueEn : dialogueId;
            targetUiElementName = targetUi;
            requireSpecificAction = reqAction;
        }

        public string GetLocalizedSpeaker()
        {
            bool isEn = FantasyDefense.Localization.CitadelLocalizationManager.ActiveLanguage == FantasyDefense.Localization.AppLanguage.English;
            return (isEn && !string.IsNullOrEmpty(speakerNameEn)) ? speakerNameEn : speakerName;
        }

        public string GetLocalizedDialogue()
        {
            bool isEn = FantasyDefense.Localization.CitadelLocalizationManager.ActiveLanguage == FantasyDefense.Localization.AppLanguage.English;
            return (isEn && !string.IsNullOrEmpty(dialogueTextEn)) ? dialogueTextEn : dialogueText;
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
        [SerializeField] private bool disableTutorial = false;
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
                        "BaldHawk (Mentor)",
                        "Welcome to the Citadel, Commander! Monsters from the Abyssal chasm are drawing near. We must prepare at once!",
                        "",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.SummonFirstHero,
                        "BaldHawk (Mentor)",
                        "Tekan tombol 'Summon Hero' untuk memanggil pahlawan pertama kita ke medan perang!",
                        "BaldHawk (Mentor)",
                        "Tap the 'Summon Hero' button to call our first champion to the battlefield!",
                        "SummonButton",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.SummonSecondHero,
                        "BaldHawk (Mentor)",
                        "Luar biasa! Sekarang panggil satu pahlawan lagi agar kita bisa menggabungkan kekuatan mereka.",
                        "BaldHawk (Mentor)",
                        "Splendid! Now summon one more hero so we can combine their forces.",
                        "SummonButton",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.MergeTutorial,
                        "BaldHawk (Mentor)",
                        "Tarik (Drag) pahlawan yang bertipe sama ke atas pahlawan lainnya untuk melakukan Merge ke Tier 2!",
                        "BaldHawk (Mentor)",
                        "Drag a hero onto another of the same type and tier to merge them into Tier 2!",
                        "GridHeroUnit",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.BarricadeDefense,
                        "BaldHawk (Mentor)",
                        "Hebat! Pahlawan Tier 2 memiliki serangan lebih dahsyat. Pertahankan barikade benteng dari gelombang monster!",
                        "BaldHawk (Mentor)",
                        "Magnificent! Tier 2 heroes possess devastating attacks. Defend the citadel barricade against incoming monster waves!",
                        "CastleBarricade",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.TriggerHeroSkill,
                        "BaldHawk (Mentor)",
                        "Perhatikan bilah energi skill pahlawan! Pahlawan Tier 2 & 3 otomatis memicu jurus pamungkas saat penuh.",
                        "BaldHawk (Mentor)",
                        "Watch the hero's energy gauge! Higher tier heroes unleash their ultimate abilities when it is full.",
                        "SkillIndicator",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.VictoryStage,
                        "BaldHawk (Mentor)",
                        "Pertahanan sempurna! Pasukan musuh dipukul mundur. Mari kita periksa markas benteng kita.",
                        "BaldHawk (Mentor)",
                        "Flawless defense! The enemy forces have been repelled. Let us inspect our citadel headquarters.",
                        "VictoryContinueBtn",
                        false
                    ),
                    new TutorialStepData(
                        TutorialStep.LobbyNavigation,
                        "BaldHawk (Mentor)",
                        "Ini adalah Citadel Lobby. Buka menu 'Heroes' untuk memperkuat atribut dan perlengkapan pahlawanmu.",
                        "BaldHawk (Mentor)",
                        "This is the Citadel Lobby. Open the 'Heroes' menu to empower your champions and inspect their gear.",
                        "NavHeroes",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.UpgradeHero,
                        "BaldHawk (Mentor)",
                        "Tingkatkan level Sir Cedric untuk meningkatkan Attack & Defense benteng kita!",
                        "BaldHawk (Mentor)",
                        "Upgrade Sir Cedric to bolster our citadel's Attack and Defense attributes!",
                        "UpgradeHeroButton",
                        true
                    ),
                    new TutorialStepData(
                        TutorialStep.Completed,
                        "BaldHawk (Mentor)",
                        "Pelatihan selesai! Sekarang benteng berada di bawah komandomu. Pertahankan Citadel dengan segenap jiwamu!",
                        "BaldHawk (Mentor)",
                        "Training complete! The citadel is now under your command. Defend the Citadel with all your might!",
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

        /// <summary>
        /// Memeriksa dan memicu langkah onboarding tutorial saat pertama kali membuka Citadel Lobby.
        /// </summary>
        public void CheckAndStartLobbyGuide()
        {
            if (disableTutorial || IsCompleted) return;

            if (currentStep == TutorialStep.WelcomeGreeting)
            {
                isTutorialActive = true;
                TriggerStep(TutorialStep.WelcomeGreeting);
            }
            else if (currentStep == TutorialStep.LobbyNavigation)
            {
                isTutorialActive = true;
                TriggerStep(TutorialStep.LobbyNavigation);
            }
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
                bool match = string.Equals(data.targetUiElementName, actionName, StringComparison.OrdinalIgnoreCase);
                if (!match)
                {
                    // Aliasing toleran untuk nama elemen antarmuka editor vs runtime
                    if ((data.targetUiElementName == "NavHeroes" || data.targetUiElementName == "LobbyHeroMenuBtn") &&
                        (actionName == "NavHeroes" || actionName == "LobbyHeroMenuBtn"))
                    {
                        match = true;
                    }
                    else if ((data.targetUiElementName == "UpgradeHeroButton" || data.targetUiElementName == "HeroUpgradeBtn") &&
                             (actionName == "UpgradeHeroButton" || actionName == "HeroUpgradeBtn"))
                    {
                        match = true;
                    }
                }

                if (match)
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
