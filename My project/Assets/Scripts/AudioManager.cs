using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Project-wide audio playback. Attach this component to an AudioManager
/// GameObject in a scene; the singleton survives scene changes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonHitSound;
    [SerializeField] private float buttonHitSoundVolume = 1.2f;
    [SerializeField] private AudioClip buttonDestroyedSound;
    [SerializeField] private float buttonDestroyedSoundVolume = 1.5f;
    [SerializeField] private AudioClip shieldRemovedSound;
    [SerializeField] private float shieldRemovedSoundVolume = 0.2f;
    [SerializeField] private AudioClip nailDamagedSound;
    [SerializeField] private float nailDamagedSoundVolume = 0.5f;
    [SerializeField] private AudioClip dynamiteExplodeSound;
    [SerializeField] private float dynamiteExplodeSoundVolume = 0.7f;
    [SerializeField] private AudioClip uiButtonSound;
    [SerializeField] private float uiButtonSoundVolume = 0.9f;

    [Header("Music")]
    [SerializeField] private AudioClip soundtrack;
    [SerializeField] private float soundtrackVolume = 0.1f;

    private AudioSource audioSource;
    private AudioSource soundtrackSource;
    private readonly List<Button> hookedButtons = new List<Button>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        soundtrackSource = gameObject.AddComponent<AudioSource>();
        soundtrackSource.playOnAwake = false;
        soundtrackSource.loop = true;
        soundtrackSource.spatialBlend = 0f;
        soundtrackSource.volume = soundtrackVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        HookSceneButtons();

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartSoundtrack();
    }

    public void PlayButtonHitSound()
    {
        Play(buttonHitSound, buttonHitSoundVolume);
    }

    public void PlayButtonDestroyedSound()
    {
        Play(buttonDestroyedSound, buttonDestroyedSoundVolume);
    }

    public void PlayShieldRemovedSound()
    {
        Play(shieldRemovedSound, shieldRemovedSoundVolume);
    }

    public void PlayNailDamagedSound()
    {
        Play(nailDamagedSound, nailDamagedSoundVolume);
    }

    public void PlayDynamiteExplodeSound()
    {
        Play(dynamiteExplodeSound, dynamiteExplodeSoundVolume);
    }

    public void PlayUIButtonSound()
    {
        Play(uiButtonSound, uiButtonSoundVolume);
    }

    public void StartSoundtrack()
    {
        if (soundtrack == null || soundtrackSource == null)
            return;

        soundtrackSource.clip = soundtrack;
        soundtrackSource.loop = true;
        if (!soundtrackSource.isPlaying)
            soundtrackSource.Play();
    }

    public void StopSoundtrack()
    {
        if (soundtrackSource != null)
            soundtrackSource.Stop();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookSceneButtons();
    }

    private void HookSceneButtons()
    {
        hookedButtons.RemoveAll(button => button == null);
        Button[] sceneButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in sceneButtons)
        {
            if (button == null || hookedButtons.Contains(button))
                continue;

            button.onClick.AddListener(PlayUIButtonSound);
            hookedButtons.Add(button);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        foreach (Button button in hookedButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayUIButtonSound);
        }
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
    private void Play(AudioClip clip, float volume)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            audioSource.volume = volume;
        }
    }
}
