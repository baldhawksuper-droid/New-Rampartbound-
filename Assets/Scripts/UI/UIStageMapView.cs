using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk Layar Pemilihan Stage & Peta Dunia (Stage Progression Map).
    /// Menampilkan daftar 10 stage Chapter 1 (Stage 1-1 sampai Stage 1-10 Boss), perolehan bintang 1-3,
    /// perbandingan Team CP vs Rekomendasi CP, konsumsi stamina, serta detail hadiah First-Clear.
    /// </summary>
    public class UIStageMapView : MonoBehaviour
    {
        public static UIStageMapView Instance { get; private set; }

        [Header("Root & Navigation")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button closeButton;

        [Header("Chapter Overview")]
        [SerializeField] private Text chapterTitleText;
        [SerializeField] private Text totalStarsText;
        [SerializeField] private Slider chapterStarSlider;

        [Header("Selected Stage Detail Panel")]
        [SerializeField] private GameObject stageDetailPanel;
        [SerializeField] private GameObject bossWarningBanner;
        [SerializeField] private Text bossWarningText;
        [SerializeField] private Text stageCodeText;
        [SerializeField] private Text stageTitleText;
        [SerializeField] private Text stageDescriptionText;
        [SerializeField] private Text battleModeText;
        [SerializeField] private Text recommendedCpText;
        [SerializeField] private Text difficultyStatusText;
        [SerializeField] private Text energyCostText;
        [SerializeField] private Text firstClearRewardsText;
        [SerializeField] private Text repeatRewardsText;
        [SerializeField] private Button startBattleButton;

        [Header("Milestone Rewards")]
        [SerializeField] private Button claim10StarButton;
        [SerializeField] private Button claim20StarButton;
        [SerializeField] private Button claim30StarButton;
        [SerializeField] private Text milestoneFeedbackText;

        [Header("Stage Node Buttons (Optional Inspector Slots)")]
        [SerializeField] private List<Button> stageNodeButtons = new List<Button>();
        private Coroutine activePulseRoutine;
        private Transform activePulseTarget;
        private Coroutine stageUnlockAnimRoutine;
        private int lastSeenHighestStageId = -1;

        private StageDataSO currentStage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
        }

        private void OnEnable()
        {
            StageManager.OnStageSelected += HandleStageSelected;
            StageManager.OnStageCompleted += HandleStageCompleted;
            CitadelLobbyManager.OnEnergyChanged += HandleEnergyChanged;
            CitadelLobbyManager.OnTeamCPUpdated += HandleTeamCPUpdated;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            StageManager.OnStageSelected -= HandleStageSelected;
            StageManager.OnStageCompleted -= HandleStageCompleted;
            CitadelLobbyManager.OnEnergyChanged -= HandleEnergyChanged;
            CitadelLobbyManager.OnTeamCPUpdated -= HandleTeamCPUpdated;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (activePulseRoutine != null)
            {
                StopCoroutine(activePulseRoutine);
                activePulseRoutine = null;
            }

            if (stageUnlockAnimRoutine != null)
            {
                StopCoroutine(stageUnlockAnimRoutine);
                stageUnlockAnimRoutine = null;
            }
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage lang)
        {
            RefreshMapView();
            if (currentStage != null)
            {
                SelectStage(currentStage);
            }
        }

        private void Start()
        {
            EnsureStageManagerExists();
            RefreshMapView();

            // Default panel detail ditutup saat awal agar seluruh jalur 10 node terlihat bersih
            if (stageDetailPanel != null)
            {
                stageDetailPanel.SetActive(false);
            }
        }

        private void EnsureStageManagerExists()
        {
            if (StageManager.Instance == null)
            {
                GameObject stageObj = new GameObject("StageManager");
                stageObj.AddComponent<StageManager>();
            }
        }

        [Header("Chapter Progression")]
        [SerializeField] private int activeChapter = 1;

        public int ActiveChapter => activeChapter;

        public void SetChapter(int chapter)
        {
            activeChapter = 1; // Terkunci pada Chapter 1 sesuai arahan Komandan: Menuntaskan dan menyempurnakan Chapter 1
            BindStageNodeButtons();
            RefreshMapView();

            if (StageManager.Instance != null)
            {
                var stages = StageManager.Instance.GetStagesByChapter(activeChapter);
                if (stages.Count > 0)
                {
                    SelectStage(stages[0]);
                }
            }
        }

        private void BindStageNodeButtons()
        {
            for (int i = 0; i < stageNodeButtons.Count; i++)
            {
                int stageIdx = i + 1;
                int stageId = (activeChapter * 100) + stageIdx;
                int cachedId = stageId;
                stageNodeButtons[i].onClick.RemoveAllListeners();
                stageNodeButtons[i].onClick.AddListener(() => OnStageNodeClicked(cachedId));
            }
        }

        private void InitializeButtons()
        {
            if (closeButton == null)
            {
                closeButton = transform.Find("HeaderBanner/CloseButton")?.GetComponent<Button>()
                           ?? transform.Find("CloseButton")?.GetComponent<Button>()
                           ?? GetComponentInChildren<Button>(true);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseView);
                closeButton.onClick.AddListener(CloseView);
                closeButton.transform.SetAsLastSibling();
            }

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            }

            if (claim10StarButton != null)
                claim10StarButton.onClick.AddListener(() => ClaimMilestone(10));

            if (claim20StarButton != null)
                claim20StarButton.onClick.AddListener(() => ClaimMilestone(20));

            if (claim30StarButton != null)
                claim30StarButton.onClick.AddListener(() => ClaimMilestone(30));

            // Wire tombol silang (✕) pada StageDetailPanel agar pemain bisa menutup panel dan melihat seluruh peta kapan saja
            var closeDetailBtn = stageDetailPanel != null
                ? stageDetailPanel.transform.Find("CloseDetailButton")?.GetComponent<Button>()
                : null;
            if (closeDetailBtn != null)
            {
                closeDetailBtn.onClick.RemoveAllListeners();
                closeDetailBtn.onClick.AddListener(() =>
                {
                    if (stageDetailPanel != null) stageDetailPanel.SetActive(false);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(SoundType.ButtonClick);
                });
            }

            BindStageNodeButtons();
        }

        public void OpenView()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            else gameObject.SetActive(true);

            // Bawa panel ke tumpukan terdepan agar tidak tertutup elemen lain
            transform.SetAsLastSibling();

            var cg = GetComponent<CanvasGroup>() ?? rootPanel?.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            EnsureStageManagerExists();
            BindStageNodeButtons();
            RefreshMapView();

            // Sembunyikan panel detail saat pertama dibuka agar seluruh 10 stage terlihat jelas
            if (stageDetailPanel != null)
            {
                stageDetailPanel.SetActive(false);
            }

            if (StageManager.Instance != null)
            {
                var targetStage = StageManager.Instance.GetSelectedStage();
                if (targetStage == null && StageManager.Instance.ChapterStages != null && StageManager.Instance.ChapterStages.Count > 0)
                {
                    targetStage = StageManager.Instance.ChapterStages[0];
                }
                if (targetStage != null)
                {
                    currentStage = targetStage;
                }
            }
        }

        public void CloseView()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            else gameObject.SetActive(false);

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshMapView()
        {
            if (StageManager.Instance == null || CitadelLobbyManager.Instance == null) return;

            var panelTitle = transform.Find("HeaderBanner/TitleText")?.GetComponent<Text>();
            if (panelTitle != null)
            {
                panelTitle.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "PETA DUNIA & SELEKSI STAGE",
                    "WORLD MAP & STAGE SELECTION");
            }

            int totalStars = StageManager.Instance.GetTotalChapterStars();

            if (chapterTitleText != null)
            {
                chapterTitleText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "<b>👑 CHAPTER 1</b>  •  <color=#FFE066>Pertahanan Lembah Berbisik</color>",
                    "<b>👑 CHAPTER 1</b>  •  <color=#FFE066>Whispering Valley Bastion</color>");
            }

            if (totalStarsText != null)
            {
                string starsLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("Total Bintang", "Total Stars");
                totalStarsText.text = $"{starsLabel}: {totalStars} / 30 ★";
            }

            if (chapterStarSlider != null)
            {
                chapterStarSlider.maxValue = 30;
                chapterStarSlider.value = totalStars;
            }

            UpdateMilestoneButtons(totalStars);
            EnsureStageNodesAlignedToRoad();
            UpdateStageNodesVisuals();
            EnsureMapBackdropVisual();
        }

        private static readonly Vector4[] RoadAlignedNodeRects = new Vector4[]
        {
            new Vector4(0.14f, 0.035f, 0.30f, 0.125f), // 1-1 (Awal Jalan Setapak Lembah Kiri)
            new Vector4(0.24f, 0.135f, 0.40f, 0.225f), // 1-2 (Kelokan Jalan Tanah Lembah)
            new Vector4(0.17f, 0.245f, 0.33f, 0.335f), // 1-3 (Tikungan Menara Penjaga)
            new Vector4(0.34f, 0.345f, 0.50f, 0.435f), // 1-4 (Pintu Masuk Dek Jembatan Batu Kuno)
            new Vector4(0.47f, 0.375f, 0.67f, 0.485f), // 1-5 (👑 MINI-BOSS Tengah Jembatan Lengkung Batu)
            new Vector4(0.63f, 0.435f, 0.79f, 0.525f), // 1-6 (Pintu Keluar Jembatan & Lereng Timur)
            new Vector4(0.68f, 0.535f, 0.84f, 0.625f), // 1-7 (Dataran Perkebunan Lereng Kastil)
            new Vector4(0.60f, 0.635f, 0.76f, 0.725f), // 1-8 (Lekukan Sungai Atas & Kaki Tebing)
            new Vector4(0.52f, 0.725f, 0.68f, 0.815f), // 1-9 (Punggung Bukit Menuju Puncak)
            new Vector4(0.61f, 0.800f, 0.83f, 0.920f)  // 1-10 (💀 FINAL BOSS Gerbang Utama Citadel Cliff)
        };

        private void EnsureStageNodesAlignedToRoad()
        {
            if (stageNodeButtons == null || stageNodeButtons.Count < 10) return;
            for (int i = 0; i < 10; i++)
            {
                var btn = stageNodeButtons[i];
                if (btn == null) continue;
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(RoadAlignedNodeRects[i].x, RoadAlignedNodeRects[i].y);
                    rt.anchorMax = new Vector2(RoadAlignedNodeRects[i].z, RoadAlignedNodeRects[i].w);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            // Reposisi titik jejak (TrailDots) agar selalu mengikuti alur jalan baru antar node
            var trailPanel = transform.Find("ContentContainer/StageMapTrailPanel") ?? transform.Find("StageMapTrailPanel");
            if (trailPanel != null)
            {
                for (int r = 0; r < 9; r++)
                {
                    float x1 = (RoadAlignedNodeRects[r].x + RoadAlignedNodeRects[r].z) * 0.5f;
                    float y1 = (RoadAlignedNodeRects[r].y + RoadAlignedNodeRects[r].w) * 0.5f;
                    float x2 = (RoadAlignedNodeRects[r + 1].x + RoadAlignedNodeRects[r + 1].z) * 0.5f;
                    float y2 = (RoadAlignedNodeRects[r + 1].y + RoadAlignedNodeRects[r + 1].w) * 0.5f;

                    for (int s = 1; s <= 3; s++)
                    {
                        var dotTr = trailPanel.Find($"TrailDot_{r + 1}_{s}");
                        if (dotTr != null)
                        {
                            float t = s * 0.25f;
                            float dotX = Mathf.Lerp(x1, x2, t);
                            float dotY = Mathf.Lerp(y1, y2, t);
                            var dotRt = dotTr.GetComponent<RectTransform>();
                            if (dotRt != null)
                            {
                                dotRt.anchorMin = new Vector2(dotX - 0.008f, dotY - 0.012f);
                                dotRt.anchorMax = new Vector2(dotX + 0.008f, dotY + 0.012f);
                                dotRt.offsetMin = Vector2.zero;
                                dotRt.offsetMax = Vector2.zero;
                            }
                        }
                    }
                }
            }
        }

        private void EnsureMapBackdropVisual()
        {
            var trailPanel = transform.Find("ContentContainer/StageMapTrailPanel") ?? transform.Find("StageMapTrailPanel");
            if (trailPanel != null)
            {
                var trailImg = trailPanel.GetComponent<Image>();
                if (trailImg != null && trailImg.sprite == null)
                {
                    Sprite mapBackdrop = Resources.Load<Sprite>("Sprites/UI/WorldMap_Chapter1_Backdrop");
#if UNITY_EDITOR
                    if (mapBackdrop == null)
                    {
                        mapBackdrop = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/WorldMap_Chapter1_Backdrop.png");
                    }
#endif
                    if (mapBackdrop != null)
                    {
                        trailImg.sprite = mapBackdrop;
                        trailImg.color = Color.white;
                    }
                }
            }
        }

        private void UpdateStageNodesVisuals()
        {
            if (StageManager.Instance == null || CitadelLobbyManager.Instance == null) return;
            var accData = CitadelLobbyManager.Instance.AccountData;
            Font defaultFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            Sprite nodeNormalSp = Resources.Load<Sprite>("Sprites/UI/UI_StageNode_Normal");
            Sprite nodeMiniBossSp = Resources.Load<Sprite>("Sprites/UI/UI_StageNode_MiniBoss");
            Sprite nodeFinalBossSp = Resources.Load<Sprite>("Sprites/UI/UI_StageNode_FinalBoss");
#if UNITY_EDITOR
            if (nodeNormalSp == null) nodeNormalSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_StageNode_Normal.png");
            if (nodeMiniBossSp == null) nodeMiniBossSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_StageNode_MiniBoss.png");
            if (nodeFinalBossSp == null) nodeFinalBossSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_StageNode_FinalBoss.png");
#endif

            Transform nextTargetToPulse = null;
            for (int i = 0; i < stageNodeButtons.Count; i++)
            {
                int stageIdx = i + 1;
                int stageId = (activeChapter * 100) + stageIdx;
                var btn = stageNodeButtons[i];
                if (btn == null) continue;

                bool isUnlocked = (StageManager.Instance != null && StageManager.Instance.IsStageUnlocked(stageId)) ||
                                  (stageIdx == 1);
                var record = accData.GetStageRecord(stageId);
                bool isCleared = record != null && record.isCleared;
                int stars = record != null ? record.starsEarned : 0;

                btn.interactable = isUnlocked;

                // Pasang sprite medallion 3D fantasi beresolusi tinggi pada tombol node
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    if (stageIdx == 10 && nodeFinalBossSp != null) btnImg.sprite = nodeFinalBossSp;
                    else if (stageIdx == 5 && nodeMiniBossSp != null) btnImg.sprite = nodeMiniBossSp;
                    else if (nodeNormalSp != null) btnImg.sprite = nodeNormalSp;

                    btnImg.type = Image.Type.Simple;
                    btnImg.preserveAspect = true;

                    if (!isUnlocked)
                    {
                        btnImg.color = new Color(0.35f, 0.40f, 0.50f, 0.85f);
                    }
                    else if (isCleared)
                    {
                        btnImg.color = Color.white;
                    }
                    else
                    {
                        btnImg.color = new Color(1f, 1f, 0.90f, 1f);
                        if (nextTargetToPulse == null)
                        {
                            nextTargetToPulse = btn.transform;
                        }
                    }
                }

                var mainTxt = btn.transform.Find("Text")?.GetComponent<Text>() ?? btn.GetComponentInChildren<Text>();
                if (mainTxt != null)
                {
                    var mrt = mainTxt.GetComponent<RectTransform>();
                    if (mrt != null)
                    {
                        // Area kubah permata tengah di atas soket bintang
                        mrt.anchorMin = new Vector2(0.12f, 0.28f);
                        mrt.anchorMax = new Vector2(0.88f, 0.88f);
                        mrt.offsetMin = Vector2.zero;
                        mrt.offsetMax = Vector2.zero;
                    }

                    mainTxt.font = defaultFont;
                    if (!isUnlocked)
                    {
                        mainTxt.text = "<color=#CCCCCC>🔒</color>";
                        mainTxt.fontSize = 20;
                    }
                    else if (isCleared)
                    {
                        string label = (stageIdx == 10) ? "💀 10" : ((stageIdx == 5) ? "👑 5" : $"{stageIdx}");
                        mainTxt.text = $"<b>{label}</b>";
                        mainTxt.fontSize = (stageIdx == 10 || stageIdx == 5) ? 20 : 22;
                    }
                    else
                    {
                        string label = (stageIdx == 10) ? "💀 10" : ((stageIdx == 5) ? "👑 5" : $"{stageIdx}");
                        mainTxt.text = $"<b>{label}</b>\n<color=#00FFFF>GO</color>";
                        mainTxt.fontSize = 15;
                    }
                }

                // Nonaktifkan teks StarsBadge lama agar tidak ada teks yang menimpa
                var oldBadge = btn.transform.Find("StarsBadge");
                if (oldBadge != null) oldBadge.gameObject.SetActive(false);

                // 3 Lingkaran Kecil Fisik pada Sprite Node diisi bintang sesuai jumlah bintang yang didapatkan
                Sprite starBadgeSp = Resources.Load<Sprite>("Sprites/UI/UI_Star_Badge_Gold");
                Vector2[] socketCenters = new Vector2[]
                {
                    new Vector2(0.352f, 0.215f), // Lingkaran Kecil Kiri
                    new Vector2(0.500f, 0.176f), // Lingkaran Kecil Tengah
                    new Vector2(0.648f, 0.215f)  // Lingkaran Kecil Kanan
                };
                float halfSize = 0.068f;

                for (int s = 0; s < 3; s++)
                {
                    string socketName = $"StarSocket_{s}";
                    var socketTr = btn.transform.Find(socketName);
                    Image socketImg = null;
                    if (socketTr != null)
                    {
                        socketImg = socketTr.GetComponent<Image>();
                    }
                    else
                    {
                        var socketObj = new GameObject(socketName, typeof(RectTransform), typeof(Image));
                        socketObj.transform.SetParent(btn.transform, false);
                        socketImg = socketObj.GetComponent<Image>();
                        socketImg.raycastTarget = false;
                        socketImg.preserveAspect = true;
                    }

                    if (socketImg != null)
                    {
                        var srt = socketImg.GetComponent<RectTransform>();
                        srt.anchorMin = new Vector2(socketCenters[s].x - halfSize, socketCenters[s].y - halfSize);
                        srt.anchorMax = new Vector2(socketCenters[s].x + halfSize, socketCenters[s].y + halfSize);
                        srt.offsetMin = Vector2.zero;
                        srt.offsetMax = Vector2.zero;

                        if (starBadgeSp != null) socketImg.sprite = starBadgeSp;

                        if (isCleared && s < stars)
                        {
                            // Lingkaran kecil terisi emas berkilau sesuai bintang yang didapatkan
                            socketImg.color = Color.white;
                            socketImg.gameObject.SetActive(true);
                        }
                        else
                        {
                            // Belum didapatkan, aktif (GO), atau stage terkunci: lingkaran kecil tetap kosong alami
                            socketImg.gameObject.SetActive(false);
                        }
                    }
                }
            }

            StartPulseEffectOnActiveNode(nextTargetToPulse);
            FocusOnActiveNodePosition(nextTargetToPulse);

            // Cek apakah ada stage baru yang baru saja terbuka untuk animasi reveal
            int currentHighest = accData != null ? accData.highestStageCleared : 101;
            if (lastSeenHighestStageId != -1 && currentHighest > lastSeenHighestStageId)
            {
                int newlyUnlockedIdx = (currentHighest % 100) - 1;
                if (newlyUnlockedIdx >= 0 && newlyUnlockedIdx < stageNodeButtons.Count)
                {
                    var unlockedNode = stageNodeButtons[newlyUnlockedIdx];
                    if (unlockedNode != null && gameObject.activeInHierarchy)
                    {
                        if (stageUnlockAnimRoutine != null) StopCoroutine(stageUnlockAnimRoutine);
                        stageUnlockAnimRoutine = StartCoroutine(AnimateStageUnlockReveal(unlockedNode.transform));
                    }
                }
            }
            lastSeenHighestStageId = currentHighest;
        }

        private System.Collections.IEnumerator AnimateStageUnlockReveal(Transform nodeTf)
        {
            if (nodeTf == null) yield break;

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Success);
            }

            Vector3 startScale = Vector3.zero;
            Vector3 peakScale = new Vector3(1.35f, 1.35f, 1f);
            Vector3 normalScale = Vector3.one;

            float elapsed = 0f;
            float dur = 0.45f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                if (t < 0.6f)
                {
                    nodeTf.localScale = Vector3.Lerp(startScale, peakScale, t / 0.6f);
                }
                else
                {
                    nodeTf.localScale = Vector3.Lerp(peakScale, normalScale, (t - 0.6f) / 0.4f);
                }
                yield return null;
            }
            nodeTf.localScale = normalScale;
            stageUnlockAnimRoutine = null;
        }

        private void StartPulseEffectOnActiveNode(Transform target)
        {
            if (activePulseRoutine != null)
            {
                StopCoroutine(activePulseRoutine);
                activePulseRoutine = null;
            }

            if (activePulseTarget != null)
            {
                activePulseTarget.localScale = Vector3.one;
                activePulseTarget = null;
            }

            if (target != null && gameObject.activeInHierarchy)
            {
                activePulseTarget = target;
                activePulseRoutine = StartCoroutine(PulseNodeRoutine(target));
            }
        }

        private void FocusOnActiveNodePosition(Transform target)
        {
            if (target == null) return;
            var scrollRect = GetComponentInChildren<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                // Auto-center scroll jika map ditempatkan dalam scrollable canvas
                Canvas.ForceUpdateCanvases();
                RectTransform contentRect = scrollRect.content;
                RectTransform targetRect = target as RectTransform;
                if (targetRect != null)
                {
                    Vector2 targetLocal = contentRect.InverseTransformPoint(targetRect.position);
                    float normalizedY = Mathf.Clamp01(1f - ((targetLocal.y - contentRect.rect.yMin) / contentRect.rect.height));
                    scrollRect.verticalNormalizedPosition = normalizedY;
                }
            }
        }

        private System.Collections.IEnumerator PulseNodeRoutine(Transform target)
        {
            float speed = 3.2f;
            while (target != null)
            {
                float scale = 1.0f + 0.08f * (0.5f + 0.5f * Mathf.Sin(Time.time * speed));
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        private void UpdateMilestoneButtons(int currentStars)
        {
            if (claim10StarButton != null)
            {
                claim10StarButton.interactable = currentStars >= 10;
                var img = claim10StarButton.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    img.color = currentStars >= 10 ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.85f);
                }
            }
            if (claim20StarButton != null)
            {
                claim20StarButton.interactable = currentStars >= 20;
                var img = claim20StarButton.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    img.color = currentStars >= 20 ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.85f);
                }
            }
            if (claim30StarButton != null)
            {
                claim30StarButton.interactable = currentStars >= 30;
                var img = claim30StarButton.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    img.color = currentStars >= 30 ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.85f);
                }
            }
        }

        public void OnStageNodeClicked(int stageId)
        {
            if (StageManager.Instance == null) return;

            if (StageManager.Instance.TrySelectStage(stageId))
            {
                StageDataSO stage = StageManager.Instance.GetStageById(stageId);
                SelectStage(stage);

                if (AudioManager.Instance != null)
                {
                    // Efek suara taktis medali stage (pitch responsif berbobot)
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick, 1.15f);
                }

                if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                    FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
                {
                    FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
                }
            }
            else
            {
                Debug.LogWarning($"[UIStageMapView] Stage {stageId} belum terbuka!");
            }
        }

        public void SelectStage(StageDataSO stage)
        {
            if (stage == null) return;
            currentStage = stage;

            if (stageDetailPanel != null)
            {
                stageDetailPanel.SetActive(true);
            }

            int playerCP = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.TotalTeamCP : 0;
            int currentEnergy = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.CurrentEnergy : 0;

            Font defaultFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (defaultFont != null)
            {
                if (stageCodeText != null) stageCodeText.font = defaultFont;
                if (stageTitleText != null) stageTitleText.font = defaultFont;
                if (battleModeText != null) battleModeText.font = defaultFont;
                if (stageDescriptionText != null) stageDescriptionText.font = defaultFont;
                if (recommendedCpText != null) recommendedCpText.font = defaultFont;
                if (difficultyStatusText != null) difficultyStatusText.font = defaultFont;
                if (energyCostText != null) energyCostText.font = defaultFont;
                if (firstClearRewardsText != null) firstClearRewardsText.font = defaultFont;
                if (repeatRewardsText != null) repeatRewardsText.font = defaultFont;
                if (chapterTitleText != null) chapterTitleText.font = defaultFont;
                if (totalStarsText != null) totalStarsText.font = defaultFont;
                if (startBattleButton != null)
                {
                    var bTxt = startBattleButton.GetComponentInChildren<Text>();
                    if (bTxt != null) bTxt.font = defaultFont;
                }
            }

            if (stageCodeText != null)
            {
                string cleanCode = stage.stageCode != null 
                    ? stage.stageCode.Replace("Stage ", "").Replace("STAGE ", "").Trim() 
                    : $"{stage.stageId}";
                stageCodeText.text = $"<b>STAGE {cleanCode}</b>";
            }
            if (stageTitleText != null) stageTitleText.text = $"<b>{stage.GetLocalizedTitle().ToUpper()}</b>";

            // Tampilkan Boss Warning Banner jika Stage adalah Mini-Boss (Stage 5) atau Final Boss (Stage 10)
            int stageNum = stage.stageId % 100;
            bool isBossStage = (stageNum == 5 || stageNum == 10);
            if (bossWarningBanner != null)
            {
                bossWarningBanner.SetActive(isBossStage);
                if (isBossStage && bossWarningText != null)
                {
                    if (stageNum == 10)
                    {
                        bossWarningText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                            "⚠️ PERINGATAN: FINAL BOSS ABYSSAL TELAH BANGKIT!",
                            "⚠️ WARNING: FINAL ABYSSAL BOSS HAS AWAKENED!");
                        bossWarningText.color = new Color(1f, 0.25f, 0.25f);
                    }
                    else
                    {
                        bossWarningText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                            "👑 WASPADA: ELITE MINI-BOSS MENUNGGU!",
                            "👑 ALERT: ELITE MINI-BOSS ENCOUNTER!");
                        bossWarningText.color = new Color(1f, 0.85f, 0.2f);
                    }
                }
            }

            if (battleModeText != null)
            {
                string modeIcon = (stage.battleMode == FantasyDefense.Data.BattleMode.PuzzlePreparation) ? "🧩" : "⚔️";
                battleModeText.text = $"{modeIcon} <color=#FFD700><b>{stage.GetBattleModeLabel().ToUpper()}</b></color>";
            }

            if (stageDescriptionText != null)
            {
                string modeTag = battleModeText == null ? $"\n<color=#FFD700><b>Mode:</b> {stage.GetBattleModeLabel()}</color>" : "";
                stageDescriptionText.text = $"{stage.GetLocalizedDescription()}{modeTag}";
            }

            if (recommendedCpText != null)
            {
                string superiorTag = FantasyDefense.Localization.CitadelLocalizationManager.Text("UNGGUL!", "SUPERIOR!");
                string alertTag = FantasyDefense.Localization.CitadelLocalizationManager.Text("WASPADA!", "WARNING!");
                string cpComparison = playerCP >= stage.recommendedCP 
                    ? $"<color=#00FF66><b>{playerCP:N0}</b> ({superiorTag})</color>" 
                    : $"<color=#FF7777><b>{playerCP:N0}</b> ({alertTag})</color>";

                string recLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("🛡️ CP BENTENG", "🛡️ CITADEL CP");
                string teamLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("TIM KAMU", "YOUR TEAM");
                recommendedCpText.text = $"{recLabel}: <b>{stage.recommendedCP:N0}</b>  |  {teamLabel}: {cpComparison}";
            }

            if (difficultyStatusText != null)
            {
                string diffTag = stage.GetDifficultyLabel(playerCP).ToUpper();
                string diffLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("⚡ KESULITAN", "⚡ DIFFICULTY");
                difficultyStatusText.text = $"{diffLabel}: <b>{diffTag}</b>";
                difficultyStatusText.color = stage.GetDifficultyColor(playerCP);
            }

            if (energyCostText != null)
            {
                string energyLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("⚡ BIAYA ENERGI", "⚡ ENERGY COST");
                string availLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("TERSEDIA", "AVAILABLE");
                energyCostText.text = $"{energyLabel}: <color=#00FFFF><b>{stage.staminaCost} STAMINA</b></color> ({availLabel}: <b>{currentEnergy}/30</b>)";
            }

            if (firstClearRewardsText != null)
            {
                var accData = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.AccountData : null;
                bool isCleared = accData != null && accData.GetStageRecord(stage.stageId)?.isCleared == true;

                if (isCleared)
                {
                    string claimedLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("🎁 HADIAH PERTAMA: [SUDAH DIKLAIM]", "🎁 FIRST CLEAR REWARD: [CLAIMED]");
                    firstClearRewardsText.text = $"<color=#888888>{claimedLabel}</color>";
                }
                else
                {
                    string firstLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("🎁 HADIAH PERTAMA", "🎁 FIRST REWARD");
                    firstClearRewardsText.text = $"{firstLabel}: <color=#FFD700>🪙 +{stage.firstClearGold:N0} Gold</color>  |  <color=#00FFFF>💎 +{stage.firstClearGems:N0} Gems</color>";
                }
            }

            if (repeatRewardsText != null)
            {
                string repeatLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("REWARD GRINDING", "REPEAT REWARD");
                repeatRewardsText.text = $"{repeatLabel}: 🪙 +{stage.repeatClearGold}  |  ⭐ +{stage.repeatClearExp} EXP  |  💎 +{stage.repeatClearGems}";
            }

            if (startBattleButton != null)
            {
                bool hasEnoughEnergy = currentEnergy >= stage.staminaCost;
                startBattleButton.interactable = true; // Selalu aktif agar pemain mendapat feedback jika stamina habis
                var txt = startBattleButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    if (hasEnoughEnergy)
                    {
                        txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("MAP_START_BATTLE", "⚔️ MULAI PERTEMPURAN SEKARANG", "⚔️ COMMENCE DEFENSE NOW");
                    }
                    else
                    {
                        txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("⚡ STAMINA TIDAK CUKUP", "⚡ NOT ENOUGH ENERGY");
                    }
                }
            }
        }

        private void OnStartBattleClicked()
        {
            if (CitadelLobbyManager.Instance == null) return;

            // Fallback jika currentStage belum terisi
            if (currentStage == null && StageManager.Instance != null && StageManager.Instance.ChapterStages != null && StageManager.Instance.ChapterStages.Count > 0)
            {
                currentStage = StageManager.Instance.ChapterStages[0];
            }

            if (currentStage == null)
            {
                Debug.LogWarning("[UIStageMapView] Tidak ada stage yang dipilih untuk memulai pertempuran!");
                return;
            }

            if (!CitadelLobbyManager.Instance.CanStartBattle())
            {
                int currentEnergy = CitadelLobbyManager.Instance.CurrentEnergy;
                int cost = currentStage != null ? currentStage.staminaCost : 5;
                Debug.LogWarning($"[UIStageMapView] Stamina tidak cukup! {currentEnergy}/{cost}");

                string toastMsg = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    $"⚡ Stamina Tidak Cukup!\nMembutuhkan {cost} Stamina untuk bertempur. (Tersedia: {currentEnergy})",
                    $"⚡ Not Enough Energy!\nRequires {cost} Stamina to commence defense. (Available: {currentEnergy})"
                );
                string btnLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("💎 BUKA TOKO (REFILL)", "💎 OPEN STORE (REFILL)");
                CitadelToastNotice.ShowWithAction(toastMsg, btnLabel, () => {
                    if (FantasyDefense.Core.GameFlowManager.Instance != null)
                    {
                        FantasyDefense.Core.GameFlowManager.Instance.OpenPanel(FantasyDefense.Core.GamePanelType.Store);
                    }
                }, 4.2f);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.DefeatChime);
                }
                return;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            Debug.Log($"[UIStageMapView] Meluncur ke pertempuran arena {currentStage.stageCode} ({currentStage.stageTitle})!");

            // Memulai inisialisasi arena battle
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StartBattle(currentStage);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.StartBattleWithStage(currentStage);
            }

            if (stageDetailPanel != null)
            {
                stageDetailPanel.SetActive(false);
            }
        }

        private void ClaimMilestone(int milestone)
        {
            if (StageManager.Instance == null) return;

            bool success = StageManager.Instance.TryClaimStarMilestone(milestone, out string message);
            if (milestoneFeedbackText != null)
            {
                milestoneFeedbackText.text = message;
            }

            if (success && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            RefreshMapView();
        }

        private void HandleStageSelected(StageDataSO stage)
        {
            SelectStage(stage);
        }

        private void HandleStageCompleted(StageDataSO stage, int stars, bool isFirstClear)
        {
            RefreshMapView();
        }

        private void HandleEnergyChanged(int current, int max)
        {
            if (currentStage != null)
            {
                SelectStage(currentStage);
            }
        }

        private void HandleTeamCPUpdated(int totalCP)
        {
            if (currentStage != null)
            {
                SelectStage(currentStage);
            }
        }
    }
}
