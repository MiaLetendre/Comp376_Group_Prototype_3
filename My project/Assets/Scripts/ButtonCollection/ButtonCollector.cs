using UnityEngine;

public interface IButtonSpawnInitializer
{
    void Initialize(NeedleManager needleManager, Transform needle);
}

public enum ButtonType
{
    None,
    Regular,
    Shield,
    Dynamite,
    Strike,
    Heart,
    Dodger,
    Attacker
}

public abstract class ButtonCollector : MonoBehaviour
{
    [SerializeField] ButtonType buttonType;
    [SerializeField, Min(0)] int scorePoints = 1;
    [SerializeField, Min(0f), Tooltip("Relative spawn weight; the manager adjusts it based on the current population of this button type.")]
    float spawnWeight = 25f;

    public ButtonType Type => buttonType;
    public int ScorePoints => scorePoints;
    public float SpawnWeight => spawnWeight;

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
