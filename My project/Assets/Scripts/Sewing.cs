using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Sewing : MonoBehaviour
{
    private const int MaxNeedleHealth = 3;

    [Header("Sewing Settings")]
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] Transform sewingPoint;
    [SerializeField] LayerMask clothPoints;
    [SerializeField] int tailNodes = 40;
    [SerializeField] float tailSegmentLength = .2f;
    [SerializeField] float tailSegmentWidth = .01f;
    [SerializeField] float spriteRotaionOffset = -90f;
    [SerializeField, Tooltip("Tip position as normalized coordinates within the visible needle sprite.")]
    Vector2 needleTipNormalized = new Vector2(0.5f, 1f);
    [SerializeField, Tooltip("Thread start as normalized coordinates within the visible needle sprite.")]
    Vector2 needleTailNormalized = new Vector2(0.5f, 0f);
    [SerializeField, Min(1), Tooltip("Number of recent movement directions averaged to smooth the needle. Updates while playing.")]
    int directionSmoothingSamples = 10;
    [SerializeField, Min(0f)] float minimumDirectionMovementPixels = 2f;
    [SerializeField] Transform needleSpriteOriginal;

    [Header("Growth (harder control over time)")]
    [SerializeField] int maxTailNodes = 90;
    [SerializeField] int nodesGainedPerButton = 3;
    [SerializeField] float widthGainPerButton = 0.003f;
    [SerializeField] float lengthGainPerButton = 0.008f;
    [SerializeField] float maxTailSegmentWidth = 0.06f;
    [SerializeField] float maxTailSegmentLength = 0.45f;

    [Header("Needle")]
    [SerializeField] float needleLerpRotation = 0.5f;

    [Header("Button Additives")]
    [SerializeField] float waitTimeStrike = 5; //slwos down time for x seconds after getting a strike buttoin

    [Header("Damage Effects")]
    [SerializeField, Min(0f)] float dynamiteScreenShakeDuration = 0.35f;
    [SerializeField, Min(0f)] float dynamiteScreenShakeMagnitude = 0.18f;

    private struct Node
    {
        public Vector2 position;
    }

    private Node[] nodesArray;
    private Vector2[] directionHistory;
    private Vector2 lastDirectionSampleScreenPosition;
    private Vector2 lastNeedleDirection = Vector2.right;
    private int directionHistoryIndex;
    private int directionHistoryCount;
    private Vector2 lastMouseWorldPos;
    private NeedleData needleSprite;
    private bool previousCursorVisibility;
    private SewingButtonManager buttonManager;
    private float lastMovmentAngle = 0f;
    private Transform shakeCamera;
    private Vector3 cameraShakeOffset;
    private float cameraShakeTimer;

    public int ButtonsCollected => buttonManager != null ? buttonManager.ButtonsCollected : 0;
    public int Score => buttonManager != null ? buttonManager.Score : 0;
    public event System.Action<Vector2> DynamiteCaptured
    {
        add { if (buttonManager != null) buttonManager.DynamiteCaptured += value; }
        remove { if (buttonManager != null) buttonManager.DynamiteCaptured -= value; }
    }

    public int AliveButtonCount => buttonManager != null ? buttonManager.AliveButtonCount : 0;

    public int AliveButtonCountOf(string typeId)
    {
        return buttonManager != null ? buttonManager.AliveButtonCountOf(typeId) : 0;
    }

    public class NeedleData
    {
        public Transform needleTransform;
        public int health = MaxNeedleHealth;
    }

    private void Awake()
    {
        buttonManager = GetComponent<SewingButtonManager>();
        if (buttonManager == null)
        {
            Debug.LogError("SewingButtonManager must be added and configured on the Sewing GameObject.", this);
            return;
        }
        buttonManager.Initialize(this);
    }

    void Start()
    {
        previousCursorVisibility = Cursor.visible;
        Cursor.visible = false;

        nodesArray = new Node[tailNodes];
        directionHistory = new Vector2[Mathf.Max(1, directionSmoothingSamples)];
        Vector2 initialPosition = Vector2.zero;

        needleSprite = new NeedleData();
        needleSprite.needleTransform = needleSpriteOriginal;

        if (Mouse.current != null)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            lastDirectionSampleScreenPosition = mouseScreen;
            float zDistance = Mathf.Abs(Camera.main.transform.position.z);
            mouseScreen.z = zDistance;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDistance));
            initialPosition = new Vector2(mouseWorld.x, mouseWorld.y);
        }

        lastMouseWorldPos = initialPosition;
        for (int i = 0; i < tailNodes; i++)
        {
            nodesArray[i].position = initialPosition;
        }
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.GetComponent<LineRenderer>();

            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }
        lineRenderer.positionCount = tailNodes;
        lineRenderer.startWidth = tailSegmentWidth;
        lineRenderer.endWidth = tailSegmentWidth;
        needleSprite.needleTransform.rotation = Quaternion.Euler(0, 0, spriteRotaionOffset);

    }

    void Update()
    {
        if (needleSprite == null || needleSprite.health <= 0)
        {
            if (needleSprite != null) Cursor.visible = true;
            return;
        }

        if (Mouse.current == null || Camera.main == null) return;
        EnsureDirectionHistorySize();

        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        float zDistance = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, zDistance));
        Vector2 worldMousePos = new Vector2(mouseWorld.x, mouseWorld.y);
        Vector2 previousMouseWorldPos = lastMouseWorldPos;

        Vector2 screenPosition = mouseScreen;
        Vector2 directionDelta = screenPosition - lastDirectionSampleScreenPosition;
        if (directionDelta.magnitude >= minimumDirectionMovementPixels)
        {
            UpdateNeedleDirection(directionDelta.normalized);
            lastDirectionSampleScreenPosition = screenPosition;
        }

        float movementAngle = Mathf.Atan2(lastNeedleDirection.y, lastNeedleDirection.x) * Mathf.Rad2Deg;
        //movementAngle = Mathf.LerpAngle(lastMovmentAngle, movementAngle, 0.5f);
        movementAngle = Mathf.LerpAngle(lastMovmentAngle, movementAngle, needleLerpRotation * Time.deltaTime * 60f);

        needleSprite.needleTransform.rotation = Quaternion.Euler(
            0f,
            0f,
            movementAngle + spriteRotaionOffset);
        PositionNeedleHeadAt(worldMousePos);

        nodesArray[0] = new Node { position = GetNeedleTailPosition() };
        for (int i = 1; i < tailNodes; i++)
        {

            float nodeAngle = Mathf.Atan2(
                nodesArray[i - 1].position.y - nodesArray[i].position.y,
                nodesArray[i - 1].position.x - nodesArray[i].position.x);
            nodesArray[i] = new Node
            {
                position = new Vector2(
                    nodesArray[i - 1].position.x - Mathf.Cos(nodeAngle) * tailSegmentLength,
                    nodesArray[i - 1].position.y - Mathf.Sin(nodeAngle) * tailSegmentLength)
            };
        }

        Vector3[] linePositions = new Vector3[tailNodes];
        for (int i = 0; i < tailNodes; i++)
        {
            linePositions[i] = new Vector3(nodesArray[i].position.x, nodesArray[i].position.y, 0);
        }
        lineRenderer.SetPositions(linePositions);

        bool threadCrossingStarted = TryFindNeedleThreadCrossing(
            previousMouseWorldPos,
            worldMousePos,
            out _);
        if (buttonManager != null)
        {
            buttonManager.ProcessButtonCollection(
                previousMouseWorldPos,
                worldMousePos,
                threadCrossingStarted);
        }
        lastMouseWorldPos = worldMousePos;
        lastMovmentAngle = movementAngle;
    }

    private void OnDisable()
    {
        Cursor.visible = previousCursorVisibility;
        StopScreenShake();
    }

    private void LateUpdate()
    {
        if (shakeCamera == null && cameraShakeTimer <= 0f)
        {
            return;
        }

        if (shakeCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            shakeCamera = mainCamera.transform;
        }

        // Remove the previous frame's offset so camera movement (such as autoscroll) remains intact.
        Vector3 basePosition = shakeCamera.position - cameraShakeOffset;
        cameraShakeTimer = Mathf.Max(0f, cameraShakeTimer - Time.unscaledDeltaTime);
        if (cameraShakeTimer > 0f && dynamiteScreenShakeDuration > 0f)
        {
            float intensity = cameraShakeTimer / dynamiteScreenShakeDuration;
            Vector2 randomOffset = Random.insideUnitCircle * (dynamiteScreenShakeMagnitude * intensity);
            cameraShakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
        }
        else
        {
            cameraShakeOffset = Vector3.zero;
        }

        shakeCamera.position = basePosition + cameraShakeOffset;
        if (cameraShakeTimer <= 0f)
        {
            shakeCamera = null;
        }
    }

    private void PositionNeedleHeadAt(Vector2 worldPosition)
    {
        Transform needle = needleSprite.needleTransform;
        Vector3 tipOffset = needle.TransformVector(GetNeedlePointLocalPosition(needleTipNormalized));
        needle.position = (Vector3)worldPosition - tipOffset;
        if (sewingPoint != null && sewingPoint != needle)
        {
            sewingPoint.position = worldPosition;
        }
    }

    private Vector3 GetNeedlePointLocalPosition(Vector2 normalizedPosition)
    {
        if (!(needleSprite.needleTransform is RectTransform rect))
        {
            return Vector3.zero;
        }

        Rect visibleRect = rect.rect;
        Image image = needleSprite.needleTransform.GetComponent<Image>();
        if (image != null && image.preserveAspect && image.sprite != null)
        {
            float spriteAspect = image.sprite.rect.width / image.sprite.rect.height;
            float rectAspect = visibleRect.width / visibleRect.height;
            if (spriteAspect > rectAspect)
            {
                float height = visibleRect.width / spriteAspect;
                visibleRect.yMin += (visibleRect.height - height) * 0.5f;
                visibleRect.yMax = visibleRect.yMin + height;
            }
            else
            {
                float width = visibleRect.height * spriteAspect;
                visibleRect.xMin += (visibleRect.width - width) * 0.5f;
                visibleRect.xMax = visibleRect.xMin + width;
            }
        }

        return new Vector3(
            Mathf.Lerp(visibleRect.xMin, visibleRect.xMax, normalizedPosition.x),
            Mathf.Lerp(visibleRect.yMin, visibleRect.yMax, normalizedPosition.y),
            0f);
    }

    private void UpdateNeedleDirection(Vector2 movementDirection)
    {
        directionHistory[directionHistoryIndex] = movementDirection;
        directionHistoryIndex = (directionHistoryIndex + 1) % directionHistory.Length;
        directionHistoryCount = Mathf.Min(directionHistoryCount + 1, directionHistory.Length);

        Vector2 averageDirection = Vector2.zero;
        for (int i = 0; i < directionHistoryCount; i++)
        {
            averageDirection += directionHistory[i];
        }

        if (averageDirection.sqrMagnitude <= 0.000001f)
        {
            return;
        }
        averageDirection /= directionHistoryCount;
        lastNeedleDirection = averageDirection.normalized;

    }

    private void EnsureDirectionHistorySize()
    {
        int requestedSize = Mathf.Max(1, directionSmoothingSamples);
        if (directionHistory.Length == requestedSize)
        {
            return;
        }

        int retainedCount = Mathf.Min(directionHistoryCount, requestedSize);
        Vector2[] resizedHistory = new Vector2[requestedSize];
        int oldestIndex = (directionHistoryIndex - retainedCount + directionHistory.Length) % directionHistory.Length;
        for (int i = 0; i < retainedCount; i++)
        {
            resizedHistory[i] = directionHistory[(oldestIndex + i) % directionHistory.Length];
        }

        directionHistory = resizedHistory;
        directionHistoryCount = retainedCount;
        directionHistoryIndex = retainedCount % requestedSize;
    }

    private bool TryFindNeedleThreadCrossing(
        Vector2 previousHeadPosition,
        Vector2 currentHeadPosition,
        out Vector2 crossingPosition)
    {
        crossingPosition = Vector2.zero;
        if ((currentHeadPosition - previousHeadPosition).sqrMagnitude <= 0.00000001f)
        {
            return false;
        }

        for (int i = 9; i < tailNodes; i++)
        {
            Vector2 threadStart = nodesArray[i - 1].position;
            Vector2 threadEnd = nodesArray[i].position;
            if (TryGetSegmentIntersection(
                    previousHeadPosition,
                    currentHeadPosition,
                    threadStart,
                    threadEnd,
                    out crossingPosition))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetSegmentIntersection(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d,
        out Vector2 intersection)
    {
        intersection = Vector2.zero;
        Vector2 firstDirection = b - a;
        Vector2 secondDirection = d - c;
        float denominator = Cross(firstDirection, secondDirection);
        if (Mathf.Abs(denominator) < 0.000001f)
        {
            return false;
        }

        Vector2 offset = c - a;
        float firstAmount = Cross(offset, secondDirection) / denominator;
        float secondAmount = Cross(offset, firstDirection) / denominator;
        if (firstAmount < 0f || firstAmount > 1f || secondAmount < 0f || secondAmount > 1f)
        {
            return false;
        }

        intersection = a + firstDirection * firstAmount;
        return true;
    }

    private static float Cross(Vector2 first, Vector2 second)
    {
        return first.x * second.y - first.y * second.x;
    }

    private void TriggerScreenShake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        if (shakeCamera != mainCamera.transform && shakeCamera != null)
        {
            shakeCamera.position -= cameraShakeOffset;
            cameraShakeOffset = Vector3.zero;
        }

        shakeCamera = mainCamera.transform;
        cameraShakeTimer = dynamiteScreenShakeDuration;
    }

    private void StopScreenShake()
    {
        if (shakeCamera != null && cameraShakeOffset != Vector3.zero)
        {
            shakeCamera.position -= cameraShakeOffset;
        }

        shakeCamera = null;
        cameraShakeOffset = Vector3.zero;
        cameraShakeTimer = 0f;
    }

    void GrowTail()
    {
        int newCount = Mathf.Min(maxTailNodes, tailNodes + nodesGainedPerButton);
        if (newCount > tailNodes)
        {
            Node[] grown = new Node[newCount];
            for (int i = 0; i < tailNodes; i++)
            {
                grown[i] = nodesArray[i];
            }

            Vector2 tip = nodesArray[tailNodes - 1].position;
            for (int i = tailNodes; i < newCount; i++)
            {
                grown[i].position = tip;
            }

            nodesArray = grown;
            tailNodes = newCount;
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = tailNodes;
            }
        }

        tailSegmentWidth = Mathf.Min(maxTailSegmentWidth, tailSegmentWidth + widthGainPerButton);
        tailSegmentLength = Mathf.Min(maxTailSegmentLength, tailSegmentLength + lengthGainPerButton);
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = tailSegmentWidth;
            lineRenderer.endWidth = tailSegmentWidth;
        }
    }

    public void SpawnRandomButton(Vector2 position)
    {
        if (buttonManager != null)
        {
            buttonManager.SpawnRandomButton(position);
        }
    }

    public void DespawnButtonsBelow(float worldY)
    {
        if (buttonManager != null)
        {
            buttonManager.DespawnButtonsBelow(worldY);
        }
    }

    public Transform GetNeedleTransform()
    {
        return needleSprite != null ? needleSprite.needleTransform : null;
    }

    public Vector2 GetNeedleHeadPosition()
    {
        if (needleSprite == null || needleSprite.needleTransform == null)
        {
            return Vector2.zero;
        }

        Transform needle = needleSprite.needleTransform;
        if (needle is RectTransform rect)
        {
            return needle.TransformPoint(GetNeedlePointLocalPosition(needleTipNormalized));
        }

        return needle.position;
    }

    private Vector2 GetNeedleTailPosition()
    {
        if (needleSprite == null || needleSprite.needleTransform == null)
        {
            return Vector2.zero;
        }

        Transform needle = needleSprite.needleTransform;
        if (needle is RectTransform rect)
        {
            return needle.TransformPoint(GetNeedlePointLocalPosition(needleTailNormalized));
        }

        return needle.position;
    }

    public int GetNeedleHealth()
    {
        return needleSprite != null ? needleSprite.health : 0;
    }

    public void RestoreNeedleHealth(int amount)
    {
        if (needleSprite == null || needleSprite.health <= 0) return;

        needleSprite.health = Mathf.Min(MaxNeedleHealth, needleSprite.health + amount);
    }

    public bool IsNeedleAlive()
    {
        return needleSprite != null && needleSprite.health > 0;
    }

    public void DamageNeedle(int amount, Vector2 knockback)
    {
        if (needleSprite == null || needleSprite.health <= 0) return;

        int previousHealth = needleSprite.health;
        needleSprite.health = Mathf.Max(0, needleSprite.health - amount);
        if (needleSprite.health < previousHealth)
        {
            TriggerScreenShake();
        }

        if (needleSprite.needleTransform != null && knockback.sqrMagnitude > 0.0001f)
        {
            needleSprite.needleTransform.position += (Vector3)knockback;
            if (sewingPoint != null)
            {
                sewingPoint.position = GetNeedleHeadPosition();
            }
            if (nodesArray != null && nodesArray.Length > 0)
            {
                nodesArray[0].position = GetNeedleTailPosition();
            }
        }

        if (needleSprite.health <= 0)
        {
            Debug.Log("Needle broke! Buttons collected: " + ButtonsCollected);
        }
    }

    public void slowTime()
    {
        Time.timeScale = 0.5f;
        StartCoroutine(ResetTimeScale());
    }

    IEnumerator ResetTimeScale()
    {
        yield return new WaitForSecondsRealtime(waitTimeStrike);
        Time.timeScale = 1f;
    }


}
