using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class LevelAutoscroll : MonoBehaviour
{
    [Header("Scroll")]
    [SerializeField] float baseScrollSpeed = 1.6f;
    [SerializeField] float maxScrollSpeed = 6f;
    [SerializeField] float rampDuration = 250f;

    [Header("Spawning")]
    [SerializeField] float spawnAheadDistance = 12f;
    [SerializeField] float despawnBehindDistance = 12f;
    [SerializeField] float minButtonGap = 1.05f;
    [SerializeField] float minWallGap = 1.15f;
    [SerializeField] int easyAliveButtons = 6;
    [SerializeField] int hardAliveButtons = 11;
    [SerializeField] float minLaneWidth = 3.5f;

    private const float SpeedNoiseFrequency = 0.12f;
    private const float LaneNoiseFrequency = 0.05f;
    private const float MinSpeedWobble = 0.85f;
    private const float MaxSpeedWobble = 1.15f;
    private const float EasyButtonGap = 1.7f;
    private const float HardButtonGap = 1.05f;
    private const float EasyButtonJitter = 1.4f;
    private const float HardButtonJitter = 0.65f;
    private const float EasyPairChance = 0.25f;
    private const float HardPairChance = 0.42f;
    private const float EasyWallGap = 2.8f;
    private const float HardWallGap = 1.35f;
    private const float WallGapJitter = 1.5f;
    private const float EasyVerticalChance = 0.18f;
    private const float HardVerticalChance = 0.62f;
    private const float ButtonClearance = 0.6f;
    private const float HorizontalPassGap = 2f;

    private NeedleManager needleManager;
    private SewingButtonManager buttonManager;
    private Camera cam;
    private Transform contentRoot;
    private float aliveTime;
    private float nextButtonSpawnY;
    private float nextWallBottomY;
    private readonly List<GameObject> spawnedWalls = new List<GameObject>();
    private readonly List<Vector2> spawnedButtonPositions = new List<Vector2>();

    // AfterSceneLoad runs once per play session. Restart loads the scene again,
    // which destroys the old spawner, so this has to run on every scene load.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        NeedleManager needleManager = Object.FindFirstObjectByType<NeedleManager>();
        if (needleManager == null) return;
        if (needleManager.GetComponent<LevelAutoscroll>() != null) return;
        needleManager.gameObject.AddComponent<LevelAutoscroll>();
    }

    private void Awake()
    {
        needleManager = GetComponent<NeedleManager>();
        if (needleManager == null)
        {
            needleManager = FindFirstObjectByType<NeedleManager>();
        }

        if (needleManager != null)
        {
            buttonManager = needleManager.GetComponent<SewingButtonManager>();
        }

        cam = Camera.main;
        GameObject root = new GameObject("Autoscroll Content");
        contentRoot = root.transform;

        float startY = cam != null ? cam.transform.position.y : 0f;
        float viewTop = startY + (cam != null ? cam.orthographicSize : 5f);
        nextButtonSpawnY = viewTop + 1.5f;
        nextWallBottomY = viewTop + 3.5f;
    }

    private void Update()
    {
        if (needleManager == null || cam == null) return;
        if (!needleManager.IsNeedleAlive()) return;

        aliveTime += Time.deltaTime;
        cam.transform.position += new Vector3(0f, ScrollSpeed() * Time.deltaTime, 0f);

        TrySpawnWall();
        TrySpawnButton();
        CleanupBehindCamera();
    }

    private float Difficulty()
    {
        float t = rampDuration <= 0.01f ? 1f : Mathf.Clamp01(aliveTime / rampDuration);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private float ScrollSpeed()
    {
        float noise = Mathf.PerlinNoise(aliveTime * SpeedNoiseFrequency, 0.37f);
        float wobble = Mathf.Lerp(MinSpeedWobble, MaxSpeedWobble, noise);
        return Mathf.Lerp(baseScrollSpeed, maxScrollSpeed, Difficulty()) * wobble;
    }

    private float HalfPlayWidth()
    {
        return Mathf.Max(2f, cam.orthographicSize * cam.aspect - 1.2f);
    }

    private float ViewTop()
    {
        return cam.transform.position.y + cam.orthographicSize;
    }

    private void GetOpenLane(float difficulty, out float openLeft, out float openRight)
    {
        float halfW = HalfPlayWidth();
        float minHalf = Mathf.Min(minLaneWidth * 0.5f, halfW);
        float laneHalf = Mathf.Lerp(halfW * 0.9f, minHalf, difficulty);
        float maxCenter = Mathf.Max(0f, halfW - laneHalf);
        float noise = Mathf.PerlinNoise(aliveTime * LaneNoiseFrequency + 20f, 8.2f);
        float center = Mathf.Lerp(-maxCenter, maxCenter, noise);
        openLeft = center - laneHalf;
        openRight = center + laneHalf;
    }

    private int TargetAliveButtons(float difficulty)
    {
        return Mathf.RoundToInt(Mathf.Lerp(easyAliveButtons, hardAliveButtons, difficulty));
    }

    private void TrySpawnButton()
    {
        if (buttonManager == null) return;

        float difficulty = Difficulty();
        int target = TargetAliveButtons(difficulty);
        float lowestUseful = cam.transform.position.y - cam.orthographicSize + 1f;
        if (nextButtonSpawnY < lowestUseful)
        {
            nextButtonSpawnY = ViewTop() + 0.5f;
        }

        int spawnedThisFrame = 0;
        while (needleManager.AliveButtonCount < target && spawnedThisFrame < 3)
        {
            int before = needleManager.AliveButtonCount;
            if (!TrySpawnButtonGroup(difficulty, target - before)) break;
            spawnedThisFrame++;
        }
    }

    private bool TrySpawnButtonGroup(float difficulty, int room)
    {
        if (room <= 0) return false;

        float horizon = Mathf.Min(ViewTop() + spawnAheadDistance, nextWallBottomY - 1.2f);
        if (nextButtonSpawnY > horizon) return false;

        GetOpenLane(difficulty, out float openLeft, out float openRight);
        float y = nextButtonSpawnY;
        if (!TryPickButtonX(openLeft, openRight, y, false, 0f, out float x))
        {
            nextButtonSpawnY += 0.45f;
            return true;
        }

        SpawnButtonAt(x, y);

        float pairChance = Mathf.Lerp(EasyPairChance, HardPairChance, difficulty);
        if (room >= 2 && Random.value < pairChance)
        {
            float pairY = y + Random.Range(-0.15f, 0.15f);
            if (TryPickButtonX(openLeft, openRight, pairY, true, x, out float pairX))
            {
                SpawnButtonAt(pairX, pairY);
            }
        }

        float gap = Mathf.Lerp(EasyButtonGap, HardButtonGap, difficulty);
        gap += Random.Range(0f, Mathf.Lerp(EasyButtonJitter, HardButtonJitter, difficulty));
        nextButtonSpawnY += Mathf.Max(minButtonGap, gap);
        return true;
    }

    private void SpawnButtonAt(float offsetX, float y)
    {
        Vector2 spawnPos = new Vector2(cam.transform.position.x + offsetX, y);
        buttonManager.SpawnRandomButton(spawnPos);
        spawnedButtonPositions.Add(spawnPos);
    }

    private bool TryPickButtonX(float openLeft, float openRight, float y, bool avoidOther, float avoidOffsetX, out float offsetX)
    {
        float pad = 0.4f;
        float left = openLeft + pad;
        float right = openRight - pad;
        if (right - left < 0.5f)
        {
            left = openLeft;
            right = openRight;
        }

        float camX = cam.transform.position.x;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            float candidate = Random.Range(left, right);
            if (avoidOther && Mathf.Abs(candidate - avoidOffsetX) < 1.6f)
            {
                continue;
            }

            if (!OverlapsWall(new Vector2(camX + candidate, y), ButtonClearance))
            {
                offsetX = candidate;
                return true;
            }
        }

        offsetX = 0f;
        return false;
    }

    private void TrySpawnWall()
    {
        if (ViewTop() + spawnAheadDistance < nextWallBottomY) return;

        float difficulty = Difficulty();
        GetOpenLane(difficulty, out float openLeft, out float openRight);

        float verticalChance = Mathf.Lerp(EasyVerticalChance, HardVerticalChance, difficulty);
        bool wantVertical = Random.value < verticalChance;
        bool placed = wantVertical
            ? TrySpawnVerticalWall(openLeft, openRight, difficulty)
            : TrySpawnHorizontalWall(openLeft, openRight, difficulty);

        if (!placed)
        {
            placed = wantVertical
                ? TrySpawnHorizontalWall(openLeft, openRight, difficulty)
                : TrySpawnVerticalWall(openLeft, openRight, difficulty);
        }

        if (!placed)
        {
            nextWallBottomY += 0.5f;
            return;
        }

        float gap = Mathf.Lerp(EasyWallGap, HardWallGap, difficulty);
        gap += Random.Range(0f, WallGapJitter);
        nextWallBottomY += Mathf.Max(minWallGap, gap);
    }

    private bool TrySpawnVerticalWall(float openLeft, float openRight, float difficulty)
    {
        float halfW = HalfPlayWidth();
        float laneCenter = (openLeft + openRight) * 0.5f;
        bool fillLeft = laneCenter >= 0f;
        if (Random.value < 0.2f)
        {
            fillLeft = !fillLeft;
        }

        if (!TryVerticalSpan(fillLeft, halfW, openLeft, openRight, out float wallLeft, out float wallRight))
        {
            if (!TryVerticalSpan(!fillLeft, halfW, openLeft, openRight, out wallLeft, out wallRight))
            {
                return false;
            }
        }

        float minHeight = Mathf.Lerp(3f, 5f, difficulty);
        float maxHeight = Mathf.Lerp(5.5f, 9f, difficulty);
        float height = Random.Range(minHeight, maxHeight);
        float width = wallRight - wallLeft;
        float centerY = nextWallBottomY + height * 0.5f;
        float centerX = cam.transform.position.x + (wallLeft + wallRight) * 0.5f;
        Vector2 size = new Vector2(width, height);
        if (OverlapsSpawnedButton(new Vector2(centerX, centerY), size * 0.5f))
        {
            return false;
        }

        SpawnWall(new Vector2(centerX, centerY), size);
        nextWallBottomY += height;
        return true;
    }

    private static bool TryVerticalSpan(bool fillLeft, float halfW, float openLeft, float openRight, out float wallLeft, out float wallRight)
    {
        if (fillLeft)
        {
            wallLeft = -halfW;
            wallRight = openLeft;
        }
        else
        {
            wallLeft = openRight;
            wallRight = halfW;
        }

        return wallRight - wallLeft >= 0.55f;
    }

    private bool TrySpawnHorizontalWall(float openLeft, float openRight, float difficulty)
    {
        float laneWidth = openRight - openLeft;
        float maxWidth = laneWidth - HorizontalPassGap;
        if (maxWidth < 0.7f) return false;

        float minWidth = Mathf.Min(1.2f, maxWidth);
        float widthGrow = Mathf.Lerp(minWidth, maxWidth * 0.85f, difficulty);
        float width = Mathf.Min(maxWidth, Random.Range(minWidth, Mathf.Max(minWidth + 0.05f, widthGrow)));
        float height = Random.Range(0.4f, 0.85f);
        float halfWidth = width * 0.5f;
        float xMin = openLeft + halfWidth;
        float xMax = openRight - halfWidth;
        if (xMax < xMin) return false;

        float camX = cam.transform.position.x;
        float centerY = nextWallBottomY + height * 0.5f;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            float offsetX = Random.Range(xMin, xMax);
            Vector2 center = new Vector2(camX + offsetX, centerY);
            Vector2 half = new Vector2(halfWidth, height * 0.5f);
            if (OverlapsSpawnedButton(center, half + Vector2.one * 0.35f))
            {
                continue;
            }

            SpawnWall(center, new Vector2(width, height));
            nextWallBottomY += height;
            return true;
        }

        return false;
    }

    private void SpawnWall(Vector2 worldPos, Vector2 size)
    {
        GameObject wallObj = new GameObject("Wall");
        wallObj.transform.SetParent(contentRoot, false);
        wallObj.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        wallObj.AddComponent<BoxCollider2D>();
        Wall wall = wallObj.AddComponent<Wall>();
        wall.Initialize(needleManager, size);
        spawnedWalls.Add(wallObj);
    }

    private bool OverlapsWall(Vector2 worldPos, float padding)
    {
        for (int i = 0; i < spawnedWalls.Count; i++)
        {
            GameObject wall = spawnedWalls[i];
            if (wall == null) continue;

            Vector2 half = (Vector2)wall.transform.localScale * 0.5f + Vector2.one * padding;
            Vector2 delta = worldPos - (Vector2)wall.transform.position;
            if (Mathf.Abs(delta.x) <= half.x && Mathf.Abs(delta.y) <= half.y)
            {
                return true;
            }
        }

        return false;
    }

    private bool OverlapsSpawnedButton(Vector2 center, Vector2 halfExtents)
    {
        for (int i = 0; i < spawnedButtonPositions.Count; i++)
        {
            Vector2 delta = spawnedButtonPositions[i] - center;
            if (Mathf.Abs(delta.x) <= halfExtents.x + ButtonClearance
                && Mathf.Abs(delta.y) <= halfExtents.y + ButtonClearance)
            {
                return true;
            }
        }

        return false;
    }

    private void CleanupBehindCamera()
    {
        float cutoff = cam.transform.position.y - despawnBehindDistance;
        if (buttonManager != null)
        {
            buttonManager.DespawnButtonsBelow(cutoff);
        }

        for (int i = spawnedButtonPositions.Count - 1; i >= 0; i--)
        {
            if (spawnedButtonPositions[i].y < cutoff)
            {
                spawnedButtonPositions.RemoveAt(i);
            }
        }

        for (int i = spawnedWalls.Count - 1; i >= 0; i--)
        {
            GameObject wall = spawnedWalls[i];
            if (wall == null)
            {
                spawnedWalls.RemoveAt(i);
                continue;
            }

            float bottom = wall.transform.position.y - wall.transform.localScale.y * 0.5f;
            if (bottom < cutoff)
            {
                Destroy(wall);
                spawnedWalls.RemoveAt(i);
            }
        }
    }

    public float CurrentScrollSpeed => ScrollSpeed();
}
