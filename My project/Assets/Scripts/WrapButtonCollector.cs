using UnityEngine;

[DisallowMultipleComponent]
public class WrapButtonCollector : ButtonCollector
{
    [Tooltip("The number of wraps required to collect this button.")]
    [SerializeField, Min(1)] int wrapsRequired = 1;
    private int currentWraps;
    private float accumulatedAngle;
    private bool trackingWrap;

    public override bool TryCollect(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted,
        float buttonDistanceThreshold)
    {
        Vector2 buttonCenter = transform.position;
        Vector2 previousOffset = previousNeedlePosition - buttonCenter;
        Vector2 currentOffset = currentNeedlePosition - buttonCenter;
        if (previousOffset.sqrMagnitude <= Mathf.Epsilon ||
            currentOffset.sqrMagnitude <= Mathf.Epsilon)
        {
            trackingWrap = false;
            accumulatedAngle = 0f;
            return false;
        }

        if (!trackingWrap)
        {
            trackingWrap = true;
            accumulatedAngle = 0f;
        }

        float cross = previousOffset.x * currentOffset.y - previousOffset.y * currentOffset.x;
        float dot = Vector2.Dot(previousOffset, currentOffset);
        accumulatedAngle += Mathf.Atan2(cross, dot) * Mathf.Rad2Deg;

        if (Mathf.Abs(accumulatedAngle) < 360f)
        {
            return false;
        }

        currentWraps++;
        accumulatedAngle -= Mathf.Sign(accumulatedAngle) * 360f;
        return currentWraps >= wrapsRequired;
    }
}
