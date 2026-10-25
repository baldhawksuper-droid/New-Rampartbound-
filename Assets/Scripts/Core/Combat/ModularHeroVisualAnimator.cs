using System.Collections;
using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Animator modular 2.5D untuk hero yang menggerakkan persendian anggota tubuh secara terpisah
    /// (Kepala Chibi, Dada/Jubah, Lengan Pedang, Lengan Perisai, dan Garis Tebasan Slash VFX).
    /// Mengeliminasi kesan "stiker kaku" dan menghadirkan game feel yang hidup setara Rush Royale / Lucky Defense.
    /// </summary>
    public class ModularHeroVisualAnimator : HeroVisualAnimator
    {
        [Header("Modular Body Parts")]
        [SerializeField] private Transform torsoBone;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform swordArmBone;
        [SerializeField] private Transform shieldArmBone;
        [SerializeField] private Transform slashVfxBone;
        [SerializeField] private SpriteRenderer slashVfxSr;

        [Header("Modular Animation Tuning")]
        [SerializeField] private float headSecondaryDelay = 0.35f;
        [SerializeField] private float swordIdleSwayAngle = 3.5f;
        [SerializeField] private float shieldIdleSwayAngle = 2.0f;
        [SerializeField] private float attackPullbackAngle = -35f;
        [SerializeField] private float attackSlashAngle = 72f;

        private Vector3 headBasePos;
        private Vector3 torsoBasePos;
        private Vector3 swordArmBasePos;
        private Vector3 shieldArmBasePos;
        private Quaternion swordArmBaseRot = Quaternion.identity;
        private Quaternion shieldArmBaseRot = Quaternion.identity;

        private Coroutine modularIdleRoutine;
        private Coroutine modularAttackRoutine;
        private bool isModularConfigured = false;

        private void Awake()
        {
            SetupModularHierarchyIfNeeded();
        }

        /// <summary>
        /// Secara otomatis mencari atau menyusun hirarki modular jika aset bagian tubuh tersedia.
        /// </summary>
        public void SetupModularHierarchyIfNeeded()
        {
            if (torsoBone == null) torsoBone = transform.Find("Torso");
            if (headBone == null) headBone = transform.Find("Head");
            if (swordArmBone == null) swordArmBone = transform.Find("Arm_Sword");
            if (shieldArmBone == null) shieldArmBone = transform.Find("Arm_Shield");
            if (slashVfxBone == null) slashVfxBone = transform.Find("VFX_Slash");

            if (slashVfxBone != null && slashVfxSr == null)
            {
                slashVfxSr = slashVfxBone.GetComponent<SpriteRenderer>();
            }

            if (torsoBone != null && headBone != null && swordArmBone != null)
            {
                headBasePos = headBone.localPosition;
                torsoBasePos = torsoBone.localPosition;
                swordArmBasePos = swordArmBone.localPosition;
                swordArmBaseRot = swordArmBone.localRotation;

                if (shieldArmBone != null)
                {
                    shieldArmBasePos = shieldArmBone.localPosition;
                    shieldArmBaseRot = shieldArmBone.localRotation;
                }

                if (slashVfxSr != null)
                {
                    slashVfxSr.color = new Color(1f, 1f, 1f, 0f);
                }

                isModularConfigured = true;
            }
        }

        private void OnEnable()
        {
            if (isModularConfigured)
            {
                StartModularIdle();
            }
        }

        private void OnDisable()
        {
            StopAllModularRoutines();
        }

        private void StopAllModularRoutines()
        {
            if (modularIdleRoutine != null) { StopCoroutine(modularIdleRoutine); modularIdleRoutine = null; }
            if (modularAttackRoutine != null) { StopCoroutine(modularAttackRoutine); modularAttackRoutine = null; }
        }

        public void StartModularIdle()
        {
            if (!isModularConfigured) return;
            if (modularAttackRoutine != null) StopCoroutine(modularAttackRoutine);
            if (modularIdleRoutine != null) StopCoroutine(modularIdleRoutine);

            modularIdleRoutine = StartCoroutine(ModularIdleRoutine());
        }

        private IEnumerator ModularIdleRoutine()
        {
            float seed = Random.Range(0f, 100f);
            float breatheSpeed = 3.2f;

            while (true)
            {
                float time = (Time.time + seed) * breatheSpeed;

                // 1. Dada / Torso bernapas ritmis (Squash & Stretch lembut)
                if (torsoBone != null)
                {
                    float torsoYOffset = Mathf.Sin(time) * 0.015f;
                    torsoBone.localPosition = torsoBasePos + new Vector3(0f, torsoYOffset, 0f);
                    float breatheY = 1.0f + Mathf.Sin(time) * 0.035f;
                    float breatheX = 1.0f - Mathf.Sin(time) * 0.02f;
                    torsoBone.localScale = new Vector3(breatheX, breatheY, 1f);
                }

                // 2. Kepala Chibi mengikuti dengan jeda waktu fase (Secondary Follow-Through)
                if (headBone != null)
                {
                    float headTime = time - headSecondaryDelay;
                    float headYOffset = Mathf.Sin(headTime) * 0.025f;
                    float headTilt = Mathf.Sin(headTime * 0.8f) * 1.8f;
                    headBone.localPosition = headBasePos + new Vector3(0f, headYOffset, 0f);
                    headBone.localRotation = Quaternion.Euler(0f, 0f, headTilt);
                }

                // 3. Lengan Pedang berayun lembut dalam posisi siaga
                if (swordArmBone != null)
                {
                    float swordAngle = Mathf.Sin(time) * swordIdleSwayAngle;
                    swordArmBone.localRotation = swordArmBaseRot * Quaternion.Euler(0f, 0f, swordAngle);
                }

                // 4. Lengan Perisai bersiap menahan
                if (shieldArmBone != null)
                {
                    float shieldAngle = -Mathf.Sin(time * 0.9f) * shieldIdleSwayAngle;
                    shieldArmBone.localRotation = shieldArmBaseRot * Quaternion.Euler(0f, 0f, shieldAngle);
                }

                yield return null;
            }
        }

        /// <summary>
        /// Override PlayAttack dari HeroVisualAnimator agar memicu animasi tebasan modular secara otomatis.
        /// </summary>
        public override void PlayAttack(Vector3 targetWorldPos)
        {
            if (isModularConfigured)
            {
                PlayModularAttack(targetWorldPos, null);
            }
            else
            {
                base.PlayAttack(targetWorldPos);
            }
        }

        /// <summary>
        /// Memicu animasi tebasan pedang modular nyata (Bukan sekadar menggeser stiker utuh).
        /// </summary>
        public void PlayModularAttack(Vector3 targetWorldPos, System.Action onSlashImpact = null)
        {
            if (!isModularConfigured)
            {
                // Fallback ke animasi prosedural bawaan jika belum terkonfigurasi modular
                PlayAttack(targetWorldPos);
                return;
            }

            if (modularIdleRoutine != null) StopCoroutine(modularIdleRoutine);
            if (modularAttackRoutine != null) StopCoroutine(modularAttackRoutine);

            modularAttackRoutine = StartCoroutine(ModularAttackRoutine(targetWorldPos, onSlashImpact));
        }

        private IEnumerator ModularAttackRoutine(Vector3 targetWorldPos, System.Action onSlashImpact)
        {
            Vector3 startPos = transform.position;
            Vector3 aimDir = (targetWorldPos - startPos).normalized;

            // Orientasi serangan menghadap monster
            float aimTilt = Mathf.Clamp(aimDir.x * 20f, -22f, 22f);
            Quaternion targetAimRot = Quaternion.Euler(0f, 0f, aimTilt);

            // ==========================================
            // FASE 1: ABA-ABA (Anticipation / Pullback) - 0.06s
            // ==========================================
            float durPhase1 = 0.06f;
            float elapsed = 0f;

            while (elapsed < durPhase1)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durPhase1);

                // Tangan pedang ditarik ke belakang
                if (swordArmBone != null)
                {
                    swordArmBone.localRotation = Quaternion.Slerp(swordArmBaseRot, swordArmBaseRot * Quaternion.Euler(0f, 0f, attackPullbackAngle), t);
                }

                // Tubuh merunduk sedikit
                if (torsoBone != null)
                {
                    torsoBone.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.12f, 0.88f, 1f), t);
                }

                yield return null;
            }

            // ==========================================
            // FASE 2: AYUNAN TEBASAN (Slash Strike) - 0.08s
            // ==========================================
            float durPhase2 = 0.08f;
            elapsed = 0f;

            // Nyalakan Slash VFX Arc
            if (slashVfxSr != null)
            {
                slashVfxSr.color = new Color(1f, 1f, 1f, 0.95f);
                if (slashVfxBone != null)
                {
                    slashVfxBone.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-15f, 25f));
                    slashVfxBone.localScale = new Vector3(1.3f, 1.3f, 1f);
                }
            }

            while (elapsed < durPhase2)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durPhase2);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);

                // Lengan mengayun ke depan secara agresif
                if (swordArmBone != null)
                {
                    Quaternion pullRot = swordArmBaseRot * Quaternion.Euler(0f, 0f, attackPullbackAngle);
                    Quaternion slashRot = swordArmBaseRot * Quaternion.Euler(0f, 0f, attackSlashAngle);
                    swordArmBone.localRotation = Quaternion.Slerp(pullRot, slashRot, easeOut);
                }

                // Tubuh sedikit menerjang ke depan
                if (torsoBone != null)
                {
                    torsoBone.localScale = Vector3.Lerp(new Vector3(1.12f, 0.88f, 1f), new Vector3(0.92f, 1.15f, 1f), easeOut);
                }

                // Perisai menahan stabil
                if (shieldArmBone != null)
                {
                    shieldArmBone.localRotation = Quaternion.Slerp(shieldArmBaseRot, shieldArmBaseRot * Quaternion.Euler(0f, 0f, 15f), easeOut);
                }

                yield return null;
            }

            // Puncak hantaman
            onSlashImpact?.Invoke();

            // ==========================================
            // FASE 3: ELASTIC SETTLE & RESTORE - 0.12s
            // ==========================================
            float durPhase3 = 0.12f;
            elapsed = 0f;

            while (elapsed < durPhase3)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durPhase3);

                // Fade out Slash VFX
                if (slashVfxSr != null)
                {
                    slashVfxSr.color = Color.Lerp(new Color(1f, 1f, 1f, 0.95f), new Color(1f, 1f, 1f, 0f), t);
                }

                // Lengan dan tubuh kembali membal elastis ke posisi awal
                if (swordArmBone != null)
                {
                    Quaternion slashRot = swordArmBaseRot * Quaternion.Euler(0f, 0f, attackSlashAngle);
                    swordArmBone.localRotation = Quaternion.Slerp(slashRot, swordArmBaseRot, t);
                }

                if (torsoBone != null)
                {
                    torsoBone.localScale = Vector3.Lerp(new Vector3(0.92f, 1.15f, 1f), Vector3.one, t);
                    torsoBone.localPosition = Vector3.Lerp(torsoBone.localPosition, torsoBasePos, t);
                }

                if (shieldArmBone != null)
                {
                    shieldArmBone.localRotation = Quaternion.Slerp(shieldArmBone.localRotation, shieldArmBaseRot, t);
                }

                yield return null;
            }

            // Kembali ke siklus nafas idle
            StartModularIdle();
        }
    }
}
