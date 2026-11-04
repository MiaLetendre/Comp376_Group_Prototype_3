using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Infinite horizontal autoscroll: camera drifts forward, buttons and walls
/// spawn ahead of the view, and off-screen content is cleaned up.
/// Auto-installs next to an existing Sewing component.
/// </summary>
[DisallowMultipleComponent]
public class LevelAutoscroll : MonoBehaviour
{
    [Header("Scroll")]
    [SerializeField] float baseScrollSpeed = 1.6f;
    [SerializeField] float scrollSpeedGainPerScore = 0.05f;
    [SerializeField] float maxScrollSpeed = 6f;

    [Header("Spawning")]
    [SerializeField] float spawnAheadDistance = 12f;
    [SerializeField] float despawnBehindDistance = 10f;
    [SerializeField] float buttonSpawnInterval = 2.2f;
    [SerializeField] float wallSpawnInterval = 4.5f;
    [SerializeField] float minButtonGap = 2.5f;
    [SerializeField] Vector2 wallSize = new Vector2(0.6f, 2.8f);

    private Sewing sewing;
    private Camera cam;
    private Transform contentRoot;
    private float nextButtonSpawnX;
    private float nextWallSpawnX;
    private float lastButtonSpawnTime;
    private float lastWallSpawnTime;
    private readonly List<GameObject> spawnedWalls = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Sewing sewing = Object.FindFirstObjectByType<Sewing>();
        if (sewing == null) return;
        if (sewing.GetComponent<LevelAutoscroll>() != null) return;
        sewing.gameObject.AddComponent<LevelAutoscroll>();
    }

    private void Awake()
    {
        sewing = GetComponent<Sewing>();
        if (sewing == null)
        {
            sewing = FindFirstObjectByType<Sewing>();
        }

        cam = Camera.main;
        GameObject root = new GameObject("Autoscroll Content");
        contentRoot = root.transform;

        float startX = cam != null ? cam.transform.position.x : 0f;
        nextButtonSpawnX = startX + 6f;
        nextWallSpawnX = startX + 10f;
        lastButtonSpawnTime = Time.time;
        lastWallSpawnTime = Time.time + 1.5f;
    }

    private void Update()
    {
        if (sewing == null || cam == null) return;
        if (!sewing.IsNeedleAlive()) return;

        float speed = Mathf.Min(
            maxScrollSpeed,
            baseScrollSpeed + sewing.ButtonsCollected * scrollSpeedGainPerScore);

        // Drive the endless runner feel by scrolling the camera.
        cam.transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);

        TrySpawnButton();
        TrySpawnWall();
        CleanupBehindCamera();
    }

    private void TrySpawnButton()
    {
        float camX = cam.transform.position.x;
        if (camX + spawnAheadDistance < nextButtonSpawnX) return;
        if (Time.time - lastButtonSpawnTime < buttonSpawnInterval * 0.35f) return;

        float y = Random.Range(-cam.orthographicSize + 1.2f, cam.orthographicSize - 1.2f);
        Vector2 spawnPos = new Vector2(nextButtonSpawnX, cam.transform.position.y + y);

        Sewing.ButtonType type = PickButtonType();
        sewing.SpawnButton(spawnPos, type);

        nextButtonSpawnX += Random.Range(minButtonGap, minButtonGap + 3f);
        lastButtonSpawnTime = Time.time;
    }

    private void TrySpawnWall()
    {
        float camX = cam.transform.position.x;
        if (camX + spawnAheadDistance < nextWallSpawnX) return;
        if (Time.time - lastWallSpawnTime < wallSpawnInterval * 0.5f) return;

        float y = Random.Range(-cam.orthographicSize + 1.5f, cam.orthographicSize - 1.5f);
        Vector2 spawnPos = new Vector2(nextWallSpawnX, cam.transform.position.y + y);

        GameObject wallObj = new GameObject("Wall");
        wallObj.transform.SetParent(contentRoot, false);
        wallObj.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
        wallObj.AddComponent<BoxCollider2D>();
        Wall wall = wallObj.AddComponent<Wall>();
        wall.Initialize(sewing, wallSize);

        spawnedWalls.Add(wallObj);

        nextWallSpawnX += Random.Range(wallSpawnInterval, wallSpawnInterval + 3f);
        lastWallSpawnTime = Time.time;
    }

    private void CleanupBehindCamera()
    {
        float cutoff = cam.transform.position.x - despawnBehindDistance;

        sewing.DespawnButtonsLeftOf(cutoff);

        for (int i = spawnedWalls.Count - 1; i >= 0; i--)
        {
            GameObject wall = spawnedWalls[i];
            if (wall == null)
            {
                spawnedWalls.RemoveAt(i);
                continue;
            }

            if (wall.transform.position.x < cutoff)
            {
                Destroy(wall);
                spawnedWalls.RemoveAt(i);
            }
        }
    }

    private static Sewing.ButtonType PickButtonType()
    {
        float roll = Random.value;
        if (roll < 0.45f) return Sewing.ButtonType.Regular;
        if (roll < 0.65f) return Sewing.ButtonType.Sheild;
        if (roll < 0.82f) return Sewing.ButtonType.Dodger;
        return Sewing.ButtonType.Attacker;
    }

    public float CurrentScrollSpeed
    {
        get
        {
            if (sewing == null) return baseScrollSpeed;
            return Mathf.Min(
                maxScrollSpeed,
                baseScrollSpeed + sewing.ButtonsCollected * scrollSpeedGainPerScore);
        }
    }
}
