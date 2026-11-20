using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SlowMoVignetteEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer vignetteRenderer;
    [SerializeField, Range(0.05f, 1f), Tooltip("How slow the time scale is when the vignette is at full alpha.")]
    private float slowMoTimeScale = 0.35f;

    private readonly Dictionary<object, float> alphaBySource = new Dictionary<object, float>();
    private float defaultFixedDeltaTime;

    private void Awake()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        UpdateEffect();
    }

    public bool TrySetAlpha(object source, float alpha)
    {
        if (source == null || vignetteRenderer == null)
        {
            return false;
        }

        alphaBySource[source] = Mathf.Clamp01(alpha);
        UpdateEffect();
        return true;
    }

    public void ClearAlpha(object source)
    {
        if (source != null && alphaBySource.Remove(source))
        {
            UpdateEffect();
        }
    }

    private void UpdateEffect()
    {
        float alpha = 0f;
        foreach (float requestedAlpha in alphaBySource.Values)
        {
            alpha = Mathf.Max(alpha, requestedAlpha);
        }

        if (vignetteRenderer != null)
        {
            Color color = vignetteRenderer.color;
            color.a = alpha;
            vignetteRenderer.color = color;
        }

        ApplyTimeScale(Mathf.Lerp(1f, slowMoTimeScale, alpha));
    }

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * scale;
    }

    private void OnDestroy()
    {
        ApplyTimeScale(1f);
    }
}