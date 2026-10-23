using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Localization;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI View untuk Pemilihan Berkah / Skill Roguelike di Awal Battle & Tiap Wave Selesai.
    /// Menampilkan 3 kartu berkah taktis secara dinamis dengan visual juice, efek suara, dan deskripsi multi-bahasa.
    /// </summary>
    public class UIBattleBlessingSelectionView : MonoBehaviour
    {
        public static UIBattleBlessingSelectionView Instance { get; private set; }

        [Header("Root Panel")]
        [SerializeField] private GameObject selectionPanel;

        [Header("Header Texts")]
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text subtitleText;

        [Header("Blessing Cards")]
        [SerializeField] private GameObject card1Obj;
        [SerializeField] private Button card1Button;
        [SerializeField] private Text card1TitleText;
        [SerializeField] private Text card1RarityText;
        [SerializeField] private Text card1DescText;
        [SerializeField] private Image card1FrameImage;

        [SerializeField] private GameObject card2Obj;
        [SerializeField] private Button card2Button;
        [SerializeField] private Text card2TitleText;
        [SerializeField] private Text card2RarityText;
        [SerializeField] private Text card2DescText;
        [SerializeField] private Image card2FrameImage;

        [SerializeField] private GameObject card3Obj;
        [SerializeField] private Button card3Button;
        [SerializeField] private Text card3TitleText;
        [SerializeField] private Text card3RarityText;
        [SerializeField] private Text card3DescText;
        [SerializeField] private Image card3FrameImage;

        private List<BattleBlessingData> currentChoices = new List<BattleBlessingData>();
        private Action onSelectionCompleted;
        private bool isSelecting = false;

        public bool IsOpen => selectionPanel != null && selectionPanel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
        }

        private void InitializeButtons()
        {
            if (card1Button != null) card1Button.onClick.AddListener(() => OnCardClicked(0));
            if (card2Button != null) card2Button.onClick.AddListener(() => OnCardClicked(1));
            if (card3Button != null) card3Button.onClick.AddListener(() => OnCardClicked(2));
        }

        /// <summary>
        /// Membuka popup pemilihan berkah taktis untuk gelombang yang ditentukan.
        /// </summary>
        public void OpenSelection(int waveNumber, Action onSelected = null)
        {
            onSelectionCompleted = onSelected;
            isSelecting = false;

            if (BattleBlessingManager.Instance == null)
            {
                onSelected?.Invoke();
                return;
            }

            currentChoices = BattleBlessingManager.Instance.GetRandomBlessingChoices(3);
            if (currentChoices == null || currentChoices.Count == 0)
            {
                onSelected?.Invoke();
                return;
            }

            // Update Header Texts
            if (headerTitleText != null)
            {
                string waveLabel = waveNumber <= 1
                    ? CitadelLocalizationManager.Text("⚔️ PILIH BERKAH AWAL CITADEL ⚔️", "⚔️ CHOOSE INITIAL CITADEL BLESSING ⚔️")
                    : CitadelLocalizationManager.Text($"⚔️ BERKAH GELOMBANG {waveNumber} ⚔️", $"⚔️ WAVE {waveNumber} CITADEL BLESSING ⚔️");
                headerTitleText.text = waveLabel;
            }

            if (subtitleText != null)
            {
                subtitleText.text = CitadelLocalizationManager.Text(
                    "Pilih 1 Berkah Taktis untuk memperkuat pertahanan dan Kuartet Pahlawan!",
                    "Choose 1 Tactical Blessing to empower Citadel defenses and the Hero Quartet!"
                );
            }

            // Bind Cards
            BindCard(0, card1Obj, card1TitleText, card1RarityText, card1DescText, card1FrameImage);
            BindCard(1, card2Obj, card2TitleText, card2RarityText, card2DescText, card2FrameImage);
            BindCard(2, card3Obj, card3TitleText, card3RarityText, card3DescText, card3FrameImage);

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            // Putar audio fanfare / modal pop
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.WaveIncoming);
            }
        }

        private void BindCard(int index, GameObject cardObj, Text titleTxt, Text rarityTxt, Text descTxt, Image frameImg)
        {
            if (cardObj == null) return;

            if (index >= currentChoices.Count)
            {
                cardObj.SetActive(false);
                return;
            }

            cardObj.SetActive(true);
            var blessing = currentChoices[index];

            if (titleTxt != null)
            {
                titleTxt.text = blessing.GetLocalizedTitle();
            }

            if (rarityTxt != null)
            {
                rarityTxt.text = blessing.GetRarityLabel();
            }

            if (descTxt != null)
            {
                descTxt.text = blessing.GetLocalizedDesc();
            }

            if (frameImg != null)
            {
                frameImg.color = blessing.accentColor;
            }
        }

        private void OnCardClicked(int cardIndex)
        {
            if (isSelecting) return;
            if (cardIndex < 0 || cardIndex >= currentChoices.Count) return;

            isSelecting = true;
            var chosen = currentChoices[cardIndex];

            // Trigger card animation
            GameObject clickedObj = cardIndex == 0 ? card1Obj : (cardIndex == 1 ? card2Obj : card3Obj);
            if (clickedObj != null)
            {
                StartCoroutine(PunchSelectCardRoutine(clickedObj, chosen));
            }
            else
            {
                FinishSelection(chosen);
            }
        }

        private IEnumerator PunchSelectCardRoutine(GameObject cardObj, BattleBlessingData chosen)
        {
            Vector3 originalScale = cardObj.transform.localScale;
            float elapsed = 0f;
            float punchDuration = 0.22f;

            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / punchDuration) * Mathf.PI);
                cardObj.transform.localScale = originalScale * (1f + 0.15f * t);
                yield return null;
            }

            cardObj.transform.localScale = originalScale;
            FinishSelection(chosen);
        }

        private void FinishSelection(BattleBlessingData chosen)
        {
            if (BattleBlessingManager.Instance != null)
            {
                BattleBlessingManager.Instance.ApplyBlessing(chosen);
            }

            CloseSelection();
            onSelectionCompleted?.Invoke();
            onSelectionCompleted = null;
        }

        public void CloseSelection()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
            isSelecting = false;
        }
    }
}
