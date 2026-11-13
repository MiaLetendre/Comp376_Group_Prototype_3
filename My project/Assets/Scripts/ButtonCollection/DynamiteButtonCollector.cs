using UnityEngine;

[DisallowMultipleComponent]
public class DynamiteButtonCollector : WrapButtonCollector
{
    public override bool CountsAsCollected => false;

    public override void OnCollected(ButtonManager buttonManager)
    {
        if (NeedleManager != null)
        {
            NeedleManager.DamageNeedle(1, Vector2.zero);
        }

        buttonManager.NotifyDynamiteCaptured(transform.position);
    }
}
