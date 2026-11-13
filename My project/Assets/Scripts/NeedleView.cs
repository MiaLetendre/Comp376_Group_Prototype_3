using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NeedleView : MonoBehaviour
{
    [Header("Sewing Visuals")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform sewingPoint;
    [SerializeField] private int tailNodes = 40;
    [SerializeField] private float tailSegmentLength = 0.2f;
    [SerializeField] private float tailSegmentWidth = 0.01f;
    [SerializeField] private float spriteRotationOffset = -90f;
    [SerializeField, Tooltip("Tip position as normalized coordinates within the visible needle sprite.")]
    private Vector2 needleTipNormalized = new Vector2(0.5f, 1f);
    [SerializeField, Tooltip("Thread start as normalized coordinates within the visible needle sprite.")]
    private Vector2 needleTailNormalized = new Vector2(0.5f, 0f);
    [SerializeField, Min(1), Tooltip("Number of recent movement directions averaged to smooth the needle. Updates while playing.")]
    private int directionSmoothingSamples = 10;
    [SerializeField, Min(0f)] private float minimumDirectionMovementPixels = 2f;
    [SerializeField] private Transform needleSprite;
    [SerializeField] private float needleLerpRotation = 0.5f;

    private Vector2[] threadNodes;
    private Vector2[] directionHistory;
    private Vector2 lastDirectionSampleScreenPosition;
    private Vector2 lastNeedleDirection = Vector2.right;
    private int directionHistoryIndex;
    private int directionHistoryCount;
    private float lastMovementAngle;

    public void Initialize()
    {
        threadNodes = new Vector2[Mathf.Max(1, tailNodes)];
        directionHistory = new Vector2[Mathf.Max(1, directionSmoothingSamples)];

        Vector2 initialPosition = Vector2.zero;
        if (Mouse.current != null && Camera.main != null)
        {
            Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
            lastDirectionSampleScreenPosition = mouseScreenPosition;
            initialPosition = GetMouseWorldPosition(mouseScreenPosition);
        }

        for (int i = 0; i < threadNodes.Length; i++)
        {
            threadNodes[i] = initialPosition;
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        lineRenderer.positionCount = threadNodes.Length;
        lineRenderer.startWidth = tailSegmentWidth;
        lineRenderer.endWidth = tailSegmentWidth;
        if (needleSprite != null)
        {
            needleSprite.rotation = Quaternion.Euler(0f, 0f, spriteRotationOffset);
        }
    }

    public void UpdateView(Vector3 mouseScreenPosition, float deltaTime)
    {
        EnsureDirectionHistorySize();
        UpdateNeedleDirection(mouseScreenPosition);

        float movementAngle = Mathf.Atan2(lastNeedleDirection.y, lastNeedleDirection.x) * Mathf.Rad2Deg;
        movementAngle = Mathf.LerpAngle(
            lastMovementAngle,
            movementAngle,
            needleLerpRotation * deltaTime * 60f);

        if (needleSprite != null)
        {
            needleSprite.rotation = Quaternion.Euler(
                0f,
                0f,
                movementAngle + spriteRotationOffset);
            PositionNeedleHeadAt(GetMouseWorldPosition(mouseScreenPosition));
        }

        UpdateThreadNodes();
        RenderThread();
        lastMovementAngle = movementAngle;
    }

    public Vector2 GetMouseWorldPosition(Vector3 mouseScreenPosition)
    {
        float zDistance = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, zDistance));
        return new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
    }

    public Vector2 GetThreadNodePosition(int index)
    {
        return threadNodes[index];
    }

    public int ThreadNodeCount => threadNodes != null ? threadNodes.Length : 0;

    public Transform GetNeedleTransform()
    {
        return needleSprite;
    }

    public Vector2 GetNeedleHeadPosition()
    {
        if (needleSprite == null)
        {
            return Vector2.zero;
        }

        if (needleSprite is RectTransform)
        {
            return needleSprite.TransformPoint(GetNeedlePointLocalPosition(needleTipNormalized));
        }

        return needleSprite.position;
    }

    public void SynchronizeNeedlePosition()
    {
        if (needleSprite == null)
        {
            return;
        }

        if (sewingPoint != null)
        {
            sewingPoint.position = GetNeedleHeadPosition();
        }
        if (threadNodes != null && threadNodes.Length > 0)
        {
            threadNodes[0] = GetNeedleTailPosition();
            RenderThread();
        }
    }

    private void PositionNeedleHeadAt(Vector2 worldPosition)
    {
        Vector3 tipOffset = needleSprite.TransformVector(GetNeedlePointLocalPosition(needleTipNormalized));
        needleSprite.position = (Vector3)worldPosition - tipOffset;
        if (sewingPoint != null && sewingPoint != needleSprite)
        {
            sewingPoint.position = worldPosition;
        }
    }

    private Vector3 GetNeedlePointLocalPosition(Vector2 normalizedPosition)
    {
        if (!(needleSprite is RectTransform rect))
        {
            return Vector3.zero;
        }

        Rect visibleRect = rect.rect;
        Image image = needleSprite.GetComponent<Image>();
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

    private Vector2 GetNeedleTailPosition()
    {
        if (needleSprite == null)
        {
            return Vector2.zero;
        }

        if (needleSprite is RectTransform)
        {
            return needleSprite.TransformPoint(GetNeedlePointLocalPosition(needleTailNormalized));
        }

        return needleSprite.position;
    }

    private void UpdateNeedleDirection(Vector2 screenPosition)
    {
        Vector2 directionDelta = screenPosition - lastDirectionSampleScreenPosition;
        if (directionDelta.magnitude < minimumDirectionMovementPixels)
        {
            return;
        }

        directionHistory[directionHistoryIndex] = directionDelta.normalized;
        directionHistoryIndex = (directionHistoryIndex + 1) % directionHistory.Length;
        directionHistoryCount = Mathf.Min(directionHistoryCount + 1, directionHistory.Length);
        lastDirectionSampleScreenPosition = screenPosition;

        Vector2 averageDirection = Vector2.zero;
        for (int i = 0; i < directionHistoryCount; i++)
        {
            averageDirection += directionHistory[i];
        }

        if (averageDirection.sqrMagnitude > 0.000001f)
        {
            lastNeedleDirection = averageDirection.normalized;
        }
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

    private void UpdateThreadNodes()
    {
        threadNodes[0] = GetNeedleTailPosition();
        for (int i = 1; i < threadNodes.Length; i++)
        {
            float nodeAngle = Mathf.Atan2(
                threadNodes[i - 1].y - threadNodes[i].y,
                threadNodes[i - 1].x - threadNodes[i].x);
            threadNodes[i] = new Vector2(
                threadNodes[i - 1].x - Mathf.Cos(nodeAngle) * tailSegmentLength,
                threadNodes[i - 1].y - Mathf.Sin(nodeAngle) * tailSegmentLength);
        }
    }

    private void RenderThread()
    {
        Vector3[] linePositions = new Vector3[threadNodes.Length];
        for (int i = 0; i < threadNodes.Length; i++)
        {
            linePositions[i] = new Vector3(threadNodes[i].x, threadNodes[i].y, 0f);
        }
        lineRenderer.SetPositions(linePositions);
    }
}
