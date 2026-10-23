using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.GridSystem;
using FantasyDefense.Summon;

namespace FantasyDefense.UI
{
    /// <summary>
    /// In-Game Real-Time Debug Log Viewer & Diagnostics Console untuk Rampartbound: Merge Defense.
    /// Memungkinkan pemain dan penguji memeriksa seluruh log Unity, error, warning, dan diagnosis arena
    /// langsung di layar ponsel fisik (Android sideload/Google Play) tanpa perlu kabel USB atau adb logcat.
    /// 
    /// CARA MEMBUKA:
    /// 1. Ketuk layar 3x secara beruntun (Triple Tap di mana saja pada layar dalam jeda 1.2 detik).
    /// 2. Atau ketuk tombol melayang "🐞 LOG" di pojok kanan atas layar.
    /// </summary>
    public class CitadelInGameLogViewer : MonoBehaviour
    {
        public static CitadelInGameLogViewer Instance { get; private set; }

        public struct LogEntry
        {
            public DateTime timestamp;
            public string message;
            public string stackTrace;
            public LogType type;
        }

        private const int MAX_LOG_ENTRIES = 250;
        private readonly List<LogEntry> logEntries = new List<LogEntry>();

        private int errorCount = 0;
        private int warningCount = 0;
        private int infoCount = 0;

        // Filter: 0 = All, 1 = Errors only, 2 = Warnings only, 3 = Info only
        private int currentFilter = 0;

        // Triple Tap detection settings
        private int tapCount = 0;
        private float lastTapTime = 0f;
        private float firstTapTime = 0f;
        private const float MAX_TAP_INTERVAL = 0.50f;
        private const float MAX_TOTAL_INTERVAL = 1.30f;

        // UI GameObjects & Components
        private Canvas logCanvas;
        private GameObject windowPanel;
        private GameObject floatingButtonObj;
        private Text floatingButtonText;
        private Image floatingButtonBg;
        private Text statusSummaryText;
        private Text logContentText;
        private ScrollRect scrollRect;
        private GameObject toastObj;
        private Text toastText;
        private Coroutine toastCoroutine;

        private bool isViewerOpen = false;
        private bool isSubscribedToLog = false;
        private bool isUpdatingUI = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = UnityEngine.Object.FindAnyObjectByType<CitadelInGameLogViewer>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    var go = new GameObject("CitadelInGameLogViewer");
                    go.AddComponent<CitadelInGameLogViewer>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SubscribeToLogs();
            BuildUIHierarchy();
        }

        private void OnEnable()
        {
            SubscribeToLogs();
        }

        private void OnDisable()
        {
            UnsubscribeFromLogs();
        }

        private void OnDestroy()
        {
            UnsubscribeFromLogs();
        }

        private void SubscribeToLogs()
        {
            if (!isSubscribedToLog)
            {
                Application.logMessageReceived += HandleLog;
                isSubscribedToLog = true;
            }
        }

        private void UnsubscribeFromLogs()
        {
            if (isSubscribedToLog)
            {
                Application.logMessageReceived -= HandleLog;
                isSubscribedToLog = false;
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Cegah rekursi tak hingga jika terjadi error pada render UI Text
            if (isUpdatingUI && logString != null && logString.Contains("65000 vertices"))
            {
                return;
            }

            var entry = new LogEntry
            {
                timestamp = DateTime.Now,
                message = logString ?? string.Empty,
                stackTrace = stackTrace ?? string.Empty,
                type = type
            };

            lock (logEntries)
            {
                if (logEntries.Count >= MAX_LOG_ENTRIES)
                {
                    var oldest = logEntries[0];
                    if (oldest.type == LogType.Error || oldest.type == LogType.Exception || oldest.type == LogType.Assert) errorCount--;
                    else if (oldest.type == LogType.Warning) warningCount--;
                    else infoCount--;

                    logEntries.RemoveAt(0);
                }

                logEntries.Add(entry);

                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errorCount++;
                }
                else if (type == LogType.Warning)
                {
                    warningCount++;
                }
                else
                {
                    infoCount++;
                }
            }

            UpdateFloatingButtonVisual();

            if (isViewerOpen && !isUpdatingUI)
            {
                RefreshLogDisplay();
            }
        }

        private void Update()
        {
            // 1. Deteksi Triple Tap di layar untuk memicu / menutup Log Viewer
            DetectTripleTap();
        }

        private void DetectTripleTap()
        {
            bool tapStarted = false;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    tapStarted = true;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                tapStarted = true;
            }

            if (tapStarted)
            {
                float now = Time.unscaledTime;

                if (now - lastTapTime <= MAX_TAP_INTERVAL)
                {
                    tapCount++;
                }
                else
                {
                    tapCount = 1;
                    firstTapTime = now;
                }

                lastTapTime = now;

                if (tapCount >= 3)
                {
                    if (now - firstTapTime <= MAX_TOTAL_INTERVAL)
                    {
                        ToggleViewer();
                    }
                    tapCount = 0;
                }
            }
        }

        public void ToggleViewer()
        {
            SetViewerOpen(!isViewerOpen);
        }

        public void SetViewerOpen(bool open)
        {
            isViewerOpen = open;
            if (windowPanel != null)
            {
                windowPanel.SetActive(open);
            }

            if (open)
            {
                RefreshLogDisplay();
                // Otomatis scroll ke log terbawah
                if (scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void UpdateFloatingButtonVisual()
        {
            if (floatingButtonText == null || floatingButtonBg == null) return;

            if (errorCount > 0)
            {
                floatingButtonText.text = $"🐞 ERR:{errorCount}";
                floatingButtonBg.color = new Color(0.85f, 0.15f, 0.15f, 0.85f);
            }
            else if (warningCount > 0)
            {
                floatingButtonText.text = $"⚠️ WARN:{warningCount}";
                floatingButtonBg.color = new Color(0.85f, 0.65f, 0.10f, 0.85f);
            }
            else
            {
                floatingButtonText.text = $"🐞 LOG:{logEntries.Count}";
                floatingButtonBg.color = new Color(0.15f, 0.20f, 0.30f, 0.75f);
            }
        }

        private void RefreshLogDisplay()
        {
            if (statusSummaryText != null)
            {
                statusSummaryText.text = $"<color=#FF5555>🔴 Error: {errorCount}</color>  |  <color=#FFD700>🟡 Warn: {warningCount}</color>  |  <color=#A0D8FF>⚪ Info: {infoCount}</color>  (Total: {logEntries.Count})";
            }

            if (logContentText == null) return;

            isUpdatingUI = true;
            try
            {
                var filtered = new List<LogEntry>();
                lock (logEntries)
                {
                    for (int i = 0; i < logEntries.Count; i++)
                    {
                        var entry = logEntries[i];
                        if (currentFilter == 1 && entry.type != LogType.Error && entry.type != LogType.Exception && entry.type != LogType.Assert) continue;
                        if (currentFilter == 2 && entry.type != LogType.Warning) continue;
                        if (currentFilter == 3 && entry.type != LogType.Log) continue;
                        filtered.Add(entry);
                    }
                }

                if (filtered.Count == 0)
                {
                    logContentText.text = "<color=#888888><i>Belum ada log pesan tercatat dalam filter ini.</i></color>";
                    return;
                }

                // Batasi maksimal 35 entri log terbaru agar vertex mesh TIDAK PERNAH melampaui limit 65.000 Unity UI
                int maxDisplay = 35;
                int startIndex = Mathf.Max(0, filtered.Count - maxDisplay);
                var sb = new StringBuilder();

                if (startIndex > 0)
                {
                    sb.Append($"<color=#778899><i>[... {startIndex} log terdahulu disembunyikan agar performa ringan. Gunakan tombol 'SALIN' untuk dump lengkap ...]</i></color>\n\n");
                }

                for (int i = startIndex; i < filtered.Count; i++)
                {
                    var entry = filtered[i];
                    string timeStr = entry.timestamp.ToString("HH:mm:ss");
                    string colorPrefix;
                    string tagPrefix;

                    switch (entry.type)
                    {
                        case LogType.Error:
                        case LogType.Exception:
                        case LogType.Assert:
                            colorPrefix = "<color=#FF6B6B>";
                            tagPrefix = "[ERR]";
                            break;
                        case LogType.Warning:
                            colorPrefix = "<color=#FFD166>";
                            tagPrefix = "[WRN]";
                            break;
                        default:
                            colorPrefix = "<color=#EAEAEA>";
                            tagPrefix = "[LOG]";
                            break;
                    }

                    sb.Append(colorPrefix);
                    sb.Append($"[{timeStr}] {tagPrefix} ");
                    // Batasi panjang pesan individual jika terlalu panjang
                    string msg = entry.message ?? string.Empty;
                    if (msg.Length > 200) msg = msg.Substring(0, 197) + "...";
                    sb.Append(msg);
                    sb.Append("</color>\n");

                    if (!string.IsNullOrEmpty(entry.stackTrace) && (entry.type == LogType.Error || entry.type == LogType.Exception))
                    {
                        string trace = entry.stackTrace.Trim();
                        var lines = trace.Split('\n');
                        string shortTrace = lines.Length > 2 ? $"{lines[0].Trim()}\n    {lines[1].Trim()}" : trace;
                        sb.Append("<color=#FFAAAA><i>  StackTrace: ");
                        sb.Append(shortTrace);
                        sb.Append("</i></color>\n");
                    }
                }

                // Hard clamp batas karakter untuk perlindungan mutlak 65000 vertices
                string result = sb.ToString();
                if (result.Length > 6500)
                {
                    result = result.Substring(result.Length - 6500);
                }

                logContentText.text = result;
            }
            finally
            {
                isUpdatingUI = false;
            }
        }

        #region Actions & Diagnostics
        public void CopyAllLogsToClipboard()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== RAMPARTBOUND DIAGNOSTIC LOG DUMP ===");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Device: {SystemInfo.deviceModel} ({SystemInfo.operatingSystem})");
            sb.AppendLine($"Resolution: {Screen.width}x{Screen.height} (DPI: {Screen.dpi:F1})");
            sb.AppendLine($"Errors: {errorCount} | Warnings: {warningCount} | Total: {logEntries.Count}");
            sb.AppendLine("----------------------------------------");

            lock (logEntries)
            {
                foreach (var entry in logEntries)
                {
                    sb.AppendLine($"[{entry.timestamp:HH:mm:ss}] [{entry.type}] {entry.message}");
                    if (!string.IsNullOrEmpty(entry.stackTrace))
                    {
                        sb.AppendLine($"  StackTrace: {entry.stackTrace.Trim()}");
                    }
                }
            }

            sb.AppendLine("=== END OF LOG DUMP ===");
            GUIUtility.systemCopyBuffer = sb.ToString();
            ShowToast("✔ Semua log berhasil disalin ke clipboard!");
        }

        public void ClearLogs()
        {
            lock (logEntries)
            {
                logEntries.Clear();
                errorCount = 0;
                warningCount = 0;
                infoCount = 0;
            }

            UpdateFloatingButtonVisual();
            RefreshLogDisplay();
            ShowToast("Log berhasil dibersihkan.");
        }

        public void SetFilter(int filterIndex)
        {
            currentFilter = filterIndex;
            RefreshLogDisplay();
        }

        public void RunArenaDiagnostics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== ARENA & SYSTEM DIAGNOSTICS ==========");
            sb.AppendLine($"Time: {DateTime.Now:HH:mm:ss} | Unity: {Application.unityVersion} | Platform: {Application.platform}");
            sb.AppendLine($"Device: {SystemInfo.deviceModel} | CPU: {SystemInfo.processorType} | RAM: {SystemInfo.systemMemorySize}MB");
            sb.AppendLine($"Screen: {Screen.width}x{Screen.height} (DPI: {Screen.dpi:F1}) | Orientation: {Screen.orientation}");

            // 1. GameFlowManager Check
            if (GameFlowManager.Instance != null)
            {
                sb.AppendLine($"GameFlowManager: State={GameFlowManager.Instance.CurrentState}, ActiveSubPanel={GameFlowManager.Instance.CurrentActivePanel}");
            }
            else
            {
                sb.AppendLine("GameFlowManager: NULL");
            }

            // 2. GameManager Check
            if (GameManager.Instance != null)
            {
                sb.AppendLine($"GameManager: State={GameManager.Instance.CurrentState}, Mode={GameManager.Instance.CurrentBattleMode}, MovesRemaining={GameManager.Instance.MovesRemaining}");
                var cfg = GameManager.Instance.ActiveLevelConfig;
                sb.AppendLine($"ActiveLevelConfig: {(cfg != null ? $"{cfg.levelName} (Dims: {cfg.gridWidth}x{cfg.gridHeight}, Mode: {cfg.battleMode})" : "NULL")}");
            }
            else
            {
                sb.AppendLine("GameManager: NULL");
            }

            // 3. GridManager & Tiles Check
            if (GridManager.Instance != null)
            {
                sb.AppendLine($"GridManager: Active={GridManager.Instance.gameObject.activeInHierarchy}, Initialized={GridManager.Instance.IsInitialized}, Dims={GridManager.Instance.GridWidth}x{GridManager.Instance.GridHeight}, CellSize={GridManager.Instance.CellSize:F2}");
                int occupied = 0, empty = 0;
                if (GridManager.Instance.IsInitialized)
                {
                    for (int x = 0; x < GridManager.Instance.GridWidth; x++)
                    {
                        for (int y = 0; y < GridManager.Instance.GridHeight; y++)
                        {
                            var c = GridManager.Instance.GetCell(x, y);
                            if (c != null)
                            {
                                if (c.IsOccupied) occupied++;
                                else empty++;
                            }
                        }
                    }
                }
                sb.AppendLine($"Grid Cells: Total={occupied + empty} (Occupied={occupied}, Empty={empty})");
            }
            else
            {
                sb.AppendLine("GridManager: NULL");
            }

            // 4. HeroSummoner Check
            if (HeroSummoner.Instance != null)
            {
                sb.AppendLine($"HeroSummoner: Active={HeroSummoner.Instance.gameObject.activeInHierarchy}, ActiveHeroPoolCount={HeroSummoner.Instance.ActiveHeroPoolCount}");
            }
            else
            {
                sb.AppendLine("HeroSummoner: NULL");
            }

            // 5. UnitController Active Units
            sb.AppendLine($"UnitController ActiveUnits: {UnitController.ActiveUnits.Count}");
            for (int i = 0; i < Mathf.Min(6, UnitController.ActiveUnits.Count); i++)
            {
                var u = UnitController.ActiveUnits[i];
                if (u != null)
                {
                    var sr = u.GetComponent<SpriteRenderer>();
                    string sprName = (sr != null && sr.sprite != null) ? sr.sprite.name : "NULL";
                    string sorting = (sr != null) ? $"{sr.sortingLayerName}:{sr.sortingOrder}" : "NONE";
                    sb.AppendLine($"  - Unit[{i}]: Type={u.UnitData?.unitType}, Tier={u.CurrentTier}, Pos={u.transform.position:F2}, Sprite={sprName}, Sort={sorting}, Active={u.gameObject.activeInHierarchy}");
                }
            }

            // 6. Camera Check
            if (Camera.main != null)
            {
                sb.AppendLine($"MainCamera: Ortho={Camera.main.orthographic}, OrthoSize={Camera.main.orthographicSize:F2}, Pos={Camera.main.transform.position:F2}, CullingMask={Camera.main.cullingMask}");
            }
            else
            {
                sb.AppendLine("MainCamera: NULL");
            }

            // 7. Castle Barricade Check
            if (CastleBarricade.Instance != null)
            {
                sb.AppendLine($"CastleBarricade: HP={CastleBarricade.Instance.CurrentHealth}/{CastleBarricade.Instance.MaxHealth}, Active={CastleBarricade.Instance.gameObject.activeInHierarchy}");
            }

            sb.AppendLine("================================================");
            Debug.Log(sb.ToString());
            ShowToast("🔍 Diagnosis arena selesai! Periksa log.");
        }

        private void ShowToast(string message)
        {
            if (toastObj == null || toastText == null) return;

            toastText.text = message;
            toastObj.SetActive(true);

            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
            }
            toastCoroutine = StartCoroutine(ToastTimerRoutine());
        }

        private IEnumerator ToastTimerRoutine()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            if (toastObj != null)
            {
                toastObj.SetActive(false);
            }
        }
        #endregion

        #region Programmatic UI Construction
        private void BuildUIHierarchy()
        {
            // 1. Root Canvas
            var canvasObj = new GameObject("CitadelDebugLogCanvas");
            canvasObj.transform.SetParent(transform, false);

            logCanvas = canvasObj.AddComponent<Canvas>();
            logCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            logCanvas.sortingOrder = 32700; // Selalu berada di puncak seluruh UI

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (defaultFont == null)
            {
                var allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) defaultFont = allFonts[0];
            }

            // 2. Tombol Melayang (Floating Toggle Button di Pojok Kanan Bawah, bebas benturan dari tombol Exit / Pause di atas)
            floatingButtonObj = new GameObject("FloatingLogButton");
            floatingButtonObj.transform.SetParent(canvasObj.transform, false);

            var floatRect = floatingButtonObj.AddComponent<RectTransform>();
            floatRect.anchorMin = new Vector2(1f, 0f);
            floatRect.anchorMax = new Vector2(1f, 0f);
            floatRect.pivot = new Vector2(1f, 0f);
            floatRect.anchoredPosition = new Vector2(-20f, 130f); // Posisi bawah layar, di atas dock navigasi & bebas benturan
            floatRect.sizeDelta = new Vector2(180f, 60f);

            floatingButtonBg = floatingButtonObj.AddComponent<Image>();
            floatingButtonBg.color = new Color(0.15f, 0.20f, 0.30f, 0.75f);

            var floatBtn = floatingButtonObj.AddComponent<Button>();
            floatBtn.onClick.AddListener(ToggleViewer);

            var floatTxtObj = new GameObject("Text");
            floatTxtObj.transform.SetParent(floatingButtonObj.transform, false);
            var floatTxtRect = floatTxtObj.AddComponent<RectTransform>();
            floatTxtRect.anchorMin = Vector2.zero;
            floatTxtRect.anchorMax = Vector2.one;
            floatTxtRect.sizeDelta = Vector2.zero;

            floatingButtonText = floatTxtObj.AddComponent<Text>();
            floatingButtonText.font = defaultFont;
            floatingButtonText.fontSize = 24;
            floatingButtonText.fontStyle = FontStyle.Bold;
            floatingButtonText.alignment = TextAnchor.MiddleCenter;
            floatingButtonText.color = Color.white;
            floatingButtonText.text = "🐞 LOG:0";

            // 3. Jendela Utama Log (Window Panel)
            windowPanel = new GameObject("LogWindowPanel");
            windowPanel.transform.SetParent(canvasObj.transform, false);

            var winRect = windowPanel.AddComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.04f, 0.05f);
            winRect.anchorMax = new Vector2(0.96f, 0.95f);
            winRect.offsetMin = Vector2.zero;
            winRect.offsetMax = Vector2.zero;

            var winBg = windowPanel.AddComponent<Image>();
            winBg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f); // Dark slate semi-transparent

            // 3a. Header Top Bar
            var headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(windowPanel.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 110f);

            var headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

            // Title Text
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0.5f);
            titleRect.anchorMax = new Vector2(0.80f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(1f, 0.85f, 0.35f, 1f); // Gold title
            titleText.text = "⚔️ RAMPARTBOUND DEBUG CONSOLE";

            // Status Summary Subtext
            var summaryObj = new GameObject("SummaryText");
            summaryObj.transform.SetParent(headerObj.transform, false);
            var summaryRect = summaryObj.AddComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0.03f, 0.05f);
            summaryRect.anchorMax = new Vector2(0.80f, 0.50f);
            summaryRect.offsetMin = Vector2.zero;
            summaryRect.offsetMax = Vector2.zero;

            statusSummaryText = summaryObj.AddComponent<Text>();
            statusSummaryText.font = defaultFont;
            statusSummaryText.fontSize = 20;
            statusSummaryText.alignment = TextAnchor.MiddleLeft;
            statusSummaryText.color = Color.white;
            statusSummaryText.supportRichText = true;
            statusSummaryText.text = "Memuat log...";

            // Close Button [✕]
            var closeBtnObj = CreateButton(headerObj.transform, "CloseButton", "✕", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, 0f), new Vector2(70f, 70f), new Color(0.75f, 0.2f, 0.2f, 1f), defaultFont, 32);
            closeBtnObj.GetComponent<Button>().onClick.AddListener(() => SetViewerOpen(false));

            // 3b. Action & Filter Bar (Di Bawah Header)
            var actionToolBar = new GameObject("ActionToolBar");
            actionToolBar.transform.SetParent(windowPanel.transform, false);
            var toolRect = actionToolBar.AddComponent<RectTransform>();
            toolRect.anchorMin = new Vector2(0f, 1f);
            toolRect.anchorMax = new Vector2(1f, 1f);
            toolRect.pivot = new Vector2(0.5f, 1f);
            toolRect.anchoredPosition = new Vector2(0f, -115f);
            toolRect.sizeDelta = new Vector2(0f, 75f);

            var toolHGroup = actionToolBar.AddComponent<HorizontalLayoutGroup>();
            toolHGroup.childAlignment = TextAnchor.MiddleCenter;
            toolHGroup.spacing = 10f;
            toolHGroup.padding = new RectOffset(15, 15, 5, 5);
            toolHGroup.childForceExpandWidth = true;
            toolHGroup.childForceExpandHeight = true;

            var allFilterBtn = CreateToolbarButton(actionToolBar.transform, "BtnAll", "SEMUA", new Color(0.2f, 0.3f, 0.45f, 1f), defaultFont);
            allFilterBtn.onClick.AddListener(() => SetFilter(0));

            var errFilterBtn = CreateToolbarButton(actionToolBar.transform, "BtnErr", "ERROR", new Color(0.6f, 0.15f, 0.15f, 1f), defaultFont);
            errFilterBtn.onClick.AddListener(() => SetFilter(1));

            var warnFilterBtn = CreateToolbarButton(actionToolBar.transform, "BtnWarn", "WARN", new Color(0.6f, 0.45f, 0.1f, 1f), defaultFont);
            warnFilterBtn.onClick.AddListener(() => SetFilter(2));

            var diagBtn = CreateToolbarButton(actionToolBar.transform, "BtnDiag", "🔍 CEK ARENA", new Color(0.1f, 0.5f, 0.6f, 1f), defaultFont);
            diagBtn.onClick.AddListener(RunArenaDiagnostics);

            var copyBtn = CreateToolbarButton(actionToolBar.transform, "BtnCopy", "📋 SALIN", new Color(0.15f, 0.55f, 0.25f, 1f), defaultFont);
            copyBtn.onClick.AddListener(CopyAllLogsToClipboard);

            var clearBtn = CreateToolbarButton(actionToolBar.transform, "BtnClear", "🧹 HAPUS", new Color(0.4f, 0.2f, 0.2f, 1f), defaultFont);
            clearBtn.onClick.AddListener(ClearLogs);

            // 3c. Scrollable Content Area
            var scrollAreaObj = new GameObject("LogScrollArea");
            scrollAreaObj.transform.SetParent(windowPanel.transform, false);
            var scrollRectTransform = scrollAreaObj.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.02f, 0.08f);
            scrollRectTransform.anchorMax = new Vector2(0.98f, 1f);
            scrollRectTransform.offsetMin = new Vector2(0f, 0f);
            scrollRectTransform.offsetMax = new Vector2(0f, -195f);

            var scrollBg = scrollAreaObj.AddComponent<Image>();
            scrollBg.color = new Color(0.04f, 0.05f, 0.07f, 1f);

            var mask = scrollAreaObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            scrollRect = scrollAreaObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Content Container
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollAreaObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 300f);

            var contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var vGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            vGroup.padding = new RectOffset(15, 15, 15, 15);
            vGroup.childForceExpandWidth = true;
            vGroup.childForceExpandHeight = false;

            // Text Area
            var logTextObj = new GameObject("LogText");
            logTextObj.transform.SetParent(contentObj.transform, false);
            var logTextRect = logTextObj.AddComponent<RectTransform>();
            logTextRect.anchorMin = Vector2.zero;
            logTextRect.anchorMax = Vector2.one;

            logContentText = logTextObj.AddComponent<Text>();
            logContentText.font = defaultFont;
            logContentText.fontSize = 20;
            logContentText.lineSpacing = 1.15f;
            logContentText.alignment = TextAnchor.UpperLeft;
            logContentText.color = Color.white;
            logContentText.supportRichText = true;
            logContentText.text = "Menunggu log...";

            scrollRect.content = contentRect;

            // 3d. Footer Info Note
            var footerObj = new GameObject("FooterNote");
            footerObj.transform.SetParent(windowPanel.transform, false);
            var footerRect = footerObj.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0.07f);
            footerRect.offsetMin = Vector2.zero;
            footerRect.offsetMax = Vector2.zero;

            var footerText = footerObj.AddComponent<Text>();
            footerText.font = defaultFont;
            footerText.fontSize = 18;
            footerText.alignment = TextAnchor.MiddleCenter;
            footerText.color = new Color(0.7f, 0.75f, 0.85f, 0.85f);
            footerText.text = "💡 <i>Tip: Ketuk 3x di mana saja pada layar kapan pun untuk membuka/menutup log ini.</i>";

            // 4. Toast Popup Notification
            toastObj = new GameObject("LogToastNotification");
            toastObj.transform.SetParent(canvasObj.transform, false);
            var toastRect = toastObj.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.15f);
            toastRect.anchorMax = new Vector2(0.5f, 0.15f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.sizeDelta = new Vector2(650f, 70f);

            var toastBg = toastObj.AddComponent<Image>();
            toastBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            var toastTxtObj = new GameObject("ToastText");
            toastTxtObj.transform.SetParent(toastObj.transform, false);
            var toastTxtRect = toastTxtObj.AddComponent<RectTransform>();
            toastTxtRect.anchorMin = Vector2.zero;
            toastTxtRect.anchorMax = Vector2.one;

            toastText = toastTxtObj.AddComponent<Text>();
            toastText.font = defaultFont;
            toastText.fontSize = 22;
            toastText.fontStyle = FontStyle.Bold;
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.color = new Color(0.3f, 1f, 0.5f, 1f);
            toastText.text = "Log disalin!";

            toastObj.SetActive(false);
            windowPanel.SetActive(false); // Tersembunyi secara default
        }

        private GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color bgColor, Font font, int fontSize)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;

            var btn = btnObj.AddComponent<Button>();

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            return btnObj;
        }

        private Button CreateToolbarButton(Transform parent, string name, string label, Color bgColor, Font font)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;

            var btn = btnObj.AddComponent<Button>();

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            return btn;
        }
        #endregion
    }
}
