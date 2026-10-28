using System.Collections;
using UnityEngine;
using FantasyDefense.Data;

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

        [Header("Procedural Juice Settings")]
        [Tooltip("Aktifkan animasi nafas (breathing bobbing) saat posisi diam")]
        [SerializeField] private bool enableIdleBreathing = true;
        [SerializeField] private float breatheSpeed = 3.5f;
        [SerializeField] private float breatheAmplitude = 0.05f;

        [Header("Combat Juice Timings")]
        [SerializeField] private float attackLungeDistance = 0.22f;
        [SerializeField] private float attackDuration = 0.16f;

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
            while (currentState == AnimationState.Idle)
            {
                // Pada mode persiapan (Puzzle Preparation) atau saat ditarik, pertahankan ukuran matematis solid tanpa distorsi nafas
                if (unitController != null && (unitController.IsDragging || unitController.IsPreparationToken))
                {
                    if (baseScale.sqrMagnitude > 0.001f)
                    {
                        transform.localScale = baseScale;
                        transform.localRotation = Quaternion.identity;
                    }
                    yield return null;
                    continue;
                }

                if (baseScale.sqrMagnitude > 0.001f)
                {
                    float time = (Time.time + seed) * breatheSpeed;
                    float scaleModY = 1.0f + Mathf.Sin(time) * breatheAmplitude;
                    float scaleModX = 1.0f - Mathf.Sin(time) * (breatheAmplitude * 0.6f);

                    transform.localScale = new Vector3(baseScale.x * scaleModX, baseScale.y * scaleModY, baseScale.z);

                    // Dynamic 2.5D Subtle Breathing Sway Tilt (Sprint 44 Poin 5)
                    float tilt = Mathf.Sin(time * 0.75f) * 1.5f;
                    transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
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
            if (unitController != null && unitController.IsDragging) return;

            currentState = AnimationState.Attack;
            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                transform.position = GetAnchorPosition();
                transform.localScale = baseScale;
            }

            if (attackFrames != null && attackFrames.Length > 0)
            {
                if (frameAnimRoutine != null) StopCoroutine(frameAnimRoutine);
                frameAnimRoutine = StartCoroutine(PlayFrameSequence(attackFrames, false));
            }

            activeAnimRoutine = StartCoroutine(AttackLungeRoutine(targetWorldPos));
        }

        private IEnumerator AttackLungeRoutine(Vector3 targetWorldPos)
        {
            Vector3 anchorPos = GetAnchorPosition();
            Vector3 dir = (targetWorldPos - anchorPos).normalized;
            if (dir == Vector3.zero) dir = Vector3.up;

            Vector3 windupPos = anchorPos - dir * 0.08f;
            Vector3 lungePos = anchorPos + dir * attackLungeDistance;

            // Orientasi serangan: Kemiringan hero condong halus ke arah lintasan tebasan monster
            float attackTilt = Mathf.Clamp(-dir.x * 22f, -25f, 25f);
            Quaternion strikeRot = Quaternion.Euler(0f, 0f, attackTilt);
            Quaternion windupRot = Quaternion.Euler(0f, 0f, -attackTilt * 0.35f);

            float totalDur = Mathf.Max(0.24f, attackDuration);
            float tWindup = totalDur * 0.18f;
            float tLunge = totalDur * 0.32f;
            float tFollow = totalDur * 0.12f;
            float tReturn = totalDur * 0.38f;

            // 1. Fase Windup / Tarikan Mundur (Mengumpulkan tenaga tebasan dengan squash halus)
            float elapsed = 0f;
            while (elapsed < tWindup)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tWindup);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(anchorPos, windupPos, ease);
                transform.localScale = new Vector3(baseScale.x * Mathf.Lerp(1.0f, 1.08f, ease),
                                                   baseScale.y * Mathf.Lerp(1.0f, 0.92f, ease),
                                                   baseScale.z);
                transform.localRotation = Quaternion.Slerp(Quaternion.identity, windupRot, ease);
                yield return null;
            }

            // 2. Fase Dynamic Slash Lunge (Terjangan tebasan maju bertenaga dengan akselerasi cubic out)
            elapsed = 0f;
            while (elapsed < tLunge)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tLunge);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out
                transform.position = Vector3.Lerp(windupPos, lungePos, ease);
                transform.localScale = new Vector3(baseScale.x * Mathf.Lerp(1.08f, 0.92f, ease),
                                                   baseScale.y * Mathf.Lerp(0.92f, 1.15f, ease),
                                                   baseScale.z);
                transform.localRotation = Quaternion.Slerp(windupRot, strikeRot, ease);
                yield return null;
            }

            // 3. Fase Follow-Through Apex (Menahan sejenak di puncak tebasan saat proyektil tebasan meluncur)
            elapsed = 0f;
            while (elapsed < tFollow)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tFollow);
                transform.position = Vector3.Lerp(lungePos, lungePos + dir * 0.02f, t);
                transform.localScale = Vector3.Lerp(new Vector3(baseScale.x * 0.92f, baseScale.y * 1.15f, baseScale.z), baseScale, t);
                yield return null;
            }

            // 4. Fase Elastic Return & Settle (Kembali meluncur mulus ke tatakan plinth dengan pendaratan empuk)
            elapsed = 0f;
            Vector3 peakPos = transform.position;
            Quaternion peakRot = transform.localRotation;
            while (elapsed < tReturn)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tReturn);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(peakPos, anchorPos, ease);

                float bounceSquash = Mathf.Sin(t * Mathf.PI) * 0.05f;
                transform.localScale = new Vector3(baseScale.x * (1f + bounceSquash),
                                                   baseScale.y * (1f - bounceSquash),
                                                   baseScale.z);
                transform.localRotation = Quaternion.Slerp(peakRot, Quaternion.identity, ease);
                yield return null;
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

            activeAnimRoutine = StartCoroutine(SkillCastRoutine(auraColor));
        }

        private IEnumerator SkillCastRoutine(Color auraColor)
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
                spriteRenderer.color = defaultColor;

            transform.position = anchorPos;
            transform.localScale = baseScale;
            StartIdleAnimation();
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
            float jumpHeight = newTier == UnitTier.Tier5 ? 1.35f : (newTier == UnitTier.Tier4 ? 1.15f : (newTier == UnitTier.Tier3 ? 0.95f : 0.65f));
            float duration = 0.65f; // Diperlambat dari 0.42f agar fusi evolusi terlihat megah, realistis, dan memuaskan
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

            StartIdleAnimation();
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
