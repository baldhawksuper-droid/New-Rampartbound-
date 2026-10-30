using System.Collections;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.Core;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Mengelola efek visual grafis (VFX) dinamis, efek flash fusi hero,
    /// cincin gelombang kejut (shockwave ring) untuk skill Tier 2 & Tier 3,
    /// serta teks melayang (Floating Combat Text) untuk Critical Hit dan Stun.
    /// Berjalan secara dinamis tanpa ketergantungan aset eksternal yang hilang.
    /// </summary>
    public class SkillVfxManager : MonoBehaviour
    {
        public static SkillVfxManager Instance { get; private set; }

        [Header("VFX Visual Settings")]
        [SerializeField] private bool enableFloatingText = true;
        [SerializeField] private bool enableShockwaves = true;
        [SerializeField] private bool enableParticleEffects = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            UnitController.OnUnitSkillTriggered += HandleUnitSkillTriggered;
            UnitController.OnUnitMerged += HandleUnitMerged;
            MonsterController.OnMonsterDamaged += HandleMonsterDamaged;
        }

        private void OnDisable()
        {
            UnitController.OnUnitSkillTriggered -= HandleUnitSkillTriggered;
            UnitController.OnUnitMerged -= HandleUnitMerged;
            MonsterController.OnMonsterDamaged -= HandleMonsterDamaged;
        }

        private void HandleUnitSkillTriggered(UnitController unit, string skillName)
        {
            if (unit == null) return;

            Vector3 pos = unit.transform.position;

            // 1. Tampilkan teks nama skill di atas pahlawan dengan banner perayaan taktil
            if (enableFloatingText)
            {
                Color skillColor = unit.CurrentTier >= UnitTier.Tier3 ? new Color(1f, 0.85f, 0.2f) : new Color(0.4f, 0.9f, 1f);
                string heroIcon = unit.UnitType switch
                {
                    UnitType.Cedric => "⚔️",
                    UnitType.RoseTanker => "🛡️",
                    UnitType.RabbitMage => "🔮",
                    UnitType.WindRanger => "🏹",
                    _ => "✨"
                };
                SpawnFloatingText(pos + Vector3.up * 0.7f, $"{heroIcon} {skillName.ToUpper()}!", skillColor, 1.35f, true);
            }

            // 2. Tampilkan efek cincin kejut (Shockwave) jika diaktifkan
            if (enableShockwaves)
            {
                Color waveColor = unit.CurrentTier >= UnitTier.Tier3 ? new Color(1f, 0.6f, 0.1f, 0.8f) : new Color(0.2f, 0.8f, 1f, 0.7f);
                float radius = unit.CurrentTier >= UnitTier.Tier3 ? 1.8f : 1.2f;
                StartCoroutine(SpawnShockwaveRingRoutine(pos, radius, waveColor));
            }

            // 3. Tampilkan sistem partikel spesifik pahlawan (Particle VFX Juice)
            if (enableParticleEffects)
            {
                SpawnSkillParticles(pos, unit.UnitType, unit.CurrentTier);
            }
        }

        private void HandleUnitMerged(UnitController source, UnitController target, UnitTier newTier)
        {
            if (target == null) return;

            Vector3 pos = target.transform.position;

            // Flash cahaya fusi (Tier 2 = Emas, Tier 3 = Pelangi/Ungu Megah, Tier 4 = Astral Violet, Tier 5 = Celestial Gold)
            Color mergeColor = newTier switch
            {
                UnitTier.Tier5 => new Color(1f, 0.95f, 0.4f, 1f),
                UnitTier.Tier4 => new Color(0.85f, 0.25f, 1f, 0.95f),
                UnitTier.Tier3 => new Color(1f, 0.2f, 0.9f, 0.9f),
                _ => new Color(1f, 0.9f, 0.2f, 0.8f)
            };
            float shockwaveRadius = newTier == UnitTier.Tier5 ? 3.0f : (newTier == UnitTier.Tier4 ? 2.5f : (newTier == UnitTier.Tier3 ? 2.0f : 1.4f));
            StartCoroutine(SpawnShockwaveRingRoutine(pos, shockwaveRadius, mergeColor));

            if (enableParticleEffects)
            {
                Color secondary = newTier == UnitTier.Tier5 ? new Color(1f, 1f, 0.9f) :
                                  (newTier == UnitTier.Tier4 ? new Color(1f, 0.85f, 0.2f) :
                                  (newTier == UnitTier.Tier3 ? new Color(1f, 0.85f, 0.2f) : Color.white));
                int burstCount = newTier == UnitTier.Tier5 ? 42 : (newTier == UnitTier.Tier4 ? 34 : (newTier == UnitTier.Tier3 ? 28 : 16));
                SpawnProceduralParticleBurst(pos, mergeColor, secondary, burstCount, newTier >= UnitTier.Tier4 ? 3.5f : 2.5f);
            }

            string text = newTier switch
            {
                UnitTier.Tier5 => "👑 TIER 5 CELESTIAL SOVEREIGN! 👑",
                UnitTier.Tier4 => "★ TIER 4 MYTHIC EVOLUTION! ★",
                UnitTier.Tier3 => "★ TIER 3 ULTIMATE! ★",
                _ => "TIER 2 EVOLVED!"
            };
            SpawnFloatingText(pos + Vector3.up * 0.85f, text, mergeColor, newTier >= UnitTier.Tier4 ? 1.6f : 1.35f);

            // Memicu Screen Shake dramatis saat berhasil fusi Tier 3+
            if (newTier >= UnitTier.Tier3 && MobileCameraAdapter.Instance != null)
            {
                float shakeDur = newTier == UnitTier.Tier5 ? 0.35f : (newTier == UnitTier.Tier4 ? 0.28f : 0.24f);
                float shakeMag = newTier == UnitTier.Tier5 ? 0.30f : (newTier == UnitTier.Tier4 ? 0.24f : 0.20f);
                MobileCameraAdapter.Instance.TriggerScreenShake(shakeDur, shakeMag);
            }
        }

        private void HandleMonsterDamaged(MonsterController monster, float damage, bool isCrit)
        {
            if (!enableFloatingText || monster == null) return;

            Vector3 pos = monster.transform.position + new Vector3(Random.Range(-0.25f, 0.25f), 0.45f, 0f);
            string text;
            Color textColor;
            float scale;
            bool isBurn = monster.IsBurned;

            if (isCrit)
            {
                // Cedric / Striker Critical Hit: Angka emas menyala melompat tinggi
                text = $"⚡ CRIT! {Mathf.RoundToInt(damage)}";
                textColor = new Color(1f, 0.84f, 0.08f); // Radiant Gold
                scale = 1.35f;
            }
            else if (isBurn)
            {
                // Rabbit Mage Meteor / Burn DoT: Warna merah-oranye / magenta berkobar
                text = $"🔥 {Mathf.RoundToInt(damage)}";
                textColor = new Color(1f, 0.32f, 0.18f); // Blaze Vermilion
                scale = 1.05f;
            }
            else
            {
                // Serangan fisik & magis reguler
                text = $"{Mathf.RoundToInt(damage)}";
                textColor = new Color(0.96f, 0.98f, 1f); // Crisp High-Contrast Silver White
                scale = 0.95f;
            }

            SpawnFloatingCombatNumber(pos, text, textColor, scale, isCrit);

            if (isCrit && MobileCameraAdapter.Instance != null)
            {
                MobileCameraAdapter.Instance.TriggerScreenShake(0.10f, 0.08f);
            }
        }

        /// <summary>
        /// Menghasilkan angka floating combat number dinamis dengan animasi lompatan parabolik (jump arc),
        /// gaya tipografi komik tebal (bold comic font) dengan bayangan tebal (drop shadow) kontras tinggi.
        /// </summary>
        public void SpawnFloatingCombatNumber(Vector3 worldPos, string content, Color textColor, float scale = 1.0f, bool isHighJump = false)
        {
            GameObject containerObj = new GameObject($"CombatNum_{content}");
            containerObj.transform.position = worldPos;

            // 1. Shadow TextMesh di belakang (Deep Comic Shadow)
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(containerObj.transform, false);
            shadowObj.transform.localPosition = new Vector3(0.025f, -0.025f, 0.01f);
            TextMesh shadowMesh = shadowObj.AddComponent<TextMesh>();
            shadowMesh.text = content;
            shadowMesh.fontStyle = FontStyle.Bold;
            shadowMesh.fontSize = isHighJump ? 36 : 30;
            shadowMesh.characterSize = 0.08f * scale;
            shadowMesh.anchor = TextAnchor.MiddleCenter;
            shadowMesh.alignment = TextAlignment.Center;
            shadowMesh.color = new Color(0.04f, 0.04f, 0.06f, 0.90f);
            MeshRenderer shadowMr = shadowObj.GetComponent<MeshRenderer>();
            if (shadowMr != null)
            {
                shadowMr.sortingLayerName = "UI";
                shadowMr.sortingOrder = 499;
            }

            // 2. Front TextMesh utama (Radiant Comic Text)
            TextMesh textMesh = containerObj.AddComponent<TextMesh>();
            textMesh.text = content;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.fontSize = isHighJump ? 36 : 30;
            textMesh.characterSize = 0.08f * scale;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = textColor;

            MeshRenderer mr = containerObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "UI";
                mr.sortingOrder = 500; // Prioritas mutlak di depan arena, grid, dan unit
            }

            Font comicFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (comicFont != null)
            {
                shadowMesh.font = comicFont;
                if (shadowMr != null) shadowMr.sharedMaterial = comicFont.material;
                textMesh.font = comicFont;
                if (mr != null) mr.sharedMaterial = comicFont.material;
            }

            StartCoroutine(FloatingCombatNumberRoutine(containerObj, textMesh, shadowMesh, scale, isHighJump));
        }

        private IEnumerator FloatingCombatNumberRoutine(GameObject obj, TextMesh textMesh, TextMesh shadowMesh, float baseScale, bool isCrit)
        {
            float duration = isCrit ? 0.95f : 0.80f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;
            Color shadowColor = shadowMesh != null ? shadowMesh.color : Color.black;
            float horizontalDrift = Random.Range(-0.35f, 0.35f);
            float jumpHeight = isCrit ? 0.75f : 0.45f;
            float initialCharSize = 0.08f * baseScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                if (obj != null)
                {
                    // 1. Lintasan lompatan parabolik (Jump Arc Physics)
                    float arcY = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                    float linearY = progress * 0.4f;
                    obj.transform.position = startPos + new Vector3(horizontalDrift * progress, arcY + linearY, 0f);

                    // 2. Scale punch elastis: membesar sesaat saat muncul (pop-in), lalu normal
                    float scaleMultiplier = progress < 0.20f
                        ? Mathf.Lerp(0.7f, 1.25f, progress / 0.20f)
                        : Mathf.Lerp(1.25f, 1.0f, (progress - 0.20f) / 0.30f);
                    textMesh.characterSize = initialCharSize * scaleMultiplier;
                    if (shadowMesh != null) shadowMesh.characterSize = initialCharSize * scaleMultiplier;

                    // 3. Fading transparan halus pada separuh akhir durasi
                    float alpha = progress > 0.55f ? Mathf.Lerp(1f, 0f, (progress - 0.55f) / 0.45f) : 1f;
                    textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    if (shadowMesh != null) shadowMesh.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a * alpha);
                }

                yield return null;
            }

            if (obj != null)
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// Membuat teks melayang dinamis di dunia 2D yang perlahan naik dan memudar (fallback umum).
        /// Mendukung parameter opsional isCritical untuk efek teks lebih tebal dan dramatis (misal: Boss/Brute threat alerts).
        /// </summary>
        public void SpawnFloatingText(Vector3 worldPos, string content, Color textColor, float scale = 1.0f, bool isCritical = false)
        {
            GameObject textObj = new GameObject($"FloatingText_{content}");
            textObj.transform.position = worldPos;

            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = content;
            textMesh.fontSize = isCritical ? 34 : 28;
            textMesh.fontStyle = isCritical ? FontStyle.Bold : FontStyle.Normal;
            textMesh.characterSize = 0.08f * scale;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = textColor;

            MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "UI";
                mr.sortingOrder = 500; // Prioritas mutlak di depan arena, grid, dan unit
            }

            StartCoroutine(FloatingTextRoutine(textObj, textMesh, isCritical));
        }

        private IEnumerator FloatingTextRoutine(GameObject obj, TextMesh textMesh, bool isCritical = false)
        {
            float duration = isCritical ? 1.35f : 0.9f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;
            Vector3 baseScale = obj.transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                if (obj != null)
                {
                    if (isCritical && progress < 0.25f)
                    {
                        float pop = Mathf.Sin((progress / 0.25f) * Mathf.PI * 0.5f);
                        obj.transform.localScale = baseScale * (1f + pop * 0.2f);
                    }
                    else if (isCritical)
                    {
                        obj.transform.localScale = baseScale * 1.15f;
                    }

                    obj.transform.position = startPos + Vector3.up * (progress * (isCritical ? 1.1f : 0.8f));
                    textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                }

                yield return null;
            }

            if (obj != null)
            {
                Destroy(obj);
            }
        }

        private static Sprite cachedRingSprite;

        private static Sprite GetOrCreateRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float outerRadius = center;
            float innerRadius = center * 0.70f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        float edgeFactor = Mathf.Sin(Mathf.InverseLerp(innerRadius, outerRadius, dist) * Mathf.PI);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, edgeFactor);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            return cachedRingSprite;
        }

        private IEnumerator SpawnShockwaveRingRoutine(Vector3 center, float maxRadius, Color color)
        {
            GameObject ringObj = new GameObject("ShockwaveRing");
            ringObj.transform.position = center;

            SpriteRenderer sr = ringObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateRingSprite();
            sr.color = color;
            sr.sortingOrder = 15;

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentRadius = Mathf.Lerp(0.2f, maxRadius, progress);

                if (ringObj != null)
                {
                    ringObj.transform.localScale = Vector3.one * (currentRadius * 2f);
                    sr.color = new Color(color.r, color.g, color.b, Mathf.Lerp(color.a, 0f, progress));
                }

                yield return null;
            }

            if (ringObj != null)
            {
                Destroy(ringObj);
            }
        }

        #region Procedural Particle VFX System

        /// <summary>
        /// Menghasilkan semburan partikel spesifik pahlawan (Blade sparks, Earth tremors, Arcane flames).
        /// </summary>
        public void SpawnSkillParticles(Vector3 pos, UnitType unitType, UnitTier tier)
        {
            switch (unitType)
            {
                case UnitType.Cedric:
                    // Tebasan Pedang Merah/Perak
                    Color cedricMain = tier >= UnitTier.Tier3 ? new Color(1f, 0.2f, 0.1f) : new Color(0.95f, 0.35f, 0.2f);
                    Color cedricSec = new Color(1f, 0.85f, 0.7f);
                    int cedricCount = tier >= UnitTier.Tier3 ? 24 : 14;
                    SpawnProceduralParticleBurst(pos, cedricMain, cedricSec, cedricCount, tier >= UnitTier.Tier3 ? 3.0f : 2.0f);
                    break;

                case UnitType.RoseTanker:
                    // Hantaman Palu & Gempa Mawar (Rose Gold, Deep Crimson Ruby & Keemasan Seismik)
                    Color roseMain = tier >= UnitTier.Tier4 ? new Color(1f, 0.25f, 0.55f) : (tier >= UnitTier.Tier3 ? new Color(1f, 0.45f, 0.65f) : new Color(0.9f, 0.5f, 0.5f));
                    Color roseSec = tier >= UnitTier.Tier4 ? new Color(1f, 0.90f, 0.35f) : new Color(0.9f, 0.8f, 0.4f);
                    int roseCount = tier >= UnitTier.Tier4 ? 38 : (tier >= UnitTier.Tier3 ? 26 : 15);
                    float roseSpeed = tier >= UnitTier.Tier4 ? 3.6f : (tier >= UnitTier.Tier3 ? 2.5f : 1.8f);
                    SpawnProceduralParticleBurst(pos, roseMain, roseSec, roseCount, roseSpeed);
                    break;

                case UnitType.RabbitMage:
                    // Komet Sihir & Arcane Embers (Toska Kristal & Oranye Nyala Api)
                    Color rabbitMain = tier >= UnitTier.Tier3 ? new Color(0.2f, 0.9f, 1f) : new Color(0.4f, 0.7f, 1f);
                    Color rabbitSec = new Color(1f, 0.6f, 0.1f);
                    int rabbitCount = tier >= UnitTier.Tier3 ? 30 : 18;
                    SpawnProceduralParticleBurst(pos, rabbitMain, rabbitSec, rabbitCount, tier >= UnitTier.Tier3 ? 3.5f : 2.2f);
                    break;

                case UnitType.WindRanger:
                    // Panah Angin Zamrud & Zephyr Whirlwind (Hijau Zamrud & Putih Angin)
                    Color windMain = tier >= UnitTier.Tier3 ? new Color(0.1f, 1f, 0.5f) : new Color(0.2f, 0.85f, 0.4f);
                    Color windSec = new Color(0.8f, 1f, 0.9f);
                    int windCount = tier >= UnitTier.Tier3 ? 32 : 16;
                    SpawnProceduralParticleBurst(pos, windMain, windSec, windCount, tier >= UnitTier.Tier3 ? 3.8f : 2.5f);
                    break;
            }
        }

        #region Attack Elements Visual FX (Mockup Battle Alignment)

        /// <summary>
        /// Pippin / Rabbit Mage: Menembakkan berkas laser sihir safir kembar (Twin Sapphire Arcane Beams)
        /// yang melesat lurus ke atas dari tongkat sihir ke monster sasaran.
        /// </summary>
        public void SpawnUpwardArcaneBeam(Vector3 startPos, Vector3 targetPos, UnitTier tier)
        {
            StartCoroutine(UpwardArcaneBeamRoutine(startPos, targetPos, tier));
        }

        private IEnumerator UpwardArcaneBeamRoutine(Vector3 startPos, Vector3 targetPos, UnitTier tier)
        {
            GameObject beamObj = new GameObject("VFX_Pippin_TwinLaserBeam");
            LineRenderer lr = beamObj.AddComponent<LineRenderer>();

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit");
            if (shader != null) lr.material = new Material(shader);

            Color coreColor = tier >= UnitTier.Tier4 ? new Color(0.85f, 0.45f, 1f) : new Color(0.25f, 0.85f, 1f);
            Color glowColor = tier >= UnitTier.Tier4 ? new Color(0.6f, 0.1f, 1f, 0.8f) : new Color(0.1f, 0.45f, 1f, 0.8f);

            lr.startColor = coreColor;
            lr.endColor = glowColor;
            float lineWidth = tier >= UnitTier.Tier3 ? 0.22f : 0.14f;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth * 0.75f;
            lr.positionCount = 2;
            lr.sortingOrder = 60;

            Vector3 origin = startPos + new Vector3(0f, 0.35f, 0f);
            lr.SetPosition(0, origin);
            lr.SetPosition(1, targetPos);

            // Muzzle spark di tongkat
            SpawnProceduralParticleBurst(origin, coreColor, Color.white, 8, 2.0f);

            float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float alpha = 1f - progress;
                lr.startColor = new Color(coreColor.r, coreColor.g, coreColor.b, alpha);
                lr.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, alpha * 0.7f);
                yield return null;
            }

            // Impact burst di monster
            SpawnProceduralParticleBurst(targetPos, coreColor, Color.cyan, tier >= UnitTier.Tier3 ? 16 : 10, 2.8f);
            Destroy(beamObj);
        }

        /// <summary>
        /// Cedric: Menghasilkan tebasan sabit api membara (Crescent Flame Slash)
        /// yang melesat melengkung ke atas menyapu formasi monster musuh.
        /// </summary>
        public void SpawnUpwardCrescentSlash(Vector3 startPos, Vector3 targetPos, UnitTier tier)
        {
            StartCoroutine(UpwardCrescentSlashRoutine(startPos, targetPos, tier));
        }

        private IEnumerator UpwardCrescentSlashRoutine(Vector3 startPos, Vector3 targetPos, UnitTier tier)
        {
            GameObject slashObj = new GameObject("VFX_Cedric_FlameCrescent");
            Vector3 spawnPos = startPos + new Vector3(0f, 0.40f, 0f);
            slashObj.transform.position = spawnPos;

            SpriteRenderer sr = slashObj.AddComponent<SpriteRenderer>();
            Sprite slashSprite = Resources.Load<Sprite>("Sprites/VFX/VFX_Slash_Arc");
            if (slashSprite != null) sr.sprite = slashSprite;

            Color flameColor = tier >= UnitTier.Tier3 ? new Color(1f, 0.3f, 0.05f) : new Color(1f, 0.55f, 0.1f);
            sr.color = flameColor;
            sr.sortingOrder = 60;

            Vector3 dir = (targetPos - startPos).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            slashObj.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            float scale = tier >= UnitTier.Tier3 ? 0.75f : 0.55f;
            slashObj.transform.localScale = Vector3.one * (scale * 0.5f);

            // Snappy compact duration agar tidak menumpuk saat banyak hero menyerang
            float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                slashObj.transform.position = Vector3.Lerp(spawnPos, targetPos, t);
                slashObj.transform.localScale = Vector3.one * Mathf.Lerp(scale * 0.5f, scale * 1.05f, t);
                sr.color = new Color(flameColor.r, flameColor.g, flameColor.b, (1f - t) * (1f - t));
                yield return null;
            }

            SpawnProceduralParticleBurst(targetPos, flameColor, Color.yellow, 6, 2.2f);
            Destroy(slashObj);
        }

        /// <summary>
        /// Rose Tanker: Menghasilkan pilar kejut suci keemasan (Radiant Hammer Shockwave)
        /// yang meletup dan merambat ke atas saat palu godam menghantam tanah.
        /// </summary>
        public void SpawnUpwardRadiantShockwave(Vector3 startPos, Vector3 targetPos, UnitTier tier)
        {
            StartCoroutine(UpwardRadiantShockwaveRoutine(startPos, targetPos, tier));
        }

        private IEnumerator UpwardRadiantShockwaveRoutine(Vector3 startPos, Vector3 targetPos, UnitTier tier)
        {
            Vector3 origin = startPos + new Vector3(0f, -0.15f, 0f);

            // 1. Cincin gelombang kejut kompak di tanah tempat palu menghantam
            Color roseGold = tier >= UnitTier.Tier4 ? new Color(1f, 0.30f, 0.60f) : new Color(1f, 0.85f, 0.25f);
            StartCoroutine(SpawnShockwaveRingRoutine(origin, tier >= UnitTier.Tier3 ? 0.9f : 0.65f, roseGold));

            // 2. Partikel dampak mikro terfokus pada target (tidak menutupi seluruh jalur)
            int burstCount = tier >= UnitTier.Tier4 ? 8 : 5;
            SpawnProceduralParticleBurst(origin, roseGold, Color.white, burstCount, 2.0f);
            yield return new WaitForSeconds(0.04f);
            SpawnProceduralParticleBurst(targetPos, roseGold, new Color(1f, 0.2f, 0.4f), burstCount, 2.4f);
        }

        #endregion

        /// <summary>
        /// Membuat objek ParticleSystem instan dan mandiri yang menyala sesaat lalu menghilang otomatis.
        /// Sepenuhnya kompatibel tanpa dependensi prefab eksternal di Unity Editor, Android, maupun iOS.
        /// </summary>
        public void SpawnProceduralParticleBurst(Vector3 worldPos, Color mainColor, Color secondaryColor, int count, float speed)
        {
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsBatterySaverEnabled)
            {
                count = Mathf.Max(6, count / 2);
            }

            GameObject psObj = new GameObject("VFX_ParticleBurst");
            psObj.transform.position = worldPos;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = psObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.sortingOrder = 55;
            if (psRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit");
                if (shader != null)
                {
                    psRenderer.material = new Material(shader);
                }
            }

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(mainColor, secondaryColor);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(mainColor, 0f), new GradientColorKey(secondaryColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            ps.Play();
        }

        /// <summary>
        /// Menampilkan efek visual sambaran petir badai (Thunderstorm Lightning VFX).
        /// </summary>
        public void SpawnLightningStrike(Vector3 targetPos)
        {
            if (enableShockwaves)
            {
                StartCoroutine(SpawnShockwaveRingRoutine(targetPos, 1.5f, new Color(0.3f, 0.9f, 1f, 0.9f)));
            }

            if (enableParticleEffects)
            {
                SpawnProceduralParticleBurst(targetPos, new Color(0.4f, 0.9f, 1f), Color.white, 22, 3.5f);
            }
        }

        /// <summary>
        /// Menampilkan letupan angin partikel zamrud saat panah Wind Ranger menembus musuh.
        /// </summary>
        public void SpawnArrowImpactVfx(Vector3 impactPos, bool isCrit)
        {
            if (!enableParticleEffects) return;

            Color emerald = isCrit ? new Color(0.4f, 1f, 0.65f) : new Color(0.15f, 0.90f, 0.45f);
            Color windWhite = new Color(0.9f, 1f, 0.95f);
            int count = isCrit ? 20 : 12;
            float speed = isCrit ? 3.0f : 2.0f;

            SpawnProceduralParticleBurst(impactPos, emerald, windWhite, count, speed);

            if (isCrit && enableShockwaves)
            {
                StartCoroutine(SpawnShockwaveRingRoutine(impactPos, 0.85f, new Color(0.2f, 1f, 0.5f, 0.6f)));
            }
        }

        /// <summary>
        /// Menampilkan percikan sihir kristal dan semburan area Rabbit Mage.
        /// </summary>
        public void SpawnMagicImpactVfx(Vector3 impactPos, bool isAoe, bool isMeteor)
        {
            if (!enableParticleEffects) return;

            Color primary = isMeteor ? new Color(1f, 0.35f, 0.1f) : new Color(0.25f, 0.85f, 1f);
            Color secondary = isMeteor ? new Color(1f, 0.85f, 0.2f) : new Color(0.8f, 0.45f, 1f);
            int count = isMeteor ? 26 : (isAoe ? 18 : 10);
            float speed = isMeteor ? 3.2f : 2.2f;

            SpawnProceduralParticleBurst(impactPos, primary, secondary, count, speed);

            if (enableShockwaves && (isAoe || isMeteor))
            {
                float radius = isMeteor ? 1.6f : 1.1f;
                Color waveColor = isMeteor ? new Color(1f, 0.4f, 0.1f, 0.7f) : new Color(0.2f, 0.8f, 1f, 0.6f);
                StartCoroutine(SpawnShockwaveRingRoutine(impactPos, radius, waveColor));
            }
        }

        /// <summary>
        /// Menampilkan letupan ledakan artileri meriam benteng (Incendiary Siege Detonation)
        /// dengan serpihan bara api, gelombang kejut AOE, dan kilatan api vulkanik.
        /// </summary>
        public void SpawnCannonImpactVfx(Vector3 impactPos, float aoeRadius)
        {
            if (!enableParticleEffects) return;

            Color fireOrange = new Color(1f, 0.45f, 0.08f);
            Color emberGold = new Color(1f, 0.88f, 0.22f);
            int count = 24;
            float speed = 3.6f;

            SpawnProceduralParticleBurst(impactPos, fireOrange, emberGold, count, speed);

            if (enableShockwaves)
            {
                float radius = Mathf.Max(aoeRadius, 1.4f);
                Color waveColor = new Color(1f, 0.40f, 0.05f, 0.75f);
                StartCoroutine(SpawnShockwaveRingRoutine(impactPos, radius, waveColor));
            }
        }

        /// <summary>
        /// Menampilkan letupan kinetik penembus zirah baut balista benteng (Armor-Piercing Kinetic Shock)
        /// dengan kilatan kristal toska runik tajam, garis serpihan kinetik, dan gelombang kejut udara.
        /// </summary>
        public void SpawnBallistaPierceImpactVfx(Vector3 impactPos, bool isCrit)
        {
            if (!enableParticleEffects) return;

            Color cyanRunic = isCrit ? new Color(0.25f, 1f, 0.95f) : new Color(0.12f, 0.82f, 0.98f);
            Color kineticWhite = new Color(0.95f, 0.98f, 1f);
            int count = isCrit ? 22 : 14;
            float speed = isCrit ? 3.8f : 2.6f;

            SpawnProceduralParticleBurst(impactPos, cyanRunic, kineticWhite, count, speed);

            if (enableShockwaves)
            {
                float radius = isCrit ? 1.1f : 0.75f;
                Color waveColor = isCrit ? new Color(0.3f, 0.95f, 1f, 0.7f) : new Color(0.15f, 0.75f, 0.95f, 0.5f);
                StartCoroutine(SpawnShockwaveRingRoutine(impactPos, radius, waveColor));
            }
        }

        /// <summary>
        /// Menampilkan garis lintasan parabola berpendar (Artillery Trajectory Arc)
        /// saat artileri meriam membidik dan menembakkan peluru ke target musuh.
        /// </summary>
        public void SpawnArtilleryTrajectoryArc(Vector3 startPos, Vector3 targetPos)
        {
            if (!enableParticleEffects) return;
            StartCoroutine(ArtilleryTrajectoryArcRoutine(startPos, targetPos));
        }

        private IEnumerator ArtilleryTrajectoryArcRoutine(Vector3 startPos, Vector3 targetPos)
        {
            GameObject arcObj = new GameObject("VFX_ArtilleryTrajectoryArc");
            LineRenderer line = arcObj.AddComponent<LineRenderer>();

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                line.material = new Material(spriteShader);
            }

            int segments = 18;
            line.positionCount = segments + 1;
            line.useWorldSpace = true;
            line.sortingOrder = 22;

            float dist = Vector2.Distance(startPos, targetPos);
            float arcHeight = Mathf.Clamp(0.8f + dist * 0.18f, 1.0f, 2.5f);
            Vector3 midPoint = (startPos + targetPos) * 0.5f + Vector3.up * arcHeight;

            Color goldStart = new Color(1f, 0.82f, 0.25f, 0.85f);
            Color fireEnd = new Color(1f, 0.40f, 0.08f, 0.90f);

            line.startColor = goldStart;
            line.endColor = fireEnd;
            line.startWidth = 0.055f;
            line.endWidth = 0.025f;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 pt = (1f - t) * (1f - t) * startPos + 2f * (1f - t) * t * midPoint + t * t * targetPos;
                line.SetPosition(i, pt);
            }

            GameObject reticleObj = new GameObject("VFX_CannonTargetReticle");
            reticleObj.transform.position = targetPos;
            var reticleSr = reticleObj.AddComponent<SpriteRenderer>();
            reticleSr.sortingOrder = 20;
            var reticleTex = GetOrCreateReticleSprite();
            if (reticleTex != null) reticleSr.sprite = reticleTex;
            reticleSr.color = new Color(1f, 0.45f, 0.1f, 0.75f);
            reticleObj.transform.localScale = Vector3.one * 0.35f;

            float duration = 0.38f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - (elapsed / duration);
                line.startColor = new Color(goldStart.r, goldStart.g, goldStart.b, goldStart.a * fade);
                line.endColor = new Color(fireEnd.r, fireEnd.g, fireEnd.b, fireEnd.a * fade);
                line.startWidth = 0.055f * fade;
                line.endWidth = 0.025f * fade;

                if (reticleObj != null)
                {
                    reticleSr.color = new Color(1f, 0.45f, 0.1f, 0.75f * fade);
                    reticleObj.transform.localScale = Vector3.one * (0.35f + (1f - fade) * 0.2f);
                }

                yield return null;
            }

            Destroy(arcObj);
            if (reticleObj != null) Destroy(reticleObj);
        }

        private static Sprite cachedReticleSprite;
        private static Sprite GetOrCreateReticleSprite()
        {
            if (cachedReticleSprite != null) return cachedReticleSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float outerR = center - 2f;
            float innerR = outerR - 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    if (d >= innerR && d <= outerR)
                    {
                        float alpha = Mathf.Clamp01(1f - Mathf.Abs(d - (innerR + outerR) * 0.5f) / 2f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else if (d <= 3f)
                    {
                        pixels[y * size + x] = new Color(1f, 1f, 1f, 1f);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedReticleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return cachedReticleSprite;
        }

        /// <summary>
        /// Menampilkan efek runtuh dramatis hancur berkeping (Shatter Debris Burst)
        /// saat HP barikade benteng mencapai 0 (Kekalahan / Defeat).
        /// Balok kayu, paku besi, dan kepulan debu beterbangan dengan gravitasi fisik.
        /// </summary>
        public void SpawnBarricadeCollapseShatter(Vector3 centerPos)
        {
            if (!enableParticleEffects) return;
            StartCoroutine(BarricadeCollapseShatterRoutine(centerPos));
        }

        private IEnumerator BarricadeCollapseShatterRoutine(Vector3 centerPos)
        {
            int debrisCount = 18;
            List<GameObject> debrisList = new List<GameObject>();
            List<Vector3> velocities = new List<Vector3>();
            List<float> rotSpeeds = new List<float>();

            Color woodColor = new Color(0.48f, 0.32f, 0.18f);
            Color ironColor = new Color(0.35f, 0.38f, 0.42f);

            var boxTex = Texture2D.whiteTexture;
            Sprite boxSprite = Sprite.Create(boxTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);

            for (int i = 0; i < debrisCount; i++)
            {
                GameObject chunk = new GameObject($"BarricadeDebris_{i}");
                chunk.transform.position = centerPos + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(-0.2f, 0.3f), 0f);

                var sr = chunk.AddComponent<SpriteRenderer>();
                sr.sprite = boxSprite;
                sr.color = (i % 3 == 0) ? ironColor : woodColor;
                sr.sortingOrder = 30;

                float w = (i % 3 == 0) ? Random.Range(0.08f, 0.15f) : Random.Range(0.20f, 0.45f);
                float h = (i % 3 == 0) ? Random.Range(0.08f, 0.15f) : Random.Range(0.08f, 0.18f);
                chunk.transform.localScale = new Vector3(w, h, 1f);

                float vx = Random.Range(-3.2f, 3.2f);
                float vy = Random.Range(2.0f, 5.2f);
                velocities.Add(new Vector3(vx, vy, 0f));
                rotSpeeds.Add(Random.Range(-360f, 360f));
                debrisList.Add(chunk);
            }

            // Kepulan debu batu kapur & serbuk kayu
            SpawnProceduralParticleBurst(centerPos, new Color(0.85f, 0.78f, 0.68f), new Color(0.55f, 0.45f, 0.35f), 25, 3.2f);
            if (enableShockwaves)
            {
                StartCoroutine(SpawnShockwaveRingRoutine(centerPos, 1.8f, new Color(0.9f, 0.3f, 0.1f, 0.8f)));
            }

            float duration = 0.75f;
            float elapsed = 0f;
            float gravity = -14.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = Mathf.Clamp01(1f - (elapsed / duration));

                for (int i = 0; i < debrisList.Count; i++)
                {
                    if (debrisList[i] == null) continue;

                    Vector3 vel = velocities[i];
                    vel.y += gravity * Time.deltaTime;
                    velocities[i] = vel;

                    debrisList[i].transform.position += vel * Time.deltaTime;
                    debrisList[i].transform.Rotate(0f, 0f, rotSpeeds[i] * Time.deltaTime);

                    var sr = debrisList[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = fade;
                        sr.color = c;
                    }
                }

                yield return null;
            }

            for (int i = 0; i < debrisList.Count; i++)
            {
                if (debrisList[i] != null) Destroy(debrisList[i]);
            }
        }

        #region Hero Defender Battle Emotes (Point 3)

        private static Sprite cachedEmoteBubbleSprite;
        private static Sprite GetOrCreateEmoteBubbleSprite()
        {
            if (cachedEmoteBubbleSprite != null) return cachedEmoteBubbleSprite;

            int w = 64;
            int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            float r = 12f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - (w - 1) * 0.5f) - ((w - 1) * 0.5f - r));
                    float dy = Mathf.Max(0f, Mathf.Abs(y - (h - 1) * 0.5f) - ((h - 1) * 0.5f - r));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= r)
                    {
                        float alpha = Mathf.Clamp01(r - dist + 0.5f);
                        pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedEmoteBubbleSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return cachedEmoteBubbleSprite;
        }

        /// <summary>
        /// Menampilkan balon reaksi pertahanan (Hero Defender Battle Emotes - Point 3) di atas pahlawan saat gerbang diserang.
        /// </summary>
        public void SpawnDefenderBattleEmote(Vector3 worldPos, string emoteText, Color themeColor)
        {
            if (!enableFloatingText) return;

            GameObject emoteObj = new GameObject($"DefenderEmote_{emoteText}");
            emoteObj.transform.position = worldPos;

            // Background bubble pill
            GameObject bgObj = new GameObject("BubbleBg");
            bgObj.transform.SetParent(emoteObj.transform, false);
            SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = GetOrCreateEmoteBubbleSprite();
            bgRenderer.color = new Color(0.08f, 0.10f, 0.16f, 0.94f);
            bgRenderer.sortingLayerName = "UI";
            bgRenderer.sortingOrder = 505;

            // Border glow
            GameObject borderObj = new GameObject("BubbleBorder");
            borderObj.transform.SetParent(emoteObj.transform, false);
            SpriteRenderer borderRenderer = borderObj.AddComponent<SpriteRenderer>();
            borderRenderer.sprite = GetOrCreateEmoteBubbleSprite();
            borderRenderer.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.85f);
            borderRenderer.sortingLayerName = "UI";
            borderRenderer.sortingOrder = 504;

            // Text
            GameObject textObj = new GameObject("BubbleText");
            textObj.transform.SetParent(emoteObj.transform, false);
            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = emoteText;
            tm.fontSize = 24;
            tm.fontStyle = FontStyle.Bold;
            tm.characterSize = 0.072f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "UI";
                mr.sortingOrder = 506;
            }

            float width = Mathf.Clamp(0.7f + emoteText.Length * 0.065f, 1.1f, 2.6f);
            bgObj.transform.localScale = new Vector3(width, 0.42f, 1f);
            borderObj.transform.localScale = new Vector3(width * 1.05f, 0.48f, 1f);

            StartCoroutine(DefenderEmoteRoutine(emoteObj, bgRenderer, borderRenderer, tm));
        }

        private IEnumerator DefenderEmoteRoutine(GameObject obj, SpriteRenderer bg, SpriteRenderer border, TextMesh text)
        {
            float duration = 1.6f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Vector3 startScale = Vector3.zero;
            obj.transform.localScale = startScale;

            // 1. Pop-in bounce
            float popDuration = 0.18f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                float s = Mathf.Sin(t * Mathf.PI * 0.75f) * 1.2f;
                obj.transform.localScale = Vector3.one * s;
                yield return null;
            }
            obj.transform.localScale = Vector3.one;

            // 2. Float & fade out
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                obj.transform.position = startPos + Vector3.up * (0.35f * t);

                if (t > 0.70f)
                {
                    float fade = 1f - ((t - 0.70f) / 0.30f);
                    if (bg != null) bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 0.94f * fade);
                    if (border != null) border.color = new Color(border.color.r, border.color.g, border.color.b, 0.85f * fade);
                    if (text != null) text.color = new Color(text.color.r, text.color.g, text.color.b, fade);
                }

                yield return null;
            }

            if (obj != null)
            {
                Destroy(obj);
            }
        }

        #endregion

        #endregion
    }
}
