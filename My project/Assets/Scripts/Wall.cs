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
    [SerializeField] Color clashColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] float clashPunchScale = 1.18f;
    [SerializeField] float clashPunchDuration = 0.12f;

    private NeedleManager needleManager;
    private float lastHitTime = -999f;
    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private float punchTimer;

    public void Initialize(NeedleManager needleManagerRef, Vector2 size)
    {
        needleManager = needleManagerRef;
        baseScale = new Vector3(size.x, size.y, 1f);
        transform.localScale = baseScale;

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
        UpdateClashPunch();

        if (needleManager == null || !needleManager.IsNeedleAlive()) return;

        Transform needle = needleManager.GetNeedleTransform();
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

        needleManager.DamageNeedle(damage, away * knockback);
        PlayClash(needlePos);
    }

    private void PlayClash(Vector2 hitPos)
    {
        ClashEffect.Play(hitPos, clashColor);
        punchTimer = clashPunchDuration;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(wallColor, clashColor, 0.65f);
        }
    }

    private void UpdateClashPunch()
    {
        if (punchTimer <= 0f) return;

        punchTimer -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(punchTimer / clashPunchDuration);
        float punch = Mathf.Sin((1f - t) * Mathf.PI);
        transform.localScale = baseScale * Mathf.Lerp(1f, clashPunchScale, punch);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(clashColor, wallColor, t);
        }

        if (punchTimer <= 0f)
        {
            transform.localScale = baseScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = wallColor;
            }
        }
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
