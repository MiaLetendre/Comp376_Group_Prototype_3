using UnityEngine;

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

[DisallowMultipleComponent]
public abstract class ButtonCollector : MonoBehaviour
{
    [SerializeField] ButtonType buttonType;
    [SerializeField, Min(0)] int scorePoints = 1;
    [SerializeField, Min(0f), Tooltip("Relative spawn weight; the manager adjusts it based on the current population of this button type.")]
    float spawnWeight = 25f;

    protected NeedleManager NeedleManager { get; private set; }
    protected Transform Needle { get; private set; }

    public ButtonType Type => buttonType;
    public int ScorePoints => scorePoints;
    public float SpawnWeight => spawnWeight;
    public virtual bool CountsAsCollected => true;

    public void Initialize(NeedleManager needleManager, Transform needle)
    {
        NeedleManager = needleManager;
        Needle = needle;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 6;
        }

        OnButtonSpawned();
    }

    protected virtual void Update()
    {
        OnButtonUpdate();
    }

    protected virtual void OnButtonSpawned() { }
    protected virtual void OnButtonUpdate() { }
    public virtual void OnCollected(ButtonManager buttonManager) { }

    public abstract bool TryCollect(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted,
        float buttonDistanceThreshold);
}
