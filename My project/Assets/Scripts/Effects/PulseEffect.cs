using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StrikeButtonCollector))]
public class PulseEffect : MonoBehaviour
{
    [Tooltip("How much the object scales up during the pulse.")]
    [SerializeField, Min(1f)] private float scaleMultiplier = 1.12f;
    [Tooltip("How long the pulse takes to grow and return to normal, in unscaled seconds.")]
    [SerializeField, Min(0.01f)] private float duration = 0.18f;
    [Tooltip("Animation speed multiplier while the gold button slash window is active.")]
    [SerializeField, Min(1f)] private float goldActiveSpeedMultiplier = 3f;

    private StrikeButtonCollector strikeButtonCollector;
    private GoldButtonCollector goldButtonCollector;
    private Vector3 originalScale;
    private float elapsed = -1f;

    private void Awake()
    {
        strikeButtonCollector = GetComponent<StrikeButtonCollector>();
        goldButtonCollector = strikeButtonCollector as GoldButtonCollector;
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (strikeButtonCollector == null)
        {
            strikeButtonCollector = GetComponent<StrikeButtonCollector>();
            goldButtonCollector = strikeButtonCollector as GoldButtonCollector;
        }

        strikeButtonCollector.OnStrikeCounted += Play;
    }

    private void OnDisable()
    {
        if (strikeButtonCollector != null)
        {
            strikeButtonCollector.OnStrikeCounted -= Play;
        }
    }

    private void Update()
    {
        if (elapsed < 0f) return;

        float speedMultiplier = goldButtonCollector != null && goldButtonCollector.IsSlashWindowActive
            ? goldActiveSpeedMultiplier
            : 1f;

        // Done let pulse be affected by tme scale
        elapsed += Time.unscaledDeltaTime * speedMultiplier;
        float progress = Mathf.Clamp01(elapsed / duration);
        Vector3 peakScale = originalScale * scaleMultiplier;

        // Scale up 
        if (progress < 0.5f)
        {
            float growProgress = Mathf.SmoothStep(0f, 1f, progress * 2f);
            transform.localScale = Vector3.Lerp(originalScale, peakScale, growProgress);
            return;
        }

        // Scale down 
        float shrinkProgress = Mathf.SmoothStep(0f, 1f, (progress - 0.5f) * 2f);
        transform.localScale = Vector3.Lerp(peakScale, originalScale, shrinkProgress);

        // Reset when complete so the next slash can trigger a new pulse.
        if (progress >= 1f)
        {
            transform.localScale = originalScale;
            elapsed = -1f;
        }
    }

    private void Play()
    {
        // Let the current pulse finish before starting a new one.
        if (elapsed >= 0f) return;

        elapsed = 0f;
    }
}