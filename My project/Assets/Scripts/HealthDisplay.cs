using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the needle's three hearts in the top-left corner.
/// </summary>
[DisallowMultipleComponent]
public class HealthDisplay : MonoBehaviour
{
    const int HeartCount = 3;
    const float HeartSize = 56f;
    const float Margin = 18f;
    const float Gap = 8f;

    [SerializeField] Sprite heartSprite;

    Image[] hearts;
    Sewing sewing;
    bool needleReady;
    bool visible;
    Sprite runtimeSprite;
    Texture2D runtimeTexture;

    void Awake()
    {
        Sprite sprite = PrepareSprite(heartSprite);
        hearts = new Image[HeartCount];

        for (int i = 0; i < HeartCount; i++)
        {
            var heartObject = new GameObject("Heart " + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            heartObject.layer = gameObject.layer;
            heartObject.transform.SetParent(transform, false);

            var rect = heartObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(HeartSize, HeartSize);
            rect.anchoredPosition = new Vector2(Margin + i * (HeartSize + Gap), -Margin);

            var image = heartObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            hearts[i] = image;
        }

        SetVisible(false);
    }

    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
        if (hearts == null)
            return;

        for (int i = 0; i < hearts.Length; i++)
            hearts[i].gameObject.SetActive(visible);
    }

    void Update()
    {
        if (!visible)
            return;

        if (sewing == null)
            sewing = FindFirstObjectByType<Sewing>();
        if (sewing == null || hearts == null)
            return;

        if (!needleReady)
        {
            if (sewing.GetNeedleTransform() == null)
                return;
            needleReady = true;
        }

        int health = sewing.GetNeedleHealth();
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < health ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.4f);
    }

    Sprite PrepareSprite(Sprite source)
    {
        if (source == null)
        {
            Debug.LogWarning("HealthDisplay has no heart sprite assigned.");
            return null;
        }

        Texture2D sourceTexture = source.texture;
        if (sourceTexture == null || !sourceTexture.isReadable)
            return source;

        Rect textureRect = source.textureRect;
        int originX = Mathf.RoundToInt(textureRect.x);
        int originY = Mathf.RoundToInt(textureRect.y);
        int width = Mathf.RoundToInt(textureRect.width);
        int height = Mathf.RoundToInt(textureRect.height);
        Color[] pixels = sourceTexture.GetPixels(originX, originY, width, height);

        bool[] background = new bool[pixels.Length];
        var queue = new Queue<int>();

        void Consider(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            int index = y * width + x;
            if (background[index] || !IsBackdrop(pixels[index]))
                return;

            background[index] = true;
            queue.Enqueue(index);
        }

        for (int x = 0; x < width; x++)
        {
            Consider(x, 0);
            Consider(x, height - 1);
        }
        for (int y = 0; y < height; y++)
        {
            Consider(0, y);
            Consider(width - 1, y);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            Consider(x + 1, y);
            Consider(x - 1, y);
            Consider(x, y + 1);
            Consider(x, y - 1);
        }

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (background[index])
                {
                    pixels[index].a = 0f;
                    continue;
                }

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return source;

        int cropWidth = maxX - minX + 1;
        int cropHeight = maxY - minY + 1;
        var cropped = new Color[cropWidth * cropHeight];
        for (int y = 0; y < cropHeight; y++)
        {
            for (int x = 0; x < cropWidth; x++)
                cropped[y * cropWidth + x] = pixels[(y + minY) * width + (x + minX)];
        }

        runtimeTexture = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false);
        runtimeTexture.name = "Pixel Heart HUD";
        runtimeTexture.filterMode = FilterMode.Point;
        runtimeTexture.wrapMode = TextureWrapMode.Clamp;
        runtimeTexture.SetPixels(cropped);
        runtimeTexture.Apply();

        runtimeSprite = Sprite.Create(runtimeTexture, new Rect(0f, 0f, cropWidth, cropHeight), new Vector2(0.5f, 0.5f), 100f);
        runtimeSprite.name = "Pixel Heart HUD";
        return runtimeSprite;
    }

    static bool IsBackdrop(Color color)
    {
        float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        return min > 0.78f && (max - min) < 0.15f;
    }

    void OnDestroy()
    {
        if (runtimeSprite != null)
            Destroy(runtimeSprite);
        if (runtimeTexture != null)
            Destroy(runtimeTexture);
    }
}
