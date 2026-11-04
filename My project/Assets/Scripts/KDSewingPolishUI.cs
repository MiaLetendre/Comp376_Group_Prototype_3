using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// COMP376 KD polish: a quick visual tutorial and a simple pause/main menu.
/// Auto-installs at runtime in scenes that contain Mia's Sewing component.
/// Does not change Sewing.cs, its prefab references, or the scene YAML.
///
/// GDD checklist:
///  - Quick tutorial to show how to kill buttons
///  - Simple UI Menu (can resume UI from last game)
/// </summary>
[DisallowMultipleComponent]
public sealed class KDSewingPolishUI : MonoBehaviour
{
    private enum Page { Playing, Tutorial, Pause, Help }

    private const float ReferenceWidth = 960f;
    private const float ReferenceHeight = 540f;

    private static KDSewingPolishUI instance;

    private Sewing sewing;
    private Page page = Page.Playing;
    private bool wasSewingEnabled;
    private float previousTimeScale = 1f;
    private Texture2D circleTexture;
    private GUIStyle headingStyle;
    private GUIStyle subheadingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;
    private bool stylesReady;

    // No scene edits or prefab hookup needed: detect the existing sewing game.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<Sewing>() == null) return;
        if (FindFirstObjectByType<KDSewingPolishUI>() != null) return;
        new GameObject("KD Sewing - Tutorial & Menu").AddComponent<KDSewingPolishUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        CreateCircleTexture();
        ConnectToCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectToCurrentScene();
    }

    private void ConnectToCurrentScene()
    {
        // Restores the time scale if a restart happened from the pause menu.
        if (page != Page.Playing) UnfreezeGame();
        sewing = FindFirstObjectByType<Sewing>();

        // Every fresh round begins with a concise explanation of the controls.
        if (sewing != null) ShowOverlay(Page.Tutorial);
        else page = Page.Playing;
    }

    private void Update()
    {
        if (sewing == null) return;
        Keyboard keys = Keyboard.current;
        if (keys == null) return;

        if (keys.escapeKey.wasPressedThisFrame)
        {
            if (page == Page.Playing) ShowOverlay(Page.Pause);
            else if (page == Page.Help) page = Page.Pause;
            else Resume();
        }
        else if (page == Page.Tutorial &&
                 (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame))
        {
            Resume();
        }
        else if (page == Page.Playing && keys.hKey.wasPressedThisFrame)
        {
            ShowOverlay(Page.Help);
        }
    }

    private void ShowOverlay(Page next)
    {
        if (page == Page.Playing)
        {
            previousTimeScale = Time.timeScale;
            wasSewingEnabled = sewing != null && sewing.enabled;
            if (sewing != null) sewing.enabled = false;
            Time.timeScale = 0f;
        }
        page = next;
    }

    private void Resume()
    {
        UnfreezeGame();
        page = Page.Playing;
    }

    private void UnfreezeGame()
    {
        Time.timeScale = previousTimeScale;
        if (sewing != null && wasSewingEnabled) sewing.enabled = true;
        page = Page.Playing;
    }

    private void Restart()
    {
        UnfreezeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Quit()
    {
        UnfreezeGame();
#if UNITY_EDITOR
        Debug.Log("KD Sewing menu: Quit works in a built game. Stopping Editor play mode.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnGUI()
    {
        if (sewing == null) return;
        if (!stylesReady) SetupStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / ReferenceWidth,
                                                  Screen.height / ReferenceHeight, 1f));

        if (page == Page.Playing)
        {
            DrawRect(new Rect(744, 12, 206, 29), new Color(.06f, .08f, .13f, .7f));
            GUI.Label(new Rect(755, 16, 195, 24), "ESC: Menu    H: Help", smallStyle);
        }
        else
        {
            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color(.03f, .05f, .10f, .88f));
            DrawRect(new Rect(155, 60, 650, 420), new Color(.10f, .13f, .21f, 1f));
            DrawRect(new Rect(155, 60, 650, 7), new Color(.65f, .42f, .91f, 1f));

            if (page == Page.Pause) DrawPause();
            else DrawTutorial(page == Page.Tutorial);
        }

        GUI.matrix = previousMatrix;
    }

    private void DrawPause()
    {
        GUI.Label(new Rect(200, 94, 560, 55), "SEWING SNAKE", headingStyle);
        GUI.Label(new Rect(200, 144, 560, 35), "Game paused", subheadingStyle);

        if (Button(new Rect(333, 205, 294, 48), "Resume   [ESC]")) Resume();
        if (Button(new Rect(333, 266, 294, 48), "How to Play")) page = Page.Help;
        if (Button(new Rect(333, 327, 294, 48), "Restart")) Restart();
        if (Button(new Rect(333, 388, 294, 48), "Quit Game")) Quit();
    }

    private void DrawTutorial(bool firstTime)
    {
        GUI.Label(new Rect(188, 88, 590, 55), "HOW TO SEW", headingStyle);
        GUI.Label(new Rect(192, 153, 425, 32), "1   Move your mouse to steer the needle", bodyStyle);
        GUI.Label(new Rect(192, 205, 425, 58), "2   Circle a button with the thread,\n     crossing the loop to collect it", bodyStyle);
        GUI.Label(new Rect(192, 275, 425, 28), "3   Black: one wrap   Blue shield: two wraps", bodyStyle);
        GUI.Label(new Rect(192, 305, 425, 28), "4   Green dodges you   Red damages the needle", bodyStyle);
        GUI.Label(new Rect(192, 335, 425, 28), "5   Avoid brown walls while the level scrolls", bodyStyle);

        DrawLoopDemo();
        GUI.Label(new Rect(581, 366, 205, 30), "Loop around the button", smallStyle);

        string label = firstTime ? "Start Sewing   [Enter]" : "Back to Menu   [ESC]";
        if (Button(new Rect(323, 410, 314, 46), label))
        {
            if (firstTime) Resume();
            else page = Page.Pause;
        }
    }

    private void DrawLoopDemo()
    {
        float cx = 674f;
        float cy = 265f;
        float radius = 67f;
        Color old = GUI.color;

        // A continuously drawn thread loop, using unscaled time so the demo
        // continues animating while the gameplay is paused.
        GUI.color = new Color(.83f, .71f, 1f, 1f);
        float amount = Mathf.Repeat(Time.unscaledTime * .22f, 1f);
        int segments = Mathf.Max(1, Mathf.RoundToInt(56 * amount));
        Vector2 previous = new Vector2(cx + radius, cy);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / 56f;
            Vector2 next = new Vector2(cx + Mathf.Cos(angle) * radius,
                                       cy + Mathf.Sin(angle) * radius);
            DrawLine(previous, next, 4f);
            previous = next;
        }

        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx - 32, cy - 32, 64, 64), circleTexture);
        GUI.color = new Color(.22f, .49f, .92f, 1f);
        GUI.DrawTexture(new Rect(cx - 27, cy - 27, 54, 54), circleTexture);

        float needleAngle = amount * Mathf.PI * 2f;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx + Mathf.Cos(needleAngle) * radius - 7,
                                 cy + Mathf.Sin(needleAngle) * radius - 7,
                                 14, 14), circleTexture);
        GUI.color = old;
    }

    private static void DrawLine(Vector2 from, Vector2 to, float thickness)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < .00001f) return;
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
        GUI.DrawTexture(new Rect(from.x, from.y - thickness / 2f,
                                 delta.magnitude, thickness), Texture2D.whiteTexture);
        GUI.matrix = saved;
    }

    private static void DrawRect(Rect rect, Color tint)
    {
        Color old = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private bool Button(Rect rect, string label)
    {
        return GUI.Button(rect, label, buttonStyle);
    }

    private void SetupStyles()
    {
        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 35, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headingStyle.normal.textColor = new Color(.94f, .88f, 1f);

        subheadingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19, alignment = TextAnchor.MiddleCenter
        };
        subheadingStyle.normal.textColor = new Color(.77f, .79f, .88f);

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18, alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        bodyStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, alignment = TextAnchor.MiddleCenter
        };
        smallStyle.normal.textColor = new Color(.87f, .86f, .95f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 19, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        stylesReady = true;
    }

    private void CreateCircleTexture()
    {
        const int size = 64;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTexture.name = "KD UI Circle (Runtime)";
        circleTexture.hideFlags = HideFlags.HideAndDontSave;
        Color32[] pixels = new Color32[size * size];
        float radius = size * .5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y),
                    new Vector2((size - 1f) * .5f, (size - 1f) * .5f));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        circleTexture.SetPixels32(pixels);
        circleTexture.Apply(false, true);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (page != Page.Playing) UnfreezeGame();
        if (circleTexture != null) Destroy(circleTexture);
    }
}
