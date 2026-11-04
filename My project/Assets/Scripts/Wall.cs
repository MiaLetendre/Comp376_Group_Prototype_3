using UnityEngine;

/// <summary>
/// Scrolling fabric wall / obstacle. Damages the needle on contact
/// and briefly knocks it (and the sewing point) away.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class Wall : MonoBehaviour
{
    [SerializeField] int damage = 1;
    [SerializeField] float knockback = 1.25f;
    [SerializeField] float damageCooldown = 0.75f;
    [SerializeField] Color wallColor = new Color(0.45f, 0.28f, 0.18f, 1f);

    private Sewing sewing;
    private float lastHitTime = -999f;
    private SpriteRenderer spriteRenderer;

    public void Initialize(Sewing sewingRef, Vector2 size)
    {
        sewing = sewingRef;
        transform.localScale = new Vector3(size.x, size.y, 1f);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateUnitSprite();
        }
        spriteRenderer.color = wallColor;
        spriteRenderer.sortingOrder = 2;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = Vector2.one;
    }

    private void Update()
    {
        if (sewing == null || !sewing.IsNeedleAlive()) return;

        Transform needle = sewing.GetNeedleTransform();
        if (needle == null) return;

        Collider2D wallCollider = GetComponent<Collider2D>();
        if (wallCollider == null) return;

        if (wallCollider.OverlapPoint(needle.position))
        {
            TryHitNeedle(needle.position);
        }
    }

    private void TryHitNeedle(Vector2 needlePos)
    {
        if (Time.time - lastHitTime < damageCooldown) return;
        lastHitTime = Time.time;

        Vector2 away = ((Vector2)needlePos - (Vector2)transform.position).normalized;
        if (away.sqrMagnitude < 0.001f)
        {
            away = Vector2.up;
        }

        sewing.DamageNeedle(damage, away * knockback);
    }

    private static Sprite CreateUnitSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
