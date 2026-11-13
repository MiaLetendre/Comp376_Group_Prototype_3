using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public sealed class KDSewingPolishUI : MonoBehaviour
{
    private enum Page { Playing, Tutorial, Pause, Death }

    private static KDSewingPolishUI instance;

    [Header("Pages")]
    [SerializeField] private GameObject tutorialPage;
    [SerializeField] private GameObject pausePage;
    [SerializeField] private GameObject deathPage;
    [SerializeField] private TMP_Text deathScoreText;
    [SerializeField] private TMP_Text deathHighScoreText;

    private NeedleManager needleManager;
    private HealthDisplay healthDisplay;
    private LineRenderer threadLine;
    private Page page = Page.Playing;
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
        if (page != Page.Playing)
            UnfreezeGame();
        page = Page.Playing;

        needleManager = FindFirstObjectByType<NeedleManager>();
        threadLine = needleManager != null ? needleManager.GetComponent<LineRenderer>() : null;

        SetPage(needleManager != null ? Page.Tutorial : Page.Playing);
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
                SetPage(Page.Death);
                return;
            }
        }

        Keyboard keys = Keyboard.current;
        if (keys == null) return;

        if (keys.escapeKey.wasPressedThisFrame)
        {
            if (page == Page.Playing) SetPage(Page.Pause);
            else if (page == Page.Tutorial) CloseTutorial();
            else if (page == Page.Pause) Resume();
        }
        else if (page == Page.Tutorial &&
                 (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame))
        {
            CloseTutorial();
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

    private void SetPage(Page next)
    {
        if (next != Page.Playing && page == Page.Playing)
        {
            previousTimeScale = Time.timeScale;
            wasNeedleManagerEnabled = needleManager != null && needleManager.enabled;
            if (needleManager != null)
                needleManager.enabled = false;
            Time.timeScale = 0f;
        }

        if (next == Page.Playing)
        {
            SetGameplayVisualsVisible(true);
            SetHealthDisplayVisible(true);
            SetScoreDisplayVisible(true);
            UnfreezeGame();
        }
        else
        {
            SetHealthDisplayVisible(false);
            SetScoreDisplayVisible(false);
        }

        if (next == Page.Death)
            UpdateDeathScoreDisplay();

        if (next == Page.Tutorial)
            SetGameplayVisualsVisible(false);

        page = next;
        UpdatePageVisibility();
    }

    private void UpdatePageVisibility()
    {
        SetPageActive(tutorialPage, page == Page.Tutorial);
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

        GetScoreDisplay().SetVisible(visible);
    }

    private void UpdateDeathScoreDisplay()
    {
        if (needleManager == null)
            return;

        KDScoreAndCollectionHUD scoreDisplay = GetScoreDisplay();
        if (deathScoreText != null)
            deathScoreText.text = $"Score: {scoreDisplay.CurrentScore}";
        if (deathHighScoreText != null)
            deathHighScoreText.text = $"High Score: {scoreDisplay.GetBestScore()}";
    }

    private KDScoreAndCollectionHUD GetScoreDisplay()
    {
        KDScoreAndCollectionHUD scoreDisplay = needleManager.GetComponent<KDScoreAndCollectionHUD>();
        return scoreDisplay != null
            ? scoreDisplay
            : needleManager.gameObject.AddComponent<KDScoreAndCollectionHUD>();
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
        if (needleManager != null && wasNeedleManagerEnabled)
            needleManager.enabled = true;
    }

    public void CloseTutorial()
    {
        if (page != Page.Tutorial)
            return;

        SetPage(tutorialReturn);
    }

    public void ShowTutorial()
    {
        if (page == Page.Tutorial)
            return;

        tutorialReturn = page;
        SetPage(Page.Tutorial);
    }

    public void Resume()
    {
        SetPage(Page.Playing);
    }

    public void RestartWithTutorial()
    {
        SetPage(Page.Playing);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        SetPage(Page.Playing);
        SceneManager.LoadScene("Menu");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (page != Page.Playing) UnfreezeGame();
    }
}
