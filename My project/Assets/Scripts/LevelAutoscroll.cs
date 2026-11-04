using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class LevelAutoscroll : MonoBehaviour
{
    [Header("Scroll")]
    [SerializeField] float baseScrollSpeed = 1.6f;
    [SerializeField] float scrollSpeedGainPerScore = 0.05f;
    [SerializeField] float maxScrollSpeed = 6f;

    [Header("Spawning")]
    [SerializeField] float spawnAheadDistance = 10f;
    [SerializeField] float despawnBehindDistance = 10f;
    [SerializeField] float buttonSpawnInterval = 2.2f;
    [SerializeField] float wallSpawnInterval = 4.5f;
    [SerializeField] float minButtonGap = 2.5f;
    // Wide horizontal barriers that drop in from the top.
    [SerializeField] Vector2 wallSize = new Vector2(2.8f, 0.6f);

    private Sewing sewing;
    private Camera cam;
    private Transform contentRoot;
    private float nextButtonSpawnY;
    private float nextWallSpawnY;
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

        float startY = cam != null ? cam.transform.position.y : 0f;
        nextButtonSpawnY = startY + 6f;
        nextWallSpawnY = startY + 10f;
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

        
        cam.transform.position += new Vector3(0f, speed * Time.deltaTime, 0f);

        TrySpawnButton();
        TrySpawnWall();
        CleanupBehindCamera();
    }

    private float HalfPlayWidth()
    {
        return cam.orthographicSize * cam.aspect - 1.2f;
    }

    private void TrySpawnButton()
    {
        float camY = cam.transform.position.y;
        if (camY + spawnAheadDistance < nextButtonSpawnY) return;
        if (Time.time - lastButtonSpawnTime < buttonSpawnInterval * 0.35f) return;

        float x = Random.Range(-HalfPlayWidth(), HalfPlayWidth());
        Vector2 spawnPos = new Vector2(cam.transform.position.x + x, nextButtonSpawnY);

        Sewing.ButtonType type = PickButtonType();
        sewing.SpawnButton(spawnPos, type);

        nextButtonSpawnY += Random.Range(minButtonGap, minButtonGap + 3f);
        lastButtonSpawnTime = Time.time;
    }

    private void TrySpawnWall()
    {
        float camY = cam.transform.position.y;
        if (camY + spawnAheadDistance < nextWallSpawnY) return;
        if (Time.time - lastWallSpawnTime < wallSpawnInterval * 0.5f) return;

        float x = Random.Range(-HalfPlayWidth(), HalfPlayWidth());
        Vector2 spawnPos = new Vector2(cam.transform.position.x + x, nextWallSpawnY);

        GameObject wallObj = new GameObject("Wall");
        wallObj.transform.SetParent(contentRoot, false);
        wallObj.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
        wallObj.AddComponent<BoxCollider2D>();
        Wall wall = wallObj.AddComponent<Wall>();
        wall.Initialize(sewing, wallSize);

        spawnedWalls.Add(wallObj);

        nextWallSpawnY += Random.Range(wallSpawnInterval, wallSpawnInterval + 3f);
        lastWallSpawnTime = Time.time;
    }

    private void CleanupBehindCamera()
    {
      
        float cutoff = cam.transform.position.y - despawnBehindDistance;

        sewing.DespawnButtonsBelow(cutoff);

        for (int i = spawnedWalls.Count - 1; i >= 0; i--)
        {
            GameObject wall = spawnedWalls[i];
            if (wall == null)
            {
                spawnedWalls.RemoveAt(i);
                continue;
            }

            if (wall.transform.position.y < cutoff)
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
