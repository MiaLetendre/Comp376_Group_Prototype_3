using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// COMP376 Iteration 1 (KD): current score, persistent best score, and a small
/// animated feedback burst when the needle system collects a button.
/// Auto-installs without modifying scenes, prefabs, or teammates' scripts.
/// </summary>
[DisallowMultipleComponent]
public sealed class KDScoreAndCollectionHUD : MonoBehaviour
{
    private const string BestScoreKey = "COMP376_Sewing_KD_BestScore_v1";
    private const float FeedbackDuration = 0.55f;

    private sealed class Feedback
    {
        public Vector3 worldPosition;
        public int points;
        public bool isDynamite;
        public float age;
    }

    private readonly List<Feedback> feedback = new List<Feedback>();
    private NeedleManager needleManager;
    private int previousCollected;
    private int previousScore;
    private int bestScore;
    private bool bestScoreDirty;
    private GUIStyle scoreStyle;
    private GUIStyle bestStyle;
    private GUIStyle pointsStyle;
    private bool visible;

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

        previousCollected = needleManager.ButtonsCollected;
        previousScore = needleManager.Score;
        bestScore = Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0));
        visible = false;
    }

    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
    }

    private void OnEnable()
    {
        if (needleManager != null)
        {
            needleManager.OnDynamiteCaptured += ShowDynamiteFeedback;
        }
    }

    private void ShowDynamiteFeedback(Vector2 worldPosition)
    {
        feedback.Add(new Feedback
        {
            worldPosition = worldPosition,
            isDynamite = true,
            age = 0f
        });
    }

    // NeedleManager collects buttons in Update(). LateUpdate() detects confirmed
    // pickups that same frame, without relying on prefab names or scene setup.
    private void LateUpdate()
    {
        if (needleManager == null || !visible) return;

        int score = needleManager.Score;
        int collected = needleManager.ButtonsCollected;

        if (collected > previousCollected)
        {
            feedback.Add(new Feedback
            {
                worldPosition = needleManager.GetNeedleHeadPosition(),
                points = Mathf.Max(0, score - previousScore),
                age = 0f
            });
        }

        previousCollected = collected;
        previousScore = score;

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            bestScoreDirty = true;
        }

        for (int i = feedback.Count - 1; i >= 0; i--)
        {
            feedback[i].age += Time.deltaTime;
            if (feedback[i].age >= FeedbackDuration)
                feedback.RemoveAt(i);
        }
    }

    private void OnGUI()
    {
        if (needleManager == null || !visible) return;

        if (scoreStyle == null) CreateStyles();

        float scale = Mathf.Clamp(Screen.height / 540f, 0.7f, 1.6f);
        scoreStyle.fontSize = Mathf.RoundToInt(27f * scale);
        bestStyle.fontSize = Mathf.RoundToInt(15f * scale);
        pointsStyle.fontSize = Mathf.RoundToInt(21f * scale);

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
            "BEST  " + bestScore, bestStyle);

        DrawCollectionFeedback(scale);

        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private void DrawCollectionFeedback(float scale)
    {
        if (feedback.Count == 0) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        for (int j = 0; j < feedback.Count; j++)
        {
            Feedback burst = feedback[j];
            Vector3 screenPoint = camera.WorldToScreenPoint(burst.worldPosition);
            if (screenPoint.z <= 0f) continue;

            float t = Mathf.Clamp01(burst.age / FeedbackDuration);
            float alpha = 1f - t;
            Vector2 center = new Vector2(screenPoint.x, Screen.height - screenPoint.y);

            // Six outward streaks make the slash noticeable even for a 0-point button.
            GUI.color = burst.isDynamite
                ? new Color(1f, 0.2f, 0.2f, 0.9f * alpha)
                : new Color(1f, 0.86f, 0.45f, 0.85f * alpha);
            float radius = Mathf.Lerp(4f, 28f, t) * scale;
            for (int i = 0; i < 6; i++)
            {
                Matrix4x4 previousMatrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(i * 60f, center);
                GUI.DrawTexture(new Rect(center.x + radius, center.y - 1.5f * scale,
                    12f * scale, 3f * scale), Texture2D.whiteTexture);
                GUI.matrix = previousMatrix;
            }

            GUI.color = burst.isDynamite
                ? new Color(1f, 0.15f, 0.15f, alpha)
                : new Color(1f, 1f, 1f, alpha);
            string label = burst.isDynamite
                ? "BOOM"
                : burst.points > 0 ? "+" + burst.points : "NICE!";
            GUI.Label(new Rect(center.x - 60f * scale,
                center.y - (55f + 35f * t) * scale,
                120f * scale, 32f * scale), label, pointsStyle);
        }
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

        pointsStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        pointsStyle.normal.textColor = new Color(1f, 0.93f, 0.64f);
    }

    // Avoid forcing a disk save on every collected button. Save on scene exit,
    // application quit, or suspension; Unity also persists PlayerPrefs on quit.
    private void SaveBestScore()
    {
        if (!bestScoreDirty) return;
        PlayerPrefs.Save();
        bestScoreDirty = false;
    }

    private void OnDisable()
    {
        if (needleManager != null)
        {
            needleManager.OnDynamiteCaptured -= ShowDynamiteFeedback;
        }

        SaveBestScore();
    }
    private void OnApplicationQuit() { SaveBestScore(); }
    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveBestScore();
    }
}
