using UnityEngine;

/// <summary>
/// Short starburst flash used when the needle clashes with a wall.
/// </summary>
public class ClashEffect : MonoBehaviour
{
    private const int SparkCount = 6;
    private const float Duration = 0.22f;

    private Transform[] sparks;
    private SpriteRenderer[] renderers;
    private Vector2[] directions;
    private float age;
    private Color color;

    public static void Play(Vector2 position, Color tint)
    {
        GameObject go = new GameObject("ClashEffect");
        go.transform.position = new Vector3(position.x, position.y, -1f);
        ClashEffect effect = go.AddComponent<ClashEffect>();
        effect.Build(tint);
    }

    private void Build(Color tint)
    {
        color = tint;
        sparks = new Transform[SparkCount];
        renderers = new SpriteRenderer[SparkCount];
        directions = new Vector2[SparkCount];

        Sprite sprite = CreateUnitSprite();
        for (int i = 0; i < SparkCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / SparkCount + Random.Range(-0.2f, 0.2f);
            directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject spark = new GameObject("Spark");
            spark.transform.SetParent(transform, false);
            spark.transform.localPosition = Vector3.zero;
            spark.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
            spark.transform.localScale = new Vector3(0.35f, 0.06f, 1f);

            SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 40;

            sparks[i] = spark.transform;
            renderers[i] = sr;
        }
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Duration);
        float expand = Mathf.Lerp(0.05f, 0.55f, t);
        float fade = 1f - t;

        for (int i = 0; i < SparkCount; i++)
        {
            sparks[i].localPosition = directions[i] * expand;
            Color c = color;
            c.a = fade;
            renderers[i].color = c;
            sparks[i].localScale = new Vector3(Mathf.Lerp(0.4f, 0.15f, t), 0.05f, 1f);
        }

        if (age >= Duration)
        {
            Destroy(gameObject);
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
