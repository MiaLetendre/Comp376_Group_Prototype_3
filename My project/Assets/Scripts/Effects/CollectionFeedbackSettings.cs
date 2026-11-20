using UnityEngine;

[DisallowMultipleComponent] // Ensures only one effect exists on the needle manager, which is shared by all button collectors.
public sealed class CollectionFeedbackSettings : MonoBehaviour
{
    [SerializeField, TextArea, Tooltip("Optional text shown above awarded points. Leave empty for +points or NICE!.")]
    private string message;
    [SerializeField, Tooltip("Color of the feedback text.")]
    private Color textColor = Color.white;
    [SerializeField, Tooltip("Color of the animated burst.")]
    private Color burstColor = new Color(1f, 0.86f, 0.45f);
    [SerializeField, Tooltip("Show the feedback over this button instead of at the needle.")]
    private bool showAtButtonPosition;

    // Collection feedback uses custom text as a heading, with awarded points on the next line.
    public void Show(CollectionFeedbackEffect effect, Vector3 defaultPosition, int pointsAwarded)
    {
        string displayText;
        if (string.IsNullOrWhiteSpace(message))
        {
            displayText = pointsAwarded > 0 ? "+" + pointsAwarded : "NICE!";
        }
        else
        {
            string heading = message.Replace("+{points}", "").Replace("{points}", "").Trim();
            if (string.IsNullOrWhiteSpace(heading))
            {
                displayText = pointsAwarded > 0 ? "+" + pointsAwarded : "NICE!";
            }
            else
            {
                displayText = pointsAwarded > 0
                    ? heading + "\n+" + pointsAwarded
                    : heading;
            }
        }

        Show(effect, defaultPosition, displayText);
    }

    public void ShowScoreOnly(CollectionFeedbackEffect effect, Vector3 defaultPosition, int pointsAwarded)
    {
        // Used for gold slashes so their per-slash feedback contains no collection message.
        Show(effect, defaultPosition, "+" + pointsAwarded);
    }

    public void Show(CollectionFeedbackEffect effect, Vector3 defaultPosition, string displayText)
    {
        if (effect == null)
        {
            Debug.LogError("CollectionFeedbackEffect is required to show button feedback.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(displayText))
        {
            Debug.LogError("Button feedback requires display text.", this);
            return;
        }

        Vector3 position = showAtButtonPosition ? transform.position : defaultPosition;
        effect.Show(position, displayText, textColor, burstColor);
    }
}
