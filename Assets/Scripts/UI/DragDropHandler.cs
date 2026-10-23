using UnityEngine;
using UnityEngine.EventSystems;
using FantasyDefense.Combat;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Menangani interaksi seret-lepas UI (Drag & Drop Handler) untuk layar sentuh ponsel dan tablet (Mode 2).
    /// Mendeteksi pointer events dan mengoordinasikan visual drag feedback dengan UnitController.
    /// </summary>
    public class DragDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Drag Configuration")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;

        private Vector2 originalPosition;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (rectTransform != null)
                originalPosition = rectTransform.anchoredPosition;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.75f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rectTransform != null && canvas != null)
            {
                rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }

            // Kembalikan ke posisi awal jika tidak di-drop ke target yang valid
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
        }
    }
}
