[UnityEngine.DisallowMultipleComponent]
public class HeartButtonCollector : StrikeButtonCollector
{
    public override void OnCollected(ButtonManager buttonManager)
    {
        if (NeedleManager != null)
        {
            NeedleManager.RestoreNeedleHealth(1);
        }
    }
}
