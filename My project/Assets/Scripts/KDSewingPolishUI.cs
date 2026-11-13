using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class KDSewingPolishUI : MonoBehaviour
{
    private enum Page { Playing, Tutorial, Pause, Help, Death }

    private static KDSewingPolishUI instance;
    private static bool skipTutorialOnNextScene;

    [Header("Pages")]
    [SerializeField] private GameObject tutorialPage;
    [SerializeField] private GameObject pausePage;
    [SerializeField] private GameObject deathPage;

    private NeedleManager needleManager;
    private HealthDisplay healthDisplay;
    private LineRenderer threadLine;
    private Page page = Page.Playing;
    private Page helpReturn = Page.Pause;
    private Page tutorialReturn = Page.Playing;
    private bool watchForDeath;
    private bool wasNeedleManagerEnabled;
    private float previousTimeScale = 1f;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectToCurrentScene();
    }

    private void ConnectToCurrentScene()
    {
        watchForDeath = false;
        if (page != Page.Playing) UnfreezeGame();
        needleManager = FindFirstObjectByType<NeedleManager>();
        threadLine = needleManager != null ? needleManager.GetComponent<LineRenderer>() : null;

        bool showTutorial = !skipTutorialOnNextScene;
        skipTutorialOnNextScene = false;

        if (needleManager != null && showTutorial)
        {
            ShowOverlay(Page.Tutorial);
        }
        else
        {
            Time.timeScale = 1f;
            page = Page.Playing;
            SetGameplayVisualsVisible(true);
            SetHealthDisplayVisible(true);
            SetScoreDisplayVisible(true);
            UpdatePageVisibility();
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
                ShowOverlay(Page.Death);
                return;
            }
        }

        Keyboard keys = Keyboard.current;
        if (keys == null) return;

        if (keys.escapeKey.wasPressedThisFrame)
        {
            if (page == Page.Playing) ShowOverlay(Page.Pause);
            else if (page == Page.Tutorial) CloseTutorial();
            else if (page == Page.Help) ReturnFromHelp();
            else if (page == Page.Pause) Resume();
        }
        else if (page == Page.Tutorial &&
                 (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame))
        {
            CloseTutorial();
        }
        else if (page == Page.Playing && keys.hKey.wasPressedThisFrame)
        {
            OpenHelpFromPlaying();
        }
    }

    private void LateUpdate()
    {
        bool anyPageActive =
            (tutorialPage != null && tutorialPage.activeInHierarchy) ||
            (pausePage != null && pausePage.activeInHierarchy) ||
            (deathPage != null && deathPage.activeInHierarchy);

        Cursor.visible = anyPageActive;
        if (anyPageActive)
            Cursor.lockState = CursorLockMode.None;
    }

    private void ShowOverlay(Page next)
    {
        if (page == Page.Playing)
        {
            previousTimeScale = Time.timeScale;
            wasNeedleManagerEnabled = needleManager != null && needleManager.enabled;
            if (needleManager != null)
                needleManager.enabled = false;
            Time.timeScale = 0f;
            Cursor.visible = true;
        }

        if (next == Page.Tutorial)
            SetGameplayVisualsVisible(false);

        page = next;
        SetHealthDisplayVisible(false);
        SetScoreDisplayVisible(false);
        UpdatePageVisibility();
    }

    private void ReturnFromHelp()
    {
        if (helpReturn == Page.Playing)
        {
            Resume();
            return;
        }

        page = helpReturn;
        UpdatePageVisibility();
    }

    private void OpenHelpFromPlaying()
    {
        helpReturn = Page.Playing;
        ShowOverlay(Page.Help);
    }

    private void UpdatePageVisibility()
    {
        SetPageActive(tutorialPage, page == Page.Tutorial || page == Page.Help);
        SetPageActive(pausePage, page == Page.Pause);
        SetPageActive(deathPage, page == Page.Death);
    }

    private static void SetPageActive(GameObject pageObject, bool active)
    {
        if (pageObject != null && pageObject.activeSelf != active)
            pageObject.SetActive(active);
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
        if (needleManager != null && wasNeedleManagerEnabled)
            needleManager.enabled = true;
        page = Page.Playing;
    }

    public void CloseTutorial()
    {
        if (page == Page.Help)
        {
            ReturnFromHelp();
            return;
        }

        if (tutorialReturn == Page.Playing)
        {
            Resume();
            return;
        }

        page = tutorialReturn;
        UpdatePageVisibility();
    }

    public void ShowTutorial()
    {
        tutorialReturn = page;
        if (page == Page.Playing)
        {
            ShowOverlay(Page.Tutorial);
            return;
        }

        page = Page.Tutorial;
        UpdatePageVisibility();
    }

    public void Resume()
    {
        SetGameplayVisualsVisible(true);
        SetHealthDisplayVisible(true);
        SetScoreDisplayVisible(true);
        UnfreezeGame();
        page = Page.Playing;
        UpdatePageVisibility();
    }

    public void Restart()
    {
        skipTutorialOnNextScene = true;
        UnfreezeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestartWithTutorial()
    {
        skipTutorialOnNextScene = false;
        UnfreezeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        UnfreezeGame();
        SceneManager.LoadScene("Menu");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (page != Page.Playing) UnfreezeGame();
    }
}
