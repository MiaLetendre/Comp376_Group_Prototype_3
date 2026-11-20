using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent] // Ensures only one effect exists on the needle manager, which is shared by all button collectors.

// Renderer for the collection feedback effect, which shows a burst and text when points are awarded for button collection. 
// The effect is shared by all button collectors on the same needle manager.
public sealed class CollectionFeedbackEffect : MonoBehaviour
{
    private const float FeedbackDuration = 0.55f;
    private const float FeedbackTextHeight = 60f; // Fits a heading and score on separate lines.
    private const float FeedbackTextStackSpacing = 64f; // Slightly exceeds the label height to prevent overlap.
    private const float FeedbackStackDistance = 0.5f; // Nearby anchors share a vertical text stack.

    private sealed class Feedback
    {
        public Vector3 worldPosition;
        public string text;
        public Color textColor;
        public Color burstColor;
        public float age; // Time since the feedback was shown, in seconds.
        public int textStackIndex; // Index of the vertical stack for nearby feedback, to prevent overlapping text.
    }

    private readonly List<Feedback> feedback = new List<Feedback>();
    private GUIStyle textStyle;
    private bool visible;

    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
    }

    public void Show(Vector3 worldPosition, string text)
    {
        Show(worldPosition, text, Color.white, new Color(1f, 0.86f, 0.45f));
    }

    // Show feedback with custom text and colors.
    public void Show(Vector3 worldPosition, string text, Color textColor, Color burstColor)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogError("Collection feedback requires display text.", this);
            return;
        }

        feedback.Add(new Feedback
        {
            worldPosition = worldPosition,
            text = text,
            textColor = textColor,
            burstColor = burstColor,
            age = 0f,
            textStackIndex = GetTextStackIndex(worldPosition)
        });
    }

    private int GetTextStackIndex(Vector3 worldPosition)
    {
        // Separate labels from nearby active effects so rapid feedback does not overlap.
        int stackIndex = 0;
        float stackDistanceSquared = FeedbackStackDistance * FeedbackStackDistance;

        // Check all active feedback to see if any are close enough to the new feedback to require stacking.
        for (int i = 0; i < feedback.Count; i++)
        {
            Feedback activeFeedback = feedback[i];
            if ((activeFeedback.worldPosition - worldPosition).sqrMagnitude <= stackDistanceSquared)
            {
                stackIndex = Mathf.Max(stackIndex, activeFeedback.textStackIndex + 1);
            }
        }

        return stackIndex;
    }

    private void Update()
    {
        if (!visible) return;

        // Update the age of each feedback and remove any that have exceeded the duration.
        for (int i = feedback.Count - 1; i >= 0; i--)
        {
            feedback[i].age += Time.deltaTime;
            if (feedback[i].age >= FeedbackDuration)
            {
                feedback.RemoveAt(i);
            }
        }
    }

    private void OnGUI()
    {
        if (!visible || feedback.Count == 0) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            // Remove skin padding so scaled text has the full label height available.
            textStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        float scale = Mathf.Clamp(Screen.height / 540f, 0.7f, 1.6f);
        textStyle.fontSize = Mathf.RoundToInt(21f * scale);

        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = 5;

        for (int i = 0; i < feedback.Count; i++)
        {
            DrawFeedback(camera, feedback[i], scale);
        }

        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    // Draws a single feedback item, including the burst and text, at the appropriate screen position.
    private void DrawFeedback(Camera camera, Feedback feedbackItem, float scale)
    {
        Vector3 screenPosition = camera.WorldToScreenPoint(feedbackItem.worldPosition);
        if (screenPosition.z <= 0f) return;

        float progress = Mathf.Clamp01(feedbackItem.age / FeedbackDuration);
        float alpha = 1f - progress;
        Vector2 center = new Vector2(screenPosition.x, Screen.height - screenPosition.y);

        DrawBurst(center, feedbackItem.burstColor, progress, alpha, scale);
        DrawFeedbackText(center, feedbackItem, progress, alpha, scale);
    }

    // Draws the animated burst effect behind the feedback text.
    private void DrawBurst(Vector2 center, Color color, float progress, float alpha, float scale)
    {
        color.a *= 0.85f * alpha;
        GUI.color = color;

        float radius = Mathf.Lerp(4f, 28f, progress) * scale;

        // Draw six streaks of the burst
        for (int i = 0; i < 6; i++)
        {
            DrawBurstStreak(center, radius, i * 60f, scale);
        }
    }

    // Draws a single streak of the burst effect at the specified angle and distance from the center.
    private static void DrawBurstStreak(Vector2 center, float radius, float angle, float scale)
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        GUI.DrawTexture(new Rect(center.x + radius, center.y - 1.5f * scale, 12f * scale, 3f * scale), Texture2D.whiteTexture);
        GUI.matrix = previousMatrix;
    }

    // Draws the feedback text above the burst, with vertical stacking to prevent overlap with nearby feedback.
    private void DrawFeedbackText(Vector2 center, Feedback feedbackItem, float progress, float alpha, float scale)
    {
        Color textColor = feedbackItem.textColor;
        textColor.a *= alpha;
        GUI.color = textColor;

        // Calculate the vertical position of the text based on the feedback's age, stack index, and screen boundaries.
        float textRise = (55f + 35f * progress) * scale;
        float textHeight = FeedbackTextHeight * scale;
        float stackOffset = feedbackItem.textStackIndex * FeedbackTextStackSpacing * scale;
        float maxTextY = Mathf.Max(0f, Screen.height - textHeight);
        float baseTextY = center.y - textRise - (textHeight - 32f * scale) * 0.5f;
        baseTextY = Mathf.Clamp(baseTextY, 0f, maxTextY);

        // Stack below the anchor when possible; move upward when the screen edge leaves no room.
        float textY = baseTextY + stackOffset;
        if (textY > maxTextY)
        {
            textY = baseTextY - stackOffset;
        }
        textY = Mathf.Clamp(textY, 0f, maxTextY);

        // Draw the feedback text centered at the calculated position, with a fixed width to accommodate multi-line messages.
        Rect textRect = new Rect(center.x - 60f * scale, textY, 120f * scale, textHeight);
        GUI.Label(textRect, feedbackItem.text, textStyle);
    }
}
