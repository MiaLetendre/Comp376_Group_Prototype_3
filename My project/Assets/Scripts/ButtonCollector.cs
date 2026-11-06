using UnityEngine;

public interface IButtonSpawnInitializer
{
    void Initialize(Sewing sewing, Transform needle);
}

public abstract class ButtonCollector : MonoBehaviour
{
    [SerializeField] string buttonTypeId;
    [SerializeField, Min(0)] int scorePoints = 1;

    public string ButtonTypeId => buttonTypeId;
    public int ScorePoints => scorePoints;

    public void Initialize()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 6;
        }
    }

    public abstract bool TryCollect(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted,
        float buttonDistanceThreshold);
}
