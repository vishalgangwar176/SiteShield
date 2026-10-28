using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Runtime
{
    public sealed class NpcSpeechBubbleView : MonoBehaviour
    {
        [SerializeField] TMP_Text reply;
        [SerializeField] TMP_Text pageIndicator;
        [SerializeField] CanvasGroup canvasGroup;

        Coroutine transition;
        Vector3 restingScale;

        public void Configure(TMP_Text replyText, TMP_Text pageText, CanvasGroup group)
        {
            reply = replyText;
            pageIndicator = pageText;
            canvasGroup = group;
            restingScale = transform.localScale;
        }

        public void Show(string message, int page, int pageCount)
        {
            if (reply != null)
                reply.text = message;
            if (pageIndicator != null)
            {
                pageIndicator.text = pageCount > 1 ? $"{page + 1} / {pageCount}" : string.Empty;
                pageIndicator.gameObject.SetActive(pageCount > 1);
            }

            if (restingScale == Vector3.zero)
                restingScale = transform.localScale;
            gameObject.SetActive(true);
            if (!Application.isPlaying || canvasGroup == null)
            {
                transform.localScale = restingScale;
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f;
                return;
            }

            if (transition != null)
                StopCoroutine(transition);
            transition = StartCoroutine(AnimateVisibility(true));
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
                return;
            if (!Application.isPlaying || canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (transition != null)
                StopCoroutine(transition);
            transition = StartCoroutine(AnimateVisibility(false));
        }

        IEnumerator AnimateVisibility(bool visible)
        {
            const float duration = 0.16f;
            var startAlpha = canvasGroup.alpha;
            var endAlpha = visible ? 1f : 0f;
            var startScale = transform.localScale;
            var endScale = visible ? restingScale : restingScale * 0.94f;
            if (visible && startAlpha <= 0.01f)
                startScale = restingScale * 0.94f;

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var progress = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - progress, 3f);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                transform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            transform.localScale = endScale;
            transition = null;
            if (!visible)
                gameObject.SetActive(false);
        }
    }
}
