using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Localization;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI View untuk Hub Pratinjau Fitur Citadel (Feature Preview & Showcase Hub).
    /// Memungkinkan pemain melihat dan menjelajahi seluruh sistem metagame & mode tempur yang telah dirancang:
    /// Pohon Talenta, Bengkel Tempa, Endless Abyssal Trial, Guild, Season Pass, Arena Event, dan Artefak Kuno.
    /// Setiap fitur dapat dibuka langsung untuk inspeksi UI dan navigasi preview.
    /// </summary>
    public class UIFeaturePreviewHubView : MonoBehaviour
    {
        public static UIFeaturePreviewHubView Instance { get; private set; }

        [Header("Root Panel")]
        [SerializeField] private GameObject previewHubPanel;

        [Header("Header Elements")]
        [SerializeField] private Text hubTitleText;
        [SerializeField] private Text hubSubtitleText;
        [SerializeField] private Button closeHubButton;

        [Header("Feature Preview Cards")]
        [SerializeField] private Button previewTalentsButton;
        [SerializeField] private Button previewForgeButton;
        [SerializeField] private Button previewEndlessButton;
        [SerializeField] private Button previewGuildButton;
        [SerializeField] private Button previewPassButton;
        [SerializeField] private Button previewEventButton;
        [SerializeField] private Button previewArtifactsButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
            if (previewHubPanel != null)
            {
                previewHubPanel.SetActive(false);
            }
        }

        private void InitializeButtons()
        {
            if (closeHubButton != null)
                closeHubButton.onClick.AddListener(CloseView);

            if (previewTalentsButton != null)
                previewTalentsButton.onClick.AddListener(() => OpenFeature(GamePanelType.CitadelTalents));

            if (previewForgeButton != null)
                previewForgeButton.onClick.AddListener(() => OpenFeature(GamePanelType.RelicForge));

            if (previewEndlessButton != null)
                previewEndlessButton.onClick.AddListener(() => OpenFeature(GamePanelType.EndlessTrial));

            if (previewGuildButton != null)
                previewGuildButton.onClick.AddListener(() => OpenFeature(GamePanelType.Guild));

            if (previewPassButton != null)
                previewPassButton.onClick.AddListener(() => OpenFeature(GamePanelType.BattlePass));

            if (previewEventButton != null)
                previewEventButton.onClick.AddListener(() => OpenFeature(GamePanelType.EventArena));

            if (previewArtifactsButton != null)
                previewArtifactsButton.onClick.AddListener(() => OpenFeature(GamePanelType.Artifacts));
        }

        public void OpenView()
        {
            if (previewHubPanel != null)
            {
                previewHubPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            RefreshLocalization();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void CloseView()
        {
            if (previewHubPanel != null)
            {
                previewHubPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshLocalization()
        {
            if (hubTitleText != null)
            {
                hubTitleText.text = CitadelLocalizationManager.Text(
                    "🏛️ PRATINJAU FITUR CITADEL",
                    "🏛️ CITADEL FEATURE PREVIEW"
                );
            }

            if (hubSubtitleText != null)
            {
                hubSubtitleText.text = CitadelLocalizationManager.Text(
                    "Jelajahi seluruh sistem metagame & mode pertahanan yang telah disiapkan untuk Citadel!",
                    "Explore all metagame systems & battle modes prepared for the Citadel!"
                );
            }
        }

        private void OpenFeature(GamePanelType panelType)
        {
            CloseView();

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenPanel(panelType);
            }
        }
    }
}
