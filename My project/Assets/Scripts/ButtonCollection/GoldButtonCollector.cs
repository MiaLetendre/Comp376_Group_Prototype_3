using UnityEngine;

[DisallowMultipleComponent]
public class GoldButtonCollector : StrikeButtonCollector
{
    protected override void OnButtonSpawned()
    {
        base.OnButtonSpawned();
        OnStrikeCounted += IncreaseScoreMultiplier;
    }

    public override void OnCollected(ButtonManager buttonManager)
    {
        buttonManager.ApplyScoreMultiplierToTotal();
    }

    private void OnDestroy()
    {
        OnStrikeCounted -= IncreaseScoreMultiplier;
    }

    private void IncreaseScoreMultiplier()
    {
        if (NeedleManager != null)
        {
            NeedleManager.IncreaseScoreMultiplier();
        }
    }
}
