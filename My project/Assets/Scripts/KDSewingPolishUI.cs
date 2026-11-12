using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// COMP376 KD polish: a quick visual tutorial and a simple pause/main menu.
/// Auto-installs at runtime in scenes that contain the needle manager.
///
/// GDD checklist:
///  - Quick tutorial to show how to kill buttons
///  - Simple UI Menu (can resume UI from last game)
/// </summary>
[DisallowMultipleComponent]
public sealed class KDSewingPolishUI : MonoBehaviour
{
    private enum Page { Playing, Tutorial, Pause, Help, Death }

    private const float ReferenceWidth = 960f;
    private const float ReferenceHeight = 540f;

    private static KDSewingPolishUI instance;
    private static bool skipTutorialOnNextScene;
    private static bool showTutorialOnNextScene;

    private NeedleManager needleManager;
    private HealthDisplay healthDisplay;
    private LineRenderer threadLine;
    private Page page = Page.Playing;
    private Page helpReturn = Page.Pause;
    // The view initializes the needle in Start. Until then, it looks dead.
    private bool watchForDeath;
    private bool wasNeedleManagerEnabled;
    private float previousTimeScale = 1f;
    private CanvasGroup tutorialCanvasGroup;
    private GUIStyle headingStyle;
    private GUIStyle subheadingStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;
    private bool stylesReady;

    [Header("Tutorial UI Panel")]
    [SerializeField] private GameObject tutorialPanel; // Drag your Tutorial Panel here in the Inspector
    [SerializeField] private Button startButton;         // Drag your Start/Close button here

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<NeedleManager>() == null) return;
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
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConnectToCurrentScene();
    }

    private void Start()
    {
        SetTutorialPanelVisible(page == Page.Tutorial);
        SetGameplayVisualsVisible(page != Page.Tutorial);

        if (startButton != null)
        {
            startButton.onClick.AddListener(CloseTutorial);
        }
    }

    public void CloseTutorial()
    {
        SetTutorialPanelVisible(false);
        Resume();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectToCurrentScene();
    }

    private void ConnectToCurrentScene()
    {
        // Restores the time scale if a restart happened from the pause menu.
        watchForDeath = false;
        if (page != Page.Playing) UnfreezeGame();
        needleManager = FindFirstObjectByType<NeedleManager>();
        threadLine = needleManager != null ? needleManager.GetComponent<LineRenderer>() : null;

        // Every fresh round begins with a concise explanation of the controls.
        bool showTutorial = showTutorialOnNextScene || !skipTutorialOnNextScene;
        skipTutorialOnNextScene = false;
        showTutorialOnNextScene = false;
        if (needleManager != null && showTutorial)
        {
            ShowOverlay(Page.Tutorial);
            SetTutorialPanelVisible(true);
            SetScoreDisplayVisible(false);
            Cursor.visible = true;
        } else {
            Time.timeScale = 1f;
            page = Page.Playing;
            SetTutorialPanelVisible(false);
            SetGameplayVisualsVisible(true);
            SetHealthDisplayVisible(true);
            SetScoreDisplayVisible(true);
        }
    }

    private void Update()
    {
        if (needleManager == null) return;

        if (page == Page.Playing)
        {
            if (!watchForDeath)
            {
                watchForDeath = needleManager.GetNeedleTransform() != null;
            }
            else if (!needleManager.IsNeedleAlive())
            {
                SetTutorialPanelVisible(false);
                ShowOverlay(Page.Death);

                return;
            }
        }

        Keyboard keys = Keyboard.current;
        if (keys == null) return;

        if (keys.escapeKey.wasPressedThisFrame)
        {
            if (page == Page.Playing) ShowOverlay(Page.Pause);
            else if (page == Page.Tutorial)
            {
                SetTutorialPanelVisible(false);
                page = helpReturn;
            }
            else if (page == Page.Help) page = helpReturn;
            else if (page != Page.Death) Resume();
        }
        else if (page == Page.Tutorial &&
                 (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame))
        {
            Resume();
        }
        else if (page == Page.Playing && keys.hKey.wasPressedThisFrame)
        {
            helpReturn = Page.Pause;
            ShowOverlay(Page.Help);
        }
    }

    private void LateUpdate()
    {
        Cursor.visible = page != Page.Playing;
        if (page != Page.Playing)
            Cursor.lockState = CursorLockMode.None;
    }

    private void ShowOverlay(Page next)
    {
        if (page == Page.Playing)
        {
            previousTimeScale = Time.timeScale;
            wasNeedleManagerEnabled = needleManager != null && needleManager.enabled;
            if (next != Page.Tutorial && needleManager != null) needleManager.enabled = false;
            Time.timeScale = 0f;
            Cursor.visible = true;
        }
        if (next == Page.Tutorial)
            SetGameplayVisualsVisible(false);
        page = next;
    }

    private void Resume()
    {
        SetTutorialPanelVisible(false);
        SetGameplayVisualsVisible(true);
        SetHealthDisplayVisible(true);
        SetScoreDisplayVisible(true);
        UnfreezeGame();
        page = Page.Playing;
    }

    private void SetTutorialPanelVisible(bool visible)
    {
        if (tutorialPanel != null)
        {
            if (tutorialPanel == gameObject)
            {
                if (tutorialCanvasGroup == null)
                {
                    tutorialCanvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
                    if (tutorialCanvasGroup == null)
                    {
                        tutorialCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
                    }
                }

                tutorialCanvasGroup.alpha = visible ? 1f : 0f;
                tutorialCanvasGroup.interactable = visible;
                tutorialCanvasGroup.blocksRaycasts = visible;
                Cursor.visible = visible;
            }
            else
            {
                tutorialPanel.SetActive(visible);
            }
        }
    }

    private void SetHealthDisplayVisible(bool visible)
    {
        if (healthDisplay == null)
            healthDisplay = FindFirstObjectByType<HealthDisplay>();

        if (healthDisplay != null)
            healthDisplay.SetVisible(visible);
    }

    private void SetScoreDisplayVisible(bool visible)
    {
        if (needleManager == null)
            return;

        KDScoreAndCollectionHUD scoreDisplay = needleManager.GetComponent<KDScoreAndCollectionHUD>();
        if (scoreDisplay == null)
            scoreDisplay = needleManager.gameObject.AddComponent<KDScoreAndCollectionHUD>();

        scoreDisplay.SetVisible(visible);
    }

    private void SetGameplayVisualsVisible(bool visible)
    {
        if (threadLine == null && needleManager != null)
            threadLine = needleManager.GetComponent<LineRenderer>();

        if (threadLine != null)
            threadLine.enabled = visible;
    }

    private void UnfreezeGame()
    {
        Time.timeScale = previousTimeScale;
        Cursor.visible = false;
        if (needleManager != null && wasNeedleManagerEnabled) needleManager.enabled = true;
        page = Page.Playing;
    }

    private void Restart()
    {
        skipTutorialOnNextScene = true;
        showTutorialOnNextScene = false;
        UnfreezeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RestartWithTutorial()
    {
        skipTutorialOnNextScene = false;
        showTutorialOnNextScene = true;
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
        if (needleManager == null) return;
        if (page == Page.Tutorial) return;
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
            else if (page == Page.Death) DrawDeath();
        }

        GUI.matrix = previousMatrix;
    }

    private void DrawPause()
    {
        GUI.Label(new Rect(200, 94, 560, 55), "SEWING SNAKE", headingStyle);
        GUI.Label(new Rect(200, 144, 560, 35), "Game paused", subheadingStyle);

        if (Button(new Rect(333, 205, 294, 48), "Resume   [ESC]")) Resume();
        if (Button(new Rect(333, 266, 294, 48), "How to Play")) RestartWithTutorial();
        if (Button(new Rect(333, 327, 294, 48), "Restart")) Restart();
        if (Button(new Rect(333, 388, 294, 48), "Quit Game")) Quit();
    }

    private void DrawDeath()
    {
        GUI.Label(new Rect(200, 94, 560, 55), "SEWING SNAKE", headingStyle);
        GUI.Label(new Rect(200, 144, 560, 35), "you died", subheadingStyle);

        if (Button(new Rect(333, 220, 294, 48), "Restart")) Restart();
        if (Button(new Rect(333, 281, 294, 48), "How to Play")) RestartWithTutorial();
        if (Button(new Rect(333, 342, 294, 48), "Quit Game")) Quit();
    }

    private void OpenHelp(Page returnPage)
    {
        helpReturn = returnPage;
        page = Page.Help;
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

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (page != Page.Playing) UnfreezeGame();
    }
}
