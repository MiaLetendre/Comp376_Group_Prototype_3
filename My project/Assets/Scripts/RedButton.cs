using UnityEngine;

/// <summary>
/// Red button. Sewn and collected like any other button, but the needle
/// tip overlapping the button body damages the player the same way a wall does.
/// </summary>
[DisallowMultipleComponent]
public class RedButton : MonoBehaviour, IButtonSpawnInitializer
{
    [SerializeField] int damage = 1;
    [SerializeField] float knockback = 1.25f;
    [SerializeField] float damageCooldown = 0.75f;
    [SerializeField] Color clashColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] float clashPunchScale = 1.18f;
    [SerializeField] float clashPunchDuration = 0.12f;

    private NeedleManager needleManager;
    private float lastHitTime = -999f;
    private SpriteRenderer spriteRenderer;
    private Color bodyColor = Color.red;
    private Vector3 baseScale;
    private float punchTimer;

    public void Initialize(NeedleManager needleManagerRef, Transform needle)
    {
        needleManager = needleManagerRef;
        CacheVisuals();
        FitBodyCollider();
    }

    private void Awake()
    {
        CacheVisuals();
        FitBodyCollider();
    }

    private void Start()
    {
        if (needleManager == null)
        {
            needleManager = FindFirstObjectByType<NeedleManager>();
        }
    }

    private void Update()
    {
        UpdateClashPunch();

        if (needleManager == null || !needleManager.IsNeedleAlive()) return;

        Collider2D body = GetComponent<Collider2D>();
        if (body == null) return;

        Vector2 head = needleManager.GetNeedleHeadPosition();
        if (body.OverlapPoint(head))
        {
            TryHitNeedle(head);
        }
    }

    private void TryHitNeedle(Vector2 headPos)
    {
        if (Time.time - lastHitTime < damageCooldown) return;
        lastHitTime = Time.time;

        Vector2 away = headPos - (Vector2)transform.position;
        if (away.sqrMagnitude < 0.001f)
        {
            away = Vector2.up;
        }

        needleManager.DamageNeedle(damage, away.normalized * knockback);
        PlayClash(headPos);
    }

    private void PlayClash(Vector2 hitPos)
    {
        ClashEffect.Play(hitPos, clashColor);
        punchTimer = clashPunchDuration;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(bodyColor, clashColor, 0.65f);
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
            spriteRenderer.color = Color.Lerp(clashColor, bodyColor, t);
        }

        if (punchTimer <= 0f)
        {
            transform.localScale = baseScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = bodyColor;
            }
        }
    }

    private void CacheVisuals()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            bodyColor = spriteRenderer.color;
        }
    }

    private void FitBodyCollider()
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle == null)
        {
            circle = gameObject.AddComponent<CircleCollider2D>();
        }

        circle.isTrigger = true;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector3 extents = spriteRenderer.sprite.bounds.extents;
            circle.radius = Mathf.Min(extents.x, extents.y);
            circle.offset = spriteRenderer.sprite.bounds.center;
        }
    }
}
