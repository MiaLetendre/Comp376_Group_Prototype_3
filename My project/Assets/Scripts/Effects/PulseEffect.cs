using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StrikeButtonCollector))]
public class PulseEffect : MonoBehaviour
{
    [Tooltip("How much the object scales up during the pulse.")]
    [SerializeField, Min(1f)] private float scaleMultiplier = 1.12f;
    [Tooltip("How long the pulse takes to grow and return to normal.")]
    [SerializeField, Min(0.01f)] private float duration = 0.18f;

    private StrikeButtonCollector strikeButtonCollector;
    private Vector3 originalScale;
    private Vector3 pulseStartScale;
    private float elapsed = -1f;

    private void Awake()
    {
        strikeButtonCollector = GetComponent<StrikeButtonCollector>();
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (strikeButtonCollector == null)
        {
            strikeButtonCollector = GetComponent<StrikeButtonCollector>();
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

        // Update the pulse effect over time
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        Vector3 peakScale = originalScale * scaleMultiplier;

        // If the pulse is in the first half of its duration, scale up
        if (progress < 0.5f)
        {
            float growProgress = Mathf.SmoothStep(0f, 1f, progress * 2f);
            transform.localScale = Vector3.Lerp(pulseStartScale, peakScale, growProgress);
            return;
        }

        // If the pulse is in the second half of its duration, scale back down 
        float shrinkProgress = Mathf.SmoothStep(0f, 1f, (progress - 0.5f) * 2f);
        transform.localScale = Vector3.Lerp(peakScale, originalScale, shrinkProgress);

        // Reset the pulse effect when complete
        if (progress >= 1f)
        {
            transform.localScale = originalScale;
            elapsed = -1f;
        }
    }

    private void Play()
    {
        pulseStartScale = transform.localScale;
        elapsed = 0f;
    }
}
