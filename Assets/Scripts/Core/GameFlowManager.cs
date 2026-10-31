using System;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Data;
using FantasyDefense.Metagame;
using FantasyDefense.UI;

namespace FantasyDefense.Core
{
    public enum GameFlowState
    {
        TitleScreen,
        Lobby,
        InBattle
    }

    public enum GamePanelType
    {
        None,
        StageMap,
        HeroUpgrade,
        SummonHall,
        Store,
        RelicForge,
        CitadelTalents,
        Quests,
        Guild,
        BattlePass,
        EventArena,
        Artifacts,
        AudioSettings,
        EndlessTrial
    }

    /// <summary>
    /// Master Orchestrator untuk Seluruh Alur Game (Master Game Flow & Scene Director).
    /// Menghubungkan secara mulus Title Screen -> Citadel Lobby -> Metagame Sub-Panels (Store, Gacha, Hero, dsb) -> Battle Arena -> Result -> Return to Lobby.
    /// Memastikan tidak ada layar kosong, semua panel terintegrasi, dan perpindahan status berjalan aman dan reaktif.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("Current Flow State")]
        [SerializeField] private GameFlowState currentState = GameFlowState.TitleScreen;
        [SerializeField] private GamePanelType currentActivePanel = GamePanelType.None;

        [Header("Primary Navigation Panels")]
        [SerializeField] private UITitleScreenView titleScreenView;
        [SerializeField] private UICitadelLobbyView lobbyView;
        [SerializeField] private GameObject battleHudPanel;

        [Header("Arena 2D World Objects Container")]
        [Tooltip("Container objek arena (Grid, Castle Barricade, Waypoints) yang hanya aktif saat bertarung")]
        [SerializeField] private GameObject arenaWorldContainer;

        [Header("Metagame Sub-Panels")]
        [SerializeField] private UIStageMapView stageMapView;
        [SerializeField] private UIHeroUpgradeView heroUpgradeView;
        [SerializeField] private UISummonHallView summonHallView;
        [SerializeField] private UIStoreView storeView;
        [SerializeField] private UIRelicForgeView relicForgeView;
        [SerializeField] private UICitadelTalentView talentView;
        [SerializeField] private UIQuestView questView;
        [SerializeField] private UIGuildView guildView;
        [SerializeField] private UIBattlePassView battlePassView;
        [SerializeField] private UIEventArenaView eventArenaView;
        [SerializeField] private UIArtifactView artifactView;
        [SerializeField] private UIAudioSettingsView audioSettingsView;
        [SerializeField] private UIEndlessTrialView endlessTrialView;

        public GameFlowState CurrentState => currentState;
        public GamePanelType CurrentActivePanel => currentActivePanel;

        public static event Action<GameFlowState> OnFlowStateChanged;
        public static event Action<GamePanelType> OnPanelChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoDetectViewsIfNull();
        }

        private void Start()
        {
            // Pastikan saat aplikasi dibuka, masuk ke Title Screen terlebih dahulu
            ShowTitle();
        }

        public void AutoDetectViewsIfNull()
        {
            if (titleScreenView == null) titleScreenView = UnityEngine.Object.FindAnyObjectByType<UITitleScreenView>(FindObjectsInactive.Include);
            if (lobbyView == null) lobbyView = UnityEngine.Object.FindAnyObjectByType<UICitadelLobbyView>(FindObjectsInactive.Include);
            if (stageMapView == null) stageMapView = UnityEngine.Object.FindAnyObjectByType<UIStageMapView>(FindObjectsInactive.Include);
            if (heroUpgradeView == null) heroUpgradeView = UnityEngine.Object.FindAnyObjectByType<UIHeroUpgradeView>(FindObjectsInactive.Include);
            if (summonHallView == null) summonHallView = UnityEngine.Object.FindAnyObjectByType<UISummonHallView>(FindObjectsInactive.Include);
            if (storeView == null) storeView = UnityEngine.Object.FindAnyObjectByType<UIStoreView>(FindObjectsInactive.Include);
            if (relicForgeView == null) relicForgeView = UnityEngine.Object.FindAnyObjectByType<UIRelicForgeView>(FindObjectsInactive.Include);
            if (talentView == null) talentView = UnityEngine.Object.FindAnyObjectByType<UICitadelTalentView>(FindObjectsInactive.Include);
            if (questView == null) questView = UnityEngine.Object.FindAnyObjectByType<UIQuestView>(FindObjectsInactive.Include);
            if (guildView == null) guildView = UnityEngine.Object.FindAnyObjectByType<UIGuildView>(FindObjectsInactive.Include);
            if (battlePassView == null) battlePassView = UnityEngine.Object.FindAnyObjectByType<UIBattlePassView>(FindObjectsInactive.Include);
            if (eventArenaView == null) eventArenaView = UnityEngine.Object.FindAnyObjectByType<UIEventArenaView>(FindObjectsInactive.Include);
            if (artifactView == null) artifactView = UnityEngine.Object.FindAnyObjectByType<UIArtifactView>(FindObjectsInactive.Include);
            if (audioSettingsView == null) audioSettingsView = UnityEngine.Object.FindAnyObjectByType<UIAudioSettingsView>(FindObjectsInactive.Include);
            if (endlessTrialView == null) endlessTrialView = UnityEngine.Object.FindAnyObjectByType<UIEndlessTrialView>(FindObjectsInactive.Include);

            if (battleHudPanel == null)
            {
                var battleView = UnityEngine.Object.FindAnyObjectByType<UIBattleView>(FindObjectsInactive.Include);
                if (battleView != null) battleHudPanel = battleView.gameObject;
            }
        }

        /// <summary>
        /// Menampilkan Title Screen / Layar Pembuka Game.
        /// </summary>
        public void ShowTitle()
        {
            currentState = GameFlowState.TitleScreen;
            CloseAllSubPanels();

            if (titleScreenView != null) titleScreenView.OpenView();
            if (lobbyView != null) lobbyView.CloseView();
            if (battleHudPanel != null) battleHudPanel.SetActive(false);

            SetArenaWorldVisible(false);

            OnFlowStateChanged?.Invoke(currentState);
            Debug.Log("[GameFlowManager] Berada di Title Screen.");
        }

        /// <summary>
        /// Menampilkan Pusat Benteng (Citadel Lobby) dan menyinkronkan seluruh status akun.
        /// </summary>
        public void ShowLobby()
        {
            currentState = GameFlowState.Lobby;
            CloseAllSubPanels();

            if (titleScreenView != null) titleScreenView.CloseView();
            if (lobbyView != null) lobbyView.OpenView();
            if (battleHudPanel != null) battleHudPanel.SetActive(false);

            SetArenaWorldVisible(false);

            // Perbarui BGM Lobby
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(BgmTrack.LobbyTheme);
            }

            OnFlowStateChanged?.Invoke(currentState);
            Debug.Log("[GameFlowManager] Berada di Citadel Lobby.");
        }

        /// <summary>
        /// Membuka salah satu sub-panel metagame (misal: Toko, Upgrade Hero, Gacha, dsb).
        /// </summary>
        public void OpenPanel(GamePanelType panelType)
        {
            if (currentState == GameFlowState.InBattle)
            {
                Debug.LogWarning("[GameFlowManager] Tidak dapat membuka sub-panel saat pertempuran sedang aktif.");
                return;
            }

            CloseAllSubPanels();
            currentActivePanel = panelType;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            switch (panelType)
            {
                case GamePanelType.StageMap:
                    if (stageMapView != null) stageMapView.OpenView();
                    break;
                case GamePanelType.HeroUpgrade:
                    if (heroUpgradeView != null) heroUpgradeView.OpenView();
                    break;
                case GamePanelType.SummonHall:
                    if (summonHallView != null) summonHallView.OpenView();
                    break;
                case GamePanelType.Store:
                    if (storeView != null) storeView.OpenView();
                    break;
                case GamePanelType.RelicForge:
                    if (relicForgeView != null) relicForgeView.OpenView();
                    break;
                case GamePanelType.CitadelTalents:
                    if (talentView != null) talentView.OpenPanel();
                    break;
                case GamePanelType.Quests:
                    if (questView != null) questView.OpenQuestView();
                    break;
                case GamePanelType.Guild:
                    if (guildView != null) guildView.OpenView();
                    break;
                case GamePanelType.BattlePass:
                    if (battlePassView != null) battlePassView.OpenView();
                    break;
                case GamePanelType.EventArena:
                    if (eventArenaView != null) eventArenaView.OpenView();
                    break;
                case GamePanelType.Artifacts:
                    if (artifactView != null) artifactView.OpenView();
                    break;
                case GamePanelType.AudioSettings:
                    if (audioSettingsView != null) audioSettingsView.OpenSettings();
                    break;
                case GamePanelType.EndlessTrial:
                    if (endlessTrialView != null) endlessTrialView.OpenView();
                    break;
                default:
                    currentActivePanel = GamePanelType.None;
                    break;
            }

            OnPanelChanged?.Invoke(currentActivePanel);
            Debug.Log($"[GameFlowManager] Membuka Sub-Panel: {panelType}");
        }

        private bool isClosingPanels = false;

        /// <summary>
        /// Menutup sub-panel yang sedang aktif dan kembali fokus ke Lobby utama.
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (isClosingPanels) return;
            isClosingPanels = true;

            try
            {
                currentActivePanel = GamePanelType.None;
                CloseAllSubPanels();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.AltarWhooshClose, 1.15f);
                }

                if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
                {
                    FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
                }

                if (lobbyView != null)
                {
                    lobbyView.RefreshDisplays();
                }

                OnPanelChanged?.Invoke(currentActivePanel);
                Debug.Log("[GameFlowManager] Sub-Panel ditutup, kembali ke tampilan Citadel Lobby.");
            }
            finally
            {
                isClosingPanels = false;
            }
        }

        private void CloseAllSubPanels()
        {
            if (stageMapView != null) stageMapView.CloseView();
            if (heroUpgradeView != null) heroUpgradeView.CloseView();
            if (summonHallView != null) summonHallView.CloseView();
            if (storeView != null) storeView.CloseView();
            if (relicForgeView != null) relicForgeView.CloseView();
            if (talentView != null) talentView.ClosePanel();
            if (questView != null) questView.CloseQuestView();
            if (guildView != null) guildView.CloseView();
            if (battlePassView != null) battlePassView.CloseView();
            if (eventArenaView != null) eventArenaView.CloseView();
            if (artifactView != null) artifactView.CloseView();
            if (audioSettingsView != null) audioSettingsView.CloseSettings();
            if (endlessTrialView != null) endlessTrialView.CloseView();
            currentActivePanel = GamePanelType.None;
        }

        /// <summary>
        /// Memulai pertempuran arena untuk stage tertentu atau stage default.
        /// </summary>
        public void StartBattle(StageDataSO stage = null)
        {
            currentState = GameFlowState.InBattle;
            CloseAllSubPanels();

            if (titleScreenView != null) titleScreenView.CloseView();
            if (lobbyView != null) lobbyView.CloseView();

            SetArenaWorldVisible(true);

            if (battleHudPanel != null)
            {
                battleHudPanel.SetActive(true);
            }

            var battleView = battleHudPanel != null ? battleHudPanel.GetComponent<UIBattleView>() : null;
            if (battleView != null)
            {
                battleView.OpenView();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(BgmTrack.BattleTheme);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartBattleWithStage(stage);
            }

            // Pemicu tutorial pertarungan onboarding FTUE hanya saat battle pertama dimulai
            if (TutorialGuideManager.Instance != null)
            {
                TutorialGuideManager.Instance.StartBattleTutorialIfNeeded();
            }

            OnFlowStateChanged?.Invoke(currentState);
            Debug.Log($"[GameFlowManager] Pertempuran Arena Dimulai! Stage: {(stage != null ? stage.stageCode : "Default")}");
        }

        /// <summary>
        /// Mengakhiri pertempuran dan kembali ke Citadel Lobby dengan pembersihan grid dan monster.
        /// </summary>
        public void ReturnToLobbyFromBattle()
        {
            Debug.Log("[GameFlowManager] Mengakhiri pertempuran, membersihkan arena...");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToLobby();
            }

            ShowLobby();
        }

        private void SetArenaWorldVisible(bool visible)
        {
            if (arenaWorldContainer == null)
            {
                arenaWorldContainer = GameObject.Find("ArenaWorldContainer");
                if (arenaWorldContainer == null)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    if (scene.isLoaded)
                    {
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            if (root.name == "ArenaWorldContainer")
                            {
                                arenaWorldContainer = root;
                                break;
                            }
                            var child = root.transform.Find("ArenaWorldContainer");
                            if (child != null)
                            {
                                arenaWorldContainer = child.gameObject;
                                break;
                            }
                        }
                    }
                }
            }

            if (arenaWorldContainer != null)
            {
                arenaWorldContainer.SetActive(visible);
            }

            // Atur visibilitas Barikade jika berada di luar container
            if (CastleBarricade.Instance != null && CastleBarricade.Instance.gameObject != null)
            {
                CastleBarricade.Instance.gameObject.SetActive(visible);
            }

            // Atur visibilitas GridManager
            if (GridSystem.GridManager.Instance != null && GridSystem.GridManager.Instance.gameObject != null)
            {
                GridSystem.GridManager.Instance.gameObject.SetActive(visible);
            }
        }
    }
}
