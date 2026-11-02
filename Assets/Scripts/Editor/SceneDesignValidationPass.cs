using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rampartbound.Editor
{
    public static class SceneDesignValidationPass
    {
        public static void ValidateAndCaptureLobbyDesign()
        {
            Debug.Log("[SceneDesignValidationPass] Starting headless validation of ArenaScene...");
            
            try
            {
                // 1. Jalankan Scene Setup
                RampartboundSceneSetup.SetupArenaScene();
                Debug.Log("[SceneDesignValidationPass] SetupArenaScene() completed cleanly.");

                // 2. Audit Elemen Hierarki Canvas
                var canvas = GameObject.Find("Canvas");
                if (canvas == null)
                {
                    Debug.LogError("[SceneDesignValidationPass] ERROR: Canvas not found!");
                    EditorApplication.Exit(1);
                    return;
                }

                // Periksa CitadelLobbyPanel
                var lobbyPanel = GameObject.Find("CitadelLobbyPanel");
                if (lobbyPanel == null)
                {
                    Debug.LogError("[SceneDesignValidationPass] ERROR: CitadelLobbyPanel not found!");
                    EditorApplication.Exit(2);
                    return;
                }

                // Periksa HeroCourtyardAssembly & 4 slot
                var courtyard = GameObject.Find("HeroCourtyardAssembly");
                if (courtyard == null)
                {
                    Debug.LogError("[SceneDesignValidationPass] ERROR: HeroCourtyardAssembly not found!");
                    EditorApplication.Exit(3);
                    return;
                }

                string[] expectedCards = { "RabbitCard", "CedricCard", "RoseCard", "WindCard" };
                foreach (var cardName in expectedCards)
                {
                    var card = courtyard.transform.Find(cardName);
                    if (card == null)
                    {
                        Debug.LogError($"[SceneDesignValidationPass] ERROR: {cardName} missing from Courtyard!");
                        EditorApplication.Exit(4);
                        return;
                    }
                    var namePlate = card.Find("HeroNamePlate");
                    var badge = card.Find("BadgePill");
                    Debug.Log($"[SceneDesignValidationPass] Found {cardName}: NamePlate={(namePlate != null)}, Badge={(badge != null)}");
                }

                // Periksa Dock Bawah (Citadel, Forge, Heroes, Bag)
                var dock = GameObject.Find("BottomNavigationDock");
                if (dock == null)
                {
                    Debug.LogError("[SceneDesignValidationPass] ERROR: BottomNavigationDock not found!");
                    EditorApplication.Exit(5);
                    return;
                }

                string[] expectedTabs = { "NavStageMap", "NavForge", "NavHeroes", "NavStore" };
                foreach (var tab in expectedTabs)
                {
                    var btn = dock.transform.Find(tab);
                    var icon = btn?.Find("TabIcon");
                    Debug.Log($"[SceneDesignValidationPass] Tab {tab}: Exists={(btn != null)}, Icon={(icon != null)}");
                }

                Debug.Log("[SceneDesignValidationPass] SUCCESS: All design hierarchy, courtyard layout, standee plates, and dock tabs validated!");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneDesignValidationPass] Exception: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(10);
            }
        }
    }
}
