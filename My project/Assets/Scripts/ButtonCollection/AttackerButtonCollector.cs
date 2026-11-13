using UnityEngine;

[DisallowMultipleComponent]
public class AttackerButtonCollector : WrapButtonCollector
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockback = 1.25f;
    [SerializeField] private float damageCooldown = 0.75f;
    [SerializeField] private Color clashColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private float clashPunchScale = 1.18f;
    [SerializeField] private float clashPunchDuration = 0.12f;

    private float lastHitTime = -999f;
    private SpriteRenderer spriteRenderer;
    private Color bodyColor = Color.red;
    private Vector3 baseScale;
    private float punchTimer;
    private Collider2D bodyCollider;

    protected override void OnButtonSpawned()
    {
        CacheVisuals();
        FitBodyCollider();
    }

    protected override void OnButtonUpdate()
    {
        UpdateClashPunch();

        if (NeedleManager == null || !NeedleManager.IsNeedleAlive() || bodyCollider == null)
        {
            return;
        }

        Vector2 head = NeedleManager.GetNeedleHeadPosition();
        if (bodyCollider.OverlapPoint(head))
        {
            TryHitNeedle(head);
        }
    }

    private void TryHitNeedle(Vector2 headPosition)
    {
        if (Time.time - lastHitTime < damageCooldown) return;
        lastHitTime = Time.time;

        Vector2 away = headPosition - (Vector2)transform.position;
        if (away.sqrMagnitude < 0.001f)
        {
            away = Vector2.up;
        }

        NeedleManager.DamageNeedle(damage, away.normalized * knockback);
        PlayClash(headPosition);
    }

    private void PlayClash(Vector2 hitPosition)
    {
        ClashEffect.Play(hitPosition, clashColor);
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
        float progress = 1f - Mathf.Clamp01(punchTimer / clashPunchDuration);
        float punch = Mathf.Sin((1f - progress) * Mathf.PI);
        transform.localScale = baseScale * Mathf.Lerp(1f, clashPunchScale, punch);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(clashColor, bodyColor, progress);
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

        bodyCollider = circle;
    }
}
