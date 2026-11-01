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
        private Button actionButton;
        private Text actionButtonText;
        private CanvasGroup toastCanvasGroup;
        private Coroutine activeRoutine;
        private System.Action onActionCallback;

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
            rect.anchorMin = new Vector2(0.06f, 0.70f);
            rect.anchorMax = new Vector2(0.94f, 0.81f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bgImg = toastRoot.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.12f, 0.20f, 0.96f);

            var outline = toastRoot.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.75f, 0.2f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            toastCanvasGroup = toastRoot.AddComponent<CanvasGroup>();
            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.blocksRaycasts = true;

            // Text Label
            var textObj = new GameObject("ToastMessage");
            textObj.transform.SetParent(toastRoot.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.04f, 0.36f);
            textRect.anchorMax = new Vector2(0.96f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            toastText = textObj.AddComponent<Text>();
            toastText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            toastText.fontSize = 24;
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.fontStyle = FontStyle.Bold;
            toastText.color = Color.white;
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastText.verticalOverflow = VerticalWrapMode.Truncate;

            // Action Button Container (Pintasan Cepat ke Toko)
            var btnObj = new GameObject("ToastActionButton");
            btnObj.transform.SetParent(toastRoot.transform, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.20f, 0.06f);
            btnRect.anchorMax = new Vector2(0.80f, 0.34f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            var btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.92f, 0.65f, 0.15f, 1f);

            actionButton = btnObj.AddComponent<Button>();
            actionButton.targetGraphic = btnBg;
            actionButton.onClick.AddListener(OnActionButtonClicked);

            var btnTxtObj = new GameObject("BtnText");
            btnTxtObj.transform.SetParent(btnObj.transform, false);

            var btnTxtRect = btnTxtObj.AddComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;
            btnTxtRect.offsetMin = Vector2.zero;
            btnTxtRect.offsetMax = Vector2.zero;

            actionButtonText = btnTxtObj.AddComponent<Text>();
            actionButtonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            actionButtonText.fontSize = 18;
            actionButtonText.alignment = TextAnchor.MiddleCenter;
            actionButtonText.fontStyle = FontStyle.Bold;
            actionButtonText.color = new Color(0.12f, 0.08f, 0.02f);
            actionButtonText.text = "💎 BUKA TOKO (REFILL)";

            btnObj.SetActive(false);
            toastRoot.SetActive(false);
        }

        public static void Show(string message, float duration = 2.8f)
        {
            ShowWithAction(message, null, null, duration);
        }

        public static void ShowWithAction(string message, string actionLabel, System.Action action, float duration = 4.0f)
        {
            if (Instance == null)
            {
                var go = new GameObject("CitadelToastNoticeManager");
                Instance = go.AddComponent<CitadelToastNotice>();
            }

            Instance.TriggerToast(message, actionLabel, action, duration);
        }

        public void TriggerToast(string message, string actionLabel, System.Action action, float duration)
        {
            if (toastRoot == null) BuildToastUI();
            if (toastText != null) toastText.text = message;

            onActionCallback = action;
            if (actionButton != null)
            {
                bool hasAction = !string.IsNullOrEmpty(actionLabel) && action != null;
                actionButton.gameObject.SetActive(hasAction);
                if (hasAction && actionButtonText != null)
                {
                    actionButtonText.text = actionLabel;
                }
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }
            activeRoutine = StartCoroutine(ToastRoutine(duration));
        }

        private void OnActionButtonClicked()
        {
            var cb = onActionCallback;
            HideImmediate();
            cb?.Invoke();
        }

        private void HideImmediate()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
            if (toastRoot != null) toastRoot.SetActive(false);
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

    /// <summary>
    /// Alias manager untuk memudahkan pemanggilan toast UI di seluruh proyek.
    /// </summary>
    public class UIToastManager : MonoBehaviour
    {
        private static UIToastManager _instance;
        public static UIToastManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UIToastManager");
                    _instance = go.AddComponent<UIToastManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public void ShowToast(string message)
        {
            CitadelToastNotice.Show(message);
        }
    }
}
