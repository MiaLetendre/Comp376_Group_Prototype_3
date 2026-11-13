using System;
using UnityEngine;

[DisallowMultipleComponent]
public class StrikeButtonCollector : ButtonCollector
{
    [Tooltip("The number of strikes required to collect this button.")]
    [SerializeField, Min(1)] int strikesRequired = 1;
    [Tooltip("The fraction of the button's diameter that the needle must cover to count as a strike.")]
    [SerializeField, Range(0f, 1f)] float requiredBodyCoverage = 0.9f;
    private CircleCollider2D buttonCollider;
    private int currentStrikes;
    private float currentStrikePathLength;
    private bool strikeCountedThisPass;
    public event Action OnStrikeCounted;

    protected virtual void Awake()
    {
        buttonCollider = GetComponent<CircleCollider2D>();
        if (buttonCollider == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Vector3 extents = spriteRenderer.sprite.bounds.extents;
                circle.radius = Mathf.Min(extents.x, extents.y);
                circle.offset = spriteRenderer.sprite.bounds.center;
            }

            buttonCollider = circle;
        }
    }

    public override bool TryCollect(Vector2 previousNeedlePosition, Vector2 currentNeedlePosition, bool threadCrossingStarted, float buttonDistanceThreshold)
    {
        Vector2 movement = currentNeedlePosition - previousNeedlePosition;
        float distance = movement.magnitude;
        if (buttonCollider == null || distance <= Mathf.Epsilon)
        {
            return false;
        }
        // Calculate the direction of the needle's movement and the button's center and radius
        Vector2 direction = movement / distance;
        Vector2 center = buttonCollider.transform.TransformPoint(buttonCollider.offset);
        Vector3 scale = buttonCollider.transform.lossyScale;
        float radius = buttonCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Vector2 fromCenter = previousNeedlePosition - center;
        float distanceFromPathToCenter = Mathf.Abs(fromCenter.x * direction.y - fromCenter.y * direction.x);

        // If the distance from the path to the center is greater than the radius, the needle does not intersect the button
        if (distanceFromPathToCenter >= radius)
        {
            return false;
        }

        // Calculate the length of the path that is inside the button's circle
        float halfChordLength = Mathf.Sqrt(radius * radius - distanceFromPathToCenter * distanceFromPathToCenter);
        float pathStart = Vector2.Dot(fromCenter, direction);
        float pathEnd = pathStart + distance;

        // The length of the path inside the button is the overlap of the path segment and the chord segment
        float insideLength = Mathf.Max(0f, Mathf.Min(pathEnd, halfChordLength) - Mathf.Max(pathStart, -halfChordLength));
        bool needleInsideButton = (currentNeedlePosition - center).sqrMagnitude <= radius * radius;
        if (!strikeCountedThisPass)
        {
            currentStrikePathLength += insideLength;
            // If the path length inside the button exceeds the required coverage, count a strike
            if (currentStrikePathLength >= radius * 2f * requiredBodyCoverage)
            {
                currentStrikes++;
                strikeCountedThisPass = true;
                OnStrikeCounted?.Invoke();
                if (currentStrikes >= strikesRequired)
                {
                    return true;
                }
            }
        }

        if (!needleInsideButton)
        {
            currentStrikePathLength = 0f;
            strikeCountedThisPass = false;
        }

        return false;
    }
}
