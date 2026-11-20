using UnityEngine;

// Controls the screen vignette opacity and synchronizes slow-motion time scaling.
[DisallowMultipleComponent]
public class SlowMoVignetteEffect : MonoBehaviour
{
    [SerializeField, Tooltip("The SpriteRenderer used to display the screen vignette.")]
    private SpriteRenderer vignetteRenderer;

    [SerializeField, Range(0.05f, 1f), Tooltip("Target Time.timeScale when the vignette is at full alpha.")]
    private float slowMoTimeScale = 0.35f;
    private float defaultFixedDeltaTime;

    private void Awake()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        Clear();
    }

    // Sets the vignette alpha and scales game time to match.
    public bool TrySetAlpha(float alpha)
    {
        if (vignetteRenderer == null)
        {
            return false;
        }

        float clampedAlpha = Mathf.Clamp01(alpha);

        Color color = vignetteRenderer.color;
        color.a = clampedAlpha;
        vignetteRenderer.color = color;

        ApplyTimeScale(Mathf.Lerp(1f, slowMoTimeScale, clampedAlpha));
        return true;
    }

    // Resets the vignette to transparent and restores normal game speed.
    public void Clear()
    {
        if (vignetteRenderer != null)
        {
            Color color = vignetteRenderer.color;
            color.a = 0f;
            vignetteRenderer.color = color;
        }

        ApplyTimeScale(1f);
    }

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * scale;
    }

    private void OnDestroy()
    {
        // Always restore normal time scale when this object is destroyed or the scene reloads.
        ApplyTimeScale(1f);
    }
}