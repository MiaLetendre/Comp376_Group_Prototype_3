using UnityEngine;
using UnityEngine.UI;

public interface IButtonSpawnInitializer
{
    void Initialize(Sewing sewing, Transform needle);
}

public abstract class ButtonCollector : MonoBehaviour
{
    [SerializeField] string buttonTypeId;
    [SerializeField] Color buttonColor = Color.white;

    public string ButtonTypeId => buttonTypeId;

    public void Initialize()
    {
        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = buttonColor;
        }
        else
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = buttonColor;
                spriteRenderer.sortingOrder = 6;
            }
        }
    }

    public abstract bool TryCollect(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted,
        float buttonDistanceThreshold);
}
