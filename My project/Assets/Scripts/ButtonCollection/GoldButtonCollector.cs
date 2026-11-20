using UnityEngine;

[DisallowMultipleComponent]
public class GoldButtonCollector : StrikeButtonCollector
{
    [SerializeField, Min(0), Tooltip("Points awarded for each completed slash.")]
    private int pointsPerSlash = 25;

    protected override void OnButtonSpawned()
    {
        base.OnButtonSpawned();
        // Award points only when a complete slash is counted by the base collector.
        OnStrikeCounted += AwardSlashPoints;
    }

    private void OnDestroy()
    {
        OnStrikeCounted -= AwardSlashPoints;
    }

    private void AwardSlashPoints()
    {
        if (NeedleManager == null)
        {
            Debug.LogError("Cannot award gold-button slash points because NeedleManager is missing.", this);
            return;
        }

        // Gold slashes show only their points; the custom message is reserved for collection.
        NeedleManager.AddScorePoints(pointsPerSlash);
        NeedleManager.ShowScoreFeedback(this, pointsPerSlash);
    }
}
