using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StrikeButtonCollector))]
public class PulseEffect : MonoBehaviour
{
    [Tooltip("How much the object scales up at the peak of the pulse.")]
    [SerializeField, Min(1f)] private float scaleMultiplier = 1.15f;
    [Tooltip("Total duration of the pulse in unscaled seconds.")]
    [SerializeField, Min(0.01f)] private float duration = 0.12f;
    [Tooltip("Fraction of the duration spent increasing in scale (the rest smoothly decays).")]
    [SerializeField, Range(0.05f, 0.5f)] private float popRatio = 0.5f;
    [Tooltip("Brief starting scale multiplier on each slash so rapid hits visibly thump.")]
    [SerializeField, Range(0.8f, 1f)] private float retriggerSquash = 0.94f;
    [Tooltip("Max Z-axis tilt in degrees on each slash (alternates left/right).")]
    [SerializeField, Min(0f)] private float tiltAngle = 5f;

    private StrikeButtonCollector strikeButtonCollector;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 pulseStartScale;
    private Quaternion pulsePeakRotation;
    private float tiltDirection = 1f;
    private float elapsed = -1f;

    private void Awake()
    {
        strikeButtonCollector = GetComponent<StrikeButtonCollector>();
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
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

        // Dont have time be affected by time scale 
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        Vector3 peakScale = originalScale * scaleMultiplier;

        if (progress < popRatio)
        {
            // Fast snap up to peak scale and tilt.
            float popProgress = Mathf.SmoothStep(0f, 1f, progress / popRatio);
            transform.localScale = Vector3.Lerp(pulseStartScale, peakScale, popProgress);
            transform.localRotation = Quaternion.Slerp(originalRotation, pulsePeakRotation, popProgress);
            return;
        }

        // Smooth decay back to resting scale and rotation.
        float decayProgress = Mathf.SmoothStep(0f, 1f, (progress - popRatio) / (1f - popRatio));
        transform.localScale = Vector3.Lerp(peakScale, originalScale, decayProgress);
        transform.localRotation = Quaternion.Slerp(pulsePeakRotation, originalRotation, decayProgress);

        if (progress >= 1f)
        {
            transform.localScale = originalScale;
            transform.localRotation = originalRotation;
            elapsed = -1f;
        }
    }

    private void Play()
    {
        // Snap briefly to a slight squash so rapid slashes always register a fresh pop.
        pulseStartScale = originalScale * retriggerSquash;
        transform.localScale = pulseStartScale;

        // Alternate tilt direction on each hit for tactile rapid-fire feedback.
        tiltDirection = -tiltDirection;
        pulsePeakRotation = originalRotation * Quaternion.Euler(0f, 0f, tiltAngle * tiltDirection);
        elapsed = 0f;
    }
}