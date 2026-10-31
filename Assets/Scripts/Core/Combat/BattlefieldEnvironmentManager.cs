using System;
using UnityEngine;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Mengelola latar visual lingkungan medan pertempuran (Battlefield Environment Manager).
    /// Mengganti backdrop visual 2D secara dinamis sesuai bioma babak aktif:
    /// - Forest: Padang rumput asri & jalan batu gerbang Citadel (Stage 1-4).
    /// - Mountain: Punggung bukit karang & batuan granit alpine dingin (Stage 5-8).
    /// - Volcanic: Dataran basal obsidian & retakan urat magma membara (Stage 9-10 & Boss).
    /// </summary>
    public class BattlefieldEnvironmentManager : MonoBehaviour
    {
        public static BattlefieldEnvironmentManager Instance { get; private set; }

        [Header("Active Theme")]
        [SerializeField] private BattlefieldEnvironmentTheme currentTheme = BattlefieldEnvironmentTheme.Forest;

        [Header("Backdrop Renderer")]
        [SerializeField] private SpriteRenderer backdropSr;
        [SerializeField] private GameObject backdropObj;

        public BattlefieldEnvironmentTheme CurrentTheme => currentTheme;

        public static event Action<BattlefieldEnvironmentTheme> OnEnvironmentChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureBackdropRenderer();
        }

        private void Start()
        {
            ApplyTheme(currentTheme);
        }

        public void SetEnvironmentTheme(BattlefieldEnvironmentTheme newTheme)
        {
            currentTheme = newTheme;
            ApplyTheme(currentTheme);
            OnEnvironmentChanged?.Invoke(currentTheme);
        }

        private void EnsureBackdropRenderer()
        {
            if (backdropSr != null) return;

            // Cari di dalam ArenaWorldContainer jika ada
            var arena = GameObject.Find("ArenaWorldContainer");
            Transform parent = arena != null ? arena.transform : transform;

            var existing = parent.Find("BattlefieldBackdrop");
            if (existing != null)
            {
                backdropObj = existing.gameObject;
                backdropSr = backdropObj.GetComponent<SpriteRenderer>();
            }
            else
            {
                backdropObj = new GameObject("BattlefieldBackdrop");
                backdropObj.transform.SetParent(parent, false);
                backdropObj.transform.position = new Vector3(0f, 0f, 5f);
                backdropSr = backdropObj.AddComponent<SpriteRenderer>();
            }

            if (backdropSr != null)
            {
                backdropSr.sortingLayerName = "Default";
                backdropSr.sortingOrder = -50;
            }
        }

        private void ApplyTheme(BattlefieldEnvironmentTheme theme)
        {
            EnsureBackdropRenderer();
            if (backdropSr == null) return;

            string spritePath = theme switch
            {
                BattlefieldEnvironmentTheme.Forest => "Sprites/Environment/Battlefield_Forest",
                BattlefieldEnvironmentTheme.Mountain => "Sprites/Environment/Battlefield_Mountain",
                BattlefieldEnvironmentTheme.Volcanic => "Sprites/Environment/Battlefield_Volcanic",
                _ => "Sprites/Environment/Battlefield_Forest"
            };

            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite == null)
            {
                // Fallback ke layer1_ground_battlefield jika tersedia
                sprite = Resources.Load<Sprite>("Sprites/Environment/layer1_ground_battlefield");
            }

            if (sprite != null)
            {
                backdropSr.sprite = sprite;
                // Pastikan ukuran menutupi layar orthographic secara penuh
                backdropObj.transform.localScale = Vector3.one;
            }

            Debug.Log($"[BattlefieldEnvironmentManager] Lingkungan medan tempur diubah ke: {theme}");
        }
    }
}
