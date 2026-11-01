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

        [Header("Backdrop Renderers")]
        [SerializeField] private SpriteRenderer backdropSr;
        [SerializeField] private GameObject backdropObj;
        [SerializeField] private SpriteRenderer fadeBackdropSr;
        [SerializeField] private GameObject fadeBackdropObj;

        [Header("Transition Settings")]
        [SerializeField] private float crossfadeDuration = 0.5f;
        private Coroutine crossfadeCoroutine;

        [Header("Ambient Weather FX")]
        [SerializeField] private bool enableAmbientWeatherFx = true;
        private Coroutine ambientWeatherCoroutine;

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
            ApplyTheme(currentTheme, immediate: true);
            RestartAmbientWeatherRoutine();
            PlayBiomeBgm(currentTheme);
        }

        public void SetEnvironmentTheme(BattlefieldEnvironmentTheme newTheme)
        {
            if (currentTheme == newTheme && backdropSr != null && backdropSr.sprite != null)
            {
                return;
            }

            currentTheme = newTheme;
            ApplyTheme(currentTheme, immediate: false);
            RestartAmbientWeatherRoutine();
            PlayBiomeBgm(currentTheme);
            OnEnvironmentChanged?.Invoke(currentTheme);
        }

        private void PlayBiomeBgm(BattlefieldEnvironmentTheme theme)
        {
            if (Audio.AudioManager.Instance == null) return;

            // Lindungi BGM Boss & MiniBoss agar tidak tertimpa saat stage boss berlangsung
            var current = Audio.AudioManager.Instance.CurrentBgmTrack;
            if (current == Audio.BgmTrack.BossTheme || current == Audio.BgmTrack.MiniBossTheme)
            {
                return;
            }

            Audio.BgmTrack targetTrack = theme switch
            {
                BattlefieldEnvironmentTheme.Forest => Audio.BgmTrack.BattlefieldForest,
                BattlefieldEnvironmentTheme.Mountain => Audio.BgmTrack.BattlefieldMountain,
                BattlefieldEnvironmentTheme.Volcanic => Audio.BgmTrack.BattlefieldVolcanic,
                _ => Audio.BgmTrack.BattlefieldForest
            };

            Audio.AudioManager.Instance.PlayBGM(targetTrack);
        }

        private void EnsureBackdropRenderer()
        {
            var arena = GameObject.Find("ArenaWorldContainer");
            Transform parent = arena != null ? arena.transform : transform;

            // 1. Primary Backdrop
            if (backdropSr == null)
            {
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

            // 2. Secondary Crossfade Backdrop
            if (fadeBackdropSr == null)
            {
                var existingFade = parent.Find("BattlefieldBackdrop_Fade");
                if (existingFade != null)
                {
                    fadeBackdropObj = existingFade.gameObject;
                    fadeBackdropSr = fadeBackdropObj.GetComponent<SpriteRenderer>();
                }
                else
                {
                    fadeBackdropObj = new GameObject("BattlefieldBackdrop_Fade");
                    fadeBackdropObj.transform.SetParent(parent, false);
                    fadeBackdropObj.transform.position = new Vector3(0f, 0f, 4.95f);
                    fadeBackdropSr = fadeBackdropObj.AddComponent<SpriteRenderer>();
                }

                if (fadeBackdropSr != null)
                {
                    fadeBackdropSr.sortingLayerName = "Default";
                    fadeBackdropSr.sortingOrder = -49;
                    fadeBackdropSr.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

        private Sprite LoadThemeSprite(BattlefieldEnvironmentTheme theme)
        {
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
                sprite = Resources.Load<Sprite>("Sprites/Environment/layer1_ground_battlefield");
            }
            return sprite;
        }

        private void ApplyTheme(BattlefieldEnvironmentTheme theme, bool immediate)
        {
            EnsureBackdropRenderer();
            Sprite newSprite = LoadThemeSprite(theme);
            if (newSprite == null || backdropSr == null) return;

            if (immediate || backdropSr.sprite == null)
            {
                if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
                backdropSr.sprite = newSprite;
                backdropSr.color = Color.white;
                backdropObj.transform.localScale = Vector3.one;

                if (fadeBackdropSr != null)
                {
                    fadeBackdropSr.color = new Color(1f, 1f, 1f, 0f);
                }
            }
            else
            {
                if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = StartCoroutine(CrossfadeBackdropRoutine(newSprite));
            }

            Debug.Log($"[BattlefieldEnvironmentManager] Lingkungan medan tempur diubah ke: {theme} (Immediate: {immediate})");
        }

        private System.Collections.IEnumerator CrossfadeBackdropRoutine(Sprite newSprite)
        {
            if (fadeBackdropSr == null || backdropSr == null) yield break;

            fadeBackdropSr.sprite = newSprite;
            fadeBackdropObj.transform.localScale = Vector3.one;

            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / crossfadeDuration);
                fadeBackdropSr.color = new Color(1f, 1f, 1f, t);
                yield return null;
            }

            // Rampungkan transisi: jadikan newSprite sebagai sprite utama
            backdropSr.sprite = newSprite;
            backdropSr.color = Color.white;
            fadeBackdropSr.color = new Color(1f, 1f, 1f, 0f);
            fadeBackdropSr.sprite = null;
            crossfadeCoroutine = null;
        }

        #region Biome Ambient Weather FX

        private void RestartAmbientWeatherRoutine()
        {
            if (ambientWeatherCoroutine != null)
            {
                StopCoroutine(ambientWeatherCoroutine);
                ambientWeatherCoroutine = null;
            }

            if (enableAmbientWeatherFx && gameObject.activeInHierarchy)
            {
                ambientWeatherCoroutine = StartCoroutine(BiomeAmbientWeatherLoop());
            }
        }

        private System.Collections.IEnumerator BiomeAmbientWeatherLoop()
        {
            yield return new WaitForSeconds(0.5f);

            while (enableAmbientWeatherFx)
            {
                float waitInterval = currentTheme switch
                {
                    BattlefieldEnvironmentTheme.Forest => UnityEngine.Random.Range(0.45f, 0.75f),
                    BattlefieldEnvironmentTheme.Mountain => UnityEngine.Random.Range(0.35f, 0.65f),
                    BattlefieldEnvironmentTheme.Volcanic => UnityEngine.Random.Range(0.25f, 0.50f),
                    _ => 0.5f
                };

                yield return new WaitForSeconds(waitInterval);

                if (SkillVfxManager.Instance != null)
                {
                    SpawnBiomeAmbientParticle();
                }
            }
        }

        private void SpawnBiomeAmbientParticle()
        {
            float spawnX = UnityEngine.Random.Range(-2.8f, 2.8f);
            float spawnY = UnityEngine.Random.Range(0.5f, 5.5f);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            switch (currentTheme)
            {
                case BattlefieldEnvironmentTheme.Forest:
                    Color leafMain = new Color(0.42f, 0.82f, 0.28f, 0.85f);
                    Color leafSec = new Color(0.85f, 0.95f, 0.35f, 0.80f);
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(spawnPos, leafMain, leafSec, 2, 0.85f, sortingOrder: -40);
                    break;

                case BattlefieldEnvironmentTheme.Mountain:
                    Color snowMain = new Color(0.80f, 0.92f, 1.0f, 0.90f);
                    Color snowSec = new Color(1.0f, 1.0f, 1.0f, 0.85f);
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(spawnPos, snowMain, snowSec, 3, 1.1f, sortingOrder: -40);
                    break;

                case BattlefieldEnvironmentTheme.Volcanic:
                    Color ashMain = new Color(1.0f, 0.35f, 0.08f, 0.95f);
                    Color ashSec = new Color(1.0f, 0.80f, 0.15f, 0.85f);
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(spawnPos, ashMain, ashSec, 4, 1.4f, sortingOrder: -40);
                    break;
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
            if (ambientWeatherCoroutine != null) StopCoroutine(ambientWeatherCoroutine);
        }
    }
}
