#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FantasyDefense.Data;

namespace Rampartbound.Editor
{
    /// <summary>
    /// Editor Window & Utilitas Audit Aset untuk Rampartbound: Merge Defense.
    /// Memverifikasi integritas Drop-In Asset Replacement Pipeline:
    /// 1. Memeriksa keberadaan berkas sprite pahlawan, monster, grid, dan barricade.
    /// 2. Memastikan konfigurasi Texture Importer (Sprite 2D, PPU 100, Center Pivot, Alpha Transparency).
    /// 3. Memverifikasi relasi GUID pada ScriptableObject (UnitDataSO & MonsterDataSO).
    /// 4. Menyediakan tombol 1-Klik "Auto-Fix Sprite Import Settings" jika file baru ditumpuk.
    /// </summary>
    public class RampartboundAssetValidator : EditorWindow
    {
        private Vector2 scrollPos;
        private List<AssetAuditItem> heroAuditItems = new List<AssetAuditItem>();
        private List<AssetAuditItem> monsterAuditItems = new List<AssetAuditItem>();
        private List<AssetAuditItem> environmentAuditItems = new List<AssetAuditItem>();
        private int totalOk = 0;
        private int totalWarnings = 0;

        public class AssetAuditItem
        {
            public string displayName;
            public string assetPath;
            public string targetGuid;
            public bool exists;
            public bool isSpriteType;
            public float ppu;
            public bool isCenterPivot;
            public bool isAlphaTransparent;
            public bool isLinkedToSO;
            public string statusMessage;

            public bool IsFullyValid => exists && isSpriteType && Mathf.Approximately(ppu, 100f) && isCenterPivot && isAlphaTransparent && isLinkedToSO;
        }

        [MenuItem("Rampartbound/Validate & Audit Assets (Drop-In Pipeline)", false, 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<RampartboundAssetValidator>("Asset Validator");
            window.minSize = new Vector2(620, 680);
            window.RunAudit();
        }

        private void OnEnable()
        {
            RunAudit();
        }

        public void RunAudit()
        {
            heroAuditItems.Clear();
            monsterAuditItems.Clear();
            environmentAuditItems.Clear();
            totalOk = 0;
            totalWarnings = 0;

            // 1. Audit Heroes (Cedric, Rose, Rabbit, Wind Ranger)
            AuditHero("Cedric Tier 1", "Assets/Art/Sprites/Heroes/Cedric_T1.png", "Assets/ScriptableObjects/Units/SO_Cedric.asset", 1);
            AuditHero("Cedric Tier 2", "Assets/Art/Sprites/Heroes/Cedric_T2.png", "Assets/ScriptableObjects/Units/SO_Cedric.asset", 2);
            AuditHero("Cedric Tier 3", "Assets/Art/Sprites/Heroes/Cedric_T3.png", "Assets/ScriptableObjects/Units/SO_Cedric.asset", 3);
            AuditHero("Cedric Tier 4", "Assets/Art/Sprites/Heroes/Cedric_T4.png", "Assets/ScriptableObjects/Units/SO_Cedric.asset", 4);
            AuditHero("Cedric Tier 5", "Assets/Art/Sprites/Heroes/Cedric_T5.png", "Assets/ScriptableObjects/Units/SO_Cedric.asset", 5);

            AuditHero("Rose Tanker Tier 1", "Assets/Art/Sprites/Heroes/Rose_T1.png", "Assets/ScriptableObjects/Units/SO_RoseTanker.asset", 1);
            AuditHero("Rose Tanker Tier 2", "Assets/Art/Sprites/Heroes/Rose_T2.png", "Assets/ScriptableObjects/Units/SO_RoseTanker.asset", 2);
            AuditHero("Rose Tanker Tier 3", "Assets/Art/Sprites/Heroes/Rose_T3.png", "Assets/ScriptableObjects/Units/SO_RoseTanker.asset", 3);
            AuditHero("Rose Tanker Tier 4", "Assets/Art/Sprites/Heroes/Rose_T4.png", "Assets/ScriptableObjects/Units/SO_RoseTanker.asset", 4);
            AuditHero("Rose Tanker Tier 5", "Assets/Art/Sprites/Heroes/Rose_T5.png", "Assets/ScriptableObjects/Units/SO_RoseTanker.asset", 5);

            AuditHero("Rabbit Mage Tier 1", "Assets/Art/Sprites/Heroes/RabbitMage_T1.png", "Assets/ScriptableObjects/Units/SO_RabbitMage.asset", 1);
            AuditHero("Rabbit Mage Tier 2", "Assets/Art/Sprites/Heroes/RabbitMage_T2.png", "Assets/ScriptableObjects/Units/SO_RabbitMage.asset", 2);
            AuditHero("Rabbit Mage Tier 3", "Assets/Art/Sprites/Heroes/RabbitMage_T3.png", "Assets/ScriptableObjects/Units/SO_RabbitMage.asset", 3);
            AuditHero("Rabbit Mage Tier 4", "Assets/Art/Sprites/Heroes/RabbitMage_T4.png", "Assets/ScriptableObjects/Units/SO_RabbitMage.asset", 4);
            AuditHero("Rabbit Mage Tier 5", "Assets/Art/Sprites/Heroes/RabbitMage_T5.png", "Assets/ScriptableObjects/Units/SO_RabbitMage.asset", 5);

            AuditHero("Wind Ranger Tier 1", "Assets/Art/Sprites/Heroes/WindRanger_T1.png", "Assets/ScriptableObjects/Units/SO_WindRanger.asset", 1);
            AuditHero("Wind Ranger Tier 2", "Assets/Art/Sprites/Heroes/WindRanger_T2.png", "Assets/ScriptableObjects/Units/SO_WindRanger.asset", 2);
            AuditHero("Wind Ranger Tier 3", "Assets/Art/Sprites/Heroes/WindRanger_T3.png", "Assets/ScriptableObjects/Units/SO_WindRanger.asset", 3);
            AuditHero("Wind Ranger Tier 4", "Assets/Art/Sprites/Heroes/WindRanger_T4.png", "Assets/ScriptableObjects/Units/SO_WindRanger.asset", 4);
            AuditHero("Wind Ranger Tier 5", "Assets/Art/Sprites/Heroes/WindRanger_T5.png", "Assets/ScriptableObjects/Units/SO_WindRanger.asset", 5);

            // 2. Audit Monsters (Mire Horned Scrapper, Abyssal Obsidian Maw, Malakor Void Tyrant)
            AuditMonster("Monster Creep (Mire Horned Scrapper)", "Assets/Art/Sprites/Monsters/Monster_Creep.png", "Assets/ScriptableObjects/Monsters/SO_Monster_Creep.asset");
            AuditMonster("Monster Brute (Abyssal Obsidian Maw)", "Assets/Art/Sprites/Monsters/Monster_Brute.png", "Assets/ScriptableObjects/Monsters/SO_Monster_Brute.asset");
            AuditMonster("Monster Boss (Malakor Void Tyrant)", "Assets/Art/Sprites/Monsters/Monster_Boss.png", "Assets/ScriptableObjects/Monsters/SO_Monster_Boss.asset");

            // 3. Audit Environment (Grid & Barricade)
            AuditEnvironment("Grid Tile (Empty)", "Assets/Art/Sprites/Grid/Tile_Empty.png");
            AuditEnvironment("Grid Tile (Hover)", "Assets/Art/Sprites/Grid/Tile_Hover.png");
            AuditEnvironment("Citadel Barricade Fence", "Assets/Art/Sprites/UI/Barricade_Fence.png");

            // 4. Audit Modular UI Sprites (Drop-In Replacement Ready)
            AuditEnvironment("UI Frame Ornate Gold 9-Slice", "Assets/Art/Sprites/UI/Frame_OrnateGold_9Slice.png");
            AuditEnvironment("UI Frame Stone Bastion 9-Slice", "Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png");
            AuditEnvironment("UI Button Primary Gold", "Assets/Art/Sprites/UI/Btn_Primary_Gold.png");
            AuditEnvironment("UI Button Primary Gold (Pressed)", "Assets/Art/Sprites/UI/Btn_Primary_Gold_Pressed.png");
            AuditEnvironment("UI Button Secondary Stone", "Assets/Art/Sprites/UI/Btn_Secondary_Stone.png");
            AuditEnvironment("UI Button Secondary Stone (Pressed)", "Assets/Art/Sprites/UI/Btn_Secondary_Stone_Pressed.png");
            AuditEnvironment("UI Button Close X", "Assets/Art/Sprites/UI/Btn_Close_X.png");
            AuditEnvironment("UI Button Close X (Pressed)", "Assets/Art/Sprites/UI/Btn_Close_X_Pressed.png");
            AuditEnvironment("UI Header Banner Ribbon", "Assets/Art/Sprites/UI/Header_Banner_Ribbon.png");
            AuditEnvironment("UI Banner Tactical Phase", "Assets/Art/Sprites/UI/Banner_Tactical_Phase.png");
            AuditEnvironment("UI Icon Tactical Move", "Assets/Art/Sprites/UI/Icon_Tactical_Move.png");
            AuditEnvironment("UI Icon Resource Pill", "Assets/Art/Sprites/UI/Icon_Resource_Pill.png");
            AuditEnvironment("UI Key Visual Banner", "Assets/Art/Sprites/UI/Rampartbound_KeyVisual_Banner.jpg");
            AuditEnvironment("UI Citadel Lobby Backdrop", "Assets/Art/Sprites/UI/Citadel_Lobby_Backdrop.png");
            AuditEnvironment("Combat Projectile Magic Orb", "Assets/Art/Sprites/Combat/Projectile_Magic_Orb.png");
            AuditEnvironment("Combat Projectile Wind Arrow", "Assets/Art/Sprites/Combat/Projectile_Wind_Arrow.png");
            AuditEnvironment("Bastion Left Cannon", "Assets/Art/Sprites/Combat/bastion_cannon_left.png");
            AuditEnvironment("Bastion Right Archer", "Assets/Art/Sprites/Combat/ballista_turret.png");
            AuditEnvironment("Gem T0 Cedric (Ruby)", "Assets/Art/Sprites/Grid/Gem_T0_Cedric.png");
            AuditEnvironment("Gem T0 Rose (Rose Quartz)", "Assets/Art/Sprites/Grid/Gem_T0_Rose.png");
            AuditEnvironment("Gem T0 Rabbit (Sapphire)", "Assets/Art/Sprites/Grid/Gem_T0_Rabbit.png");
            AuditEnvironment("Gem T0 Wind (Emerald)", "Assets/Art/Sprites/Grid/Gem_T0_Wind.png");
            AuditEnvironment("Battlefield Backdrop", "Assets/Art/Sprites/Environment/layer1_ground_battlefield.png");
            AuditEnvironment("Battlefield Forest", "Assets/Art/Sprites/Environment/Battlefield_Forest.png");
            AuditEnvironment("Battlefield Mountain", "Assets/Art/Sprites/Environment/Battlefield_Mountain.png");
            AuditEnvironment("Battlefield Volcanic", "Assets/Art/Sprites/Environment/Battlefield_Volcanic.png");
            AuditEnvironment("VFX Magma Aura Ring", "Assets/Art/Sprites/VFX/VFX_Aura_Magma_Ring.png");
            AuditEnvironment("VFX Charred Ground Fissure", "Assets/Art/Sprites/VFX/VFX_Charred_Ground_Fissure.png");
            AuditEnvironment("UI Badge Mini-Boss Horns", "Assets/Art/Sprites/UI/UI_Badge_MiniBoss_Horns.png");
            AuditEnvironment("UI Badge Final Boss Crown", "Assets/Art/Sprites/UI/UI_Badge_FinalBoss_Crown.png");

            Repaint();
        }

        private void AuditHero(string name, string spritePath, string soPath, int tier)
        {
            var item = CreateAuditItem(name, spritePath);
            var so = AssetDatabase.LoadAssetAtPath<UnitDataSO>(soPath);
            if (so != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                Sprite tierSprite = so.GetSpriteForTier((UnitTier)tier);
                item.isLinkedToSO = (tierSprite != null && tierSprite == sprite);
            }
            heroAuditItems.Add(item);
            UpdateCount(item);
        }

        private void AuditMonster(string name, string spritePath, string soPath)
        {
            var item = CreateAuditItem(name, spritePath);
            var so = AssetDatabase.LoadAssetAtPath<MonsterDataSO>(soPath);
            if (so != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                item.isLinkedToSO = (so.monsterSprite != null && so.monsterSprite == sprite);
            }
            monsterAuditItems.Add(item);
            UpdateCount(item);
        }

        private void AuditEnvironment(string name, string spritePath)
        {
            var item = CreateAuditItem(name, spritePath);
            item.isLinkedToSO = true; // Langsung dipakai via GridManager/Barricade
            environmentAuditItems.Add(item);
            UpdateCount(item);
        }

        private AssetAuditItem CreateAuditItem(string name, string spritePath)
        {
            var item = new AssetAuditItem
            {
                displayName = name,
                assetPath = spritePath,
                exists = File.Exists(spritePath),
                targetGuid = AssetDatabase.AssetPathToGUID(spritePath)
            };

            if (item.exists)
            {
                TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                if (importer != null)
                {
                    item.isSpriteType = (importer.textureType == TextureImporterType.Sprite);
                    item.ppu = importer.spritePixelsPerUnit;
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    item.isCenterPivot = (settings.spritePivot == new Vector2(0.5f, 0.5f) || settings.spriteAlignment == (int)SpriteAlignment.Center);
                    item.isAlphaTransparent = importer.alphaIsTransparency;
                }
            }
            return item;
        }

        private void UpdateCount(AssetAuditItem item)
        {
            if (item.IsFullyValid)
                totalOk++;
            else
                totalWarnings++;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            DrawHeader();

            EditorGUILayout.Space(8);
            DrawActionBar();

            EditorGUILayout.Space(8);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawCategorySection("Kuartet Pahlawan (Heroes Roster)", heroAuditItems);
            EditorGUILayout.Space(10);
            DrawCategorySection("Monster Invaders (Creep, Brute, Boss)", monsterAuditItems);
            EditorGUILayout.Space(10);
            DrawCategorySection("Arena & Barikade (Grid & Barricade)", environmentAuditItems);

            EditorGUILayout.Space(15);
            DrawGuideBox();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🛡️ Rampartbound: Drop-In Asset Pipeline & Validator", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Alat verifikasi integritas penumpukan file aset tanpa merusak referensi ScriptableObject / GUID.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.green;
            GUILayout.Label($"✔ Aset Siap & Valid: {totalOk}", EditorStyles.boldLabel);
            GUI.color = totalWarnings > 0 ? Color.yellow : Color.gray;
            GUILayout.Label($"⚠ Perlu Perhatian / Belum Terkait: {totalWarnings}", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawActionBar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔄 Refresh / Re-Audit", GUILayout.Height(30)))
            {
                RunAudit();
            }

            if (GUILayout.Button("🛠️ 1-Click Auto-Fix Sprite Importers", GUILayout.Height(30)))
            {
                AutoFixAllImporters();
            }

            if (GUILayout.Button("🔗 Re-Bind ScriptableObjects", GUILayout.Height(30)))
            {
                RebindScriptableObjects();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategorySection(string categoryName, List<AssetAuditItem> items)
        {
            GUILayout.Label(categoryName, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foreach (var item in items)
            {
                EditorGUILayout.BeginHorizontal();

                // Status Icon
                if (item.IsFullyValid)
                {
                    GUI.color = Color.green;
                    GUILayout.Label("✔ OK", GUILayout.Width(45));
                }
                else
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("⚠ FIX", GUILayout.Width(45));
                }
                GUI.color = Color.white;

                // Name & Path
                EditorGUILayout.BeginVertical();
                GUILayout.Label(item.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(item.assetPath, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                // Details Tag
                string tags = $"[PPU: {item.ppu:0}] [Sprite: {(item.isSpriteType ? "Ya" : "Bukan")}] [SO: {(item.isLinkedToSO ? "Linked" : "Unlinked")}]";
                EditorGUILayout.LabelField(tags, GUILayout.Width(220));

                // Ping Button
                if (GUILayout.Button("Find", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(item.assetPath);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGuideBox()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("💡 Panduan Menumpuk File Aset (Drop-In Replacement Rule):", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "1. Simpan gambar desain baru komandan dengan nama file PNG yang sama persis.\n" +
                "2. Timpa (overwrite) file ke folder tujuan: Assets/Art/Sprites/ (Heroes, Monsters, Grid, atau UI).\n" +
                "3. PENTING: JANGAN hapus file pendamping berakhiran '.meta' agar GUID tidak berubah.\n" +
                "4. Jika Unity mengimpor gambar dengan format texture biasa, cukup klik tombol '1-Click Auto-Fix Sprite Importers' di atas.\n" +
                "5. Desain baru akan langsung aktif di arena pertempuran secara instan tanpa perlu drag-and-drop ulang!",
                EditorStyles.wordWrappedMiniLabel
            );
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Mengoreksi pengaturan TextureImporter untuk seluruh berkas PNG di Assets/Art/Sprites/
        /// ke standar resmi: Sprite 2D, PPU 100, Center Pivot, Alpha Transparency.
        /// </summary>
        public static void AutoFixAllImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" });
            int fixedCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        bool changed = false;

                        if (importer.textureType != TextureImporterType.Sprite)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            changed = true;
                        }
                        if (importer.spriteImportMode != SpriteImportMode.Single)
                        {
                            importer.spriteImportMode = SpriteImportMode.Single;
                            changed = true;
                        }
                        if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f))
                        {
                            importer.spritePixelsPerUnit = 100f;
                            changed = true;
                        }
                        if (!importer.alphaIsTransparency)
                        {
                            importer.alphaIsTransparency = true;
                            changed = true;
                        }
                        if (importer.filterMode != FilterMode.Bilinear && importer.filterMode != FilterMode.Point)
                        {
                            importer.filterMode = FilterMode.Bilinear;
                            changed = true;
                        }

                        if (changed)
                        {
                            importer.SaveAndReimport();
                            fixedCount++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[Rampartbound Asset Validator] Sukses memeriksa dan mengoreksi {fixedCount} berkas importer aset visual!");
            EditorUtility.DisplayDialog("Auto-Fix Selesai", $"Berhasil memverifikasi seluruh aset visual!\n{fixedCount} file diperbarui ke standar PPU 100 & Sprite 2D.", "OK");
        }

        /// <summary>
        /// Mengaitkan ulang seluruh ScriptableObject ke sprite yang sesuai jika ada yang terlepas.
        /// </summary>
        public static void RebindScriptableObjects()
        {
            BindUnitSO("Assets/ScriptableObjects/Units/SO_Cedric.asset", "Cedric");
            BindUnitSO("Assets/ScriptableObjects/Units/SO_RoseTanker.asset", "Rose");
            BindUnitSO("Assets/ScriptableObjects/Units/SO_RabbitMage.asset", "RabbitMage");
            BindUnitSO("Assets/ScriptableObjects/Units/SO_WindRanger.asset", "WindRanger");

            BindMonsterSO("Assets/ScriptableObjects/Monsters/SO_Monster_Creep.asset", "Assets/Art/Sprites/Monsters/Monster_Creep.png", "Assets/Art/Sprites/Monsters/Monster_Creep_Walk.png", "Assets/Art/Sprites/Monsters/Monster_Creep_Attack.png");
            BindMonsterSO("Assets/ScriptableObjects/Monsters/SO_Monster_Brute.asset", "Assets/Art/Sprites/Monsters/Monster_Brute.png", "Assets/Art/Sprites/Monsters/Monster_Brute_Walk.png", "Assets/Art/Sprites/Monsters/Monster_Brute_Attack.png");
            BindMonsterSO("Assets/ScriptableObjects/Monsters/SO_Monster_Boss.asset", "Assets/Art/Sprites/Monsters/Monster_Boss.png", "Assets/Art/Sprites/Monsters/Monster_Boss_Walk.png", "Assets/Art/Sprites/Monsters/Monster_Boss_Attack.png");

            AssetDatabase.SaveAssets();
            Debug.Log("[Rampartbound Asset Validator] Sukses mengaitkan ulang seluruh ScriptableObjects unit & monster!");
            EditorUtility.DisplayDialog("Re-Bind Selesai", "Seluruh referensi ScriptableObject telah dikaitkan ulang secara otomatis ke aset sprite terkait!", "OK");
        }

        private static void BindUnitSO(string soPath, string heroPrefix)
        {
            var so = AssetDatabase.LoadAssetAtPath<UnitDataSO>(soPath);
            if (so == null) return;

            var t1 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Heroes/{heroPrefix}_T1.png");
            var t2 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Heroes/{heroPrefix}_T2.png");
            var t3 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Heroes/{heroPrefix}_T3.png");
            var t4 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Heroes/{heroPrefix}_T4.png");
            var t5 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Heroes/{heroPrefix}_T5.png");

            if (t1 != null) { so.unitIcon = t1; so.tier1Sprite = t1; }
            if (t2 != null) so.tier2Sprite = t2;
            if (t3 != null) so.tier3Sprite = t3;
            if (t4 != null) so.tier4Sprite = t4;
            if (t5 != null) so.tier5Sprite = t5;

            var tokPrefix = heroPrefix == "RabbitMage" ? "Rabbit" : (heroPrefix == "RoseTanker" ? "Rose" : (heroPrefix == "WindRanger" ? "Wind" : heroPrefix));
            var tok1 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Grid/Token_Medallion_{tokPrefix}.png");
            var tok2 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Grid/Token_Medallion_{tokPrefix}_T2.png");
            var tok3 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Grid/Token_Medallion_{tokPrefix}_T3.png");
            var tok4 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Grid/Token_Medallion_{tokPrefix}_T4.png");
            var tok5 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Grid/Token_Medallion_{tokPrefix}_T5.png");

            if (tok1 != null) so.preparationTokenSprite = tok1;
            if (tok2 != null) so.preparationTokenTier2Sprite = tok2;
            if (tok3 != null) so.preparationTokenTier3Sprite = tok3;
            if (tok4 != null) so.preparationTokenTier4Sprite = tok4;
            if (tok5 != null) so.preparationTokenTier5Sprite = tok5;

            EditorUtility.SetDirty(so);
        }

        private static void BindMonsterSO(string soPath, string spritePath, string walkPath = null, string attackPath = null)
        {
            var so = AssetDatabase.LoadAssetAtPath<MonsterDataSO>(soPath);
            if (so == null) return;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                so.monsterSprite = sprite;
            }

            if (!string.IsNullOrEmpty(walkPath))
            {
                var walk = AssetDatabase.LoadAssetAtPath<Sprite>(walkPath);
                if (walk != null) so.monsterWalkSprite = walk;
            }

            if (!string.IsNullOrEmpty(attackPath))
            {
                var atk = AssetDatabase.LoadAssetAtPath<Sprite>(attackPath);
                if (atk != null) so.monsterAttackSprite = atk;
            }

            EditorUtility.SetDirty(so);
        }
    }
}
#endif
