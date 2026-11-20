using UnityEngine;

/// <summary>
/// COMP376 Iteration 1 (KD): current score and session best score.
/// Shows and hides the shared collection-feedback renderer with the score HUD.
/// Auto-installs without modifying scenes, prefabs, or teammates' scripts.
/// </summary>
[DisallowMultipleComponent]
public sealed class KDScoreAndCollectionHUD : MonoBehaviour
{
    private static int sessionBestScore;

    private NeedleManager needleManager;
    private CollectionFeedbackEffect collectionFeedbackEffect;
    private GUIStyle scoreStyle;
    private GUIStyle bestStyle;
    private bool visible;

    public int CurrentScore => needleManager != null ? needleManager.Score : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionBestScore()
    {
        sessionBestScore = 0;
    }

    // One HUD per needle manager. A scene restart creates a fresh HUD.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        NeedleManager controller = Object.FindFirstObjectByType<NeedleManager>();
        if (controller == null || controller.GetComponent<KDScoreAndCollectionHUD>() != null)
            return;

        controller.gameObject.AddComponent<KDScoreAndCollectionHUD>();
    }

    private void Awake()
    {
        needleManager = GetComponent<NeedleManager>();
        if (needleManager == null)
        {
            enabled = false;
            return;
        }

        collectionFeedbackEffect = GetComponent<CollectionFeedbackEffect>();
        if (collectionFeedbackEffect == null)
        {
            collectionFeedbackEffect = gameObject.AddComponent<CollectionFeedbackEffect>();
        }

        visible = false;
        collectionFeedbackEffect.SetVisible(visible);
    }

    public void SetVisible(bool isVisible)
    {
        // Keep feedback effect visible only while the score HUD is active.
        visible = isVisible;
        if (collectionFeedbackEffect != null)
        {
            collectionFeedbackEffect.SetVisible(isVisible);
        }
    }

    public int GetBestScore()
    {
        sessionBestScore = Mathf.Max(sessionBestScore, CurrentScore);
        return sessionBestScore;
    }

    private void LateUpdate()
    {
        if (needleManager == null || !visible) return;

        GetBestScore();
    }

    private void OnGUI()
    {
        if (needleManager == null || !visible) return;

        if (scoreStyle == null) CreateStyles();

        float scale = Mathf.Clamp(Screen.height / 540f, 0.7f, 1.6f);
        scoreStyle.fontSize = Mathf.RoundToInt(27f * scale);
        bestStyle.fontSize = Mathf.RoundToInt(15f * scale);

        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = 5; // Draw behind the existing tutorial/pause overlays.

        float panelWidth = 230f * scale;
        float panelHeight = 70f * scale;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = 9f * scale;

        GUI.color = new Color(0.07f, 0.08f, 0.14f, 0.7f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(panelX, panelY + 3f * scale, panelWidth, 36f * scale),
            "SCORE  " + needleManager.Score, scoreStyle);
        GUI.Label(new Rect(panelX, panelY + 37f * scale, panelWidth, 24f * scale),
            "BEST  " + sessionBestScore, bestStyle);

        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private void CreateStyles()
    {
        scoreStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        scoreStyle.normal.textColor = Color.white;

        bestStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter
        };
        bestStyle.normal.textColor = new Color(0.88f, 0.82f, 1f);
    }

    private void OnEnable()
    {
        if (collectionFeedbackEffect != null)
        {
            collectionFeedbackEffect.SetVisible(visible);
        }
    }

    private void OnDisable()
    {
        if (collectionFeedbackEffect != null)
        {
            collectionFeedbackEffect.SetVisible(false);
        }
    }
}
