#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FantasyDefense.Analytics;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.GridSystem;
using FantasyDefense.Metagame;
using FantasyDefense.Mobile;
using FantasyDefense.Summon;
using FantasyDefense.UI;
using FantasyDefense.Wave;

namespace Rampartbound.Editor
{
    /// <summary>
    /// Master Scene Setup & Generator untuk Rampartbound: Merge Defense (Unity 6 / 2022.3 LTS+).
    /// Menginisialisasi arsitektur permainan lengkap secara otomatis dalam 1 klik:
    /// 1. Layar Judul (Title Screen & Splash)
    /// 2. Pusat Benteng (Citadel Lobby) lengkap dengan saldo komoditas live, display Kuartet Pahlawan, dan Dermaga Navigasi Bawah
    /// 3. Seluruh Sub-Panel Metagame: World Map Stages, Hero Upgrade, Gacha Summon Hall, IAP Store, Relic Forge, Talents, Quests, Guild, Battle Pass, Event Arena, Artifacts, Settings, Endless Trial
    /// 4. Arena Tempur (Battle Arena) 2D Grid, Barikade Benteng, Waypoints, Monster Waves, Dual Battle Mode HUD, dan GameFlowManager orchestrator.
    /// </summary>
    public static class RampartboundSceneSetup
    {
        private const string SCENE_DIR = "Assets/Scenes";
        private const string SCENE_PATH = "Assets/Scenes/RampartboundMainScene.unity";

        [MenuItem("Rampartbound/1-Click Setup Arena Scene", false, 1)]
        public static void SetupArenaScene()
        {
            Debug.Log("[Rampartbound] Memulai inisialisasi alur penuh game Rampartbound: Merge Defense...");

            // 0. Kunci Orientasi Layar ke Portrait (Standar Mobile & Tablet Anti-Stretch)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Muat aset sprite UI modular (Drop-in Ready)
            LoadUISprites();

            // 1. Buat Scene Baru atau buka scene yang ada untuk diisi
            Scene scene;
            if (File.Exists(SCENE_PATH))
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            // Bersihkan objek scene lama kecuali kamera dan global light untuk menghindari duplikasi
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                if (root.name != "Main Camera" && !root.name.Contains("Light"))
                {
                    Object.DestroyImmediate(root);
                }
            }

            // 2. Kamera Utama Mobile & Adapter Sentuhan
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.4f;
            mainCam.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.transform.position = new Vector3(0, -0.45f, -10f);

            var camAdapter = mainCam.GetComponent<MobileCameraAdapter>();
            if (camAdapter == null) camAdapter = mainCam.gameObject.AddComponent<MobileCameraAdapter>();
            var camSO = new SerializedObject(camAdapter);
            var vOffsetProp = camSO.FindProperty("verticalCenterOffset");
            if (vOffsetProp != null) vOffsetProp.floatValue = -0.45f;
            camSO.ApplyModifiedProperties();

            var raycaster = mainCam.GetComponent<Physics2DRaycaster>();
            if (raycaster == null) mainCam.gameObject.AddComponent<Physics2DRaycaster>();

            // 3. Pastikan EventSystem untuk UI Input Mobile (Dukungan Penuh Unity 6 & Input System Modern)
            var eventSys = Object.FindAnyObjectByType<EventSystem>();
            if (eventSys == null)
            {
                var eventSysObj = new GameObject("EventSystem", typeof(EventSystem));
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    eventSysObj.AddComponent(inputModuleType);
                }
                else
                {
                    eventSysObj.AddComponent<StandaloneInputModule>();
                }
            }
            else
            {
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null && eventSys.GetComponent(inputModuleType) == null)
                {
                    var oldMod = eventSys.GetComponent<StandaloneInputModule>();
                    if (oldMod != null) Object.DestroyImmediate(oldMod);
                    eventSys.gameObject.AddComponent(inputModuleType);
                }
            }

            if (eventSys == null) eventSys = Object.FindAnyObjectByType<EventSystem>();
            if (eventSys != null)
            {
                eventSys.pixelDragThreshold = 12; // Menjamin sentuhan jari mobile langsung responsif mendeteksi drag
            }

            // 4. Container Dunia Pertempuran 2D (ArenaWorldContainer)
            var arenaWorldObj = new GameObject("ArenaWorldContainer");
            arenaWorldObj.transform.position = Vector3.zero;

            // Papan Arena Tempur 5x5 (Battlefield Board Frame)
            var boardObj = new GameObject("BattlefieldBoard");
            boardObj.transform.SetParent(arenaWorldObj.transform);
            boardObj.transform.position = new Vector3(0f, -0.65f, 0.2f);
            var boardSr = boardObj.AddComponent<SpriteRenderer>();
            boardSr.sortingLayerName = "Board";
            boardSr.sortingOrder = 0;
            var boardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png");
            if (boardSprite != null)
            {
                boardSr.sprite = boardSprite;
                boardSr.drawMode = SpriteDrawMode.Sliced;
                boardSr.size = new Vector2(6.2f, 6.2f);
                boardSr.color = new Color(0.9f, 0.9f, 0.95f, 0.95f);
            }

            // Barisan Benteng Citadel (Castle Barricade Berpagar Kokoh & Visual Terintegrasi)
            var barricadeObj = new GameObject("CastleBarricade");
            barricadeObj.transform.SetParent(arenaWorldObj.transform);
            barricadeObj.transform.position = new Vector3(0, -3.5f, 0);
            var barricade = barricadeObj.AddComponent<CastleBarricade>();
            var barricadeSr = barricadeObj.AddComponent<SpriteRenderer>();
            var fenceSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Barricade_Fence.png");
            if (fenceSprite != null)
            {
                barricadeSr.sprite = fenceSprite;
                barricadeSr.sortingLayerName = "Board";
                barricadeSr.sortingOrder = 8;
                // At PPU 100, 512x128 sprite is 5.12 x 1.28 world units. Skala 1.2x = 6.14m width, sempurna memagari papan 6.2m.
                barricadeObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            }
            var barricadeCol = barricadeObj.AddComponent<BoxCollider2D>();
            barricadeCol.size = new Vector2(8f, 1.0f);
            barricadeCol.isTrigger = true;

            // Pondasi Apron Dinding Benteng Bawah (Citadel Foundation Rampart Apron)
            // Mengisi ruang kosong di bawah benteng hingga ke dasar layar mobile
            var apronObj = new GameObject("Citadel_Foundation_Apron");
            apronObj.transform.SetParent(barricadeObj.transform);
            apronObj.transform.localPosition = new Vector3(0f, -1.85f, 0.05f);
            var apronSr = apronObj.AddComponent<SpriteRenderer>();
            apronSr.sortingLayerName = "Board";
            apronSr.sortingOrder = 4;
            var rampartWallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/rampart_wall.png");
            if (rampartWallSprite == null) rampartWallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/palisade_wall.png");
            if (rampartWallSprite != null)
            {
                apronSr.sprite = rampartWallSprite;
                apronSr.drawMode = SpriteDrawMode.Sliced;
                apronSr.size = new Vector2(7.0f, 3.2f);
                apronSr.color = new Color(0.85f, 0.88f, 0.92f, 1f);
            }

            // Menara Pertahanan Kiri: Meriam Pelontar Roket Benteng (Bastion Cannon Turret)
            var cannonObj = new GameObject("Turret_Left_Cannon");
            cannonObj.transform.SetParent(barricadeObj.transform);
            cannonObj.transform.localPosition = new Vector3(-2.15f, 0.28f, 0);
            var cannonSr = cannonObj.AddComponent<SpriteRenderer>();
            var cannonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/bastion_cannon_left.png");
            if (cannonSprite == null) cannonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/cannon_turret.png");
            if (cannonSprite != null) cannonSr.sprite = cannonSprite;
            cannonSr.sortingLayerName = "Board";
            cannonSr.sortingOrder = 15;
            cannonObj.transform.localScale = Vector3.one * 0.70f;
            var cannonTurret = cannonObj.AddComponent<CitadelDefenseTurret>();
            cannonTurret.Initialize(CitadelTurretType.Cannon, cannonSprite);

            // Menara Pertahanan Kanan: Pemanah & Balista Sniper Benteng (Bastion Ballista / Archer Turret)
            var archerObj = new GameObject("Turret_Right_Archer");
            archerObj.transform.SetParent(barricadeObj.transform);
            archerObj.transform.localPosition = new Vector3(2.15f, 0.28f, 0);
            var archerSr = archerObj.AddComponent<SpriteRenderer>();
            var archerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/ballista_turret.png");
            if (archerSprite != null) archerSr.sprite = archerSprite;
            archerSr.sortingLayerName = "Board";
            archerSr.sortingOrder = 15;
            archerObj.transform.localScale = Vector3.one * 0.70f;
            var archerTurret = archerObj.AddComponent<CitadelDefenseTurret>();
            archerTurret.Initialize(CitadelTurretType.Archer, archerSprite);

            // Relik Inti Benteng Kerajaan (Citadel Arcane Core Relic)
            var coreObj = new GameObject("Citadel_Core_Relic");
            coreObj.transform.SetParent(barricadeObj.transform);
            coreObj.transform.localPosition = new Vector3(0f, -0.65f, 0);
            var coreSr = coreObj.AddComponent<SpriteRenderer>();
            var coreSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/arcane_rune_cube.png");
            if (coreSprite != null)
            {
                coreSr.sprite = coreSprite;
                coreSr.sortingLayerName = "Board";
                coreSr.sortingOrder = 9;
                coreObj.transform.localScale = Vector3.one * 0.48f;
            }

            // Waypoints Lintasan Monster Adaptif (MonsterPathManager)
            // Membiarkan MonsterPathManager menghitung lintasan U-shape perimeter di sisi luar petak Grid
            var pathObj = new GameObject("MonsterPathManager");
            pathObj.transform.SetParent(arenaWorldObj.transform);
            pathObj.AddComponent<MonsterPathManager>();

            // GridManager, GridLayout & GridInputHandler
            var gridObj = new GameObject("GridManager");
            gridObj.transform.SetParent(arenaWorldObj.transform);
            gridObj.transform.position = Vector3.zero;
            var gridLayout = gridObj.AddComponent<FantasyDefense.GridSystem.GridLayout>();
            var gridManager = gridObj.AddComponent<GridManager>();
            var inputHandler = gridObj.AddComponent<FantasyDefense.GridSystem.GridInputHandler>();
            var match3Solver = gridObj.AddComponent<FantasyDefense.GridSystem.GridMatch3Solver>();
            var hintManager = gridObj.AddComponent<FantasyDefense.GridSystem.GridMatchHintManager>();

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfigSO>("Assets/ScriptableObjects/Levels/SO_Level1_Config.asset");
            var emptyTileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Grid/Tile_Empty.png");
            var hoverTileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Grid/Tile_Hover.png");

            var gridSO = new SerializedObject(gridManager);
            if (levelConfig != null) gridSO.FindProperty("levelConfig").objectReferenceValue = levelConfig;
            if (emptyTileSprite != null) gridSO.FindProperty("defaultTileSprite").objectReferenceValue = emptyTileSprite;
            if (hoverTileSprite != null) gridSO.FindProperty("hoverTileSprite").objectReferenceValue = hoverTileSprite;
            gridSO.FindProperty("gridLayout").objectReferenceValue = gridLayout;
            gridSO.FindProperty("boardRenderer").objectReferenceValue = boardSr;
            gridSO.ApplyModifiedProperties();

            // HeroSummoner & Roster Pahlawan Kuartet
            var summonerObj = new GameObject("HeroSummoner");
            summonerObj.transform.SetParent(arenaWorldObj.transform);
            var summoner = summonerObj.AddComponent<HeroSummoner>();
            var summonerSO = new SerializedObject(summoner);
            var activePoolProp = summonerSO.FindProperty("activeHeroPool");

            var heroGuids = AssetDatabase.FindAssets("t:UnitDataSO", new[] { "Assets/ScriptableObjects/Units" });
            var heroList = new List<UnitDataSO>();
            for (int i = 0; i < heroGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(heroGuids[i]);
                var unitSO = AssetDatabase.LoadAssetAtPath<UnitDataSO>(path);
                if (unitSO != null) heroList.Add(unitSO);
            }
            if (heroList.Count == 0)
            {
                string[] heroPaths = new string[]
                {
                    "Assets/ScriptableObjects/Units/SO_Cedric.asset",
                    "Assets/ScriptableObjects/Units/SO_RoseTanker.asset",
                    "Assets/ScriptableObjects/Units/SO_RabbitMage.asset",
                    "Assets/ScriptableObjects/Units/SO_WindRanger.asset"
                };
                foreach (var p in heroPaths)
                {
                    var unitSO = AssetDatabase.LoadAssetAtPath<UnitDataSO>(p);
                    if (unitSO != null && !heroList.Contains(unitSO)) heroList.Add(unitSO);
                }
            }
            heroList.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            Debug.Log($"[RampartboundSceneSetup] Berhasil mendaftarkan {heroList.Count} pahlawan ke HeroSummoner.");

            if (activePoolProp != null)
            {
                activePoolProp.arraySize = heroList.Count;
                for (int i = 0; i < heroList.Count; i++)
                {
                    activePoolProp.GetArrayElementAtIndex(i).objectReferenceValue = heroList[i];
                }
                summonerSO.ApplyModifiedProperties();
            }

            // WaveSpawner
            var spawnerObj = new GameObject("WaveSpawner");
            spawnerObj.transform.SetParent(arenaWorldObj.transform);
            var waveSpawner = spawnerObj.AddComponent<WaveSpawner>();
            var spawnerSO = new SerializedObject(waveSpawner);
            var wavesProp = spawnerSO.FindProperty("waveList");
            if (wavesProp != null)
            {
                var waveGuids = AssetDatabase.FindAssets("t:WaveDataSO", new[] { "Assets/ScriptableObjects/Waves" });
                var waveList = new List<WaveDataSO>();
                for (int i = 0; i < waveGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(waveGuids[i]);
                    var wSO = AssetDatabase.LoadAssetAtPath<WaveDataSO>(path);
                    if (wSO != null) waveList.Add(wSO);
                }
                waveList.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

                wavesProp.arraySize = waveList.Count;
                for (int i = 0; i < waveList.Count; i++)
                {
                    wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = waveList[i];
                }
                spawnerSO.ApplyModifiedProperties();
            }

            // Awal permainan: Dunia arena di-nonaktifkan sampai pertempuran dimulai
            arenaWorldObj.SetActive(false);

            // 5. Singleton Pendukung Gameplay
            var economyObj = new GameObject("EconomyManager");
            economyObj.AddComponent<EconomyManager>();

            var inputObj = new GameObject("MobileTouchManager");
            inputObj.AddComponent<MobileTouchManager>();

            var audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();

            var vfxObj = new GameObject("SkillVfxManager");
            vfxObj.AddComponent<SkillVfxManager>();

            var weatherObj = new GameObject("BattlefieldWeatherManager");
            weatherObj.AddComponent<BattlefieldWeatherManager>();

            var autoMergeObj = new GameObject("AutoMergeAssistant");
            autoMergeObj.AddComponent<AutoMergeAssistant>();

            var perfObj = new GameObject("DevicePerformanceManager");
            perfObj.AddComponent<FantasyDefense.Mobile.DevicePerformanceManager>();

            // 6. Metagame & Live-Ops Systems
            var metaObj = new GameObject("MetagameControllers");
            var flowManager = metaObj.AddComponent<GameFlowManager>();
            var lobbyManager = metaObj.AddComponent<CitadelLobbyManager>();
            var stageManager = metaObj.AddComponent<StageManager>();
            var summonManager = metaObj.AddComponent<CitadelSummonManager>();
            var questManager = metaObj.AddComponent<QuestManager>();
            var endlessTrialManager = metaObj.AddComponent<EndlessTrialManager>();
            var relicForgeManager = metaObj.AddComponent<RelicForgeManager>();
            var talentManager = metaObj.AddComponent<CitadelTalentManager>();
            var artifactManager = metaObj.AddComponent<ArtifactManager>();
            var guildManager = metaObj.AddComponent<GuildManager>();
            var battlePassManager = metaObj.AddComponent<BattlePassManager>();
            var storeManager = metaObj.AddComponent<CitadelStoreManager>();
            var billingService = metaObj.AddComponent<FantasyDefense.Billing.CitadelBillingService>();
            metaObj.AddComponent<BattleBlessingManager>();
            var eventManager = metaObj.AddComponent<EventChallengeManager>();
            var tutorialManager = metaObj.AddComponent<TutorialGuideManager>();
            metaObj.AddComponent<CitadelNotificationService>();
            metaObj.AddComponent<CitadelTelemetryManager>();
            metaObj.AddComponent<FantasyDefense.Save.CloudSaveSyncService>();
            metaObj.AddComponent<FantasyDefense.Localization.CitadelLocalizationManager>();
            metaObj.AddComponent<FantasyDefense.Localization.CitadelDynamicLocalizer>();
            metaObj.AddComponent<CitadelToastNotice>();
            metaObj.AddComponent<CitadelInGameLogViewer>();

            // 7. GameManager Master Loop
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            var gmSO = new SerializedObject(gm);
            if (levelConfig != null) gmSO.FindProperty("activeLevelConfig").objectReferenceValue = levelConfig;
            var heroPoolProp = gmSO.FindProperty("starterHeroPool");
            // Only the approved opening trio should be free starter heroes.
            // Keep the wider activeHeroPool above intact for combat/gacha behavior.
            var starterHeroList = heroList.FindAll(hero => hero != null &&
                (hero.unitType == UnitType.Cedric ||
                 hero.unitType == UnitType.RoseTanker ||
                 hero.unitType == UnitType.RabbitMage));
            if (heroPoolProp != null)
            {
                heroPoolProp.arraySize = starterHeroList.Count;
                for (int i = 0; i < starterHeroList.Count; i++)
                {
                    heroPoolProp.GetArrayElementAtIndex(i).objectReferenceValue = starterHeroList[i];
                }
            }
            var autoStartProp = gmSO.FindProperty("autoStartBattleOnAwake");
            if (autoStartProp != null) autoStartProp.boolValue = false;
            gmSO.ApplyModifiedProperties();

            // 8. Master UI Canvas & Font
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasObj = new GameObject("UIBattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0.5f;

            // ==========================================
            // A. TITLE SCREEN PANEL (Aktif Pertama Kali)
            // ==========================================
            LoadUISprites();
            var titlePanelObj = CreateFullScreenPanel(canvasObj.transform, "TitleScreenPanel", new Color(0.04f, 0.06f, 0.1f, 1f));
            var titleImg = titlePanelObj.GetComponent<Image>();
            if (_titleBackdrop != null)
            {
                titleImg.sprite = _titleBackdrop;
                titleImg.color = Color.white;
            }
            var titleView = titlePanelObj.AddComponent<UITitleScreenView>();

            // Official Title Logo (Sprite Gambar 3D Megah)
            var logoObj = new GameObject("Title_Game_Logo_Img", typeof(RectTransform), typeof(Image));
            logoObj.transform.SetParent(titlePanelObj.transform, false);
            var logoRt = logoObj.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.05f, 0.65f);
            logoRt.anchorMax = new Vector2(0.95f, 0.90f);
            logoRt.offsetMin = Vector2.zero;
            logoRt.offsetMax = Vector2.zero;
            var logoImg = logoObj.GetComponent<Image>();
            if (_titleGameLogo != null)
            {
                logoImg.sprite = _titleGameLogo;
                logoImg.preserveAspect = true;
            }
            logoImg.raycastTarget = false;

            var gameTitleTxt = CreateText(titlePanelObj.transform, "GameTitleText", "RAMPARTBOUND\nMERGE DEFENSE", 58, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f), defaultFont);
            var titleRect = gameTitleTxt.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.65f);
            titleRect.anchorMax = new Vector2(0.9f, 0.88f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            if (_titleGameLogo != null) gameTitleTxt.gameObject.SetActive(false); // Sembunyikan teks polos jika sprite logo ada

            var subtitleTxt = CreateText(titlePanelObj.transform, "SubtitleText", "Pertahankan Gerbang Kerajaan dari Serbuan Abyssal", 28, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f), defaultFont);
            var subRect = subtitleTxt.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.05f, 0.58f);
            subRect.anchorMax = new Vector2(0.95f, 0.64f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            if (_titleGameLogo != null) subtitleTxt.gameObject.SetActive(false); // Sembunyikan jika sprite logo sudah memiliki banner 'MERGE DEFENSE'

            // Start Button: 3D Button Timbul Tanpa Frame Kosong & Anti-Teks Bertumpuk
            // Menggunakan warna teks Putih Bersih Tebal (High-Readability) dengan Outline Gelap Tajam
            var startBtnObj = CreateButton(titlePanelObj.transform, "EnterCitadelButton", "🏰 TAP TO START", Color.white, Color.white, 38, defaultFont);
            var startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.12f, 0.12f);
            startBtnRect.anchorMax = new Vector2(0.88f, 0.22f);
            startBtnRect.offsetMin = Vector2.zero;
            startBtnRect.offsetMax = Vector2.zero;
            if (_titleBtnTap != null)
            {
                var btnImg = startBtnObj.GetComponent<Image>();
                btnImg.sprite = _titleBtnTap;
                btnImg.type = Image.Type.Sliced;
            }

            // Tambahkan Outline dan Shadow pada teks tombol agar terbaca sangat tajam dan kontras
            var btnTxt = startBtnObj.GetComponentInChildren<Text>();
            if (btnTxt != null)
            {
                btnTxt.color = new Color(1f, 0.98f, 0.88f); // Warm ivory gold
                btnTxt.fontStyle = FontStyle.Bold;
                var outline = btnTxt.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.25f, 0.12f, 0.02f, 0.95f); // Dark bronze outline
                outline.effectDistance = new Vector2(2.5f, -2.5f);
                var shadow = btnTxt.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                shadow.effectDistance = new Vector2(3f, -3f);
            }

            var pulseTxt = CreateText(titlePanelObj.transform, "PulsingHintText", "Ketuk Layar untuk Memulai Pertahanan", 24, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f), defaultFont);
            var pulseRect = pulseTxt.GetComponent<RectTransform>();
            pulseRect.anchorMin = new Vector2(0.1f, 0.06f);
            pulseRect.anchorMax = new Vector2(0.9f, 0.11f);
            pulseRect.offsetMin = Vector2.zero;
            pulseRect.offsetMax = Vector2.zero;

            // Dual Heroes Title Screen (Pippin di kiri, Cedric di kanan menyambut berdua)
            // A. Pippin Container & Plinth
            var pippinGroup = new GameObject("Title_Hero_Pippin_Group", typeof(RectTransform));
            pippinGroup.transform.SetParent(titlePanelObj.transform, false);
            var pippinGroupRt = pippinGroup.GetComponent<RectTransform>();
            pippinGroupRt.anchorMin = new Vector2(0.04f, 0.23f);
            pippinGroupRt.anchorMax = new Vector2(0.48f, 0.60f);
            pippinGroupRt.offsetMin = Vector2.zero;
            pippinGroupRt.offsetMax = Vector2.zero;

            // Pippin Plinth Base
            var pippinPlinthObj = new GameObject("Pippin_Plinth_Base", typeof(RectTransform), typeof(Image));
            pippinPlinthObj.transform.SetParent(pippinGroup.transform, false);
            var pippinPlinthRt = pippinPlinthObj.GetComponent<RectTransform>();
            pippinPlinthRt.anchorMin = new Vector2(0.08f, 0f);
            pippinPlinthRt.anchorMax = new Vector2(0.92f, 0.26f);
            pippinPlinthRt.offsetMin = Vector2.zero;
            pippinPlinthRt.offsetMax = Vector2.zero;
            var pippinPlinthImg = pippinPlinthObj.GetComponent<Image>();
            if (_plinthT1 != null) { pippinPlinthImg.sprite = _plinthT1; pippinPlinthImg.preserveAspect = true; }
            pippinPlinthImg.raycastTarget = false;

            // Pippin Sprite
            var pippinSpriteObj = new GameObject("Pippin_Sprite_Img", typeof(RectTransform), typeof(Image));
            pippinSpriteObj.transform.SetParent(pippinGroup.transform, false);
            var pippinSpriteRt = pippinSpriteObj.GetComponent<RectTransform>();
            pippinSpriteRt.anchorMin = new Vector2(0.05f, 0.12f);
            pippinSpriteRt.anchorMax = new Vector2(0.95f, 1f);
            pippinSpriteRt.offsetMin = Vector2.zero;
            pippinSpriteRt.offsetMax = Vector2.zero;
            var pippinImg = pippinSpriteObj.GetComponent<Image>();
            if (_heroRabbitSprite != null) { pippinImg.sprite = _heroRabbitSprite; pippinImg.preserveAspect = true; }
            pippinImg.raycastTarget = false;

            // B. Cedric Container & Plinth
            var cedricGroup = new GameObject("Title_Hero_Cedric_Group", typeof(RectTransform));
            cedricGroup.transform.SetParent(titlePanelObj.transform, false);
            var cedricGroupRt = cedricGroup.GetComponent<RectTransform>();
            cedricGroupRt.anchorMin = new Vector2(0.52f, 0.23f);
            cedricGroupRt.anchorMax = new Vector2(0.96f, 0.60f);
            cedricGroupRt.offsetMin = Vector2.zero;
            cedricGroupRt.offsetMax = Vector2.zero;

            // Cedric Plinth Base
            var cedricPlinthObj = new GameObject("Cedric_Plinth_Base", typeof(RectTransform), typeof(Image));
            cedricPlinthObj.transform.SetParent(cedricGroup.transform, false);
            var cedricPlinthRt = cedricPlinthObj.GetComponent<RectTransform>();
            cedricPlinthRt.anchorMin = new Vector2(0.08f, 0f);
            cedricPlinthRt.anchorMax = new Vector2(0.92f, 0.26f);
            cedricPlinthRt.offsetMin = Vector2.zero;
            cedricPlinthRt.offsetMax = Vector2.zero;
            var cedricPlinthImg = cedricPlinthObj.GetComponent<Image>();
            if (_plinthT1 != null) { cedricPlinthImg.sprite = _plinthT1; cedricPlinthImg.preserveAspect = true; }
            cedricPlinthImg.raycastTarget = false;

            // Cedric Sprite (Dibalik horizontal scale X = -1 agar menghadap ke kiri, menyambut dan berhadapan dengan Pippin!)
            var cedricSpriteObj = new GameObject("Cedric_Sprite_Img", typeof(RectTransform), typeof(Image));
            cedricSpriteObj.transform.SetParent(cedricGroup.transform, false);
            var cedricSpriteRt = cedricSpriteObj.GetComponent<RectTransform>();
            cedricSpriteRt.anchorMin = new Vector2(0.05f, 0.12f);
            cedricSpriteRt.anchorMax = new Vector2(0.95f, 1f);
            cedricSpriteRt.offsetMin = Vector2.zero;
            cedricSpriteRt.offsetMax = Vector2.zero;
            cedricSpriteRt.localScale = new Vector3(-1f, 1f, 1f); // Mirror horizontal agar saling berhadapan!
            var cedricImg = cedricSpriteObj.GetComponent<Image>();
            if (_heroCedricSprite != null) { cedricImg.sprite = _heroCedricSprite; cedricImg.preserveAspect = true; }
            cedricImg.raycastTarget = false;

            // Layer Kursor Tangan Interaktif (Draggable & Idle Tapping)
            var handObj = new GameObject("Interactive_Hand_Cursor", typeof(RectTransform), typeof(Image));
            handObj.transform.SetParent(titlePanelObj.transform, false);
            var handRt = handObj.GetComponent<RectTransform>();
            handRt.anchorMin = new Vector2(0.55f, 0.22f);
            handRt.anchorMax = new Vector2(0.68f, 0.32f);
            handRt.offsetMin = Vector2.zero;
            handRt.offsetMax = Vector2.zero;
            var handImg = handObj.GetComponent<Image>();
            if (_handCursor != null)
            {
                handImg.sprite = _handCursor;
                handImg.preserveAspect = true;
            }
            handImg.raycastTarget = false;
            var handShadow = handObj.AddComponent<Shadow>();
            handShadow.effectColor = new Color(0, 0, 0, 0.75f);
            handShadow.effectDistance = new Vector2(3, -3);

            var versionTxt = CreateText(titlePanelObj.transform, "VersionText", "Versi 1.2.11 (Google Play Edition)", 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f), defaultFont);
            var verRect = versionTxt.GetComponent<RectTransform>();
            verRect.anchorMin = new Vector2(0.1f, 0.04f);
            verRect.anchorMax = new Vector2(0.9f, 0.08f);
            verRect.offsetMin = Vector2.zero;
            verRect.offsetMax = Vector2.zero;

            var titleSO = new SerializedObject(titleView);
            titleSO.FindProperty("titleScreenPanel").objectReferenceValue = titlePanelObj;
            titleSO.FindProperty("titleBackgroundImage").objectReferenceValue = titleImg;
            titleSO.FindProperty("gameTitleLogoImage").objectReferenceValue = logoImg;
            titleSO.FindProperty("gameTitleText").objectReferenceValue = gameTitleTxt;
            titleSO.FindProperty("subtitleText").objectReferenceValue = subtitleTxt;
            titleSO.FindProperty("versionText").objectReferenceValue = versionTxt;
            titleSO.FindProperty("pulsingHintText").objectReferenceValue = pulseTxt;
            titleSO.FindProperty("enterCitadelButton").objectReferenceValue = startBtnObj.GetComponent<Button>();
            titleSO.FindProperty("enterCitadelButtonText").objectReferenceValue = startBtnObj.GetComponentInChildren<Text>();
            titleSO.FindProperty("handCursorRt").objectReferenceValue = handRt;
            titleSO.FindProperty("handCursorImage").objectReferenceValue = handImg;
            if (_handCursor != null) titleSO.FindProperty("handCursorSprite").objectReferenceValue = _handCursor;
            titleSO.FindProperty("pippinHeroImage").objectReferenceValue = pippinImg;
            titleSO.FindProperty("cedricHeroImage").objectReferenceValue = cedricImg;
            titleSO.FindProperty("pippinHeroRt").objectReferenceValue = pippinSpriteRt;
            titleSO.FindProperty("cedricHeroRt").objectReferenceValue = cedricSpriteRt;
            titleSO.ApplyModifiedProperties();

            // ==========================================
            // B. CITADEL LOBBY PANEL (Pusat Metagame Hub)
            // ==========================================
            var lobbyPanelObj = CreateFullScreenPanel(canvasObj.transform, "CitadelLobbyPanel", new Color(0.06f, 0.08f, 0.13f, 1f));
            if (lobbyPanelObj.GetComponent<SafeAreaFitter>() == null) lobbyPanelObj.AddComponent<SafeAreaFitter>();
            var lobbyImg = lobbyPanelObj.GetComponent<Image>();
            if (_lobbyBackdrop != null)
            {
                lobbyImg.sprite = _lobbyBackdrop;
                lobbyImg.color = Color.white;
            }
            var lobbyView = lobbyPanelObj.AddComponent<UICitadelLobbyView>();

            // Top Header Panel
            var lobbyHeaderObj = CreatePanel(lobbyPanelObj.transform, "HeaderPanel", new Color(0.09f, 0.12f, 0.2f, 0.95f));
            var headerRect = lobbyHeaderObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.88f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            var cmdNameTxt = CreateText(lobbyHeaderObj.transform, "CommanderName", "Komandan Benteng", 28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(cmdNameTxt.gameObject, 0.04f, 0.55f, 0.35f, 0.9f);

            var lvlTxt = CreateText(lobbyHeaderObj.transform, "CitadelLevel", "Citadel Lv. 1", 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0.2f), defaultFont);
            PositionUI(lvlTxt.gameObject, 0.04f, 0.12f, 0.35f, 0.48f);

            Text energyTxt;
            var energyPill = CreateResourcePill(lobbyHeaderObj.transform, "EnergyPill", _iconEnergyCyan, "30 / 30", new Color(0.3f, 0.9f, 1f), ResourceCommodityType.Energy, defaultFont, out energyTxt);
            PositionUI(energyPill, 0.36f, 0.52f, 0.62f, 0.92f);
            CreateNotificationBadge(energyPill.transform, NotificationBadgeType.EnergyFull);

            Text goldTxt;
            var goldPill = CreateResourcePill(lobbyHeaderObj.transform, "GoldPill", _iconGoldCoin, "1,000", new Color(1f, 0.85f, 0.2f), ResourceCommodityType.Gold, defaultFont, out goldTxt);
            PositionUI(goldPill, 0.36f, 0.08f, 0.62f, 0.48f);

            Text gemsTxt;
            var gemsPill = CreateResourcePill(lobbyHeaderObj.transform, "GemsPill", _iconGemBlue, "100", new Color(0.35f, 0.95f, 0.8f), ResourceCommodityType.Gems, defaultFont, out gemsTxt);
            PositionUI(gemsPill, 0.64f, 0.52f, 0.88f, 0.92f);

            var cpTxt = CreateText(lobbyHeaderObj.transform, "TeamCpText", "CP: 500", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.45f, 0.45f), defaultFont);
            PositionUI(cpTxt.gameObject, 0.64f, 0.08f, 0.88f, 0.48f);

            var settingsBtn = CreateButton(lobbyHeaderObj.transform, "SettingsButton", "⚙️", new Color(0.2f, 0.25f, 0.35f), Color.white, 28, defaultFont);
            PositionUI(settingsBtn, 0.9f, 0.25f, 0.98f, 0.75f);

            // Hero Courtyard Assembly (Mendukung Formasi hingga 4 Hero Standee Sesuai Bab 4.2)
            var heroesContainer = CreatePanel(lobbyPanelObj.transform, "HeroCourtyardAssembly", new Color(0.08f, 0.1f, 0.16f, 0.45f));
            if (_frameStoneBastion != null)
            {
                var hImg = heroesContainer.GetComponent<Image>();
                hImg.sprite = _frameStoneBastion;
                hImg.type = Image.Type.Sliced;
                hImg.color = new Color(1f, 1f, 1f, 0.75f);
            }
            PositionUI(heroesContainer, 0.03f, 0.28f, 0.97f, 0.85f);

            // Formasi 4 Slot: Pippin (Slot 1), Cedric (Slot 2/Center), Rose (Slot 3), Wind Ranger / Locked (Slot 4)
            var rabbitCard = CreateButton(heroesContainer.transform, "RabbitCard", "<b>Pippin</b>\nLv.12 ★★★\n<color=#FFD700>CP: 1,600</color>", new Color(0.12f, 0.2f, 0.38f, 0.85f), Color.white, 18, defaultFont);
            PositionUI(rabbitCard, 0.015f, 0.04f, 0.25f, 0.96f);
            AttachRarityBorder(rabbitCard.transform, _borderEpic);
            SetupHeroCardPortrait(rabbitCard, _heroRabbitSprite);

            var cedricCard = CreateButton(heroesContainer.transform, "CedricCard", "<b>Cedric</b>\nLv.15 ★★★\n<color=#FFD700>CP: 1,850</color>", new Color(0.38f, 0.12f, 0.12f, 0.85f), Color.white, 18, defaultFont);
            PositionUI(cedricCard, 0.26f, 0.04f, 0.495f, 0.96f);
            AttachRarityBorder(cedricCard.transform, _borderLegendary ?? _borderRare);
            SetupHeroCardPortrait(cedricCard, _heroCedricSprite);

            var roseCard = CreateButton(heroesContainer.transform, "RoseCard", "<b>Rose Tank</b>\nLv.14 ★★★\n<color=#FFD700>CP: 1,400</color>", new Color(0.38f, 0.15f, 0.28f, 0.85f), Color.white, 18, defaultFont);
            PositionUI(roseCard, 0.505f, 0.04f, 0.74f, 0.96f);
            AttachRarityBorder(roseCard.transform, _borderRare);
            SetupHeroCardPortrait(roseCard, _heroRoseSprite);

            // Slot Ke-4 (Mendukung slot ekspansi hingga 4 hero)
            var windCard = CreateButton(heroesContainer.transform, "WindCard", "<b>[SLOT 4]</b>\n<color=#FFD700>🔒 Gacha to Unlock</color>", new Color(0.12f, 0.15f, 0.22f, 0.8f), Color.white, 17, defaultFont);
            PositionUI(windCard, 0.75f, 0.04f, 0.985f, 0.96f);
            AttachRarityBorder(windCard.transform, _borderCommon ?? _borderRare);
            SetupHeroCardPortrait(windCard, _heroWindSprite);

            // Core Action Buttons (Tombol Besar Terpusat)
            var defendBtn = CreateButton(lobbyPanelObj.transform, "DefendCitadelButton", "⚔️ BERTAHAN (5 ⚡)", new Color(0.95f, 0.38f, 0.15f), Color.white, 30, defaultFont);
            PositionUI(defendBtn, 0.08f, 0.14f, 0.92f, 0.25f);

            var trialBtn = CreateButton(lobbyPanelObj.transform, "AbyssalTrialButton", "🌌 UJIAN ABYSSAL (ENDLESS)", new Color(0.42f, 0.25f, 0.75f), Color.white, 24, defaultFont);
            PositionUI(trialBtn, 0.12f, 0.23f, 0.88f, 0.30f);
            trialBtn.SetActive(false); // Sembunyikan Abyssal Trial sesuai arahan

            // Mid Quick Actions Bar (6 Feature Shortcuts)
            var quickBarObj = CreatePanel(lobbyPanelObj.transform, "QuickActionsBar", new Color(0.06f, 0.09f, 0.15f, 0.9f));
            PositionUI(quickBarObj, 0.02f, 0.125f, 0.98f, 0.215f);

            var navTalentBtn = CreateButton(quickBarObj.transform, "NavTalents", "✨\nTalenta", new Color(0.18f, 0.25f, 0.38f), Color.white, 16, defaultFont);
            PositionUI(navTalentBtn, 0.01f, 0.08f, 0.16f, 0.92f);

            var navQuestBtn = CreateButton(quickBarObj.transform, "NavQuests", "📜\nMisi", new Color(0.18f, 0.25f, 0.38f), Color.white, 16, defaultFont);
            PositionUI(navQuestBtn, 0.175f, 0.08f, 0.325f, 0.92f);
            CreateNotificationBadge(navQuestBtn.transform, NotificationBadgeType.QuestsUnclaimed);

            var navGuildBtn = CreateButton(quickBarObj.transform, "NavGuild", "🏰\nGuild", new Color(0.18f, 0.25f, 0.38f), Color.white, 16, defaultFont);
            PositionUI(navGuildBtn, 0.34f, 0.08f, 0.49f, 0.92f);

            var navPassBtn = CreateButton(quickBarObj.transform, "NavBattlePass", "🎖️\nPass", new Color(0.18f, 0.25f, 0.38f), Color.white, 16, defaultFont);
            PositionUI(navPassBtn, 0.505f, 0.08f, 0.655f, 0.92f);

            var navEventBtn = CreateButton(quickBarObj.transform, "NavEventArena", "🔥\nEvent", new Color(0.18f, 0.25f, 0.38f), Color.white, 16, defaultFont);
            PositionUI(navEventBtn, 0.67f, 0.08f, 0.82f, 0.92f);

            var navArtifactBtn = CreateButton(quickBarObj.transform, "NavArtifacts", "👑\nArtefak", new Color(0.18f, 0.25f, 0.38f), Color.white, 16, defaultFont);
            PositionUI(navArtifactBtn, 0.835f, 0.08f, 0.99f, 0.92f);

            quickBarObj.SetActive(false); // Sembunyikan talenta, misi, guild, pass, event, artefak

            // Bottom Navigation Dock (Dermaga Navigasi Bawah Responsif)
            var dockObj = CreatePanel(lobbyPanelObj.transform, "BottomNavigationDock", new Color(0.05f, 0.07f, 0.12f, 0.98f));
            PositionUI(dockObj, 0f, 0f, 1f, 0.12f);

            // Pasang HorizontalLayoutGroup agar tombol secara dinamis dan simetris mengisi 100% lebar layar
            var dockHlg = dockObj.AddComponent<HorizontalLayoutGroup>();
            dockHlg.childAlignment = TextAnchor.MiddleCenter;
            dockHlg.childControlWidth = true;
            dockHlg.childControlHeight = true;
            dockHlg.childForceExpandWidth = true;
            dockHlg.childForceExpandHeight = true;
            dockHlg.spacing = 12f;
            dockHlg.padding = new RectOffset(16, 16, 8, 8);

            var navStageBtn = CreateButton(dockObj.transform, "NavStageMap", "🗺️\nPeta", new Color(0.15f, 0.2f, 0.3f), Color.white, 20, defaultFont);
            var navHeroBtn = CreateButton(dockObj.transform, "NavHeroes", "🛡️\nHero", new Color(0.15f, 0.2f, 0.3f), Color.white, 20, defaultFont);
            var navSummonBtn = CreateButton(dockObj.transform, "NavSummon", "🔮\nGacha", new Color(0.15f, 0.2f, 0.3f), Color.white, 20, defaultFont);
            CreateNotificationBadge(navSummonBtn.transform, NotificationBadgeType.DailySummonFree);

            var navForgeBtn = CreateButton(dockObj.transform, "NavForge", "🔨\nTempa", new Color(0.15f, 0.2f, 0.3f), Color.white, 20, defaultFont);
            navForgeBtn.SetActive(false); // Sembunyikan Tempa (Forge) sesuai instruksi, sehingga 4 tombol tersisa otomatis merenggang penuh!

            var navStoreBtn = CreateButton(dockObj.transform, "NavStore", "💎\nToko", new Color(0.2f, 0.35f, 0.25f), new Color(1f, 0.9f, 0.3f), 20, defaultFont);

            // Bind serialized properties ke UICitadelLobbyView
            var lobbySO = new SerializedObject(lobbyView);
            lobbySO.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanelObj;
            lobbySO.FindProperty("commanderNameText").objectReferenceValue = cmdNameTxt;
            lobbySO.FindProperty("citadelLevelText").objectReferenceValue = lvlTxt;
            lobbySO.FindProperty("energyText").objectReferenceValue = energyTxt;
            lobbySO.FindProperty("goldText").objectReferenceValue = goldTxt;
            lobbySO.FindProperty("gemsText").objectReferenceValue = gemsTxt;
            lobbySO.FindProperty("teamCpText").objectReferenceValue = cpTxt;
            lobbySO.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
            lobbySO.FindProperty("rabbitCardButton").objectReferenceValue = rabbitCard.GetComponent<Button>();
            lobbySO.FindProperty("rabbitInfoText").objectReferenceValue = rabbitCard.GetComponentInChildren<Text>();
            lobbySO.FindProperty("cedricCardButton").objectReferenceValue = cedricCard.GetComponent<Button>();
            lobbySO.FindProperty("cedricInfoText").objectReferenceValue = cedricCard.GetComponentInChildren<Text>();
            lobbySO.FindProperty("roseCardButton").objectReferenceValue = roseCard.GetComponent<Button>();
            lobbySO.FindProperty("roseInfoText").objectReferenceValue = roseCard.GetComponentInChildren<Text>();
            lobbySO.FindProperty("windRangerCardButton").objectReferenceValue = windCard.GetComponent<Button>();
            lobbySO.FindProperty("windRangerInfoText").objectReferenceValue = windCard.GetComponentInChildren<Text>();
            lobbySO.FindProperty("defendCitadelButton").objectReferenceValue = defendBtn.GetComponent<Button>();
            lobbySO.FindProperty("abyssalTrialButton").objectReferenceValue = trialBtn.GetComponent<Button>();
            var prevProp = lobbySO.FindProperty("featurePreviewButton");
            if (prevProp != null) prevProp.objectReferenceValue = null;

            // 11 Navigation Buttons
            lobbySO.FindProperty("navStageMapButton").objectReferenceValue = navStageBtn.GetComponent<Button>();
            lobbySO.FindProperty("navHeroesButton").objectReferenceValue = navHeroBtn.GetComponent<Button>();
            lobbySO.FindProperty("navSummonButton").objectReferenceValue = navSummonBtn.GetComponent<Button>();
            lobbySO.FindProperty("navForgeButton").objectReferenceValue = navForgeBtn.GetComponent<Button>();
            lobbySO.FindProperty("navStoreButton").objectReferenceValue = navStoreBtn.GetComponent<Button>();
            lobbySO.FindProperty("navTalentsButton").objectReferenceValue = navTalentBtn.GetComponent<Button>();
            lobbySO.FindProperty("navQuestsButton").objectReferenceValue = navQuestBtn.GetComponent<Button>();
            lobbySO.FindProperty("navGuildButton").objectReferenceValue = navGuildBtn.GetComponent<Button>();
            lobbySO.FindProperty("navBattlePassButton").objectReferenceValue = navPassBtn.GetComponent<Button>();
            lobbySO.FindProperty("navEventArenaButton").objectReferenceValue = navEventBtn.GetComponent<Button>();
            lobbySO.FindProperty("navArtifactsButton").objectReferenceValue = navArtifactBtn.GetComponent<Button>();
            lobbySO.ApplyModifiedProperties();

            lobbyPanelObj.SetActive(false);

            // ==========================================
            // C. BATTLE HUD PANEL (Antarmuka Tempur Arena)
            // ==========================================
            var battleHudObj = CreateFullScreenPanel(canvasObj.transform, "BattleHUDPanel", new Color(0, 0, 0, 0));
            var battleHudImg = battleHudObj.GetComponent<Image>();
            if (battleHudImg != null) battleHudImg.raycastTarget = false; // JAMINAN: Tidak memblokir sentuhan jari/mouse ke unit dan petak grid!
            if (battleHudObj.GetComponent<SafeAreaFitter>() == null) battleHudObj.AddComponent<SafeAreaFitter>();
            var battleView = battleHudObj.AddComponent<UIBattleView>();

            // Top Bar Panel Battle
            var topBarObj = CreatePanel(battleHudObj.transform, "TopBarPanel", new Color(0.06f, 0.08f, 0.14f, 0.88f));
            var topImg = topBarObj.GetComponent<Image>();
            if (topImg != null) topImg.raycastTarget = false;
            if (_headerBannerRibbon != null && topImg != null)
            {
                topImg.sprite = _headerBannerRibbon;
                topImg.type = Image.Type.Sliced;
                topImg.color = Color.white;
            }
            var topBarRect = topBarObj.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = Vector2.zero;
            topBarRect.sizeDelta = new Vector2(-40, 130);

            var waveTxt = CreateText(topBarObj.transform, "WaveText", "Wave 1 / 10", 28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(waveTxt.gameObject, 0.04f, 0.52f, 0.30f, 0.95f);

            Text battleGemsTxt;
            var battleGemsPill = CreateResourcePill(topBarObj.transform, "BattleGemsPill", _iconGemBlue, "100 Gems", new Color(0.35f, 0.95f, 1f), ResourceCommodityType.Gems, defaultFont, out battleGemsTxt);
            PositionUI(battleGemsPill, 0.32f, 0.52f, 0.62f, 0.95f);

            var speedBtnObj = CreateButton(topBarObj.transform, "SpeedToggleButton", "1x", new Color(0.2f, 0.35f, 0.6f), Color.white, 24, defaultFont);
            PositionUI(speedBtnObj, 0.64f, 0.52f, 0.80f, 0.95f);

            var pauseBtnObj = CreateButton(topBarObj.transform, "PauseButton", "⏸️ MENU", new Color(0.45f, 0.25f, 0.6f), Color.white, 22, defaultFont);
            PositionUI(pauseBtnObj, 0.82f, 0.52f, 0.98f, 0.95f);

            // Indikator Baris Wave Progress Bar & Penanda Tengkorak Bos (Wave Milestone Progress Bar - Baris Bawah TopBar)
            var waveProgContainer = CreatePanel(topBarObj.transform, "WaveProgressBarContainer", new Color(0, 0, 0, 0));
            var waveProgImg = waveProgContainer.GetComponent<Image>();
            if (waveProgImg != null) waveProgImg.raycastTarget = false;
            PositionUI(waveProgContainer, 0.04f, 0.08f, 0.96f, 0.46f);

            var waveSliderObj = CreateProgressBar(waveProgContainer.transform, "WaveProgressBar", 0, 100, 0, new Color(0.95f, 0.45f, 0.15f));
            PositionUI(waveSliderObj, 0.02f, 0.1f, 0.98f, 0.9f);
            var waveSlider = waveSliderObj.GetComponent<Slider>();

            // Teks progress musuh diletakkan persis di tengah (center) tanpa terpotong
            var waveProgressTxt = CreateText(waveSliderObj.transform, "WaveProgressText", "0 / 10 Musuh", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(waveProgressTxt.gameObject, 0f, 0f, 1f, 1f);
            waveProgressTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            waveProgressTxt.verticalOverflow = VerticalWrapMode.Overflow;
            waveProgressTxt.raycastTarget = false;

            var skullSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Wave_Skull.png");
            Image midBossSkull = null;
            Image finalBossSkull = null;
            if (skullSprite != null)
            {
                // Ikon tengkorak diletakkan di ujung paling kanan progress bar (tidak menimpa teks progress)
                var finalSkullObj = new GameObject("FinalBossSkull", typeof(RectTransform), typeof(Image));
                finalSkullObj.transform.SetParent(waveSliderObj.transform, false);
                var skullRt = finalSkullObj.GetComponent<RectTransform>();
                skullRt.anchorMin = new Vector2(1f, 0.5f);
                skullRt.anchorMax = new Vector2(1f, 0.5f);
                skullRt.pivot = new Vector2(1f, 0.5f);
                skullRt.anchoredPosition = new Vector2(12f, 0f);
                skullRt.sizeDelta = new Vector2(36f, 36f);
                finalBossSkull = finalSkullObj.GetComponent<Image>();
                finalBossSkull.sprite = skullSprite;
                finalBossSkull.preserveAspect = true;
                finalBossSkull.raycastTarget = false;
            }

            // Bottom Bar Panel untuk Barikade Benteng (Dipindahkan ke Bawah Layar sesuai Arahan Komandan)
            var bottomBarricadeObj = CreatePanel(battleHudObj.transform, "BottomBarricadePanel", new Color(0.06f, 0.08f, 0.14f, 0.92f));
            if (_frameStoneBastion != null)
            {
                var bbImg = bottomBarricadeObj.GetComponent<Image>();
                bbImg.sprite = _frameStoneBastion;
                bbImg.type = Image.Type.Sliced;
                bbImg.color = Color.white;
            }
            var bbRect = bottomBarricadeObj.GetComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0f, 0f);
            bbRect.anchorMax = new Vector2(1f, 0f);
            bbRect.pivot = new Vector2(0.5f, 0f);
            bbRect.anchoredPosition = new Vector2(0, 18);
            bbRect.sizeDelta = new Vector2(-40, 56);

            // Castle Barricade HP Slider di Bawah dengan Visual Jelas (Background, Fill & Label HP)
            var hpSliderObj = CreateProgressBar(bottomBarricadeObj.transform, "CastleHpSlider", 0, 100, 100, new Color(0.22f, 0.85f, 0.38f));
            PositionUI(hpSliderObj, 0.04f, 0.15f, 0.96f, 0.85f);
            var hpSlider = hpSliderObj.GetComponent<Slider>();
            var castleHpTxt = CreateText(hpSliderObj.transform, "CastleHpText", "PERTAHANAN BENTENG: 100 / 100 HP (100%)", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(castleHpTxt.gameObject, 0.05f, 0.05f, 0.95f, 0.95f);
            castleHpTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Summon Button di Bawah Layar
            var summonBtnObj = CreateButton(battleHudObj.transform, "SummonButton", "SUMMON (40)", new Color(0.96f, 0.72f, 0.16f), new Color(0.12f, 0.1f, 0.05f), 36, defaultFont);
            var summonRect = summonBtnObj.GetComponent<RectTransform>();
            summonRect.anchorMin = new Vector2(0.5f, 0.05f);
            summonRect.anchorMax = new Vector2(0.5f, 0.05f);
            summonRect.pivot = new Vector2(0.5f, 0f);
            summonRect.sizeDelta = new Vector2(440, 110);
            var summonBtnUI = summonBtnObj.AddComponent<SummonButtonUI>();

            var summonSO = new SerializedObject(summonBtnUI);
            summonSO.FindProperty("button").objectReferenceValue = summonBtnObj.GetComponent<Button>();
            summonSO.FindProperty("costText").objectReferenceValue = summonBtnObj.GetComponentInChildren<Text>();
            summonSO.ApplyModifiedProperties();
            summonBtnObj.SetActive(false); // Mode 1 Puzzle Merge tidak membutuhkan tombol Summon manual

            // Dynamic Battle Mode: Puzzle Preparation Panel (Mode 1 & Inter-Wave Tactical Phase)
            var puzzlePanel = CreatePanel(battleHudObj.transform, "PuzzlePreparationPanel", new Color(0.1f, 0.12f, 0.2f, 0.92f));
            var pImg = puzzlePanel.GetComponent<Image>();
            if (pImg != null)
            {
                pImg.raycastTarget = false;
                if (_frameStoneBastion != null)
                {
                    pImg.sprite = _frameStoneBastion;
                    pImg.type = Image.Type.Sliced;
                    pImg.color = Color.white;
                }
            }
            var puzzleRect = puzzlePanel.GetComponent<RectTransform>();
            puzzleRect.anchorMin = new Vector2(0.5f, 0.88f);
            puzzleRect.anchorMax = new Vector2(0.5f, 0.88f);
            puzzleRect.pivot = new Vector2(0.5f, 0.5f);
            puzzleRect.sizeDelta = new Vector2(520, 60);

            // Icon Tactical Move (Sprint 28)
            var moveIconObj = new GameObject("TacticalMoveIcon", typeof(RectTransform), typeof(Image));
            moveIconObj.transform.SetParent(puzzlePanel.transform, false);
            var moveIconImg = moveIconObj.GetComponent<Image>();
            moveIconImg.sprite = _iconTacticalMove;
            moveIconImg.raycastTarget = false;
            var moveIconRect = moveIconObj.GetComponent<RectTransform>();
            moveIconRect.anchorMin = new Vector2(0.04f, 0.5f);
            moveIconRect.anchorMax = new Vector2(0.04f, 0.5f);
            moveIconRect.pivot = new Vector2(0.5f, 0.5f);
            moveIconRect.sizeDelta = new Vector2(40, 40);

            var threatTxt = CreateText(puzzlePanel.transform, "UpcomingThreatText", "👾 Intel Musuh: Mengintai...", 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.2f, 0.95f, 1f), defaultFont);
            PositionUI(threatTxt.gameObject, 0.12f, 0.52f, 0.58f, 0.95f);

            var movesTxt = CreateText(puzzlePanel.transform, "MovesRemainingText", "⚡ Sisa Langkah: 15", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.25f), defaultFont);
            PositionUI(movesTxt.gameObject, 0.12f, 0.05f, 0.58f, 0.52f);

            // Tombol Mulai Pertempuran (Start Defense Now)
            var startDefenseBtn = CreateButton(puzzlePanel.transform, "StartDefenseNowButton", "MULAI TEMPUR", new Color(0.85f, 0.55f, 0.1f), Color.white, 17, defaultFont);
            if (_btnPrimaryGold != null)
            {
                var bImg = startDefenseBtn.GetComponent<Image>();
                bImg.sprite = _btnPrimaryGold;
                bImg.type = Image.Type.Sliced;
            }
            PositionUI(startDefenseBtn, 0.59f, 0.10f, 0.98f, 0.90f);
            startDefenseBtn.SetActive(true);
            puzzlePanel.SetActive(true);

            // Inter-Wave Tactical Phase Banner Announcement (Sprint 28)
            var interWaveBanner = CreatePanel(battleHudObj.transform, "InterWaveBannerPanel", new Color(0.08f, 0.1f, 0.18f, 0.95f));
            if (_bannerTacticalPhase != null)
            {
                var bImg = interWaveBanner.GetComponent<Image>();
                bImg.sprite = _bannerTacticalPhase;
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
            }
            var bCanvasGroup = interWaveBanner.AddComponent<CanvasGroup>();
            bCanvasGroup.blocksRaycasts = false; // Zero UI raycast blocker guarantee!
            var bRect = interWaveBanner.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.76f);
            bRect.anchorMax = new Vector2(0.5f, 0.76f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(620, 115);

            var bTitle = CreateText(interWaveBanner.transform, "BannerTitle", "⚔️ GELOMBANG 2 BERSIAP!", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.25f), defaultFont);
            PositionUI(bTitle.gameObject, 0.05f, 0.55f, 0.95f, 0.95f);

            var bSub = CreateText(interWaveBanner.transform, "BannerSub", "Bonus +5 Langkah Taktis! Atur posisi & tingkatkan hero sekarang!", 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f), defaultFont);
            PositionUI(bSub.gameObject, 0.05f, 0.05f, 0.95f, 0.55f);

            interWaveBanner.SetActive(false);

            // Victory Modal
            var victoryPanel = CreatePanel(battleHudObj.transform, "VictoryModal", new Color(0.05f, 0.08f, 0.15f, 0.95f));
            PositionUI(victoryPanel, 0.1f, 0.25f, 0.9f, 0.75f);
            if (_frameOrnateGold != null)
            {
                var vicImg = victoryPanel.GetComponent<Image>();
                vicImg.sprite = _frameOrnateGold;
                vicImg.type = Image.Type.Sliced;
                vicImg.color = Color.white;
            }
            var vicTitle = CreateText(victoryPanel.transform, "Title", "KEMENANGAN TELAK!", 42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f), defaultFont);
            PositionUI(vicTitle.gameObject, 0.05f, 0.75f, 0.95f, 0.92f);
            var vicStars = CreateText(victoryPanel.transform, "StarsText", "★★★", 48, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f), defaultFont);
            PositionUI(vicStars.gameObject, 0.1f, 0.55f, 0.9f, 0.72f);
            var vicRewards = CreateText(victoryPanel.transform, "RewardsText", "+150 Gold  |  +50 Citadel EXP  |  +30 Gems", 26, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(vicRewards.gameObject, 0.05f, 0.35f, 0.95f, 0.5f);
            var vicReturnBtn = CreateButton(victoryPanel.transform, "ReturnButton", "KEMBALI KE CITADEL", new Color(0.2f, 0.65f, 0.35f), Color.white, 30, defaultFont);
            PositionUI(vicReturnBtn, 0.15f, 0.1f, 0.85f, 0.25f);
            victoryPanel.SetActive(false);

            // Defeat Modal
            var defeatPanel = CreatePanel(battleHudObj.transform, "DefeatModal", new Color(0.12f, 0.05f, 0.05f, 0.95f));
            PositionUI(defeatPanel, 0.1f, 0.25f, 0.9f, 0.75f);
            if (_frameStoneBastion != null)
            {
                var defImg = defeatPanel.GetComponent<Image>();
                defImg.sprite = _frameStoneBastion;
                defImg.type = Image.Type.Sliced;
                defImg.color = Color.white;
            }
            var defTitle = CreateText(defeatPanel.transform, "Title", "PERTAHANAN GAGAL!", 42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f), defaultFont);
            PositionUI(defTitle.gameObject, 0.05f, 0.75f, 0.95f, 0.92f);
            var defRetryBtn = CreateButton(defeatPanel.transform, "RetryButton", "COBA LAGI (RETRY)", new Color(0.85f, 0.35f, 0.2f), Color.white, 30, defaultFont);
            PositionUI(defRetryBtn, 0.15f, 0.42f, 0.85f, 0.58f);
            var defReturnBtn = CreateButton(defeatPanel.transform, "ReturnButton", "KEMBALI KE CITADEL", new Color(0.25f, 0.3f, 0.4f), Color.white, 28, defaultFont);
            PositionUI(defReturnBtn, 0.15f, 0.2f, 0.85f, 0.36f);
            defeatPanel.SetActive(false);

            // Pause Modal
            var pausePanel = CreatePanel(battleHudObj.transform, "PauseModal", new Color(0.04f, 0.06f, 0.1f, 0.95f));
            PositionUI(pausePanel, 0.15f, 0.3f, 0.85f, 0.7f);
            if (_frameStoneBastion != null)
            {
                var pauseImg = pausePanel.GetComponent<Image>();
                pauseImg.sprite = _frameStoneBastion;
                pauseImg.type = Image.Type.Sliced;
                pauseImg.color = Color.white;
            }
            var pauseTitle = CreateText(pausePanel.transform, "Title", "PERMAINAN DIHENTIKAN", 36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(pauseTitle.gameObject, 0.05f, 0.78f, 0.95f, 0.93f);
            var pauseResumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "LANJUTKAN (RESUME)", new Color(0.2f, 0.65f, 0.35f), Color.white, 26, defaultFont);
            PositionUI(pauseResumeBtn, 0.12f, 0.58f, 0.88f, 0.73f);
            var pauseVibBtn = CreateButton(pausePanel.transform, "VibrationButton", "📳 GETARAN: AKTIF", new Color(0.20f, 0.40f, 0.60f), Color.white, 22, defaultFont);
            PositionUI(pauseVibBtn, 0.12f, 0.38f, 0.88f, 0.53f);
            var pauseSurrenderBtn = CreateButton(pausePanel.transform, "SurrenderButton", "MENYERAH & KEMBALI KE CITADEL", new Color(0.65f, 0.25f, 0.25f), Color.white, 22, defaultFont);
            PositionUI(pauseSurrenderBtn, 0.12f, 0.18f, 0.88f, 0.33f);
            pausePanel.SetActive(false);

            // Bind UIBattleView
            var battleSO = new SerializedObject(battleView);
            battleSO.FindProperty("waveText").objectReferenceValue = waveTxt;
            battleSO.FindProperty("gemsText").objectReferenceValue = battleGemsTxt;
            battleSO.FindProperty("speedToggleButton").objectReferenceValue = speedBtnObj.GetComponent<Button>();
            battleSO.FindProperty("speedToggleText").objectReferenceValue = speedBtnObj.GetComponentInChildren<Text>();
            battleSO.FindProperty("pauseButton").objectReferenceValue = pauseBtnObj.GetComponent<Button>();
            battleSO.FindProperty("castleHpSlider").objectReferenceValue = hpSlider;
            battleSO.FindProperty("castleHpText").objectReferenceValue = castleHpTxt;
            battleSO.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            battleSO.FindProperty("victoryStarsText").objectReferenceValue = vicStars;
            battleSO.FindProperty("victoryRewardsText").objectReferenceValue = vicRewards;
            battleSO.FindProperty("victoryReturnButton").objectReferenceValue = vicReturnBtn.GetComponent<Button>();
            battleSO.FindProperty("defeatPanel").objectReferenceValue = defeatPanel;
            battleSO.FindProperty("defeatRetryButton").objectReferenceValue = defRetryBtn.GetComponent<Button>();
            battleSO.FindProperty("defeatReturnButton").objectReferenceValue = defReturnBtn.GetComponent<Button>();
            battleSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            var pauseVibBtnProp = battleSO.FindProperty("pauseVibrationButton");
            if (pauseVibBtnProp != null) pauseVibBtnProp.objectReferenceValue = pauseVibBtn.GetComponent<Button>();
            var pauseVibTxtProp = battleSO.FindProperty("pauseVibrationText");
            if (pauseVibTxtProp != null) pauseVibTxtProp.objectReferenceValue = pauseVibBtn.GetComponentInChildren<Text>();
            battleSO.FindProperty("puzzlePreparationPanel").objectReferenceValue = puzzlePanel;
            battleSO.FindProperty("movesRemainingText").objectReferenceValue = movesTxt;
            var threatProp = battleSO.FindProperty("upcomingThreatText");
            if (threatProp != null) threatProp.objectReferenceValue = threatTxt;
            battleSO.FindProperty("startDefenseNowButton").objectReferenceValue = startDefenseBtn.GetComponent<Button>();
            battleSO.FindProperty("summonButtonContainer").objectReferenceValue = summonBtnObj;
            battleSO.FindProperty("interWaveBannerPanel").objectReferenceValue = interWaveBanner;
            battleSO.FindProperty("interWaveTitleText").objectReferenceValue = bTitle;
            battleSO.FindProperty("interWaveSubText").objectReferenceValue = bSub;
            battleSO.FindProperty("waveProgressBarContainer").objectReferenceValue = waveProgContainer;
            battleSO.FindProperty("waveProgressBar").objectReferenceValue = waveSlider;
            battleSO.FindProperty("waveProgressFill").objectReferenceValue = waveSliderObj.transform.Find("Fill")?.GetComponent<Image>();
            battleSO.FindProperty("midBossMarker").objectReferenceValue = midBossSkull;
            battleSO.FindProperty("finalBossMarker").objectReferenceValue = finalBossSkull;
            battleSO.FindProperty("waveProgressText").objectReferenceValue = waveProgressTxt;
            battleSO.ApplyModifiedProperties();

            battleHudObj.SetActive(false);

            // ==========================================
            // ==========================================
            // D. METAGAME SUB-PANELS (Modals)
            // ==========================================

            // 1. STAGE MAP PANEL (Peta Dunia & Seleksi Stage)
            var stageMapPanel = CreateSubPanel(canvasObj.transform, "StageMapPanel", "PETA DUNIA & SELEKSI STAGE", defaultFont);
            var stageMapView = stageMapPanel.AddComponent<UIStageMapView>();
            var stageContent = stageMapPanel.transform.Find("ContentContainer") ?? stageMapPanel.transform;

            var stageTop = CreatePanel(stageContent, "StageTopHeader", new Color(0.06f, 0.09f, 0.16f, 0.9f));
            PositionUI(stageTop, 0.02f, 0.85f, 0.98f, 0.98f);
            var chapterTxt = CreateText(stageTop.transform, "ChapterTitle", "Chapter 1: Pertahanan Pinggiran Kerajaan", 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(chapterTxt.gameObject, 0.03f, 0.52f, 0.65f, 0.95f);
            var starsTxt = CreateText(stageTop.transform, "TotalStarsText", "Total Bintang: 0 / 30 ★", 22, FontStyle.Bold, TextAnchor.MiddleRight, Color.white, defaultFont);
            PositionUI(starsTxt.gameObject, 0.65f, 0.52f, 0.97f, 0.95f);
            var starSliderObj = CreateProgressBar(stageTop.transform, "ChapterStarSlider", 0, 30, 0, new Color(1f, 0.82f, 0.2f));
            PositionUI(starSliderObj, 0.03f, 0.1f, 0.5f, 0.45f);
            var m10Btn = CreateButton(stageTop.transform, "Claim10StarBtn", "10★", new Color(0.2f, 0.4f, 0.6f), Color.white, 18, defaultFont);
            PositionUI(m10Btn, 0.53f, 0.08f, 0.67f, 0.48f);
            var m20Btn = CreateButton(stageTop.transform, "Claim20StarBtn", "20★", new Color(0.2f, 0.4f, 0.6f), Color.white, 18, defaultFont);
            PositionUI(m20Btn, 0.69f, 0.08f, 0.83f, 0.48f);
            var m30Btn = CreateButton(stageTop.transform, "Claim30StarBtn", "30★", new Color(0.5f, 0.35f, 0.1f), new Color(1f, 0.85f, 0.3f), 18, defaultFont);
            PositionUI(m30Btn, 0.85f, 0.08f, 0.98f, 0.48f);

            // =========================================================================
            // PETUALANGAN PETA DUNIA (Expedition Adventure Trail Progression Map)
            // Lintasan jalur dinamis S-Curve dari Gerbang 1-1 hingga Sarang Naga 1-10
            // =========================================================================
            var stageTrailPanel = CreatePanel(stageContent, "StageMapTrailPanel", new Color(0.04f, 0.06f, 0.1f, 0.95f));
            if (_frameStoneBastion != null)
            {
                var trailImg = stageTrailPanel.GetComponent<Image>();
                trailImg.sprite = _frameStoneBastion;
                trailImg.type = Image.Type.Sliced;
                trailImg.color = Color.white;
            }
            PositionUI(stageTrailPanel, 0.02f, 0.45f, 0.98f, 0.84f);

            Vector4[] nodeRects = new Vector4[]
            {
                new Vector4(0.04f, 0.05f, 0.22f, 0.23f),  // 1-1
                new Vector4(0.28f, 0.12f, 0.46f, 0.30f),  // 1-2
                new Vector4(0.53f, 0.06f, 0.71f, 0.24f),  // 1-3
                new Vector4(0.76f, 0.18f, 0.94f, 0.36f),  // 1-4
                new Vector4(0.48f, 0.38f, 0.72f, 0.58f),  // 1-5 (Mini-Boss Fortress Node)
                new Vector4(0.20f, 0.45f, 0.38f, 0.63f),  // 1-6
                new Vector4(0.04f, 0.64f, 0.22f, 0.82f),  // 1-7
                new Vector4(0.28f, 0.74f, 0.46f, 0.92f),  // 1-8
                new Vector4(0.53f, 0.68f, 0.71f, 0.86f),  // 1-9
                new Vector4(0.74f, 0.75f, 0.98f, 0.97f)   // 1-10 (Final Boss Abyssal Drake Citadel)
            };

            // Waypoint stepping stones (titik-titik lintasan petualangan elegan tanpa kotak menumpuk)
            for (int r = 0; r < 9; r++)
            {
                float x1 = (nodeRects[r].x + nodeRects[r].z) * 0.5f;
                float y1 = (nodeRects[r].y + nodeRects[r].w) * 0.5f;
                float x2 = (nodeRects[r + 1].x + nodeRects[r + 1].z) * 0.5f;
                float y2 = (nodeRects[r + 1].y + nodeRects[r + 1].w) * 0.5f;

                for (int s = 1; s <= 3; s++)
                {
                    float t = s * 0.25f;
                    float dotX = Mathf.Lerp(x1, x2, t);
                    float dotY = Mathf.Lerp(y1, y2, t);

                    var dotObj = CreatePanel(stageTrailPanel.transform, $"TrailDot_{r + 1}_{s}", (r == 3 || r == 8) ? new Color(1f, 0.45f, 0.45f, 0.85f) : new Color(0.95f, 0.82f, 0.35f, 0.85f));
                    PositionUI(dotObj, dotX - 0.008f, dotY - 0.012f, dotX + 0.008f, dotY + 0.012f);

                    var dotImg = dotObj.GetComponent<Image>();
                    if (dotImg != null)
                    {
                        dotImg.raycastTarget = false;
                    }
                }
            }

            var stageNodeButtonsList = new List<Button>();
            string[] nodeTitles = new string[] {
                "<b>1-1</b>\nGerbang", "<b>1-2</b>\nLembah", "<b>1-3</b>\nHutan", "<b>1-4</b>\nSungai", "<b>👑 1-5</b>\n<color=#FFD700>Mini-Boss</color>",
                "<b>1-6</b>\nRawa", "<b>1-7</b>\nJembatan", "<b>1-8</b>\nLereng", "<b>1-9</b>\nKawah", "<b>💀 1-10</b>\n<color=#FF4444>DRAKE BOSS</color>"
            };

            for (int i = 0; i < 10; i++)
            {
                Color bColor = (i == 9) ? new Color(0.7f, 0.15f, 0.15f) : ((i == 4) ? new Color(0.65f, 0.45f, 0.15f) : new Color(0.16f, 0.22f, 0.35f));
                var sBtnObj = CreateButton(stageTrailPanel.transform, $"StageNode_{i + 1}", nodeTitles[i], bColor, Color.white, (i == 4 || i == 9) ? 17 : 16, defaultFont);
                PositionUI(sBtnObj, nodeRects[i].x, nodeRects[i].y, nodeRects[i].z, nodeRects[i].w);

                if (i == 9) AttachRarityBorder(sBtnObj.transform, _borderLegendary);
                else if (i == 4) AttachRarityBorder(sBtnObj.transform, _borderEpic);
                else AttachRarityBorder(sBtnObj.transform, _borderRare ?? _borderCommon);

                stageNodeButtonsList.Add(sBtnObj.GetComponent<Button>());
            }

            var detailPanel = CreatePanel(stageContent, "StageDetailPanel", new Color(0.07f, 0.1f, 0.16f, 0.95f));
            PositionUI(detailPanel, 0.02f, 0.02f, 0.98f, 0.44f);
            var codeTxt = CreateText(detailPanel.transform, "StageCodeText", "STAGE 1-1", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.4f, 0.85f, 1f), defaultFont);
            PositionUI(codeTxt.gameObject, 0.04f, 0.85f, 0.4f, 0.97f);
            var titleTxt = CreateText(detailPanel.transform, "StageTitleText", "Gerbang Pinggiran Kerajaan", 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(titleTxt.gameObject, 0.42f, 0.85f, 0.96f, 0.97f);

            var modeTxt = CreateText(detailPanel.transform, "BattleModeText", "Mode: Real-Time Deploy", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(modeTxt.gameObject, 0.04f, 0.72f, 0.5f, 0.83f);
            var diffTxt = CreateText(detailPanel.transform, "DifficultyStatusText", "Status: Normal", 20, FontStyle.Bold, TextAnchor.MiddleRight, Color.green, defaultFont);
            PositionUI(diffTxt.gameObject, 0.52f, 0.72f, 0.96f, 0.83f);

            var descTxt = CreateText(detailPanel.transform, "StageDescriptionText", "Hadapi gelombang monster Abyssal awal yang mengancam pinggiran benteng.", 19, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 0.95f), defaultFont);
            PositionUI(descTxt.gameObject, 0.04f, 0.48f, 0.96f, 0.70f);

            var recCpTxt = CreateText(detailPanel.transform, "RecommendedCpText", "Rekomendasi CP: 150 (Team CP Kamu: 500)", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(recCpTxt.gameObject, 0.04f, 0.36f, 0.96f, 0.46f);

            var stageEnergyTxt = CreateText(detailPanel.transform, "EnergyCostText", "Biaya Stamina: ⚡ 5 (Stamina: 30/30)", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.3f, 0.9f, 1f), defaultFont);
            PositionUI(stageEnergyTxt.gameObject, 0.04f, 0.26f, 0.96f, 0.35f);

            var firstRewardsTxt = CreateText(detailPanel.transform, "FirstClearRewardsText", "First-Clear: +150 Gold, +20 Gems [Tersedia]", 19, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0.2f), defaultFont);
            PositionUI(firstRewardsTxt.gameObject, 0.04f, 0.16f, 0.96f, 0.25f);

            var startBattleBtn = CreateButton(detailPanel.transform, "StartBattleButton", "⚔️ MULAI PERTEMPURAN SEKARANG", new Color(0.92f, 0.35f, 0.18f), Color.white, 26, defaultFont);
            PositionUI(startBattleBtn, 0.08f, 0.02f, 0.92f, 0.14f);

            var stageSO = new SerializedObject(stageMapView);
            stageSO.FindProperty("rootPanel").objectReferenceValue = stageMapPanel;
            stageSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(stageMapPanel);
            stageSO.FindProperty("chapterTitleText").objectReferenceValue = chapterTxt;
            stageSO.FindProperty("totalStarsText").objectReferenceValue = starsTxt;
            stageSO.FindProperty("chapterStarSlider").objectReferenceValue = starSliderObj.GetComponent<Slider>();
            stageSO.FindProperty("claim10StarButton").objectReferenceValue = m10Btn.GetComponent<Button>();
            stageSO.FindProperty("claim20StarButton").objectReferenceValue = m20Btn.GetComponent<Button>();
            stageSO.FindProperty("claim30StarButton").objectReferenceValue = m30Btn.GetComponent<Button>();
            stageSO.FindProperty("stageDetailPanel").objectReferenceValue = detailPanel;
            stageSO.FindProperty("stageCodeText").objectReferenceValue = codeTxt;
            stageSO.FindProperty("stageTitleText").objectReferenceValue = titleTxt;
            stageSO.FindProperty("stageDescriptionText").objectReferenceValue = descTxt;
            stageSO.FindProperty("battleModeText").objectReferenceValue = modeTxt;
            stageSO.FindProperty("recommendedCpText").objectReferenceValue = recCpTxt;
            stageSO.FindProperty("difficultyStatusText").objectReferenceValue = diffTxt;
            stageSO.FindProperty("energyCostText").objectReferenceValue = stageEnergyTxt;
            stageSO.FindProperty("firstClearRewardsText").objectReferenceValue = firstRewardsTxt;
            stageSO.FindProperty("startBattleButton").objectReferenceValue = startBattleBtn.GetComponent<Button>();

            var stageNodesProp = stageSO.FindProperty("stageNodeButtons");
            if (stageNodesProp != null)
            {
                stageNodesProp.arraySize = stageNodeButtonsList.Count;
                for (int i = 0; i < stageNodeButtonsList.Count; i++)
                {
                    stageNodesProp.GetArrayElementAtIndex(i).objectReferenceValue = stageNodeButtonsList[i];
                }
            }
            stageSO.ApplyModifiedProperties();
            stageMapPanel.SetActive(false);

            // 2. HERO UPGRADE PANEL
            var heroPanel = CreateSubPanel(canvasObj.transform, "HeroUpgradePanel", "MANAJEMEN & UPGRADE PAHLAWAN", defaultFont);
            var heroView = heroPanel.AddComponent<UIHeroUpgradeView>();
            var heroContent = heroPanel.transform.Find("ContentContainer") ?? heroPanel.transform;

            var tabCedric = CreateButton(heroContent, "TabCedric", "⚔️ Cedric", new Color(0.45f, 0.15f, 0.15f), Color.white, 20, defaultFont);
            PositionUI(tabCedric, 0.02f, 0.90f, 0.25f, 0.98f);
            var tabRose = CreateButton(heroContent, "TabRose", "🛡️ Rose Tank", new Color(0.45f, 0.2f, 0.35f), Color.white, 20, defaultFont);
            PositionUI(tabRose, 0.27f, 0.90f, 0.50f, 0.98f);
            var tabRabbit = CreateButton(heroContent, "TabRabbit", "🔮 Rabbit Mage", new Color(0.15f, 0.25f, 0.5f), Color.white, 20, defaultFont);
            PositionUI(tabRabbit, 0.52f, 0.90f, 0.75f, 0.98f);
            var tabWind = CreateButton(heroContent, "TabWind", "🏹 Wind Ranger", new Color(0.15f, 0.4f, 0.25f), Color.white, 20, defaultFont);
            PositionUI(tabWind, 0.77f, 0.90f, 0.98f, 0.98f);

            var heroCardPanel = CreatePanel(heroContent, "SelectedHeroCard", new Color(0.08f, 0.12f, 0.2f, 0.9f));
            PositionUI(heroCardPanel, 0.04f, 0.65f, 0.96f, 0.88f);

            var avatarObj = new GameObject("HeroAvatarImage", typeof(RectTransform), typeof(Image));
            avatarObj.transform.SetParent(heroCardPanel.transform, false);
            PositionUI(avatarObj, 0.03f, 0.06f, 0.26f, 0.94f);
            var avatarImg = avatarObj.GetComponent<Image>();
            avatarImg.sprite = _heroCedricSprite;
            avatarImg.preserveAspect = true;
            avatarImg.raycastTarget = false;

            var hNameTxt = CreateText(heroCardPanel.transform, "HeroNameText", "Cedric (Ksatria Merah)", 28, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(hNameTxt.gameObject, 0.28f, 0.55f, 0.70f, 0.95f);
            var hRoleTxt = CreateText(heroCardPanel.transform, "HeroRoleText", "Frontline Melee DPS Striker (1 Tile)", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.95f), defaultFont);
            PositionUI(hRoleTxt.gameObject, 0.28f, 0.10f, 0.70f, 0.50f);
            var hLvlTxt = CreateText(heroCardPanel.transform, "HeroLevelText", "Lv. 1 ★☆☆", 26, FontStyle.Bold, TextAnchor.MiddleRight, Color.white, defaultFont);
            PositionUI(hLvlTxt.gameObject, 0.70f, 0.55f, 0.97f, 0.95f);
            var hCpTxt = CreateText(heroCardPanel.transform, "HeroCpText", "⚔️ CP: 125", 22, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.45f, 0.45f), defaultFont);
            PositionUI(hCpTxt.gameObject, 0.70f, 0.10f, 0.97f, 0.50f);

            var statsPanel = CreatePanel(heroContent, "StatsPanel", new Color(0.06f, 0.08f, 0.14f, 0.85f));
            PositionUI(statsPanel, 0.04f, 0.46f, 0.96f, 0.62f);
            var atkTxt = CreateText(statsPanel.transform, "AttackStatText", "⚔️ Serangan (ATK): 35", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(atkTxt.gameObject, 0.05f, 0.66f, 0.95f, 0.95f);
            var hpTxt = CreateText(statsPanel.transform, "HealthStatText", "❤️ Daya Tahan (HP): 150", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(hpTxt.gameObject, 0.05f, 0.34f, 0.95f, 0.63f);
            var critTxt = CreateText(statsPanel.transform, "CritStatText", "🎯 Peluang Kritikal (Crit): 8%", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(critTxt.gameObject, 0.05f, 0.02f, 0.95f, 0.31f);

            var upgActionPanel = CreatePanel(heroContent, "UpgradeActionPanel", new Color(0.07f, 0.1f, 0.16f, 0.9f));
            PositionUI(upgActionPanel, 0.04f, 0.26f, 0.96f, 0.43f);
            var upgCostTxt = CreateText(upgActionPanel.transform, "UpgradeCostText", "Biaya Upgrade: 🪙 165 Gold", 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f), defaultFont);
            PositionUI(upgCostTxt.gameObject, 0.05f, 0.55f, 0.5f, 0.92f);
            var upgPreviewTxt = CreateText(upgActionPanel.transform, "UpgradePreviewText", "Efek: Stat +7.5% per Level", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.7f, 0.9f, 1f), defaultFont);
            PositionUI(upgPreviewTxt.gameObject, 0.05f, 0.1f, 0.5f, 0.48f);
            var upgHeroBtn = CreateButton(upgActionPanel.transform, "UpgradeHeroButton", "TINGKATKAN LEVEL", new Color(0.95f, 0.72f, 0.16f), new Color(0.12f, 0.08f, 0.02f), 24, defaultFont);
            PositionUI(upgHeroBtn, 0.53f, 0.15f, 0.96f, 0.85f);

            var equipPanel = CreatePanel(heroContent, "EquipmentSlotsPanel", new Color(0.06f, 0.08f, 0.14f, 0.85f));
            PositionUI(equipPanel, 0.04f, 0.07f, 0.96f, 0.23f);
            var wSlotTxt = CreateText(equipPanel.transform, "WeaponSlotText", "🗡️ Senjata: [Kosong]", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(wSlotTxt.gameObject, 0.05f, 0.68f, 0.65f, 0.96f);
            var aSlotTxt = CreateText(equipPanel.transform, "ArmorSlotText", "🛡️ Zirah: [Kosong]", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(aSlotTxt.gameObject, 0.05f, 0.36f, 0.65f, 0.64f);
            var rSlotTxt = CreateText(equipPanel.transform, "RelicSlotText", "✨ Relik: [Kosong]", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(rSlotTxt.gameObject, 0.05f, 0.04f, 0.65f, 0.32f);
            var autoEquipBtn = CreateButton(equipPanel.transform, "AutoEquipButton", "⚔️ PASANG TERBAIK", new Color(0.18f, 0.55f, 0.35f), Color.white, 18, defaultFont);
            PositionUI(autoEquipBtn, 0.67f, 0.52f, 0.97f, 0.92f);

            var unequipBtn = CreateButton(equipPanel.transform, "UnequipAllButton", "📦 LEPAS SEMUA", new Color(0.35f, 0.38f, 0.45f), Color.white, 18, defaultFont);
            PositionUI(unequipBtn, 0.67f, 0.08f, 0.97f, 0.48f);

            var deployBtn = CreateButton(heroContent, "DeployToFormationButton", "🛡️ PASANG KE FORMASI", new Color(0.18f, 0.5f, 0.75f), Color.white, 20, defaultFont);
            PositionUI(deployBtn, 0.04f, 0.02f, 0.96f, 0.065f);

            var forgeBtn = CreateButton(equipPanel.transform, "OpenForgeButton", "🔨 KE FORGE", new Color(0.2f, 0.35f, 0.55f), Color.white, 20, defaultFont);
            forgeBtn.SetActive(false);

            var heroFeedbackTxt = CreateText(heroContent, "HeroFeedbackText", "", 18, FontStyle.Italic, TextAnchor.MiddleCenter, Color.green, defaultFont);
            PositionUI(heroFeedbackTxt.gameObject, 0.05f, 0.002f, 0.95f, 0.02f);

            var heroSO = new SerializedObject(heroView);
            heroSO.FindProperty("rootPanel").objectReferenceValue = heroPanel;
            heroSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(heroPanel);
            heroSO.FindProperty("cedricTabButton").objectReferenceValue = tabCedric.GetComponent<Button>();
            heroSO.FindProperty("roseTabButton").objectReferenceValue = tabRose.GetComponent<Button>();
            heroSO.FindProperty("rabbitTabButton").objectReferenceValue = tabRabbit.GetComponent<Button>();
            heroSO.FindProperty("windRangerTabButton").objectReferenceValue = tabWind.GetComponent<Button>();
            heroSO.FindProperty("heroNameText").objectReferenceValue = hNameTxt;
            heroSO.FindProperty("heroRoleText").objectReferenceValue = hRoleTxt;
            heroSO.FindProperty("heroLevelText").objectReferenceValue = hLvlTxt;
            heroSO.FindProperty("heroCpText").objectReferenceValue = hCpTxt;
            heroSO.FindProperty("heroAvatarImage").objectReferenceValue = avatarImg;
            heroSO.FindProperty("cedricAvatar").objectReferenceValue = _heroCedricSprite;
            heroSO.FindProperty("roseAvatar").objectReferenceValue = _heroRoseSprite;
            heroSO.FindProperty("rabbitAvatar").objectReferenceValue = _heroRabbitSprite;
            heroSO.FindProperty("windRangerAvatar").objectReferenceValue = _heroWindSprite;
            heroSO.FindProperty("attackStatText").objectReferenceValue = atkTxt;
            heroSO.FindProperty("healthStatText").objectReferenceValue = hpTxt;
            heroSO.FindProperty("critStatText").objectReferenceValue = critTxt;
            heroSO.FindProperty("upgradeCostText").objectReferenceValue = upgCostTxt;
            heroSO.FindProperty("upgradePreviewText").objectReferenceValue = upgPreviewTxt;
            heroSO.FindProperty("upgradeHeroButton").objectReferenceValue = upgHeroBtn.GetComponent<Button>();
            heroSO.FindProperty("weaponSlotText").objectReferenceValue = wSlotTxt;
            heroSO.FindProperty("armorSlotText").objectReferenceValue = aSlotTxt;
            heroSO.FindProperty("relicSlotText").objectReferenceValue = rSlotTxt;
            heroSO.FindProperty("autoEquipButton").objectReferenceValue = autoEquipBtn.GetComponent<Button>();
            heroSO.FindProperty("unequipAllButton").objectReferenceValue = unequipBtn.GetComponent<Button>();
            heroSO.FindProperty("deployToFormationButton").objectReferenceValue = deployBtn.GetComponent<Button>();
            heroSO.FindProperty("deployButtonText").objectReferenceValue = deployBtn.GetComponentInChildren<Text>();
            heroSO.FindProperty("openForgeButton").objectReferenceValue = forgeBtn.GetComponent<Button>();
            heroSO.FindProperty("feedbackMessageText").objectReferenceValue = heroFeedbackTxt;
            heroSO.ApplyModifiedProperties();
            heroPanel.SetActive(false);

            // 3. SUMMON HALL PANEL (Balai Pemanggilan Gacha)
            var summonPanel = CreateSubPanel(canvasObj.transform, "SummonHallPanel", "BALAI PEMANGGILAN CITADEL", defaultFont);
            var summonView = summonPanel.AddComponent<UISummonHallView>();
            var summonContent = summonPanel.transform.Find("ContentContainer") ?? summonPanel.transform;

            var sGemsTxt = CreateText(summonContent, "GemsBalanceText", "💎 Saldo Gems: 50", 24, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.35f, 0.95f, 0.85f), defaultFont);
            PositionUI(sGemsTxt.gameObject, 0.5f, 0.92f, 0.96f, 0.98f);

            var bannerPanel = CreatePanel(summonContent, "FeaturedBanner", new Color(0.12f, 0.2f, 0.32f, 0.9f));
            PositionUI(bannerPanel, 0.04f, 0.56f, 0.96f, 0.90f);
            if (_keyVisualBanner != null)
            {
                var banImg = bannerPanel.GetComponent<Image>();
                banImg.sprite = _keyVisualBanner;
                banImg.color = new Color(0.9f, 0.9f, 1f, 1f);
            }
            var banTitle = CreateText(bannerPanel.transform, "BannerTitle", "PEMANGGILAN KHUSUS PAHLAWAN", 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f), defaultFont);
            PositionUI(banTitle.gameObject, 0.05f, 0.65f, 0.95f, 0.92f);
            var banSub = CreateText(bannerPanel.transform, "BannerSubtitle", "Rate-Up 50%: Wind Ranger (Pemanah Angin)", 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.95f, 0.8f), defaultFont);
            PositionUI(banSub.gameObject, 0.05f, 0.35f, 0.95f, 0.60f);

            var s1Btn = CreateButton(bannerPanel.transform, "SingleSummonBtn", "PANGGIL 1x\n(100 Gems)", new Color(0.2f, 0.5f, 0.7f), Color.white, 22, defaultFont);
            PositionUI(s1Btn, 0.08f, 0.04f, 0.48f, 0.30f);
            var s10Btn = CreateButton(bannerPanel.transform, "MultiSummonBtn", "PANGGIL 10x\n(900 Gems)", new Color(0.85f, 0.6f, 0.15f), Color.white, 22, defaultFont);
            PositionUI(s10Btn, 0.52f, 0.04f, 0.92f, 0.30f);

            var promoPanel = CreatePanel(summonContent, "StarPromotionPanel", new Color(0.08f, 0.1f, 0.16f, 0.9f));
            PositionUI(promoPanel, 0.04f, 0.05f, 0.96f, 0.52f);
            var promoTitle = CreateText(promoPanel.transform, "PromoTitle", "PROMOSI BINTANG PAHLAWAN (SHARDS)", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(promoTitle.gameObject, 0.05f, 0.85f, 0.95f, 0.96f);

            var sTabCedric = CreateButton(promoPanel.transform, "TabCedric", "Cedric", new Color(0.4f, 0.15f, 0.15f), Color.white, 18, defaultFont);
            PositionUI(sTabCedric, 0.04f, 0.68f, 0.25f, 0.82f);
            var sTabRose = CreateButton(promoPanel.transform, "TabRose", "Rose", new Color(0.4f, 0.15f, 0.3f), Color.white, 18, defaultFont);
            PositionUI(sTabRose, 0.28f, 0.68f, 0.49f, 0.82f);
            var sTabRabbit = CreateButton(promoPanel.transform, "TabRabbit", "Rabbit", new Color(0.15f, 0.2f, 0.45f), Color.white, 18, defaultFont);
            PositionUI(sTabRabbit, 0.52f, 0.68f, 0.73f, 0.82f);
            var sTabWind = CreateButton(promoPanel.transform, "TabWind", "Wind", new Color(0.15f, 0.35f, 0.2f), Color.white, 18, defaultFont);
            PositionUI(sTabWind, 0.76f, 0.68f, 0.97f, 0.82f);

            var sHeroName = CreateText(promoPanel.transform, "PromoHeroName", "Wind Ranger", 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(sHeroName.gameObject, 0.05f, 0.48f, 0.55f, 0.64f);
            var sHeroStar = CreateText(promoPanel.transform, "PromoStarRank", "★☆☆☆☆ (1 Bintang)", 24, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.2f), defaultFont);
            PositionUI(sHeroStar.gameObject, 0.55f, 0.48f, 0.95f, 0.64f);

            var shardSlider = CreateProgressBar(promoPanel.transform, "ShardSlider", 0, 20, 0, new Color(0.7f, 0.35f, 0.9f));
            PositionUI(shardSlider, 0.05f, 0.30f, 0.95f, 0.44f);
            var shardTxt = CreateText(promoPanel.transform, "ShardProgressText", "0 / 20 Shards", 20, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(shardTxt.gameObject, 0.1f, 0.18f, 0.9f, 0.28f);

            var promoteBtn = CreateButton(promoPanel.transform, "PromoteStarBtn", "PROMOSI BINTANG ★", new Color(0.25f, 0.65f, 0.35f), Color.white, 22, defaultFont);
            PositionUI(promoteBtn, 0.15f, 0.03f, 0.85f, 0.16f);

            var resModal = CreatePanel(summonPanel.transform, "ResultModalPanel", new Color(0.04f, 0.06f, 0.1f, 0.98f));
            PositionUI(resModal, 0.05f, 0.15f, 0.95f, 0.85f);
            var resSummary = CreateText(resModal.transform, "ResultSummaryText", "Hasil Pemanggilan Pahlawan", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(resSummary.gameObject, 0.05f, 0.82f, 0.95f, 0.96f);
            var resContainer = CreatePanel(resModal.transform, "ResultCardsContainer", new Color(0, 0, 0, 0));
            PositionUI(resContainer, 0.05f, 0.18f, 0.95f, 0.80f);
            var closeResBtn = CreateButton(resModal.transform, "CloseResultBtn", "KEMBALI", new Color(0.2f, 0.5f, 0.7f), Color.white, 24, defaultFont);
            PositionUI(closeResBtn, 0.25f, 0.04f, 0.75f, 0.14f);
            resModal.SetActive(false);

            var summonHallSO = new SerializedObject(summonView);
            summonHallSO.FindProperty("rootPanel").objectReferenceValue = summonPanel;
            summonHallSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(summonPanel);
            summonHallSO.FindProperty("gemsBalanceText").objectReferenceValue = sGemsTxt;
            summonHallSO.FindProperty("bannerTitleText").objectReferenceValue = banTitle;
            summonHallSO.FindProperty("bannerSubtitleText").objectReferenceValue = banSub;
            summonHallSO.FindProperty("singleSummonButton").objectReferenceValue = s1Btn.GetComponent<Button>();
            summonHallSO.FindProperty("singleSummonCostText").objectReferenceValue = s1Btn.GetComponentInChildren<Text>();
            summonHallSO.FindProperty("multiSummonButton").objectReferenceValue = s10Btn.GetComponent<Button>();
            summonHallSO.FindProperty("multiSummonCostText").objectReferenceValue = s10Btn.GetComponentInChildren<Text>();
            summonHallSO.FindProperty("heroNameText").objectReferenceValue = sHeroName;
            summonHallSO.FindProperty("heroStarRankText").objectReferenceValue = sHeroStar;
            summonHallSO.FindProperty("shardProgressSlider").objectReferenceValue = shardSlider.GetComponent<Slider>();
            summonHallSO.FindProperty("shardProgressText").objectReferenceValue = shardTxt;
            summonHallSO.FindProperty("promoteStarButton").objectReferenceValue = promoteBtn.GetComponent<Button>();
            summonHallSO.FindProperty("promoteButtonText").objectReferenceValue = promoteBtn.GetComponentInChildren<Text>();
            summonHallSO.FindProperty("resultModalPanel").objectReferenceValue = resModal;
            summonHallSO.FindProperty("resultCardsContainer").objectReferenceValue = resContainer.transform;
            summonHallSO.FindProperty("resultSummaryText").objectReferenceValue = resSummary;
            summonHallSO.FindProperty("closeResultButton").objectReferenceValue = closeResBtn.GetComponent<Button>();

            var heroTabsProp = summonHallSO.FindProperty("heroTabButtons");
            if (heroTabsProp != null)
            {
                heroTabsProp.arraySize = 4;
                heroTabsProp.GetArrayElementAtIndex(0).objectReferenceValue = sTabCedric.GetComponent<Button>();
                heroTabsProp.GetArrayElementAtIndex(1).objectReferenceValue = sTabRose.GetComponent<Button>();
                heroTabsProp.GetArrayElementAtIndex(2).objectReferenceValue = sTabRabbit.GetComponent<Button>();
                heroTabsProp.GetArrayElementAtIndex(3).objectReferenceValue = sTabWind.GetComponent<Button>();
            }
            summonHallSO.ApplyModifiedProperties();
            summonPanel.SetActive(false);

            // 4. STORE PANEL (Toko & In-App Purchase)
            var storePanel = CreateSubPanel(canvasObj.transform, "StorePanel", "TOKO CITADEL & IN-APP PURCHASE", defaultFont);
            var storeView = storePanel.AddComponent<UIStoreView>();
            var storeContent = storePanel.transform.Find("ContentContainer") ?? storePanel.transform;

            var storeCurPanel = CreatePanel(storeContent, "StoreCurrencies", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(storeCurPanel, 0.03f, 0.88f, 0.97f, 0.98f);
            var stGoldTxt = CreateText(storeCurPanel.transform, "AccountGoldText", "🪙 Gold: 500", 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f), defaultFont);
            PositionUI(stGoldTxt.gameObject, 0.05f, 0.1f, 0.45f, 0.9f);
            var stGemsTxt = CreateText(storeCurPanel.transform, "AccountGemsText", "💎 Gems: 50", 22, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.35f, 0.95f, 0.85f), defaultFont);
            PositionUI(stGemsTxt.gameObject, 0.55f, 0.1f, 0.95f, 0.9f);

            var starterBtn = CreateButton(storeContent, "BuyStarterPackBtn", "⭐ WELCOME STARTER PACK (500 Gems + 10k Gold)\nRp 15.000", new Color(0.85f, 0.55f, 0.15f), Color.white, 22, defaultFont);
            PositionUI(starterBtn, 0.05f, 0.72f, 0.95f, 0.85f);
            var starterStatus = CreateText(starterBtn.transform, "StarterStatusText", "Diskon 80% - Khusus Pemain Baru", 18, FontStyle.Italic, TextAnchor.LowerCenter, new Color(1f, 1f, 0.8f), defaultFont);
            PositionUI(starterStatus.gameObject, 0.05f, 0.02f, 0.95f, 0.35f);

            var g120Btn = CreateButton(storeContent, "BuyGems120Btn", "💎 120 GEMS\nRp 15.000", new Color(0.18f, 0.32f, 0.5f), Color.white, 20, defaultFont);
            PositionUI(g120Btn, 0.05f, 0.55f, 0.48f, 0.69f);
            var g650Btn = CreateButton(storeContent, "BuyGems650Btn", "💎 650 GEMS\nRp 75.000", new Color(0.18f, 0.35f, 0.55f), Color.white, 20, defaultFont);
            PositionUI(g650Btn, 0.52f, 0.55f, 0.95f, 0.69f);
            var g3500Btn = CreateButton(storeContent, "BuyGems3500Btn", "💎 3.500 GEMS (PETI ABYSSAL)\nRp 389.000", new Color(0.4f, 0.2f, 0.6f), Color.white, 22, defaultFont);
            PositionUI(g3500Btn, 0.05f, 0.40f, 0.95f, 0.52f);

            var goldBundBtn = CreateButton(storeContent, "BuyGoldBundleBtn", "🪙 10.000 GOLD\n💎 50 Gems", new Color(0.55f, 0.45f, 0.15f), Color.white, 20, defaultFont);
            PositionUI(goldBundBtn, 0.05f, 0.25f, 0.48f, 0.37f);
            var energyFlaskBtn = CreateButton(storeContent, "BuyEnergyFlaskBtn", "⚡ +30 ENERGI (PENUH)\n💎 30 Gems", new Color(0.15f, 0.45f, 0.55f), Color.white, 20, defaultFont);
            PositionUI(energyFlaskBtn, 0.52f, 0.25f, 0.95f, 0.37f);

            var restoreBtn = CreateButton(storeContent, "RestorePurchasesBtn", "🔄 Pulihkan Pembelian (Restore Purchases)", new Color(0.2f, 0.25f, 0.35f), Color.white, 20, defaultFont);
            PositionUI(restoreBtn, 0.1f, 0.12f, 0.9f, 0.21f);

            var storeFeedback = CreateText(storeContent, "StoreFeedbackText", "", 20, FontStyle.Italic, TextAnchor.MiddleCenter, Color.green, defaultFont);
            PositionUI(storeFeedback.gameObject, 0.05f, 0.02f, 0.95f, 0.09f);

            var storeSO = new SerializedObject(storeView);
            storeSO.FindProperty("rootPanel").objectReferenceValue = storePanel;
            storeSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(storePanel);
            storeSO.FindProperty("accountGemsText").objectReferenceValue = stGemsTxt;
            storeSO.FindProperty("accountGoldText").objectReferenceValue = stGoldTxt;
            storeSO.FindProperty("feedbackText").objectReferenceValue = storeFeedback;
            storeSO.FindProperty("buyStarterPackButton").objectReferenceValue = starterBtn.GetComponent<Button>();
            storeSO.FindProperty("starterPackStatusText").objectReferenceValue = starterStatus;
            storeSO.FindProperty("buyGems120Button").objectReferenceValue = g120Btn.GetComponent<Button>();
            storeSO.FindProperty("buyGems650Button").objectReferenceValue = g650Btn.GetComponent<Button>();
            storeSO.FindProperty("buyGems3500Button").objectReferenceValue = g3500Btn.GetComponent<Button>();
            storeSO.FindProperty("buyGoldBundleButton").objectReferenceValue = goldBundBtn.GetComponent<Button>();
            storeSO.FindProperty("buyEnergyFlaskButton").objectReferenceValue = energyFlaskBtn.GetComponent<Button>();
            storeSO.FindProperty("restorePurchasesButton").objectReferenceValue = restoreBtn.GetComponent<Button>();
            storeSO.ApplyModifiedProperties();
            storePanel.SetActive(false);

            // 5. RELIC FORGE PANEL (Pandai Besi & Crafting)
            var forgePanel = CreateSubPanel(canvasObj.transform, "RelicForgePanel", "PANDAI BESI & RELIC FORGE", defaultFont);
            var forgeView = forgePanel.AddComponent<UIRelicForgeView>();
            var forgeContent = forgePanel.transform.Find("ContentContainer") ?? forgePanel.transform;

            var forgeCur = CreatePanel(forgeContent, "ForgeCurrencies", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(forgeCur, 0.03f, 0.88f, 0.97f, 0.98f);
            var fDustTxt = CreateText(forgeCur.transform, "ForgeDustText", "✨ Dust: 0", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 1f), defaultFont);
            PositionUI(fDustTxt.gameObject, 0.04f, 0.1f, 0.35f, 0.9f);
            var fStoneTxt = CreateText(forgeCur.transform, "RefineStonesText", "💎 Stones: 0", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.3f), defaultFont);
            PositionUI(fStoneTxt.gameObject, 0.36f, 0.1f, 0.65f, 0.9f);
            var fGoldTxt = CreateText(forgeCur.transform, "ForgeGoldText", "🪙 Gold: 500", 20, FontStyle.Bold, TextAnchor.MiddleRight, Color.white, defaultFont);
            PositionUI(fGoldTxt.gameObject, 0.66f, 0.1f, 0.96f, 0.9f);

            var fRefineTab = CreateButton(forgeContent, "RefineTab", "Tempa (+1)", new Color(0.2f, 0.4f, 0.6f), Color.white, 20, defaultFont);
            PositionUI(fRefineTab, 0.04f, 0.77f, 0.33f, 0.86f);
            var fRarityTab = CreateButton(forgeContent, "RarityTab", "Promosi Rarity", new Color(0.2f, 0.25f, 0.38f), Color.white, 20, defaultFont);
            PositionUI(fRarityTab, 0.36f, 0.77f, 0.65f, 0.86f);
            var fSalvageTab = CreateButton(forgeContent, "SalvageTab", "Peleburan (Salvage)", new Color(0.2f, 0.25f, 0.38f), Color.white, 20, defaultFont);
            PositionUI(fSalvageTab, 0.68f, 0.77f, 0.96f, 0.86f);

            var fItemPanel = CreatePanel(forgeContent, "SelectedItemPanel", new Color(0.08f, 0.11f, 0.18f, 0.9f));
            PositionUI(fItemPanel, 0.04f, 0.26f, 0.96f, 0.74f);
            var fItemName = CreateText(fItemPanel.transform, "ItemNameText", "Pedang Baja Ksatria (+0)", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(fItemName.gameObject, 0.05f, 0.80f, 0.95f, 0.95f);
            var fItemSlot = CreateText(fItemPanel.transform, "ItemSlotText", "Slot: Weapon (Senjata)", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(fItemSlot.gameObject, 0.05f, 0.65f, 0.50f, 0.78f);
            var fItemRarity = CreateText(fItemPanel.transform, "ItemRarityText", "Kelangkaan: Rare (Langka)", 20, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.3f, 0.7f, 1f), defaultFont);
            PositionUI(fItemRarity.gameObject, 0.50f, 0.65f, 0.95f, 0.78f);

            var fCurStat = CreateText(fItemPanel.transform, "CurrentStatText", "Stat Saat Ini: Attack +12", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(fCurStat.gameObject, 0.05f, 0.45f, 0.95f, 0.60f);
            var fPreviewStat = CreateText(fItemPanel.transform, "PreviewStatText", "Prediksi Upgrade: Attack +15 (+25%)", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.green, defaultFont);
            PositionUI(fPreviewStat.gameObject, 0.05f, 0.28f, 0.95f, 0.42f);
            var fCostTxt = CreateText(fItemPanel.transform, "ActionCostText", "Biaya: 1 Refine Stone + 200 Gold", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f), defaultFont);
            PositionUI(fCostTxt.gameObject, 0.05f, 0.08f, 0.95f, 0.22f);

            var fActionBtn = CreateButton(forgeContent, "ForgeActionBtn", "TEMPA SEKARANG", new Color(0.95f, 0.72f, 0.16f), new Color(0.12f, 0.08f, 0.02f), 26, defaultFont);
            PositionUI(fActionBtn, 0.15f, 0.12f, 0.85f, 0.23f);

            var fFeedback = CreateText(forgeContent, "ForgeFeedbackText", "", 20, FontStyle.Italic, TextAnchor.MiddleCenter, Color.green, defaultFont);
            PositionUI(fFeedback.gameObject, 0.05f, 0.02f, 0.95f, 0.09f);

            var forgeSO = new SerializedObject(forgeView);
            forgeSO.FindProperty("forgeRootPanel").objectReferenceValue = forgePanel;
            forgeSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(forgePanel);
            forgeSO.FindProperty("forgeDustText").objectReferenceValue = fDustTxt;
            forgeSO.FindProperty("refineStonesText").objectReferenceValue = fStoneTxt;
            forgeSO.FindProperty("goldText").objectReferenceValue = fGoldTxt;
            forgeSO.FindProperty("refineTabButton").objectReferenceValue = fRefineTab.GetComponent<Button>();
            forgeSO.FindProperty("rarityTabButton").objectReferenceValue = fRarityTab.GetComponent<Button>();
            forgeSO.FindProperty("salvageTabButton").objectReferenceValue = fSalvageTab.GetComponent<Button>();
            forgeSO.FindProperty("itemNameText").objectReferenceValue = fItemName;
            forgeSO.FindProperty("itemSlotText").objectReferenceValue = fItemSlot;
            forgeSO.FindProperty("itemRarityText").objectReferenceValue = fItemRarity;
            forgeSO.FindProperty("currentStatText").objectReferenceValue = fCurStat;
            forgeSO.FindProperty("upgradedStatPreviewText").objectReferenceValue = fPreviewStat;
            forgeSO.FindProperty("actionCostText").objectReferenceValue = fCostTxt;
            forgeSO.FindProperty("actionButton").objectReferenceValue = fActionBtn.GetComponent<Button>();
            forgeSO.FindProperty("actionButtonLabel").objectReferenceValue = fActionBtn.GetComponentInChildren<Text>();
            forgeSO.FindProperty("feedbackMessageText").objectReferenceValue = fFeedback;
            forgeSO.ApplyModifiedProperties();
            forgePanel.SetActive(false);

            // 6. CITADEL TALENTS PANEL (Pohon Talenta Benteng)
            var talentPanel = CreateSubPanel(canvasObj.transform, "TalentsPanel", "POHON TALENTA & BERKAH BENTENG", defaultFont);
            var talentView = talentPanel.AddComponent<UICitadelTalentView>();
            var talContent = talentPanel.transform.Find("ContentContainer") ?? talentPanel.transform;

            var talHeader = CreatePanel(talContent, "TalentHeader", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(talHeader, 0.03f, 0.88f, 0.97f, 0.98f);
            var crestsTxt = CreateText(talHeader.transform, "CrestsCountText", "Citadel Crests: 0", 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 1f), defaultFont);
            PositionUI(crestsTxt.gameObject, 0.05f, 0.1f, 0.5f, 0.9f);
            var totRankTxt = CreateText(talHeader.transform, "TotalTalentRankText", "Total Talenta Aktif: Lv. 0", 22, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(totRankTxt.gameObject, 0.5f, 0.1f, 0.95f, 0.9f);

            var talContainer = CreatePanel(talContent, "TalentItemsContainer", new Color(0, 0, 0, 0));
            PositionUI(talContainer, 0.03f, 0.10f, 0.97f, 0.86f);

            string[] talentNames = new string[] {
                "Citadel Aegis Regeneration",
                "Economic Prosperity",
                "Elemental Synergy",
                "Lethal Precision"
            };
            string[] talentDescs = new string[] {
                "Benteng meregenerasi HP secara perlahan (+1% HP tiap 8 detik).",
                "Bonus modal summon (+15 gems) & dividen gems tiap wave selesai.",
                "Peningkatan Attack Speed hero permanen (+6% per rank).",
                "Peningkatan Critical Strike Chance (+3%) & Crit Damage (+15%)."
            };
            for (int i = 0; i < 4; i++)
            {
                float yMax = 0.98f - (i * 0.245f);
                float yMin = yMax - 0.22f;
                var tCard = CreatePanel(talContainer.transform, $"TalentCard_{i + 1}", new Color(0.07f, 0.1f, 0.16f, 0.9f));
                PositionUI(tCard, 0.02f, yMin, 0.98f, yMax);
                var tTitle = CreateText(tCard.transform, "Title", talentNames[i], 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
                PositionUI(tTitle.gameObject, 0.04f, 0.55f, 0.7f, 0.95f);
                var tDesc = CreateText(tCard.transform, "Desc", talentDescs[i], 18, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.85f, 0.9f, 0.95f), defaultFont);
                PositionUI(tDesc.gameObject, 0.04f, 0.1f, 0.7f, 0.52f);
                int talentIndex = i;
                var tUpgBtn = CreateButton(tCard.transform, "UpgradeBtn", "UPGRADE\n(2 Crests)", new Color(0.2f, 0.5f, 0.7f), Color.white, 18, defaultFont);
                PositionUI(tUpgBtn, 0.73f, 0.15f, 0.97f, 0.85f);
                tUpgBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (CitadelTalentManager.Instance != null && talentIndex < CitadelTalentManager.Instance.Talents.Count)
                    {
                        var talent = CitadelTalentManager.Instance.Talents[talentIndex];
                        talentView.OnClickUpgradeTalent(talent);
                    }
                });
            }

            var talFeedback = CreateText(talContent, "TalentFeedbackText", "", 20, FontStyle.Italic, TextAnchor.MiddleCenter, Color.green, defaultFont);
            PositionUI(talFeedback.gameObject, 0.05f, 0.02f, 0.95f, 0.08f);

            var talentSO = new SerializedObject(talentView);
            talentSO.FindProperty("talentPanel").objectReferenceValue = talentPanel;
            talentSO.FindProperty("closeTalentButton").objectReferenceValue = FindCloseButton(talentPanel);
            talentSO.FindProperty("crestsCountText").objectReferenceValue = crestsTxt;
            talentSO.FindProperty("totalTalentRankText").objectReferenceValue = totRankTxt;
            talentSO.FindProperty("feedbackText").objectReferenceValue = talFeedback;
            talentSO.FindProperty("talentItemsContainer").objectReferenceValue = talContainer.transform;
            talentSO.ApplyModifiedProperties();
            talentPanel.SetActive(false);

            // 7. QUESTS PANEL (Misi Harian & Pencapaian)
            var questPanel = CreateSubPanel(canvasObj.transform, "QuestsPanel", "MISI HARIAN & PENCAPAIAN", defaultFont);
            var questView = questPanel.AddComponent<UIQuestView>();
            var questContent = questPanel.transform.Find("ContentContainer") ?? questPanel.transform;

            var qDailyTab = CreateButton(questContent, "DailyTabBtn", "Misi Harian", new Color(0.2f, 0.45f, 0.65f), Color.white, 22, defaultFont);
            PositionUI(qDailyTab, 0.04f, 0.88f, 0.48f, 0.97f);
            var qAchieveTab = CreateButton(questContent, "AchieveTabBtn", "Pencapaian Kerajaan", new Color(0.2f, 0.25f, 0.38f), Color.white, 22, defaultFont);
            PositionUI(qAchieveTab, 0.52f, 0.88f, 0.96f, 0.97f);

            var qResetTxt = CreateText(questContent, "ResetCountdownText", "Reset Misi Dalam: 23j 59m 59d", 20, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f), defaultFont);
            PositionUI(qResetTxt.gameObject, 0.05f, 0.80f, 0.95f, 0.86f);

            var qContainer = CreatePanel(questContent, "QuestCardContainer", new Color(0.04f, 0.06f, 0.1f, 0.8f));
            PositionUI(qContainer, 0.04f, 0.05f, 0.96f, 0.78f);

            var questSO = new SerializedObject(questView);
            questSO.FindProperty("questPanel").objectReferenceValue = questPanel;
            questSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(questPanel);
            questSO.FindProperty("dailyTabButton").objectReferenceValue = qDailyTab.GetComponent<Button>();
            questSO.FindProperty("achievementTabButton").objectReferenceValue = qAchieveTab.GetComponent<Button>();
            questSO.FindProperty("resetCountdownText").objectReferenceValue = qResetTxt;
            questSO.FindProperty("questCardContainer").objectReferenceValue = qContainer.transform;
            questSO.ApplyModifiedProperties();
            questPanel.SetActive(false);

            // 8. GUILD PANEL (Aliansi Guild Citadel)
            var guildPanel = CreateSubPanel(canvasObj.transform, "GuildPanel", "ALIANSI GUILD & SERBUAN TITAN", defaultFont);
            var guildView = guildPanel.AddComponent<UIGuildView>();
            var guildContent = guildPanel.transform.Find("ContentContainer") ?? guildPanel.transform;

            var gTab1 = CreateButton(guildContent, "TabOverview", "Ringkasan", new Color(0.2f, 0.45f, 0.65f), Color.white, 20, defaultFont);
            PositionUI(gTab1, 0.04f, 0.89f, 0.33f, 0.98f);
            var gTab2 = CreateButton(guildContent, "TabExpeditions", "Ekspedisi", new Color(0.2f, 0.25f, 0.38f), Color.white, 20, defaultFont);
            PositionUI(gTab2, 0.36f, 0.89f, 0.65f, 0.98f);
            var gTab3 = CreateButton(guildContent, "TabTitanRaid", "Serbuan Titan", new Color(0.2f, 0.25f, 0.38f), Color.white, 20, defaultFont);
            PositionUI(gTab3, 0.68f, 0.89f, 0.96f, 0.98f);

            var gOverviewPanel = CreatePanel(guildContent, "OverviewPanel", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(gOverviewPanel, 0.04f, 0.05f, 0.96f, 0.86f);
            var gNameTxt = CreateText(gOverviewPanel.transform, "GuildNameText", "Aliansi Benteng Nusantara", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(gNameTxt.gameObject, 0.05f, 0.85f, 0.65f, 0.96f);
            var gLvlTxt = CreateText(gOverviewPanel.transform, "GuildLevelText", "Lv. 3", 24, FontStyle.Bold, TextAnchor.MiddleRight, Color.white, defaultFont);
            PositionUI(gLvlTxt.gameObject, 0.65f, 0.85f, 0.95f, 0.96f);
            var gExpSlider = CreateProgressBar(gOverviewPanel.transform, "GuildExpSlider", 0, 1000, 450, new Color(0.25f, 0.65f, 1f));
            PositionUI(gExpSlider, 0.05f, 0.72f, 0.95f, 0.82f);
            var gExpTxt = CreateText(gOverviewPanel.transform, "GuildExpText", "EXP Aliansi: 450 / 1.000", 18, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(gExpTxt.gameObject, 0.1f, 0.62f, 0.9f, 0.70f);
            var gTokensTxt = CreateText(gOverviewPanel.transform, "GuildTokensText", "🪙 Token Guild Kamu: 150", 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 1f), defaultFont);
            PositionUI(gTokensTxt.gameObject, 0.05f, 0.48f, 0.95f, 0.58f);
            var gNoticeTxt = CreateText(gOverviewPanel.transform, "GuildNoticeText", "Pengumuman: Bersiap untuk penyerangan Titan Kolosal setiap akhir pekan!", 20, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 0.95f), defaultFont);
            PositionUI(gNoticeTxt.gameObject, 0.05f, 0.25f, 0.95f, 0.45f);
            var gDonateBtn = CreateButton(gOverviewPanel.transform, "DonateButton", "DONASI 100 GOLD KE ALIANSI", new Color(0.95f, 0.72f, 0.16f), new Color(0.12f, 0.08f, 0.02f), 22, defaultFont);
            PositionUI(gDonateBtn, 0.15f, 0.08f, 0.85f, 0.20f);
            var gDonateFeedback = CreateText(gOverviewPanel.transform, "DonationFeedbackText", "", 18, FontStyle.Italic, TextAnchor.MiddleCenter, Color.green, defaultFont);
            PositionUI(gDonateFeedback.gameObject, 0.05f, 0.01f, 0.95f, 0.07f);

            var gExpPanel = CreatePanel(guildContent, "ExpeditionsPanel", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(gExpPanel, 0.04f, 0.05f, 0.96f, 0.86f);
            gExpPanel.SetActive(false);

            var gTitanPanel = CreatePanel(guildContent, "TitanRaidPanel", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(gTitanPanel, 0.04f, 0.05f, 0.96f, 0.86f);
            var titanName = CreateText(gTitanPanel.transform, "TitanNameText", "Titan Kolosal: Behemoth Abyssal", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f), defaultFont);
            PositionUI(titanName.gameObject, 0.05f, 0.85f, 0.95f, 0.96f);
            var titanSlider = CreateProgressBar(gTitanPanel.transform, "TitanHpSlider", 0, 100000, 85000, new Color(0.9f, 0.25f, 0.25f));
            PositionUI(titanSlider, 0.05f, 0.70f, 0.95f, 0.82f);
            var titanHpTxt = CreateText(gTitanPanel.transform, "TitanHpText", "HP: 85.000 / 100.000 (85%)", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(titanHpTxt.gameObject, 0.05f, 0.58f, 0.95f, 0.68f);
            var titanAtkBtn = CreateButton(gTitanPanel.transform, "AttackTitanBtn", "⚔️ SERANG TITAN ABYSSAL!", new Color(0.85f, 0.25f, 0.25f), Color.white, 24, defaultFont);
            PositionUI(titanAtkBtn, 0.15f, 0.15f, 0.85f, 0.28f);
            var titanFeedback = CreateText(gTitanPanel.transform, "AttackFeedbackText", "", 20, FontStyle.Italic, TextAnchor.MiddleCenter, Color.green, defaultFont);
            PositionUI(titanFeedback.gameObject, 0.05f, 0.04f, 0.95f, 0.12f);
            gTitanPanel.SetActive(false);

            var guildSO = new SerializedObject(guildView);
            guildSO.FindProperty("rootPanel").objectReferenceValue = guildPanel;
            guildSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(guildPanel);
            guildSO.FindProperty("tabOverviewButton").objectReferenceValue = gTab1.GetComponent<Button>();
            guildSO.FindProperty("tabExpeditionsButton").objectReferenceValue = gTab2.GetComponent<Button>();
            guildSO.FindProperty("tabTitanRaidButton").objectReferenceValue = gTab3.GetComponent<Button>();
            guildSO.FindProperty("overviewPanel").objectReferenceValue = gOverviewPanel;
            guildSO.FindProperty("expeditionsPanel").objectReferenceValue = gExpPanel;
            guildSO.FindProperty("titanRaidPanel").objectReferenceValue = gTitanPanel;
            guildSO.FindProperty("guildNameText").objectReferenceValue = gNameTxt;
            guildSO.FindProperty("guildLevelText").objectReferenceValue = gLvlTxt;
            guildSO.FindProperty("guildExpSlider").objectReferenceValue = gExpSlider.GetComponent<Slider>();
            guildSO.FindProperty("guildExpText").objectReferenceValue = gExpTxt;
            guildSO.FindProperty("guildTokensText").objectReferenceValue = gTokensTxt;
            guildSO.FindProperty("guildNoticeText").objectReferenceValue = gNoticeTxt;
            guildSO.FindProperty("donateButton").objectReferenceValue = gDonateBtn.GetComponent<Button>();
            guildSO.FindProperty("donationFeedbackText").objectReferenceValue = gDonateFeedback;
            guildSO.FindProperty("titanNameText").objectReferenceValue = titanName;
            guildSO.FindProperty("titanHpSlider").objectReferenceValue = titanSlider.GetComponent<Slider>();
            guildSO.FindProperty("titanHpText").objectReferenceValue = titanHpTxt;
            guildSO.FindProperty("attackTitanButton").objectReferenceValue = titanAtkBtn.GetComponent<Button>();
            guildSO.FindProperty("attackFeedbackText").objectReferenceValue = titanFeedback;
            guildSO.ApplyModifiedProperties();
            guildPanel.SetActive(false);

            // 9. BATTLE PASS PANEL (Citadel Season Pass 30 Tiers)
            var passPanel = CreateSubPanel(canvasObj.transform, "BattlePassPanel", "CITADEL SEASON PASS (30 TIERS)", defaultFont);
            var passView = passPanel.AddComponent<UIBattlePassView>();
            var passContent = passPanel.transform.Find("ContentContainer") ?? passPanel.transform;

            var passHeader = CreatePanel(passContent, "PassHeader", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(passHeader, 0.04f, 0.72f, 0.96f, 0.98f);
            var pSeasonTxt = CreateText(passHeader.transform, "SeasonTitleText", "Season 1: Penaklukan Abyssal", 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(pSeasonTxt.gameObject, 0.05f, 0.65f, 0.65f, 0.95f);
            var pLvlTxt = CreateText(passHeader.transform, "PassLevelText", "Level Pass: 1", 22, FontStyle.Bold, TextAnchor.MiddleRight, Color.white, defaultFont);
            PositionUI(pLvlTxt.gameObject, 0.65f, 0.65f, 0.95f, 0.95f);
            var pExpSlider = CreateProgressBar(passHeader.transform, "PassExpSlider", 0, 100, 0, new Color(0.25f, 0.85f, 0.45f));
            PositionUI(pExpSlider, 0.05f, 0.35f, 0.95f, 0.55f);
            var pExpTxt = CreateText(passHeader.transform, "PassExpText", "EXP: 0 / 100", 18, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(pExpTxt.gameObject, 0.1f, 0.15f, 0.9f, 0.32f);

            var unlockEliteBtn = CreateButton(passContent, "UnlockEliteButton", "👑 BUKA ELITE PASS (💎 500 GEMS)\nDapatkan Hadiah Ganda & Skin Eksklusif!", new Color(0.85f, 0.55f, 0.15f), Color.white, 22, defaultFont);
            PositionUI(unlockEliteBtn, 0.08f, 0.55f, 0.92f, 0.68f);

            var spotlightPanel = CreatePanel(passContent, "TierSpotlightPanel", new Color(0.08f, 0.12f, 0.2f, 0.9f));
            PositionUI(spotlightPanel, 0.05f, 0.20f, 0.95f, 0.50f);
            var tierTitle = CreateText(spotlightPanel.transform, "TierTitleText", "Hadiah Tier 1 Saat Ini", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(tierTitle.gameObject, 0.05f, 0.78f, 0.95f, 0.95f);
            var freeDesc = CreateText(spotlightPanel.transform, "FreeDescText", "Jalur Gratis: 🪙 500 Gold", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(freeDesc.gameObject, 0.05f, 0.50f, 0.65f, 0.72f);
            var claimFreeBtn = CreateButton(spotlightPanel.transform, "ClaimFreeBtn", "KLAIM", new Color(0.2f, 0.6f, 0.35f), Color.white, 20, defaultFont);
            PositionUI(claimFreeBtn, 0.70f, 0.48f, 0.95f, 0.74f);
            var eliteDesc = CreateText(spotlightPanel.transform, "EliteDescText", "Jalur Elite: 💎 100 Gems + 10 Wind Shards", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(eliteDesc.gameObject, 0.05f, 0.18f, 0.65f, 0.40f);
            var claimEliteBtn = CreateButton(spotlightPanel.transform, "ClaimEliteBtn", "KLAIM ELITE", new Color(0.85f, 0.55f, 0.15f), Color.white, 18, defaultFont);
            PositionUI(claimEliteBtn, 0.70f, 0.16f, 0.95f, 0.42f);

            var claimAllBtn = CreateButton(passContent, "ClaimAllButton", "🎁 KLAIM SEMUA HADIAH TERSEDIA", new Color(0.2f, 0.45f, 0.7f), Color.white, 24, defaultFont);
            PositionUI(claimAllBtn, 0.12f, 0.05f, 0.88f, 0.15f);

            var passSO = new SerializedObject(passView);
            passSO.FindProperty("rootPanel").objectReferenceValue = passPanel;
            passSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(passPanel);
            passSO.FindProperty("seasonTitleText").objectReferenceValue = pSeasonTxt;
            passSO.FindProperty("passLevelText").objectReferenceValue = pLvlTxt;
            passSO.FindProperty("passExpSlider").objectReferenceValue = pExpSlider.GetComponent<Slider>();
            passSO.FindProperty("passExpText").objectReferenceValue = pExpTxt;
            passSO.FindProperty("unlockEliteButton").objectReferenceValue = unlockEliteBtn.GetComponent<Button>();
            passSO.FindProperty("currentTierTitleText").objectReferenceValue = tierTitle;
            passSO.FindProperty("freeRewardDescText").objectReferenceValue = freeDesc;
            passSO.FindProperty("claimFreeTierButton").objectReferenceValue = claimFreeBtn.GetComponent<Button>();
            passSO.FindProperty("eliteRewardDescText").objectReferenceValue = eliteDesc;
            passSO.FindProperty("claimEliteTierButton").objectReferenceValue = claimEliteBtn.GetComponent<Button>();
            passSO.FindProperty("claimAllButton").objectReferenceValue = claimAllBtn.GetComponent<Button>();
            passSO.ApplyModifiedProperties();
            passPanel.SetActive(false);

            // 10. EVENT ARENA PANEL
            var eventPanel = CreateSubPanel(canvasObj.transform, "EventArenaPanel", "ARENA TANTANGAN LIVE EVENT", defaultFont);
            var eventView = eventPanel.AddComponent<UIEventArenaView>();
            var eventContent = eventPanel.transform.Find("ContentContainer") ?? eventPanel.transform;

            var evHeader = CreatePanel(eventContent, "EventHeader", new Color(0.12f, 0.06f, 0.08f, 0.95f));
            PositionUI(evHeader, 0.04f, 0.70f, 0.96f, 0.98f);
            var evTitle = CreateText(evHeader.transform, "EventTitleText", "🔥 Serbuan Abyssal: Tantangan Naga Berapi", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.4f, 0.3f), defaultFont);
            PositionUI(evTitle.gameObject, 0.05f, 0.65f, 0.95f, 0.95f);
            var evDesc = CreateText(evHeader.transform, "EventDescText", "Modifier Medan: HP Monster +30%, Drop Gems Berlipat Ganda 2x!", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(evDesc.gameObject, 0.05f, 0.25f, 0.95f, 0.60f);

            var evStatusPanel = CreatePanel(eventContent, "EventStatusPanel", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(evStatusPanel, 0.04f, 0.35f, 0.96f, 0.65f);
            var evAttempts = CreateText(evStatusPanel.transform, "AttemptsLeftText", "Tiket Masuk: 3 / 3 Hari Ini", 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 1f), defaultFont);
            PositionUI(evAttempts.gameObject, 0.05f, 0.65f, 0.95f, 0.90f);
            var evHighScore = CreateText(evStatusPanel.transform, "HighScoreText", "Skor Tertinggi Kamu: 0 Poin", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(evHighScore.gameObject, 0.05f, 0.35f, 0.95f, 0.60f);
            var evRank = CreateText(evStatusPanel.transform, "CurrentRankText", "Peringkat Global: Belum Terdaftar", 22, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(evRank.gameObject, 0.05f, 0.05f, 0.95f, 0.30f);

            var evChallengeBtn = CreateButton(eventContent, "ChallengeNowBtn", "⚔️ MASUK KE ARENA TANTANGAN", new Color(0.92f, 0.35f, 0.18f), Color.white, 26, defaultFont);
            PositionUI(evChallengeBtn, 0.10f, 0.16f, 0.90f, 0.28f);

            var evBuyBtn = CreateButton(eventContent, "BuyAttemptBtn", "Beli Tiket Tambahan (💎 50 Gems)", new Color(0.2f, 0.3f, 0.45f), Color.white, 20, defaultFont);
            PositionUI(evBuyBtn, 0.20f, 0.04f, 0.80f, 0.13f);

            var eventSO = new SerializedObject(eventView);
            eventSO.FindProperty("arenaRootPanel").objectReferenceValue = eventPanel;
            eventSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(eventPanel);
            eventSO.FindProperty("eventTitleText").objectReferenceValue = evTitle;
            eventSO.FindProperty("eventDescriptionText").objectReferenceValue = evDesc;
            eventSO.FindProperty("attemptsLeftText").objectReferenceValue = evAttempts;
            eventSO.FindProperty("highScoreText").objectReferenceValue = evHighScore;
            eventSO.FindProperty("currentRankText").objectReferenceValue = evRank;
            eventSO.FindProperty("challengeNowButton").objectReferenceValue = evChallengeBtn.GetComponent<Button>();
            eventSO.FindProperty("buyAttemptButton").objectReferenceValue = evBuyBtn.GetComponent<Button>();
            eventSO.ApplyModifiedProperties();
            eventPanel.SetActive(false);

            // 11. ARTIFACTS PANEL (Artefak & Sinergi Set)
            var artifactPanel = CreateSubPanel(canvasObj.transform, "ArtifactsPanel", "ARTEFAK & SET SINERGI", defaultFont);
            var artifactView = artifactPanel.AddComponent<UIArtifactView>();
            var artContent = artifactPanel.transform.Find("ContentContainer") ?? artifactPanel.transform;

            var artEquipPanel = CreatePanel(artContent, "EquippedSlots", new Color(0.06f, 0.08f, 0.14f, 0.9f));
            PositionUI(artEquipPanel, 0.04f, 0.55f, 0.96f, 0.98f);
            var helmTxt = CreateText(artEquipPanel.transform, "HelmSlotText", "👑 Mahkota Pelindung Benteng: [Kosong]", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(helmTxt.gameObject, 0.05f, 0.75f, 0.95f, 0.95f);
            var sigilTxt = CreateText(artEquipPanel.transform, "SigilSlotText", "🔮 Lambang Rune Kuno: [Kosong]", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(sigilTxt.gameObject, 0.05f, 0.50f, 0.95f, 0.70f);
            var ringTxt = CreateText(artEquipPanel.transform, "RingSlotText", "💍 Cincin Kekuatan Abyssal: [Kosong]", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(ringTxt.gameObject, 0.05f, 0.25f, 0.95f, 0.45f);
            var tomeTxt = CreateText(artEquipPanel.transform, "TomeSlotText", "📖 Kitab Mantra Purba: [Kosong]", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(tomeTxt.gameObject, 0.05f, 0.02f, 0.95f, 0.22f);

            var artSynergyPanel = CreatePanel(artContent, "SetSynergies", new Color(0.08f, 0.11f, 0.18f, 0.9f));
            PositionUI(artSynergyPanel, 0.04f, 0.08f, 0.96f, 0.50f);
            var synTitle = CreateText(artSynergyPanel.transform, "SynergyTitle", "Status Sinergi Set Artefak", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(synTitle.gameObject, 0.05f, 0.82f, 0.95f, 0.95f);
            var gVowTxt = CreateText(artSynergyPanel.transform, "GuardiansVowText", "🛡️ Guardian's Vow (0/4): Barikade HP +20%", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f), defaultFont);
            PositionUI(gVowTxt.gameObject, 0.05f, 0.55f, 0.95f, 0.75f);
            var bFuryTxt = CreateText(artSynergyPanel.transform, "BerserkersFuryText", "⚔️ Berserker's Fury (0/4): Attack Hero +25%", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f), defaultFont);
            PositionUI(bFuryTxt.gameObject, 0.05f, 0.30f, 0.95f, 0.50f);
            var sSurgeTxt = CreateText(artSynergyPanel.transform, "SorcerersSurgeText", "⚡ Sorcerer's Surge (0/4): Mana Skill Regen +30%", 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f), defaultFont);
            PositionUI(sSurgeTxt.gameObject, 0.05f, 0.05f, 0.95f, 0.25f);

            var artifactSO = new SerializedObject(artifactView);
            artifactSO.FindProperty("viewPanel").objectReferenceValue = artifactPanel;
            artifactSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(artifactPanel);
            artifactSO.FindProperty("helmSlotText").objectReferenceValue = helmTxt;
            artifactSO.FindProperty("sigilSlotText").objectReferenceValue = sigilTxt;
            artifactSO.FindProperty("ringSlotText").objectReferenceValue = ringTxt;
            artifactSO.FindProperty("tomeSlotText").objectReferenceValue = tomeTxt;
            artifactSO.FindProperty("guardiansVowStatusText").objectReferenceValue = gVowTxt;
            artifactSO.FindProperty("berserkersFuryStatusText").objectReferenceValue = bFuryTxt;
            artifactSO.FindProperty("sorcerersSurgeStatusText").objectReferenceValue = sSurgeTxt;
            artifactSO.ApplyModifiedProperties();
            artifactPanel.SetActive(false);

            // 12. AUDIO SETTINGS PANEL
            var settingsPanel = CreateSubPanel(canvasObj.transform, "AudioSettingsPanel", "PENGATURAN AUDIO & SISTEM", defaultFont);
            var settingsView = settingsPanel.AddComponent<UIAudioSettingsView>();
            var setContent = settingsPanel.transform.Find("ContentContainer") ?? settingsPanel.transform;

            var mSlider = CreateSlider(setContent, "MasterSlider", 0, 1, 1);
            PositionUI(mSlider, 0.35f, 0.85f, 0.78f, 0.93f);
            var mTxt = CreateText(setContent, "MasterPercentText", "Master: 100%", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(mTxt.gameObject, 0.05f, 0.85f, 0.33f, 0.93f);

            var bgmSlider = CreateSlider(setContent, "BgmSlider", 0, 1, 1);
            PositionUI(bgmSlider, 0.35f, 0.74f, 0.78f, 0.82f);
            var bgmTxt = CreateText(setContent, "BgmPercentText", "BGM: 100%", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(bgmTxt.gameObject, 0.05f, 0.74f, 0.33f, 0.82f);
            var bgmMute = CreateToggle(setContent, "BgmMuteToggle", "Mute", false, defaultFont);
            PositionUI(bgmMute, 0.80f, 0.74f, 0.98f, 0.82f);

            var sfxSlider = CreateSlider(setContent, "SfxSlider", 0, 1, 1);
            PositionUI(sfxSlider, 0.35f, 0.63f, 0.78f, 0.71f);
            var sfxTxt = CreateText(setContent, "SfxPercentText", "SFX: 100%", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, defaultFont);
            PositionUI(sfxTxt.gameObject, 0.05f, 0.63f, 0.33f, 0.71f);
            var sfxMute = CreateToggle(setContent, "SfxMuteToggle", "Mute", false, defaultFont);
            PositionUI(sfxMute, 0.80f, 0.63f, 0.98f, 0.71f);

            var fps30 = CreateButton(setContent, "Fps30Btn", "30 FPS", new Color(0.2f, 0.3f, 0.45f), Color.white, 20, defaultFont);
            PositionUI(fps30, 0.05f, 0.48f, 0.32f, 0.58f);
            var fps60 = CreateButton(setContent, "Fps60Btn", "60 FPS (Halus)", new Color(0.2f, 0.5f, 0.35f), Color.white, 20, defaultFont);
            PositionUI(fps60, 0.36f, 0.48f, 0.70f, 0.58f);
            var curFpsTxt = CreateText(setContent, "CurrentFpsText", "Target: 60 FPS", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(curFpsTxt.gameObject, 0.72f, 0.48f, 0.98f, 0.58f);

            var batToggle = CreateToggle(setContent, "BatterySaverToggle", "Mode Hemat Baterai", false, defaultFont);
            PositionUI(batToggle, 0.05f, 0.37f, 0.48f, 0.45f);
            var hapToggle = CreateToggle(setContent, "HapticToggle", "Getaran Haptic Ponsel", true, defaultFont);
            PositionUI(hapToggle, 0.52f, 0.37f, 0.95f, 0.45f);

            var langBtn = CreateButton(setContent, "LanguageSwitchBtn", "🌐 GANTI BAHASA (SWITCH)", new Color(0.2f, 0.45f, 0.55f), Color.white, 20, defaultFont);
            PositionUI(langBtn, 0.05f, 0.26f, 0.52f, 0.35f);
            var langStatusTxt = CreateText(setContent, "LanguageStatusText", "Bahasa: [ ID ] (Indonesia)", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(langStatusTxt.gameObject, 0.55f, 0.26f, 0.95f, 0.35f);

            var testBtn = CreateButton(setContent, "TestSfxBtn", "🔊 UJI COBA SUARA EFEK (TEST SFX)", new Color(0.25f, 0.4f, 0.65f), Color.white, 22, defaultFont);
            PositionUI(testBtn, 0.1f, 0.16f, 0.9f, 0.24f);

            var accountInfoTxt = CreateText(setContent, "AccountStatusText", "☁️ STATUS AKUN: MODE TAMU (GUEST) • AUTO-SAVE AKTIF\n<size=15><color=#AAAAAA>Progres tersimpan otomatis di perangkat secara aman. Hapus Cache / Data Aplikasi di Pengaturan Ponsel jika ingin memulai dari awal.</color></size>", 17, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.85f, 0.95f, 1f), defaultFont);
            PositionUI(accountInfoTxt.gameObject, 0.05f, 0.02f, 0.95f, 0.14f);

            var settingsSO = new SerializedObject(settingsView);
            settingsSO.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            settingsSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(settingsPanel);
            settingsSO.FindProperty("masterSlider").objectReferenceValue = mSlider.GetComponent<Slider>();
            settingsSO.FindProperty("masterPercentText").objectReferenceValue = mTxt;
            settingsSO.FindProperty("bgmSlider").objectReferenceValue = bgmSlider.GetComponent<Slider>();
            settingsSO.FindProperty("bgmPercentText").objectReferenceValue = bgmTxt;
            settingsSO.FindProperty("sfxSlider").objectReferenceValue = sfxSlider.GetComponent<Slider>();
            settingsSO.FindProperty("sfxPercentText").objectReferenceValue = sfxTxt;
            settingsSO.FindProperty("bgmMuteToggle").objectReferenceValue = bgmMute.GetComponent<Toggle>();
            settingsSO.FindProperty("sfxMuteToggle").objectReferenceValue = sfxMute.GetComponent<Toggle>();
            settingsSO.FindProperty("fps30Button").objectReferenceValue = fps30.GetComponent<Button>();
            settingsSO.FindProperty("fps60Button").objectReferenceValue = fps60.GetComponent<Button>();
            settingsSO.FindProperty("currentFpsText").objectReferenceValue = curFpsTxt;
            settingsSO.FindProperty("batterySaverToggle").objectReferenceValue = batToggle.GetComponent<Toggle>();
            settingsSO.FindProperty("hapticFeedbackToggle").objectReferenceValue = hapToggle.GetComponent<Toggle>();
            settingsSO.FindProperty("languageToggleButton").objectReferenceValue = langBtn.GetComponent<Button>();
            settingsSO.FindProperty("languageStatusText").objectReferenceValue = langStatusTxt;
            settingsSO.FindProperty("testSfxButton").objectReferenceValue = testBtn.GetComponent<Button>();
            settingsSO.ApplyModifiedProperties();
            settingsPanel.SetActive(false);

            // ========================================================
            // Hub Pratinjau Seluruh Fitur (UIFeaturePreviewHubView)
            // ========================================================
            var previewHubObj = CreateFullScreenPanel(canvasObj.transform, "FeaturePreviewHubPanel", new Color(0.04f, 0.06f, 0.12f, 0.98f));
            var previewHubView = previewHubObj.AddComponent<UIFeaturePreviewHubView>();

            var hubHeader = CreatePanel(previewHubObj.transform, "Header", new Color(0.12f, 0.16f, 0.28f, 0.95f));
            PositionUI(hubHeader, 0.04f, 0.92f, 0.96f, 0.98f);
            var hubTitleTxt = CreateText(hubHeader.transform, "Title", "🏛️ PRATINJAU FITUR CITADEL", 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f), defaultFont);

            var hubCloseBtn = CreateButton(hubHeader.transform, "CloseButton", "✕", new Color(0.7f, 0.2f, 0.2f), Color.white, 24, defaultFont);
            PositionUI(hubCloseBtn, 0.86f, 0.1f, 0.98f, 0.9f);

            var hubSubTxt = CreateText(previewHubObj.transform, "Subtitle", "Jelajahi seluruh sistem metagame & mode pertahanan yang telah disiapkan untuk Citadel!", 18, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, defaultFont);
            PositionUI(hubSubTxt.gameObject, 0.05f, 0.86f, 0.95f, 0.91f);

            var previewCardsContainer = CreatePanel(previewHubObj.transform, "CardsContainer", new Color(0, 0, 0, 0));
            PositionUI(previewCardsContainer, 0.04f, 0.03f, 0.96f, 0.85f);

            var btnPrevTalents = CreateButton(previewCardsContainer.transform, "CardTalents", "🌲 POHON TALENTA CITADEL\n<color=#00FFFF>[TERSEDIA - JELAJAHI]</color>\n3 Cabang Talenta: Pertahanan Benteng, Deviden Gems, & Sinergi Hero", new Color(0.15f, 0.22f, 0.35f), Color.white, 16, defaultFont);
            PositionUI(btnPrevTalents, 0.02f, 0.86f, 0.98f, 0.98f);

            var btnPrevForge = CreateButton(previewCardsContainer.transform, "CardForge", "🔨 BENGKEL TEMPA RELIK (FORGE)\n<color=#00FFFF>[TERSEDIA - JELAJAHI]</color>\nSalvage gear scrap, penempaan +1 s/d +5, dan promosi kelangkaan gear", new Color(0.15f, 0.22f, 0.35f), Color.white, 16, defaultFont);
            PositionUI(btnPrevForge, 0.02f, 0.72f, 0.98f, 0.84f);

            var btnPrevEndless = CreateButton(previewCardsContainer.transform, "CardEndless", "🌌 UJIAN ABYSSAL (ENDLESS TRIAL)\n<color=#00FFFF>[TERSEDIA - JELAJAHI]</color>\nMode pertahanan wave tak terbatas, penskalaan musuh dinamis, dan Trial Tokens", new Color(0.15f, 0.22f, 0.35f), Color.white, 16, defaultFont);
            PositionUI(btnPrevEndless, 0.02f, 0.58f, 0.98f, 0.70f);

            var btnPrevGuild = CreateButton(previewCardsContainer.transform, "CardGuild", "🏰 EKSPEDISI GUILD & TITAN RAID\n<color=#00FFFF>[TERSEDIA - JELAJAHI]</color>\nMarkas Guild komandan, absensi harian, donasi peti ekspedisi, dan Titan Boss Raid", new Color(0.15f, 0.22f, 0.35f), Color.white, 16, defaultFont);
            PositionUI(btnPrevGuild, 0.02f, 0.44f, 0.98f, 0.56f);

            var btnPrevPass = CreateButton(previewCardsContainer.transform, "CardBattlePass", "🎖️ CITADEL SEASON PASS\n<color=#00FFFF>[TERSEDIA - JELAJAHI]</color>\n30 Tingkat Progres Musiman Jalur Gratis & Elit berhadiah avatar dan peti relik", new Color(0.15f, 0.22f, 0.35f), Color.white, 16, defaultFont);
            PositionUI(btnPrevPass, 0.02f, 0.30f, 0.98f, 0.42f);

            var btnPrevEvent = CreateButton(previewCardsContainer.transform, "CardEventArena", "🔥 ARENA TANTANGAN LIVE EVENT\n<color=#00FFFF>[TERSEDIA - JELAJAHI]</color>\nTantangan berbatas waktu dengan afiks cuaca ekstrem dan bos raid berhadiah koin event", new Color(0.15f, 0.22f, 0.35f), Color.white, 16, defaultFont);
            PositionUI(btnPrevEvent, 0.02f, 0.16f, 0.98f, 0.28f);

            var btnPrevArtifacts = CreateButton(previewCardsContainer.transform, "CardArtifacts", "👑 ARTEFAK KUNO & SINERGI SET\n<color=#00FFFF>[TERSEDIA - JELAJAHI]</color>\nSinergi 2-piece / 4-piece Guardian's Vow, Berserker's Fury, dan Sorcerer's Surge", new Color(0.15f, 0.22f, 0.35f), Color.white, 16, defaultFont);
            PositionUI(btnPrevArtifacts, 0.02f, 0.02f, 0.98f, 0.14f);

            var hubSO = new SerializedObject(previewHubView);
            hubSO.FindProperty("previewHubPanel").objectReferenceValue = previewHubObj;
            hubSO.FindProperty("hubTitleText").objectReferenceValue = hubTitleTxt;
            hubSO.FindProperty("hubSubtitleText").objectReferenceValue = hubSubTxt;
            hubSO.FindProperty("closeHubButton").objectReferenceValue = hubCloseBtn.GetComponent<Button>();
            hubSO.FindProperty("previewTalentsButton").objectReferenceValue = btnPrevTalents.GetComponent<Button>();
            hubSO.FindProperty("previewForgeButton").objectReferenceValue = btnPrevForge.GetComponent<Button>();
            hubSO.FindProperty("previewEndlessButton").objectReferenceValue = btnPrevEndless.GetComponent<Button>();
            hubSO.FindProperty("previewGuildButton").objectReferenceValue = btnPrevGuild.GetComponent<Button>();
            hubSO.FindProperty("previewPassButton").objectReferenceValue = btnPrevPass.GetComponent<Button>();
            hubSO.FindProperty("previewEventButton").objectReferenceValue = btnPrevEvent.GetComponent<Button>();
            hubSO.FindProperty("previewArtifactsButton").objectReferenceValue = btnPrevArtifacts.GetComponent<Button>();
            hubSO.ApplyModifiedProperties();
            previewHubObj.SetActive(false);

            // 13. ENDLESS TRIAL PANEL (Endless Abyssal Trial)
            var endlessPanel = CreateSubPanel(canvasObj.transform, "EndlessTrialPanel", "ENDLESS ABYSSAL TRIAL", defaultFont);
            var endlessView = endlessPanel.AddComponent<UIEndlessTrialView>();
            var endlessContent = endlessPanel.transform.Find("ContentContainer") ?? endlessPanel.transform;

            var eSummaryPanel = CreatePanel(endlessContent, "SummaryPanel", new Color(0.08f, 0.06f, 0.14f, 0.95f));
            PositionUI(eSummaryPanel, 0.05f, 0.40f, 0.95f, 0.95f);
            var eFloorTxt = CreateText(eSummaryPanel.transform, "HighestFloorText", "Lantai Abyssal Tertinggi: Lantai 0", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.8f, 0.4f, 1f), defaultFont);
            PositionUI(eFloorTxt.gameObject, 0.05f, 0.78f, 0.95f, 0.95f);
            var eResetTxt = CreateText(eSummaryPanel.transform, "WeeklyResetCountdownText", "Reset Hadiah Mingguan: 6 Hari 12 Jam", 20, FontStyle.Italic, TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f), defaultFont);
            PositionUI(eResetTxt.gameObject, 0.05f, 0.60f, 0.95f, 0.75f);
            var eCostTxt = CreateText(eSummaryPanel.transform, "EnergyCostText", "Biaya Stamina: ⚡ 10 Stamina", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.3f, 0.9f, 1f), defaultFont);
            PositionUI(eCostTxt.gameObject, 0.05f, 0.42f, 0.95f, 0.55f);
            var eDescTxt = CreateText(eSummaryPanel.transform, "TrialDescriptionText", "Uji ketahanan kuartet pahlawanmu dalam pertempuran gelombang tanpa akhir. Raih Scrap, Forge Dust, dan Batu Tempa!", 20, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, defaultFont);
            PositionUI(eDescTxt.gameObject, 0.05f, 0.05f, 0.95f, 0.38f);

            var startTrialBtn = CreateButton(endlessContent, "StartTrialButton", "⚔️ MULAI EKSPEDISI SURVIVAL", new Color(0.55f, 0.25f, 0.85f), Color.white, 26, defaultFont);
            PositionUI(startTrialBtn, 0.10f, 0.18f, 0.90f, 0.30f);

            var trialFeedback = CreateText(endlessContent, "TrialFeedbackText", "", 20, FontStyle.Italic, TextAnchor.MiddleCenter, Color.green, defaultFont);
            PositionUI(trialFeedback.gameObject, 0.05f, 0.05f, 0.95f, 0.14f);

            var endlessSO = new SerializedObject(endlessView);
            endlessSO.FindProperty("trialLobbyPanel").objectReferenceValue = endlessPanel;
            endlessSO.FindProperty("closeButton").objectReferenceValue = FindCloseButton(endlessPanel);
            endlessSO.FindProperty("highestFloorText").objectReferenceValue = eFloorTxt;
            endlessSO.FindProperty("weeklyResetCountdownText").objectReferenceValue = eResetTxt;
            endlessSO.FindProperty("energyCostText").objectReferenceValue = eCostTxt;
            endlessSO.FindProperty("trialDescriptionText").objectReferenceValue = eDescTxt;
            endlessSO.FindProperty("startTrialButton").objectReferenceValue = startTrialBtn.GetComponent<Button>();
            endlessSO.FindProperty("feedbackMessageText").objectReferenceValue = trialFeedback;
            endlessSO.ApplyModifiedProperties();
            endlessPanel.SetActive(false);

            // 14. TUTORIAL OVERLAY PANEL (FTUE Mentor BaldHawk)
            var tutorialPanel = CreateFullScreenPanel(canvasObj.transform, "TutorialOverlayPanel", new Color(0, 0, 0, 0.75f));
            var tutorialView = tutorialPanel.AddComponent<UITutorialOverlayView>();
            var tutCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();

            var dialogueBox = CreatePanel(tutorialPanel.transform, "DialogueBox", new Color(0.08f, 0.11f, 0.18f, 0.98f));
            PositionUI(dialogueBox, 0.05f, 0.08f, 0.95f, 0.32f);

            var spkName = CreateText(dialogueBox.transform, "SpeakerNameText", "BaldHawk (Mentor Benteng)", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f), defaultFont);
            PositionUI(spkName.gameObject, 0.05f, 0.75f, 0.95f, 0.95f);

            var diagBody = CreateText(dialogueBox.transform, "DialogueBodyText", "Selamat datang di Benteng Citadel, Panglima! Persiapkan pertahanan pahlawanmu sekarang!", 22, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, defaultFont);
            PositionUI(diagBody.gameObject, 0.05f, 0.28f, 0.95f, 0.72f);

            var tapHint = CreateText(dialogueBox.transform, "TapHintText", "Ketuk layar untuk lanjut ▶", 20, FontStyle.Italic, TextAnchor.MiddleRight, new Color(0.7f, 0.9f, 1f, 0.9f), defaultFont);
            PositionUI(tapHint.gameObject, 0.4f, 0.05f, 0.95f, 0.25f);

            var tapBtn = CreateButton(tutorialPanel.transform, "TapToContinueButton", "", new Color(0, 0, 0, 0), Color.clear, 1, defaultFont);
            PositionUI(tapBtn, 0, 0, 1, 1);
            tapBtn.transform.SetAsFirstSibling();

            var skipBtn = CreateButton(tutorialPanel.transform, "SkipTutorialButton", "Lewati Panduan ✕", new Color(0.2f, 0.25f, 0.35f, 0.9f), Color.white, 20, defaultFont);
            PositionUI(skipBtn, 0.65f, 0.92f, 0.96f, 0.97f);

            var spotObj = new GameObject("SpotlightCutout", typeof(RectTransform));
            spotObj.transform.SetParent(tutorialPanel.transform, false);
            var ptrObj = new GameObject("PointerHand", typeof(RectTransform));
            ptrObj.transform.SetParent(tutorialPanel.transform, false);

            var tutSO = new SerializedObject(tutorialView);
            tutSO.FindProperty("tutorialRootPanel").objectReferenceValue = tutorialPanel;
            tutSO.FindProperty("canvasGroup").objectReferenceValue = tutCanvasGroup;
            tutSO.FindProperty("speakerNameText").objectReferenceValue = spkName;
            tutSO.FindProperty("dialogueBodyText").objectReferenceValue = diagBody;
            tutSO.FindProperty("tapHintText").objectReferenceValue = tapHint;
            tutSO.FindProperty("tapToContinueButton").objectReferenceValue = tapBtn.GetComponent<Button>();
            tutSO.FindProperty("skipTutorialButton").objectReferenceValue = skipBtn.GetComponent<Button>();
            tutSO.FindProperty("spotlightCutout").objectReferenceValue = spotObj.GetComponent<RectTransform>();
            tutSO.FindProperty("pointerHand").objectReferenceValue = ptrObj.GetComponent<RectTransform>();
            tutSO.ApplyModifiedProperties();
            tutCanvasGroup.alpha = 0f;
            tutCanvasGroup.interactable = false;
            tutCanvasGroup.blocksRaycasts = false;
            tapBtn.SetActive(false);
            tutorialPanel.SetActive(false);

            // ==========================================
            // E. WIRE UP MASTER GAME FLOW MANAGER
            // ==========================================
            var flowSO = new SerializedObject(flowManager);
            flowSO.FindProperty("titleScreenView").objectReferenceValue = titleView;
            flowSO.FindProperty("lobbyView").objectReferenceValue = lobbyView;
            flowSO.FindProperty("battleHudPanel").objectReferenceValue = battleHudObj;
            flowSO.FindProperty("arenaWorldContainer").objectReferenceValue = arenaWorldObj;

            flowSO.FindProperty("stageMapView").objectReferenceValue = stageMapView;
            flowSO.FindProperty("heroUpgradeView").objectReferenceValue = heroView;
            flowSO.FindProperty("summonHallView").objectReferenceValue = summonView;
            flowSO.FindProperty("storeView").objectReferenceValue = storeView;
            flowSO.FindProperty("relicForgeView").objectReferenceValue = forgeView;
            flowSO.FindProperty("talentView").objectReferenceValue = talentView;
            flowSO.FindProperty("questView").objectReferenceValue = questView;
            flowSO.FindProperty("guildView").objectReferenceValue = guildView;
            flowSO.FindProperty("battlePassView").objectReferenceValue = passView;
            flowSO.FindProperty("eventArenaView").objectReferenceValue = eventView;
            flowSO.FindProperty("artifactView").objectReferenceValue = artifactView;
            flowSO.FindProperty("audioSettingsView").objectReferenceValue = settingsView;
            flowSO.FindProperty("endlessTrialView").objectReferenceValue = endlessView;
            flowSO.ApplyModifiedProperties();

            // Simpan Scene
            if (!AssetDatabase.IsValidFolder(SCENE_DIR))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            EnsureSceneInBuildSettings(SCENE_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Rampartbound] SUKSES MENGOMPILASI ALUR LENGKAP GAME! Scene disimpan di: {SCENE_PATH}");
        }

        #region Helper UI Builders

        private static Sprite _frameOrnateGold;
        private static Sprite _frameStoneBastion;
        private static Sprite _btnPrimaryGold;
        private static Sprite _btnPrimaryGoldPressed;
        private static Sprite _btnSecondaryStone;
        private static Sprite _btnSecondaryStonePressed;
        private static Sprite _btnCloseX;
        private static Sprite _btnCloseXPressed;
        private static Sprite _headerBannerRibbon;
        private static Sprite _iconResourcePill;
        private static Sprite _iconGoldCoin;
        private static Sprite _iconGemBlue;
        private static Sprite _iconShardPurple;
        private static Sprite _iconScrapIron;
        private static Sprite _iconEnergyCyan;
        private static Sprite _borderCommon;
        private static Sprite _borderRare;
        private static Sprite _borderEpic;
        private static Sprite _borderLegendary;
        private static Sprite _titleBackdrop;
        private static Sprite _titleGameLogo;
        private static Sprite _titleBtnTap;
        private static Sprite _handCursor;
        private static Sprite _shadowOval;
        private static Sprite _plinthT1;
        private static Sprite _lobbyBackdrop;
        private static Sprite _keyVisualBanner;
        private static Sprite _heroCedricSprite;
        private static Sprite _heroRoseSprite;
        private static Sprite _heroRabbitSprite;
        private static Sprite _heroWindSprite;
        private static Sprite _bannerTacticalPhase;
        private static Sprite _iconTacticalMove;

        private static void LoadUISprites()
        {
            if (_frameOrnateGold == null) _frameOrnateGold = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_OrnateGold_9Slice.png");
            if (_frameStoneBastion == null) _frameStoneBastion = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png");
            if (_bannerTacticalPhase == null) _bannerTacticalPhase = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Banner_Tactical_Phase.png");
            if (_iconTacticalMove == null) _iconTacticalMove = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Tactical_Move.png");
            if (_btnPrimaryGold == null) _btnPrimaryGold = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Primary_Gold.png");
            if (_btnPrimaryGoldPressed == null) _btnPrimaryGoldPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Primary_Gold_Pressed.png");
            if (_btnSecondaryStone == null) _btnSecondaryStone = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Secondary_Stone.png");
            if (_btnSecondaryStonePressed == null) _btnSecondaryStonePressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Secondary_Stone_Pressed.png");
            if (_btnCloseX == null) _btnCloseX = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Close_X.png");
            if (_btnCloseXPressed == null) _btnCloseXPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Close_X_Pressed.png");
            if (_headerBannerRibbon == null) _headerBannerRibbon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Header_Banner_Ribbon.png");
            if (_iconResourcePill == null) _iconResourcePill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Resource_Pill.png");
            if (_iconGoldCoin == null) _iconGoldCoin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Gold_Coin.png");
            if (_iconGemBlue == null) _iconGemBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Gem_Blue.png");
            if (_iconShardPurple == null) _iconShardPurple = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Shard_Purple.png");
            if (_iconScrapIron == null) _iconScrapIron = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Scrap_Iron.png");
            if (_iconEnergyCyan == null) _iconEnergyCyan = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Icon_Energy_Cyan.png");
            if (_borderCommon == null) _borderCommon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Border_Rarity_Common.png");
            if (_borderRare == null) _borderRare = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Border_Rarity_Rare.png");
            if (_borderEpic == null) _borderEpic = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Border_Rarity_Epic.png");
            if (_borderLegendary == null) _borderLegendary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Border_Rarity_Legendary.png");
            if (_titleBackdrop == null) _titleBackdrop = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Title_Screen_Canonical_Backdrop_Pure.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Title_Screen_Backdrop.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Rampartbound_KeyVisual_Banner.jpg");
            if (_titleGameLogo == null) _titleGameLogo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Title_Game_Logo_Rampartbound.png");
            if (_titleBtnTap == null) _titleBtnTap = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Title_Btn_Tap_To_Start_V2.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Title_Btn_Tap_To_Start.png");
            if (_handCursor == null) _handCursor = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Hand_Cursor.png");
            if (_shadowOval == null) _shadowOval = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Shadow_Ground_Oval.png");
            if (_plinthT1 == null) _plinthT1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/Plinth_Hero_Base_T1.png");
            if (_lobbyBackdrop == null) _lobbyBackdrop = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Citadel_Lobby_Backdrop.png") ?? _titleBackdrop;
            if (_keyVisualBanner == null) _keyVisualBanner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Rampartbound_KeyVisual_Banner.jpg");
            if (_heroCedricSprite == null) _heroCedricSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Heroes/Cedric_T1.png");
            if (_heroRoseSprite == null) _heroRoseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Heroes/Rose_T1.png");
            if (_heroRabbitSprite == null) _heroRabbitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Heroes/RabbitMage_T1.png");
            if (_heroWindSprite == null) _heroWindSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Heroes/WindRanger_T1.png");
        }

        private static void AttachRarityBorder(Transform parentCard, Sprite borderSprite)
        {
            if (borderSprite == null || parentCard == null) return;
            var borderObj = new GameObject("RarityBorder", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(parentCard, false);
            var rect = borderObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = borderObj.GetComponent<Image>();
            img.sprite = borderSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private static void SetupHeroCardPortrait(GameObject cardObj, Sprite heroSprite)
        {
            if (cardObj == null) return;

            // 1. Oval Ground Shadow (di bawah kaki hero standee)
            var shadowObj = new GameObject("GroundShadow", typeof(RectTransform), typeof(Image));
            shadowObj.transform.SetParent(cardObj.transform, false);
            PositionUI(shadowObj, 0.15f, 0.28f, 0.85f, 0.40f);
            var shadowImg = shadowObj.GetComponent<Image>();
            if (_shadowOval != null)
            {
                shadowImg.sprite = _shadowOval;
            }
            shadowImg.color = new Color(0f, 0f, 0f, 0.48f);
            shadowImg.raycastTarget = false;

            // 2. Hero Standee Portrait
            if (heroSprite != null)
            {
                var portraitObj = new GameObject("HeroPortrait", typeof(RectTransform), typeof(Image));
                portraitObj.transform.SetParent(cardObj.transform, false);
                PositionUI(portraitObj, 0.05f, 0.32f, 0.95f, 0.96f);
                var img = portraitObj.GetComponent<Image>();
                img.sprite = heroSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            // 3. Info Badge Ribbon (Lv, Stars, CP) di bawah pelataran kaki
            var txtTrans = cardObj.transform.Find("Text");
            if (txtTrans != null)
            {
                var rt = txtTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.02f, 0.02f);
                    rt.anchorMax = new Vector2(0.98f, 0.30f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
        }

        private static GameObject CreateFullScreenPanel(Transform parent, string name, Color bgColor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = obj.GetComponent<Image>();
            img.color = bgColor;
            if (bgColor.a <= 0.05f)
            {
                img.raycastTarget = false; // Matikan raycastTarget jika transparan agar tidak memblokir input dunia 2D
            }
            return obj;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var img = obj.GetComponent<Image>();
            img.color = bgColor;
            if (bgColor.a <= 0.05f)
            {
                img.raycastTarget = false;
            }
            return obj;
        }

        private static GameObject CreateSubPanel(Transform parent, string name, string headerTitle, Font font)
        {
            LoadUISprites();
            var panel = CreateFullScreenPanel(parent, name, new Color(0.05f, 0.07f, 0.12f, 0.98f));
            if (panel.GetComponent<SafeAreaFitter>() == null)
            {
                panel.AddComponent<SafeAreaFitter>();
            }

            var panelImg = panel.GetComponent<Image>();
            if (_frameStoneBastion != null)
            {
                panelImg.sprite = _frameStoneBastion;
                panelImg.type = Image.Type.Sliced;
                panelImg.color = Color.white;
            }

            // Header Banner
            var header = CreatePanel(panel.transform, "HeaderBanner", new Color(0.1f, 0.14f, 0.22f, 1f));
            PositionUI(header, 0f, 0.92f, 1f, 1f);

            var headerImg = header.GetComponent<Image>();
            if (_headerBannerRibbon != null)
            {
                headerImg.sprite = _headerBannerRibbon;
                headerImg.type = Image.Type.Sliced;
                headerImg.color = Color.white;
            }

            var titleTxt = CreateText(header.transform, "TitleText", headerTitle, 30, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.35f), font);
            PositionUI(titleTxt.gameObject, 0.04f, 0.1f, 0.80f, 0.9f);

            var closeBtn = CreateButton(header.transform, "CloseButton", "✕", new Color(0.85f, 0.25f, 0.25f), Color.white, 24, font);
            PositionUI(closeBtn, 0.84f, 0.05f, 0.98f, 0.95f);

            // Container Konten Sub-Panel
            var contentContainer = CreatePanel(panel.transform, "ContentContainer", new Color(0.08f, 0.11f, 0.18f, 0.5f));
            PositionUI(contentContainer, 0.03f, 0.03f, 0.97f, 0.90f);

            if (panel.GetComponent<CanvasGroup>() == null) panel.AddComponent<CanvasGroup>();

            return panel;
        }

        private static Button FindCloseButton(GameObject panel)
        {
            if (panel == null) return null;
            var btnTrans = panel.transform.Find("HeaderBanner/CloseButton") 
                        ?? panel.transform.Find("CloseButton")
                        ?? panel.transform.Find("HeaderPanel/CloseButton");
            if (btnTrans != null) return btnTrans.GetComponent<Button>();

            // Cari tombol yang memiliki nama mengandung 'Close'
            var buttons = panel.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.name.Contains("Close") || b.name.Contains("Btn_Close"))
                {
                    return b;
                }
            }
            return buttons.Length > 0 ? buttons[0] : null;
        }

        private static GameObject CreateResourcePill(Transform parent, string name, Sprite iconSprite, string initialText, Color textColor, ResourceCommodityType commodity, Font font, out Text outText)
        {
            LoadUISprites();
            var pillObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UIResourceTooltip));
            pillObj.transform.SetParent(parent, false);

            var pillImg = pillObj.GetComponent<Image>();
            if (_iconResourcePill != null)
            {
                pillImg.sprite = _iconResourcePill;
                pillImg.type = Image.Type.Sliced;
                pillImg.color = Color.white;
            }
            else
            {
                pillImg.color = new Color(0.12f, 0.16f, 0.25f, 0.95f);
            }

            var tooltip = pillObj.GetComponent<UIResourceTooltip>();
            var tSO = new SerializedObject(tooltip);
            tSO.FindProperty("commodityType").enumValueIndex = (int)commodity;
            if (iconSprite != null) tSO.FindProperty("commodityIcon").objectReferenceValue = iconSprite;
            tSO.ApplyModifiedProperties();

            // Icon Child
            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(pillObj.transform, false);
            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.04f, 0.12f);
            iconRt.anchorMax = new Vector2(0.32f, 0.88f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = iconSprite;
            iImg.preserveAspect = true;
            iImg.raycastTarget = false;

            // Text Child
            var textObj = new GameObject("AmountText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(pillObj.transform, false);
            var txtRt = textObj.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.35f, 0.05f);
            txtRt.anchorMax = new Vector2(0.96f, 0.95f);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = textObj.GetComponent<Text>();
            if (font != null) txt.font = font;
            txt.text = initialText;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = textColor;
            txt.raycastTarget = false;

            outText = txt;
            return pillObj;
        }

        private static GameObject CreateNotificationBadge(Transform parent, NotificationBadgeType badgeType)
        {
            var badgeObj = new GameObject("NotificationBadge", typeof(RectTransform), typeof(Image), typeof(UINotificationBadge));
            badgeObj.transform.SetParent(parent, false);
            var rt = badgeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.70f, 0.65f);
            rt.anchorMax = new Vector2(0.98f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = badgeObj.GetComponent<Image>();
            img.color = new Color(0.95f, 0.2f, 0.2f, 1f);
            img.raycastTarget = false;

            var bComp = badgeObj.GetComponent<UINotificationBadge>();
            var bSO = new SerializedObject(bComp);
            bSO.FindProperty("badgeType").enumValueIndex = (int)badgeType;
            bSO.FindProperty("redDotObject").objectReferenceValue = badgeObj;
            bSO.FindProperty("redDotImage").objectReferenceValue = img;
            bSO.ApplyModifiedProperties();

            return badgeObj;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style, TextAnchor align, Color color, Font font)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var txt = obj.GetComponent<Text>();
            if (font != null) txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.raycastTarget = false; // Matikan raycastTarget agar teks statis tidak menghalangi sentuhan tombol di bawahnya
            return txt;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color btnColor, Color txtColor, int fontSize, Font font)
        {
            LoadUISprites();
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var img = btnObj.GetComponent<Image>();
            var btn = btnObj.GetComponent<Button>();

            // Matikan navigasi keyboard otomatis untuk mencegah fokus terkunci di mobile
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            bool isClose = name.Contains("Close") || name.Contains("Btn_Close");
            bool isSecondary = name.Contains("Cancel") || name.Contains("Back") || name.Contains("Batal") || name.Contains("Kembali") || name.Contains("Surrender") || name.Contains("Reset");
            bool isTitleStart = name.Contains("EnterCitadel") || name.Contains("StartBtn");

            if (isClose && _btnCloseX != null)
            {
                img.sprite = _btnCloseX;
                img.color = Color.white;
                if (_btnCloseXPressed != null)
                {
                    btn.transition = Selectable.Transition.SpriteSwap;
                    var ss = new SpriteState { pressedSprite = _btnCloseXPressed, highlightedSprite = _btnCloseX };
                    btn.spriteState = ss;
                }
            }
            else if (isTitleStart && _titleBtnTap != null)
            {
                img.sprite = _titleBtnTap;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else if (isSecondary && _btnSecondaryStone != null)
            {
                img.sprite = _btnSecondaryStone;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                if (_btnSecondaryStonePressed != null)
                {
                    btn.transition = Selectable.Transition.SpriteSwap;
                    var ss = new SpriteState { pressedSprite = _btnSecondaryStonePressed, highlightedSprite = _btnSecondaryStone };
                    btn.spriteState = ss;
                }
            }
            else if (_btnPrimaryGold != null)
            {
                img.sprite = _btnPrimaryGold;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                if (_btnPrimaryGoldPressed != null)
                {
                    btn.transition = Selectable.Transition.SpriteSwap;
                    var ss = new SpriteState { pressedSprite = _btnPrimaryGoldPressed, highlightedSprite = _btnPrimaryGold };
                    btn.spriteState = ss;
                }
            }
            else
            {
                img.color = btnColor;
            }

            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            var rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var txt = txtObj.GetComponent<Text>();
            if (font != null) txt.font = font;

            // Anti-Bertumpuk: Tombol close (X) memiliki ikon grafis X bawaan pada sprite sehingga teks dikosongkan.
            // Tombol Title Start menggunakan sprite frame bersih tanpa teks bakar, sehingga teks label dirender jelas & mendukung lokalisasi.
            txt.text = (isClose && _btnCloseX != null) ? "" : label;

            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = txtColor;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 10;
            txt.resizeTextMaxSize = fontSize;
            txt.raycastTarget = false; // WAJIB false agar sentuhan jari langsung mengenai komponen Button!

            if (btnObj.GetComponent<UIButtonJuice>() == null)
            {
                btnObj.AddComponent<UIButtonJuice>();
            }

            return btnObj;
        }

        private static void PositionUI(GameObject obj, float xMin, float yMin, float xMax, float yMax)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(xMin, yMin);
                rect.anchorMax = new Vector2(xMax, yMax);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private static GameObject CreateProgressBar(Transform parent, string name, float min, float max, float currentVal, Color fillColor)
        {
            var barObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            barObj.transform.SetParent(parent, false);
            var slider = barObj.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = currentVal;
            slider.interactable = false; // NON-INTERACTABLE: Cannot be dragged by player!
            slider.transition = Selectable.Transition.None;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(barObj.transform, false);
            PositionUI(bg, 0f, 0.15f, 1f, 0.85f);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            bgImg.raycastTarget = false;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barObj.transform, false);
            PositionUI(fillArea, 0f, 0.15f, 1f, 0.85f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            PositionUI(fill, 0f, 0f, 1f, 1f);
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;
            slider.fillRect = fill.GetComponent<RectTransform>();

            return barObj;
        }

        private static GameObject CreateSlider(Transform parent, string name, float min, float max, float currentVal)
        {
            var sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = currentVal;
            slider.interactable = true;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            PositionUI(bg, 0f, 0.25f, 1f, 0.75f);
            bg.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            PositionUI(fillArea, 0f, 0.25f, 1f, 0.75f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            PositionUI(fill, 0f, 0f, 1f, 1f);
            fill.GetComponent<Image>().color = new Color(0.25f, 0.65f, 0.95f, 1f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            PositionUI(handleArea, 0f, 0f, 1f, 1f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hRect = handle.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(26f, 26f);
            handle.GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f);
            slider.handleRect = hRect;

            return sliderObj;
        }

        private static GameObject CreateToggle(Transform parent, string name, string label, bool isOn, Font font)
        {
            var toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(parent, false);
            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = isOn;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(toggleObj.transform, false);
            PositionUI(bg, 0f, 0.15f, 0.25f, 0.85f);
            bg.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.32f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            PositionUI(check, 0.15f, 0.15f, 0.85f, 0.85f);
            check.GetComponent<Image>().color = new Color(0.3f, 0.9f, 0.4f);
            toggle.graphic = check.GetComponent<Image>();

            var lbl = CreateText(toggleObj.transform, "Label", label, 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white, font);
            PositionUI(lbl.gameObject, 0.28f, 0f, 1f, 1f);

            return toggleObj;
        }

        #endregion

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var currentScenes = EditorBuildSettings.scenes;
            if (currentScenes != null)
            {
                foreach (var s in currentScenes)
                {
                    if (s.path == scenePath) return;
                }
            }

            int currentLength = currentScenes != null ? currentScenes.Length : 0;
            var newScenes = new EditorBuildSettingsScene[currentLength + 1];
            for (int i = 0; i < currentLength; i++)
            {
                newScenes[i] = currentScenes[i];
            }
            newScenes[currentLength] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"[Rampartbound] Berhasil mendaftarkan {scenePath} ke EditorBuildSettings!");
        }
    }
}
#endif
