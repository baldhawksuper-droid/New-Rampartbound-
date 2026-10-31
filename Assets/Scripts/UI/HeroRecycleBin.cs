using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Economy;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Zona pembuangan hero (Trash / Recycle Altar) pada HUD pertempuran (Sprint 14 Polish).
    /// Mengizinkan pemain untuk mendrag unit pahlawan yang tidak diinginkan ke zona ini
    /// guna mengosongkan petak arena dan memperoleh pengembalian sebagian Gems/Mana (refund 50%).
    /// </summary>
    public class HeroRecycleBin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static HeroRecycleBin Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image binIconImage;
        [SerializeField] private Image binFrameImage;
        [SerializeField] private Text binLabelText;
        [SerializeField] private Text refundPreviewText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("State")]
        private bool isHovered = false;
        private bool isDragActive = false;
        private Coroutine pulseCoroutine;

        public static event Action<UnitController, int> OnHeroRecycled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            EnsureVisualElementsBuilt();
        }

        private void OnEnable()
        {
            UnitController.OnUnitDragStarted += HandleUnitDragStarted;
            UnitController.OnUnitDragEnded += HandleUnitDragEnded;
            SetDropZoneActive(false);
        }

        private void OnDisable()
        {
            UnitController.OnUnitDragStarted -= HandleUnitDragStarted;
            UnitController.OnUnitDragEnded -= HandleUnitDragEnded;
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        }

        private void EnsureVisualElementsBuilt()
        {
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var frame9Slice = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;
            var altarIconSprite = Resources.Load<Sprite>("Sprites/UI/UI_Altar_Recycle_Hero")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Altar_Recycle_Hero.png")
#endif
                          ;

            if (binFrameImage == null)
            {
                binFrameImage = gameObject.GetComponent<Image>();
                if (binFrameImage == null) binFrameImage = gameObject.AddComponent<Image>();
                binFrameImage.sprite = frame9Slice;
                binFrameImage.type = Image.Type.Sliced;
                binFrameImage.color = new Color(0.18f, 0.08f, 0.10f, 0.88f); // Dark crimson slate
            }

            // Ikon Grafis Altar Peleburan / Tungku Runic Recycle
            if (binIconImage == null)
            {
                var iconObj = new GameObject("AltarIcon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(transform, false);
                var rt = iconObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.62f);
                rt.anchorMax = new Vector2(0.5f, 0.62f);
                rt.sizeDelta = new Vector2(58f, 58f);

                binIconImage = iconObj.GetComponent<Image>();
                binIconImage.sprite = altarIconSprite;
                binIconImage.preserveAspect = true;
                binIconImage.raycastTarget = false;
            }

            if (binLabelText == null)
            {
                var labelObj = new GameObject("RecycleLabel", typeof(RectTransform), typeof(Text));
                labelObj.transform.SetParent(transform, false);
                var rt = labelObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.26f);
                rt.anchorMax = new Vector2(1f, 0.52f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                binLabelText = labelObj.GetComponent<Text>();
                binLabelText.font = lilitaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                binLabelText.fontSize = 15;
                binLabelText.alignment = TextAnchor.MiddleCenter;
                binLabelText.color = new Color(1.0f, 0.45f, 0.45f, 1f);
                binLabelText.text = "RECYCLE";
                binLabelText.raycastTarget = false;
            }

            if (refundPreviewText == null)
            {
                var refundObj = new GameObject("RefundPreview", typeof(RectTransform), typeof(Text));
                refundObj.transform.SetParent(transform, false);
                var rt = refundObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.02f);
                rt.anchorMax = new Vector2(1f, 0.28f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                refundPreviewText = refundObj.GetComponent<Text>();
                refundPreviewText.font = lilitaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                refundPreviewText.fontSize = 13;
                refundPreviewText.alignment = TextAnchor.MiddleCenter;
                refundPreviewText.color = new Color(1.0f, 0.85f, 0.35f, 0.95f);
                refundPreviewText.text = "+50% GEMS";
                refundPreviewText.raycastTarget = false;
            }
        }

        private void HandleUnitDragStarted(UnitController unit)
        {
            // Tempat sampah aktif di Mode 2 (Defense) atau saat pemain menyeret hero tempur
            bool isPuzzlePreparation = GameManager.Instance != null &&
                GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation &&
                GameManager.Instance.CurrentState == BattleState.Preparation;

            if (isPuzzlePreparation)
            {
                // Pada Mode 1 puzzle, token papan tidak dibuang melainkan di-swap match-3
                return;
            }

            if (unit != null)
            {
                isDragActive = true;
                int refundVal = CalculateRefund(unit);
                if (refundPreviewText != null)
                {
                    refundPreviewText.text = $"+{refundVal} 💎";
                }
                SetDropZoneActive(true);
            }
        }

        private void HandleUnitDragEnded(UnitController unit)
        {
            isDragActive = false;
            isHovered = false;
            SetDropZoneActive(false);
        }

        private void SetDropZoneActive(bool active)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 0.95f : 0f;
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }
            else
            {
                gameObject.SetActive(active);
            }

            // Audio respon buka / tutup altar
            if (active)
            {
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.AltarWhooshOpen, 1.1f);
                }

                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(PulseRoutine());
            }
            else
            {
                if (isDragActive && Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.AltarWhooshClose, 0.95f);
                }

                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                transform.localScale = Vector3.one;
            }
        }

        private IEnumerator PulseRoutine()
        {
            while (isDragActive)
            {
                float targetScale = isHovered ? 1.15f : 1.04f;
                float s = 1.0f + Mathf.Sin(Time.unscaledTime * (isHovered ? 8f : 4f)) * (targetScale - 1.0f);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isDragActive) return;
            isHovered = true;

            if (binFrameImage != null)
            {
                binFrameImage.color = new Color(0.45f, 0.12f, 0.15f, 0.98f); // Glowing active crimson
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Light);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isDragActive) return;
            isHovered = false;

            if (binFrameImage != null)
            {
                binFrameImage.color = new Color(0.18f, 0.08f, 0.10f, 0.88f);
            }
        }

        /// <summary>
        /// Menghitung jumlah refund gems dari pembuangan unit berdasarkan tier pahlawan.
        /// </summary>
        public int CalculateRefund(UnitController unit)
        {
            if (unit == null) return 10;
            int baseSummonCost = 20;
            if (EconomyManager.Instance != null)
            {
                baseSummonCost = Mathf.Max(20, EconomyManager.Instance.GetCurrentSummonCost() / 2);
            }

            int multiplier = unit.CurrentTier switch
            {
                UnitTier.Tier1 => 1,
                UnitTier.Tier2 => 2,
                UnitTier.Tier3 => 4,
                UnitTier.Tier4 => 8,
                UnitTier.Tier5 => 16,
                _ => 1
            };

            return (baseSummonCost * multiplier) / 2;
        }

        /// <summary>
        /// Mengecek apakah koordinat layar sentuhan (screen position) berada di dalam area batas Recycle Bin.
        /// </summary>
        public bool ContainsScreenPoint(Vector2 screenPoint, Camera cam = null)
        {
            if (rectTransform == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam);
        }

        /// <summary>
        /// Mengeksekusi daur ulang / penjualan unit pahlawan:
        /// 1. Memberikan refund gems ke pemain via EconomyManager.
        /// 2. Memunculkan teks melayang '+XX GEMS' dan partikel recycle.
        /// 3. Mengosongkan petak TileCell asal.
        /// 4. Menghancurkan GameObject unit dari arena.
        /// </summary>
        public bool TryRecycleHero(UnitController unit)
        {
            if (unit == null) return false;

            int refundGems = CalculateRefund(unit);

            // 1. Tambahkan refund Gems
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGems(refundGems);
            }

            // 2. Efek visual dan audio daur ulang
            Vector3 unitPos = unit.transform.position;
            Vector3 altarTargetPos = transform.position;
            Sprite heroSprite = unit.GetComponent<SpriteRenderer>() != null ? unit.GetComponent<SpriteRenderer>().sprite : null;

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.HeroRecycleChime);
            }

            // Feedback getaran perangkat
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Medium);
            }

            // 3. Kosongkan cell petak
            if (unit.CurrentCell != null)
            {
                unit.CurrentCell.ClearOccupant();
            }

            OnHeroRecycled?.Invoke(unit, refundGems);
            Debug.Log($"[HeroRecycleBin] Unit {unit.HeroType} T{(int)unit.CurrentTier} berhasil didaur ulang. Refund: {refundGems} Gems.");

            // 4. Efek animasi hisap (Suction Vortex) ke kawah altar sebelum GameObject dihancurkan
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(
                    unitPos + Vector3.up * 0.7f,
                    $"♻️ +{refundGems} 💎 REFUND!",
                    new Color(0.35f, 1.0f, 0.55f),
                    1.35f,
                    true
                );

                GameObject heroObjToDestroy = unit.gameObject;
                // Sembunyikan unit asli seketika agar aktor hisap vortex mengambil alih secara mulus
                unit.gameObject.SetActive(false);

                SkillVfxManager.Instance.SpawnRecycleSuctionVortex(unitPos, altarTargetPos, heroSprite, () =>
                {
                    if (heroObjToDestroy != null)
                    {
                        Destroy(heroObjToDestroy);
                    }
                });
            }
            else
            {
                Destroy(unit.gameObject);
            }

            return true;
        }
    }
}
