using System.Collections;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.Audio;

namespace FantasyDefense.Combat
{
    public enum AnimationState
    {
        Idle,
        Attack,
        SkillCast,
        HitRecoil,
        MergeCelebration,
        Victory
    }

    /// <summary>
    /// Pengontrol animasi visual hero yang menyajikan "visual juice" responsif dan hidup.
    /// Mendukung transisi animasi prosedural (squash & stretch, attack lunge, skill hover, bounce jump)
    /// serta pemutaran frame Sprite Sheet (Idle, Attack, Skill) jika aset sprite sheet disematkan.
    /// Memberikan game feel yang memuaskan dan hidup di layar sentuh mobile & tablet.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroVisualAnimator : MonoBehaviour
    {
        [Header("Sprite Sheet Animation Frames (Optional)")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] skillFrames;
        [SerializeField] private float frameRate = 8f;

        public void SetAttackFrames(Sprite[] frames)
        {
            attackFrames = frames;
        }

        public void SetIdleFrames(Sprite[] frames)
        {
            idleFrames = frames;
        }

        [Header("Procedural Juice Settings")]
        [Tooltip("Aktifkan animasi nafas (breathing bobbing) saat posisi diam")]
        [SerializeField] private bool enableIdleBreathing = true;
        [SerializeField] private float breatheSpeed = 2.4f;
        [SerializeField] private float breatheAmplitude = 0.020f;

        [Header("Combat Juice Timings")]
        [SerializeField] private float attackLungeDistance = 0.28f;
        [SerializeField] private float attackDuration = 0.38f;

        private SpriteRenderer spriteRenderer;
        private UnitController unitController;
        private Vector3 baseScale = Vector3.one;
        private Vector3 initialLocalPos = Vector3.zero;
        private AnimationState currentState = AnimationState.Idle;
        public AnimationState CurrentState => currentState;
        private Coroutine activeAnimRoutine;
        private Coroutine frameAnimRoutine;
        private Color defaultColor = Color.white;

        /// <summary>
        /// Mengembalikan posisi patokan petak asal hero (Anchor Position).
        /// Menjamin seluruh animasi (lunge serang, cast skill, loncat fusi) selalu kembali ke pusat petak
        /// dan TIDAK PERNAH melayang/hanyut ke luar kotak petak (Point 2).
        /// </summary>
        public Vector3 GetAnchorPosition()
        {
            if (unitController != null && unitController.CurrentCell != null)
            {
                Vector3 p = unitController.CurrentCell.transform.position;
                p.z = -0.1f;
                return p;
            }
            return transform.position;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            unitController = GetComponent<UnitController>();
            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }
        }

        private void Start()
        {
            // Pertahankan baseScale yang telah dihitung presisi oleh UnitController (75% cellSize)
            // Jangan menimpa dengan scale nol atau pecahan animasi transisi
            if ((baseScale == Vector3.one || baseScale.sqrMagnitude < 0.001f) && transform.localScale.sqrMagnitude > 0.001f)
            {
                baseScale = transform.localScale;
            }
            StartIdleAnimation();
        }

        private void OnEnable()
        {
            UnitController.OnUnitSkillTriggered += HandleUnitSkill;
            UnitController.OnUnitMerged += HandleUnitMerged;
        }

        private void OnDisable()
        {
            UnitController.OnUnitSkillTriggered -= HandleUnitSkill;
            UnitController.OnUnitMerged -= HandleUnitMerged;
            StopAllCoroutines();
        }

        public void SetBaseScale(Vector3 scale)
        {
            if (scale.sqrMagnitude > 0.001f)
            {
                baseScale = scale;
                transform.localScale = scale;
            }
        }

        #region Idle Animation

        public virtual void StartIdleAnimation()
        {
            currentState = AnimationState.Idle;
            if (activeAnimRoutine != null) StopCoroutine(activeAnimRoutine);

            if (idleFrames != null && idleFrames.Length > 0)
            {
                if (frameAnimRoutine != null) StopCoroutine(frameAnimRoutine);
                frameAnimRoutine = StartCoroutine(PlayFrameSequence(idleFrames, true));
            }

            if (enableIdleBreathing)
            {
                activeAnimRoutine = StartCoroutine(IdleBreathingRoutine());
            }
        }

        private IEnumerator IdleBreathingRoutine()
        {
            float seed = Random.Range(0f, 100f);
            Vector3 anchorLocalPos = transform.localPosition;
            while (currentState == AnimationState.Idle)
            {
                // Pada mode persiapan (Puzzle Preparation) atau saat ditarik, pertahankan ukuran matematis solid tanpa distorsi nafas
                if (unitController != null && (unitController.IsDragging || unitController.IsPreparationToken))
                {
                    if (baseScale.sqrMagnitude > 0.001f)
                    {
                        transform.localScale = baseScale;
                        transform.localPosition = anchorLocalPos;
                        transform.localRotation = Quaternion.identity;
                    }
                    yield return null;
                    continue;
                }

                if (baseScale.sqrMagnitude > 0.001f)
                {
                    float time = (Time.time + seed) * breatheSpeed;
                    float scaleModY = 1.0f + Mathf.Sin(time) * breatheAmplitude;
                    float scaleModX = 1.0f - Mathf.Sin(time) * (breatheAmplitude * 0.35f);

                    transform.localScale = new Vector3(baseScale.x * scaleModX, baseScale.y * scaleModY, baseScale.z);

                    // Kompensasi jangkar kaki:
                    // Karena pivot sprite berada di tengah, ekspansi tinggi Y akan mendorong kepala ke atas dan kaki ke bawah.
                    // Untuk mengunci posisi telapak kaki agar 100% diam menapak di atas plinth (grounded),
                    // kita angkat posisi Y sebesar setengah dari pertambahan tinggi dada.
                    float footAnchorCompensation = (scaleModY - 1.0f) * (baseScale.y * 0.48f);
                    transform.localPosition = anchorLocalPos + new Vector3(0f, footAnchorCompensation, 0f);
                    transform.localRotation = Quaternion.identity;
                }
                yield return null;
            }
        }

        #endregion

        #region Attack Animation & Lunge

        /// <summary>
        /// Memicu animasi serang (lunge forward menusuk ke target lalu kembali membal secara elastis).
        /// </summary>
        public virtual void PlayAttack(Vector3 targetWorldPos)
        {
            PlayAttack(targetWorldPos, null);
        }

        /// <summary>
        /// Memicu animasi serang terkalibrasi dengan callback impak / peluncuran proyektil presisi pada pose Strike.
        /// </summary>
        public virtual void PlayAttack(Vector3 targetWorldPos, System.Action onStrikeImpact)
        {
            if (unitController != null && unitController.IsDragging) return;

            currentState = AnimationState.Attack;
            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                transform.position = GetAnchorPosition();
                transform.localScale = baseScale;
                transform.localRotation = Quaternion.identity;
            }

            if (frameAnimRoutine != null)
            {
                StopCoroutine(frameAnimRoutine);
                frameAnimRoutine = null;
            }

            activeAnimRoutine = StartCoroutine(AttackLungeRoutine(targetWorldPos, onStrikeImpact));
        }

        private IEnumerator AttackLungeRoutine(Vector3 targetWorldPos, System.Action onStrikeImpact = null)
        {
            Vector3 anchorPos = GetAnchorPosition();
            Vector3 dir = (targetWorldPos - anchorPos).normalized;
            if (dir == Vector3.zero) dir = Vector3.up;

            // Archetype-Specific Windup & Lunge Motion Cadence
            Vector3 windupPos;
            Vector3 lungePos;
            float attackTilt = Mathf.Clamp(-dir.x * 16f, -20f, 20f);

            if (unitController != null && unitController.UnitType == UnitType.WindRanger)
            {
                // Archer: Tarik tali busur ke belakang (recoil), tidak menerjang maju seperti pendekar pedang
                windupPos = anchorPos - dir * 0.06f;
                lungePos = anchorPos + dir * 0.03f;
            }
            else if (unitController != null && unitController.UnitType == UnitType.RoseTanker)
            {
                // Paladin: Mengangkat palu perang tinggi ke atas, lalu menghantam tanah (downward slam)
                windupPos = anchorPos + Vector3.up * 0.08f;
                lungePos = anchorPos + dir * 0.12f + Vector3.down * 0.03f;
            }
            else if (unitController != null && unitController.UnitType == UnitType.RabbitMage)
            {
                // Mage: Melayang anggun saat mengumpulkan tenaga sihir bintang di tongkat kristal
                windupPos = anchorPos + Vector3.up * 0.06f;
                lungePos = anchorPos + Vector3.up * 0.02f;
            }
            else
            {
                // Melee Spellblade (Cedric & default): Tarikan mundur lalu terjang maju tajam
                windupPos = anchorPos - dir * 0.07f;
                lungePos = anchorPos + dir * attackLungeDistance;
            }

            Sprite defaultSprite = spriteRenderer != null ? spriteRenderer.sprite : null;

            // Orientasi serangan: Kemiringan hero condong halus ke arah lintasan tebasan monster
            Quaternion strikeRot = Quaternion.Euler(0f, 0f, attackTilt);
            Quaternion windupRot = Quaternion.Euler(0f, 0f, -attackTilt * 0.35f);

            // Kalibrasi Timing Animasi Serangan 3-Sprite Kanonikal:
            // Memberi jendela waktu tayang yang tegas dan jelas bagi mata pemain pada setiap frame sprite
            float tWindup = 0.08f;      // Frame 0 (Combat Ready stance)
            float tLunge = 0.12f;       // Gerakan maju menerjang
            float tStrikeHold = 0.18f;  // Freeze-frame pada pose tebasan puncak (Strike)
            float tFollow = 0.16f;      // Frame 2 (Follow-through / Recovery pose)
            float tReturn = 0.10f;      // Kembali mulus ke tatakan plinth

            // 1. Fase Windup / Tarikan Mundur (Mengumpulkan tenaga tebasan dengan squash sangat halus)
            if (attackFrames != null && attackFrames.Length > 0 && attackFrames[0] != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[0];
            }

            float elapsed = 0f;
            while (elapsed < tWindup)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tWindup);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(anchorPos, windupPos, ease);
                transform.localScale = new Vector3(baseScale.x * Mathf.Lerp(1.0f, 1.03f, ease),
                                                   baseScale.y * Mathf.Lerp(1.0f, 0.97f, ease),
                                                   baseScale.z);
                transform.localRotation = Quaternion.Slerp(Quaternion.identity, windupRot, ease);
                yield return null;
            }

            // 2. Fase Dynamic Slash / Impact (Berganti ke pose hantaman/tebasan pedang seketika saat menerjang)
            if (attackFrames != null && attackFrames.Length > 1 && attackFrames[1] != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[1];
            }

            // Audio sfx tebasan & lontaran serangan terkalibrasi per hero archetype
            if (AudioManager.Instance != null && unitController != null)
            {
                switch (unitController.UnitType)
                {
                    case UnitType.Cedric:
                        AudioManager.Instance.PlaySound(SoundType.Slash, 1.05f);
                        break;
                    case UnitType.RoseTanker:
                        AudioManager.Instance.PlaySound(SoundType.ShieldSlam, 0.95f);
                        break;
                    case UnitType.WindRanger:
                        float bowPitch = Random.Range(1.02f, 1.18f);
                        AudioManager.Instance.PlaySound(SoundType.ArrowShot, bowPitch);
                        break;
                    case UnitType.RabbitMage:
                        AudioManager.Instance.PlaySound(SoundType.MagicSpark, 1.15f);
                        break;
                }
            }

            // Spawn efek visual serangan terintegrasi (tebasan pedang, gelombang kejut, panah angin, atau berkas sihir bintang)
            if (SkillVfxManager.Instance != null && unitController != null)
            {
                if (unitController.UnitType == UnitType.Cedric)
                {
                    SkillVfxManager.Instance.SpawnUpwardCrescentSlash(anchorPos, lungePos + dir * 0.15f, unitController.CurrentTier);
                }
                else if (unitController.UnitType == UnitType.RoseTanker)
                {
                    SkillVfxManager.Instance.SpawnUpwardRadiantShockwave(anchorPos, lungePos, unitController.CurrentTier);
                }
                else if (unitController.UnitType == UnitType.WindRanger)
                {
                    // Letupan partikel angin hijau di ujung busur saat tali busur dilepaskan
                    Color windEmerald = unitController.CurrentTier >= UnitTier.Tier4 ? new Color(0.15f, 1.0f, 0.75f) : new Color(0.25f, 0.95f, 0.45f);
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(anchorPos + new Vector3(0.12f, 0.28f, 0f), windEmerald, Color.white, 5, 1.8f);
                }
                else if (unitController.UnitType == UnitType.RabbitMage)
                {
                    // Pippin menembakkan bola sihir kristal (MagicProjectile) dari RabbitMageUnit, tidak perlu beam pendek redundan
                }
            }

            elapsed = 0f;
            while (elapsed < tLunge)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tLunge);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out
                transform.position = Vector3.Lerp(windupPos, lungePos, ease);
                transform.localScale = new Vector3(baseScale.x * Mathf.Lerp(1.03f, 0.98f, ease),
                                                   baseScale.y * Mathf.Lerp(0.97f, 1.04f, ease),
                                                   baseScale.z);
                transform.localRotation = Quaternion.Slerp(windupRot, strikeRot, ease);
                yield return null;
            }

            // 2b. Freeze-Frame Strike Hold (Tahan frame strike selama 0.18s agar pose serang terlihat jelas dan tajam oleh mata pemain)
            onStrikeImpact?.Invoke();

            elapsed = 0f;
            while (elapsed < tStrikeHold)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 3. Fase Follow-Through Apex (Menahan pose recovery / pelepasan tebasan)
            if (attackFrames != null && attackFrames.Length > 2 && attackFrames[2] != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[2];
            }
            else if (attackFrames != null && attackFrames.Length > 1 && attackFrames[1] != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[1];
            }

            elapsed = 0f;
            while (elapsed < tFollow)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tFollow);
                transform.position = Vector3.Lerp(lungePos, lungePos + dir * 0.02f, t);
                transform.localScale = Vector3.Lerp(new Vector3(baseScale.x * 0.98f, baseScale.y * 1.04f, baseScale.z), baseScale, t);
                yield return null;
            }

            // 4. Fase Elastic Return & Settle (Kembali meluncur mulus ke tatakan plinth dengan pose siaga)
            elapsed = 0f;
            Vector3 peakPos = transform.position;
            Quaternion peakRot = transform.localRotation;
            while (elapsed < tReturn)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tReturn);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(peakPos, anchorPos, ease);

                float bounceSquash = Mathf.Sin(t * Mathf.PI) * 0.03f;
                transform.localScale = new Vector3(baseScale.x * (1f + bounceSquash),
                                                   baseScale.y * (1f - bounceSquash),
                                                   baseScale.z);
                transform.localRotation = Quaternion.Slerp(peakRot, Quaternion.identity, ease);
                yield return null;
            }

            if (defaultSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = defaultSprite;
            }

            transform.position = anchorPos;
            transform.localScale = baseScale;
            transform.localRotation = Quaternion.identity;
            StartIdleAnimation();
        }

        #endregion

        #region Skill Cast & Aura Pulse

        /// <summary>
        /// Memicu animasi casting skill: terangkat ke udara, kilatan warna putih-emas, dan getaran aura.
        /// </summary>
        public void PlaySkillCast(string skillName, Color auraColor)
        {
            currentState = AnimationState.SkillCast;
            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                transform.position = GetAnchorPosition();
                transform.localScale = baseScale;
            }

            if (skillFrames != null && skillFrames.Length > 0)
            {
                if (frameAnimRoutine != null) StopCoroutine(frameAnimRoutine);
                frameAnimRoutine = StartCoroutine(PlayFrameSequence(skillFrames, false));
            }

            activeAnimRoutine = StartCoroutine(SkillCastRoutine(auraColor, spriteRenderer != null ? spriteRenderer.sprite : null));
        }

        private IEnumerator SkillCastRoutine(Color auraColor, Sprite baseSprite = null)
        {
            Vector3 anchorPos = GetAnchorPosition();
            Vector3 hoverPos = anchorPos + Vector3.up * 0.25f;

            float castDuration = 0.38f;
            float elapsed = 0f;

            // Flash warna aura skill
            if (spriteRenderer != null)
                spriteRenderer.color = auraColor;

            while (elapsed < castDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / castDuration);
                float hoverProgress = Mathf.Sin(t * Mathf.PI);

                transform.position = Vector3.Lerp(anchorPos, hoverPos, hoverProgress);
                transform.localScale = baseScale * (1f + hoverProgress * 0.28f);
                yield return null;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
                if (baseSprite != null)
                {
                    spriteRenderer.sprite = baseSprite;
                }
            }

            transform.position = anchorPos;
            transform.localScale = baseScale;
            StartIdleAnimation();
        }

        private Coroutine auraPulseRoutine;

        /// <summary>
        /// Memicu denyut kilau aura magis non-interuptif saat merapal serangan berkekuatan elemen.
        /// </summary>
        public void PulseAura(Color auraColor, float duration = 0.35f)
        {
            if (auraPulseRoutine != null) StopCoroutine(auraPulseRoutine);
            if (gameObject.activeInHierarchy)
            {
                auraPulseRoutine = StartCoroutine(AuraPulseRoutine(auraColor, duration));
            }
        }

        private IEnumerator AuraPulseRoutine(Color auraColor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(defaultColor, auraColor, pulse);
                }
                yield return null;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
            auraPulseRoutine = null;
        }

        #endregion

        #region Merge & Evolution Celebration Jump

        /// <summary>
        /// Memicu animasi perayaan evolusi tanpa argumen eksplisit (menggunakan tier unit saat ini).
        /// </summary>
        public void PlayMergeCelebration()
        {
            PlayMergeCelebration(unitController != null ? unitController.CurrentTier : UnitTier.Tier1);
        }

        /// <summary>
        /// Memicu animasi perayaan evolusi: lompatan parabola gembira dengan rotasi & deburan pendaratan.
        /// </summary>
        public void PlayMergeCelebration(UnitTier newTier)
        {
            currentState = AnimationState.MergeCelebration;
            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                transform.position = GetAnchorPosition();
                transform.localScale = baseScale;
            }

            activeAnimRoutine = StartCoroutine(MergeJumpRoutine(newTier));
        }

        private IEnumerator MergeJumpRoutine(UnitTier newTier)
        {
            Vector3 anchorPos = GetAnchorPosition();
            float jumpHeight = newTier == UnitTier.Tier5 ? 0.95f : (newTier == UnitTier.Tier4 ? 0.80f : (newTier == UnitTier.Tier3 ? 0.65f : 0.45f));
            float duration = 0.38f; // Cepat, responsif, snappier arcade game feel tanpa jeda lama
            float elapsed = 0f;

            // Flash pendar fusi dramatis
            Color glowColor = newTier switch
            {
                UnitTier.Tier5 => new Color(1f, 0.95f, 0.6f),  // Celestial Divine Gold
                UnitTier.Tier4 => new Color(0.85f, 0.4f, 1f),  // Mythic Astral Violet
                UnitTier.Tier3 => new Color(1f, 0.92f, 0.45f),
                UnitTier.Tier2 => new Color(0.6f, 0.95f, 1f),
                _ => new Color(0.9f, 1f, 0.9f)
            };
            if (spriteRenderer != null)
                spriteRenderer.color = glowColor;

            bool hasLandedHaptic = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Parabola loncatan halus dengan kurva perlambatan di puncak: 4 * h * t * (1 - t)
                float currentY = 4f * jumpHeight * t * (1f - t);
                transform.position = anchorPos + new Vector3(0f, currentY, 0f);

                // Squash saat mendarat dan stretch saat melompat
                if (t > 0.85f)
                {
                    if (!hasLandedHaptic)
                    {
                        hasLandedHaptic = true;
                        if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                            FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
                        {
                            FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(
                                newTier >= UnitTier.Tier3 ? FantasyDefense.Mobile.HapticFeedbackType.Heavy : FantasyDefense.Mobile.HapticFeedbackType.Light);
                        }
                    }
                    float landT = (t - 0.85f) / 0.15f;
                    float squashX = Mathf.Lerp(1.25f, 1.0f, landT);
                    float squashY = Mathf.Lerp(0.78f, 1.0f, landT);
                    transform.localScale = new Vector3(baseScale.x * squashX, baseScale.y * squashY, baseScale.z);
                }
                else
                {
                    transform.localScale = baseScale * (1f + 0.22f * Mathf.Sin(t * Mathf.PI));
                }

                // Kembalikan warna pendar perlahan ke warna default di akhir
                if (spriteRenderer != null && t > 0.6f)
                {
                    spriteRenderer.color = Color.Lerp(glowColor, defaultColor, (t - 0.6f) / 0.4f);
                }

                yield return null;
            }

            transform.position = anchorPos;
            transform.localScale = baseScale;
            if (spriteRenderer != null)
                spriteRenderer.color = defaultColor;

            if (unitController != null)
            {
                unitController.UpdateDepthSorting();
            }

            StartIdleAnimation();
        }

        /// <summary>
        /// Menghentikan animasi aktif (misal loncatan selebrasi) secara instan dan mengunci visual ke posisi anchor petak.
        /// Sangat krusial sebelum unit dipindahkan / dinaikkan (cascade) agar tidak ada glitch tumpang tindih visual.
        /// </summary>
        public void StopActiveAnimationAndResetAnchor()
        {
            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                activeAnimRoutine = null;
            }
            Vector3 anchor = GetAnchorPosition();
            transform.position = anchor;
            transform.localScale = baseScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
            if (unitController != null)
            {
                unitController.UpdateDepthSorting();
            }
            currentState = AnimationState.Idle;
        }

        #endregion

        #region Hit Recoil

        public virtual void PlayHitRecoil()
        {
            if (currentState == AnimationState.MergeCelebration || currentState == AnimationState.SkillCast) return;
            StartCoroutine(HitRecoilRoutine());
        }

        private IEnumerator HitRecoilRoutine()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 0.4f, 0.4f);

            Vector3 anchorPos = GetAnchorPosition();
            for (int i = 0; i < 3; i++)
            {
                transform.position = anchorPos + (Vector3)(Random.insideUnitCircle * 0.08f);
                yield return new WaitForSeconds(0.03f);
            }

            transform.position = anchorPos;
            if (spriteRenderer != null)
                spriteRenderer.color = defaultColor;
        }

        public void PlayDefeatAnimation()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.45f);
            }
        }

        public void RestoreNormalVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
        }

        #endregion

        #region Sprite Sheet Frame Playback

        private IEnumerator PlayFrameSequence(Sprite[] frames, bool loop)
        {
            if (frames == null || frames.Length == 0 || spriteRenderer == null) yield break;

            float waitTime = 1f / Mathf.Max(1f, frameRate);
            int index = 0;

            while (true)
            {
                spriteRenderer.sprite = frames[index];
                index++;

                if (index >= frames.Length)
                {
                    if (loop) index = 0;
                    else break;
                }

                yield return new WaitForSeconds(waitTime);
            }
        }

        #endregion

        #region Global Event Integrations

        private void HandleUnitSkill(UnitController unit, string skillName)
        {
            if (unit == unitController)
            {
                Color aura = unit.CurrentTier switch
                {
                    UnitTier.Tier5 => new Color(1f, 0.95f, 0.6f),
                    UnitTier.Tier4 => new Color(0.85f, 0.35f, 1f),
                    UnitTier.Tier3 => new Color(1f, 0.85f, 0.2f),
                    _ => new Color(0.4f, 0.9f, 1f)
                };
                PlaySkillCast(skillName, aura);
            }
        }

        private void HandleUnitMerged(UnitController source, UnitController target, UnitTier newTier)
        {
            if (target == unitController)
            {
                PlayMergeCelebration(newTier);
            }
        }

        #endregion
    }
}
