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
        // Konfigurasi Sudut Ayunan & Tebasan (North-Oriented Combat Attack)
        [Header("North Combat Attack Angles")]
        [SerializeField] private float attackPullbackAngle = -25f; // Tarik ke belakang bawah sedikit
        [SerializeField] private float attackSlashAngle = 85f;    // Tebasan lurus ke atas / utara (Northward thrust/slash)
        [SerializeField] private Vector3 attackLungeOffset = new Vector3(0f, 0.15f, 0f); // Karakter menerjang ke atas (ke arah monster)

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
        /// Secara otomatis merakit objek-objek bagian tubuh modular (Bones/Layers) jika belum ada di GameObject.
        /// </summary>
        public void ConfigureModularSprites(Sprite torso, Sprite head, Sprite swordArm, Sprite shieldArm, Sprite slashVfx)
        {
            if (torso != null && torsoBone == null)
            {
                var torsoObj = new GameObject("Torso");
                torsoObj.transform.SetParent(transform, false);
                torsoObj.transform.localPosition = new Vector3(0f, 0f, 0f);
                var sr = torsoObj.AddComponent<SpriteRenderer>();
                sr.sprite = torso;
                sr.sortingLayerName = "Gem";
                sr.sortingOrder = 30;
                torsoBone = torsoObj.transform;
            }

            if (head != null && headBone == null)
            {
                var headObj = new GameObject("Head");
                headObj.transform.SetParent(transform, false);
                headObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                var sr = headObj.AddComponent<SpriteRenderer>();
                sr.sprite = head;
                sr.sortingLayerName = "Gem";
                sr.sortingOrder = 32;
                headBone = headObj.transform;
            }

            if (swordArm != null && swordArmBone == null)
            {
                var swordObj = new GameObject("Arm_Sword");
                swordObj.transform.SetParent(transform, false);
                swordObj.transform.localPosition = new Vector3(0.18f, 0.05f, -0.04f);
                var sr = swordObj.AddComponent<SpriteRenderer>();
                sr.sprite = swordArm;
                sr.sortingLayerName = "Gem";
                sr.sortingOrder = 34;
                swordArmBone = swordObj.transform;
            }

            if (shieldArm != null && shieldArmBone == null)
            {
                var shieldObj = new GameObject("Arm_Shield");
                shieldObj.transform.SetParent(transform, false);
                shieldObj.transform.localPosition = new Vector3(-0.20f, -0.02f, -0.03f);
                var sr = shieldObj.AddComponent<SpriteRenderer>();
                sr.sprite = shieldArm;
                sr.sortingLayerName = "Gem";
                sr.sortingOrder = 33;
                shieldArmBone = shieldObj.transform;
            }

            if (slashVfx != null && slashVfxBone == null)
            {
                var slashObj = new GameObject("VFX_Slash");
                slashObj.transform.SetParent(transform, false);
                // Tempatkan VFX Slash di bagian atas tubuh (North) tempat pedang menebas ke arah monster
                slashObj.transform.localPosition = new Vector3(0.08f, 0.38f, -0.06f);
                slashVfxSr = slashObj.AddComponent<SpriteRenderer>();
                slashVfxSr.sprite = slashVfx;
                slashVfxSr.sortingLayerName = "Gem";
                slashVfxSr.sortingOrder = 36;
                slashVfxSr.color = new Color(1f, 1f, 1f, 0f);
                slashVfxBone = slashObj.transform;
            }

            SetupModularHierarchyIfNeeded();
        }

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

        /// <summary>
        /// Menyalakan atau mematikan seluruh rig potongan tubuh modular (misal saat mode token persiapan vs mode tempur).
        /// </summary>
        public void SetModularRigActive(bool active)
        {
            if (torsoBone != null) torsoBone.gameObject.SetActive(active);
            if (headBone != null) headBone.gameObject.SetActive(active);
            if (swordArmBone != null) swordArmBone.gameObject.SetActive(active);
            if (shieldArmBone != null) shieldArmBone.gameObject.SetActive(active);
            if (slashVfxBone != null) slashVfxBone.gameObject.SetActive(active);

            if (!active)
            {
                StopAllModularRoutines();
            }
            else if (isModularConfigured)
            {
                StartModularIdle();
            }
        }

        /// <summary>
        /// Menyelaraskan sorting order seluruh bagian tubuh modular agar selalu di atas tatakan dan bayangan.
        /// </summary>
        public void UpdateModularSortingOrder(int baseSortOrder)
        {
            if (torsoBone != null)
            {
                var sr = torsoBone.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sortingLayerName = "Gem"; sr.sortingOrder = baseSortOrder; }
            }
            if (shieldArmBone != null)
            {
                var sr = shieldArmBone.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sortingLayerName = "Gem"; sr.sortingOrder = baseSortOrder + 1; }
            }
            if (headBone != null)
            {
                var sr = headBone.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sortingLayerName = "Gem"; sr.sortingOrder = baseSortOrder + 2; }
            }
            if (swordArmBone != null)
            {
                var sr = swordArmBone.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sortingLayerName = "Gem"; sr.sortingOrder = baseSortOrder + 3; }
            }
            if (slashVfxSr != null)
            {
                slashVfxSr.sortingLayerName = "Gem";
                slashVfxSr.sortingOrder = baseSortOrder + 5;
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

        public override void StartIdleAnimation()
        {
            if (isModularConfigured)
            {
                StartModularIdle();
            }
            else
            {
                base.StartIdleAnimation();
            }
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
        /// Override PlayAttack dari HeroVisualAnimator agar memicu animasi tebasan modular jika aktif,
        /// atau mendelegasikan ke PlayAttack berbasis sprite tunggal kanonikal (Strike Frame + Slash Lunge).
        /// </summary>
        public override void PlayAttack(Vector3 targetWorldPos)
        {
            PlayAttack(targetWorldPos, null);
        }

        public override void PlayAttack(Vector3 targetWorldPos, System.Action onStrikeImpact)
        {
            if (isModularConfigured && torsoBone != null && torsoBone.gameObject.activeSelf)
            {
                PlayModularAttack(targetWorldPos, onStrikeImpact);
            }
            else
            {
                base.PlayAttack(targetWorldPos, onStrikeImpact);
            }
        }

        public override void PlayHitRecoil()
        {
            if (isModularConfigured)
            {
                StartCoroutine(ModularHitRecoilRoutine());
            }
            else
            {
                base.PlayHitRecoil();
            }
        }

        private IEnumerator ModularHitRecoilRoutine()
        {
            float dur = 0.12f;
            float elapsed = 0f;
            Vector3 pushOffset = new Vector3(-0.06f, -0.04f, 0f);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float recoil = Mathf.Sin(t * Mathf.PI);

                if (torsoBone != null)
                {
                    torsoBone.localPosition = torsoBasePos + pushOffset * recoil;
                    torsoBone.localRotation = Quaternion.Euler(0f, 0f, -4f * recoil);
                }
                if (shieldArmBone != null)
                {
                    shieldArmBone.localRotation = shieldArmBaseRot * Quaternion.Euler(0f, 0f, 18f * recoil);
                }
                yield return null;
            }

            if (torsoBone != null)
            {
                torsoBone.localPosition = torsoBasePos;
                torsoBone.localRotation = Quaternion.identity;
            }
            if (shieldArmBone != null)
            {
                shieldArmBone.localRotation = shieldArmBaseRot;
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
            // FASE 1: ABA-ABA (Anticipation / Pullback) - 0.08s
            // ==========================================
            float durPhase1 = 0.08f;
            float elapsed = 0f;

            while (elapsed < durPhase1)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durPhase1);
                float easeIn = Mathf.SmoothStep(0f, 1f, t);

                // Tangan pedang ditarik ke belakang mengumpulkan tenaga tebasan
                if (swordArmBone != null)
                {
                    swordArmBone.localRotation = Quaternion.Slerp(swordArmBaseRot, swordArmBaseRot * Quaternion.Euler(0f, 0f, attackPullbackAngle), easeIn);
                }

                // Tubuh merunduk sedikit (Anticipation Squish)
                if (torsoBone != null)
                {
                    torsoBone.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.10f, 0.90f, 1f), easeIn);
                    torsoBone.localPosition = Vector3.Lerp(torsoBasePos, torsoBasePos - new Vector3(0f, 0.03f, 0f), easeIn);
                }

                yield return null;
            }

            // ==========================================
            // FASE 2: AYUNAN TEBASAN (Dynamic Slash Strike) - 0.10s
            // ==========================================
            float durPhase2 = 0.10f;
            elapsed = 0f;

            // Nyalakan Slash VFX Arc yang melengkung ke atas menyongsong monster di utara
            if (slashVfxSr != null)
            {
                slashVfxSr.color = new Color(1f, 1f, 1f, 0.95f);
                if (slashVfxBone != null)
                {
                    // Rotasi -55 derajat memposisikan kepala busur api tebasan tepat di atas/utara (North upward strike)
                    slashVfxBone.localRotation = Quaternion.Euler(0f, 0f, -55f + Random.Range(-8f, 8f));
                    slashVfxBone.localScale = new Vector3(1.40f, 1.40f, 1f);
                }
            }

            while (elapsed < durPhase2)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durPhase2);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out untuk ayunan tebasan kilat yang tajam

                // Lengan mengayun ke atas (Northward slash) secara agresif
                if (swordArmBone != null)
                {
                    Quaternion pullRot = swordArmBaseRot * Quaternion.Euler(0f, 0f, attackPullbackAngle);
                    Quaternion slashRot = swordArmBaseRot * Quaternion.Euler(0f, 0f, attackSlashAngle);
                    swordArmBone.localRotation = Quaternion.Slerp(pullRot, slashRot, easeOut);
                }

                // Tubuh menerjang ke atas (ke arah monster di utara / Y positif) dengan stretch dinamis
                if (torsoBone != null)
                {
                    torsoBone.localPosition = Vector3.Lerp(torsoBasePos - new Vector3(0f, 0.03f, 0f), torsoBasePos + attackLungeOffset, easeOut);
                    torsoBone.localScale = Vector3.Lerp(new Vector3(1.10f, 0.90f, 1f), new Vector3(0.92f, 1.15f, 1f), easeOut);
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
            // FASE 3: ELASTIC SETTLE & RESTORE - 0.16s
            // ==========================================
            float durPhase3 = 0.16f;
            elapsed = 0f;

            while (elapsed < durPhase3)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durPhase3);
                float easeSettle = Mathf.SmoothStep(0f, 1f, t);

                // Fade out Slash VFX halus
                if (slashVfxSr != null)
                {
                    slashVfxSr.color = Color.Lerp(new Color(1f, 1f, 1f, 0.95f), new Color(1f, 1f, 1f, 0f), easeSettle);
                }

                // Lengan dan tubuh kembali membal elastis ke posisi tatakan
                if (swordArmBone != null)
                {
                    Quaternion slashRot = swordArmBaseRot * Quaternion.Euler(0f, 0f, attackSlashAngle);
                    swordArmBone.localRotation = Quaternion.Slerp(slashRot, swordArmBaseRot, easeSettle);
                }

                if (torsoBone != null)
                {
                    float settleBounce = Mathf.Sin(t * Mathf.PI) * 0.04f;
                    torsoBone.localScale = new Vector3(Mathf.Lerp(0.92f, 1f, easeSettle) + settleBounce,
                                                       Mathf.Lerp(1.15f, 1f, easeSettle) - settleBounce,
                                                       1f);
                    torsoBone.localPosition = Vector3.Lerp(torsoBasePos + attackLungeOffset, torsoBasePos, easeSettle);
                }

                if (shieldArmBone != null)
                {
                    shieldArmBone.localRotation = Quaternion.Slerp(shieldArmBone.localRotation, shieldArmBaseRot, easeSettle);
                }

                yield return null;
            }

            // Kembali ke siklus nafas idle
            StartModularIdle();
        }
    }
}
