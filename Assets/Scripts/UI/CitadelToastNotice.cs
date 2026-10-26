using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master Popup / Toast Notification Ringan untuk Peringatan Game (seperti Energi Habis, Error, dll).
    /// Dapat dipanggil dari mana saja melalui CitadelToastNotice.Show("Pesan peringatan").
    /// </summary>
    public class CitadelToastNotice : MonoBehaviour
    {
        public static CitadelToastNotice Instance { get; private set; }

        private GameObject toastRoot;
        private Text toastText;
        private CanvasGroup toastCanvasGroup;
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildToastUI();
        }

        private void BuildToastUI()
        {
            if (toastRoot != null) return;

            // Canvas independen di atas seluruh panel UI
            var canvasObj = new GameObject("CitadelToastCanvas");
            canvasObj.transform.SetParent(transform, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000; // Selalu berada di puncak UI

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Root Container Toast
            toastRoot = new GameObject("ToastBanner");
            toastRoot.transform.SetParent(canvasObj.transform, false);

            var rect = toastRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.72f);
            rect.anchorMax = new Vector2(0.92f, 0.80f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bgImg = toastRoot.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.12f, 0.20f, 0.95f);

            var outline = toastRoot.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.75f, 0.2f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            toastCanvasGroup = toastRoot.AddComponent<CanvasGroup>();
            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.blocksRaycasts = false;

            // Text Label
            var textObj = new GameObject("ToastMessage");
            textObj.transform.SetParent(toastRoot.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.04f, 0.05f);
            textRect.anchorMax = new Vector2(0.96f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            toastText = textObj.AddComponent<Text>();
            toastText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            toastText.fontSize = 28;
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.fontStyle = FontStyle.Bold;
            toastText.color = Color.white;
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastText.verticalOverflow = VerticalWrapMode.Truncate;

            toastRoot.SetActive(false);
        }

        public static void Show(string message, float duration = 2.8f)
        {
            if (Instance == null)
            {
                var go = new GameObject("CitadelToastNoticeManager");
                Instance = go.AddComponent<CitadelToastNotice>();
            }

            Instance.TriggerToast(message, duration);
        }

        public void TriggerToast(string message, float duration)
        {
            if (toastRoot == null) BuildToastUI();
            if (toastText != null) toastText.text = message;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }
            activeRoutine = StartCoroutine(ToastRoutine(duration));
        }

        private IEnumerator ToastRoutine(float duration)
        {
            toastRoot.SetActive(true);

            // Fade in & pop-up
            float inDuration = 0.22f;
            float elapsed = 0f;
            Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
            Vector3 targetScale = Vector3.one;

            while (elapsed < inDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / inDuration;
                toastCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                toastRoot.transform.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }

            toastCanvasGroup.alpha = 1f;
            toastRoot.transform.localScale = targetScale;

            yield return new WaitForSecondsRealtime(duration);

            // Fade out
            float outDuration = 0.28f;
            elapsed = 0f;
            while (elapsed < outDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / outDuration;
                toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            toastCanvasGroup.alpha = 0f;
            toastRoot.SetActive(false);
            activeRoutine = null;
        }
    }
}
