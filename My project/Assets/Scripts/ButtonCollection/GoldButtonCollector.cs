using UnityEngine;

[DisallowMultipleComponent]
public class GoldButtonCollector : StrikeButtonCollector
{
    [SerializeField, Min(0), Tooltip("Points awarded for each completed slash.")]
    private int pointsPerSlash = 25;
    [SerializeField, Min(0f), Tooltip("How long the button can be slashed after the first slash.")]
    private float slashWindowDuration = 5f;
    [SerializeField, Min(0f), Tooltip("How long the vignette takes to fade in.")]
    private float vignetteFadeInDuration = 0.5f;
    [SerializeField, Min(0f), Tooltip("How long the vignette takes to fade out before collection.")]
    private float vignetteFadeOutDuration = 0.5f;

    private SlowMoVignetteEffect effectController;
    private bool slashWindowStarted;
    private bool collectionReady;
    private bool hasVignetteRenderer;
    private float slashWindowStartTime;

    // Override the usual strike count so this button remains active until its timed window fades out.
    protected override bool IsCollectionReady => collectionReady;

    public void SetEffectController(SlowMoVignetteEffect controller)
    {
        effectController = controller;
    }

    protected override void OnButtonSpawned()
    {
        base.OnButtonSpawned();
        // Award points only when a complete slash is counted by the base collector.
        OnStrikeCounted += AwardSlashPoints;
    }

    protected override void OnButtonUpdate()
    {
        base.OnButtonUpdate();
        if (!slashWindowStarted || collectionReady)
        {
            return;
        }

        float elapsed = Time.unscaledTime - slashWindowStartTime;
        if (elapsed < slashWindowDuration)
        {
            SetVignetteAlpha(GetFadeInAlpha(elapsed));
            return;
        }

        float fadeOutStartAlpha = GetFadeInAlpha(slashWindowDuration);
        float fadeOutElapsed = elapsed - slashWindowDuration;
        float fadeOutProgress = vignetteFadeOutDuration <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(fadeOutElapsed / vignetteFadeOutDuration);

        SetVignetteAlpha(fadeOutStartAlpha * (1f - fadeOutProgress));
        collectionReady = fadeOutProgress >= 1f;
    }

    public override bool TryCollect(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted,
        float buttonDistanceThreshold)
    {
        // Stop counting or rewarding slashes while the vignette is fading out.
        if (slashWindowStarted &&
            !collectionReady &&
            Time.unscaledTime - slashWindowStartTime >= slashWindowDuration)
        {
            return false;
        }

        return base.TryCollect(
            previousNeedlePosition,
            currentNeedlePosition,
            threadCrossingStarted,
            buttonDistanceThreshold);
    }

    private void OnDestroy()
    {
        OnStrikeCounted -= AwardSlashPoints;
        if (effectController != null && slashWindowStarted)
        {
            effectController.Clear();
        }
    }

    private void StartSlashWindow()
    {
        slashWindowStarted = true;
        slashWindowStartTime = Time.unscaledTime;
        hasVignetteRenderer = effectController != null && effectController.TrySetAlpha(0f);
        if (!hasVignetteRenderer)
        {
            Debug.LogError("Assign a SlowMoVignetteEffect with a vignette SpriteRenderer to enable the gold-button effect.", this);
        }
    }

    private float GetFadeInAlpha(float elapsed)
    {
        return vignetteFadeInDuration <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(elapsed / vignetteFadeInDuration);
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (hasVignetteRenderer && effectController != null)
        {
            effectController.TrySetAlpha(alpha);
        }
    }

    private void AwardSlashPoints()
    {
        if (NeedleManager == null)
        {
            Debug.LogError("Cannot award gold-button slash points because NeedleManager is missing.", this);
            return;
        }

        if (!slashWindowStarted)
        {
            StartSlashWindow();
        }
        else if (Time.unscaledTime - slashWindowStartTime >= slashWindowDuration)
        {
            return;
        }

        // Gold slashes show only their points; the custom message is reserved for collection.
        NeedleManager.AddScorePoints(pointsPerSlash);
        NeedleManager.ShowScoreFeedback(this, pointsPerSlash);
    }
}