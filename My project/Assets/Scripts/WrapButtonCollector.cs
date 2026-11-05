using UnityEngine;

[DisallowMultipleComponent]
public class WrapButtonCollector : ButtonCollector
{
    [SerializeField, Min(1)] int wrapsRequired = 1;
    private int currentWraps;

    public override bool TryCollect(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted,
        float buttonDistanceThreshold)
    {
        if (!threadCrossingStarted ||
            Vector2.Distance(currentNeedlePosition, transform.position) >= buttonDistanceThreshold)
        {
            return false;
        }

        currentWraps++;
        return currentWraps >= wrapsRequired;
    }
}
