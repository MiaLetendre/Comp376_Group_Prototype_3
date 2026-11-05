using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Sewing : MonoBehaviour
{

    [Header("Sewing Settings")]
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] Transform sewingPoint;
    [SerializeField] LayerMask clothPoints;
    [SerializeField] int tailNodes = 40;
    [SerializeField] float tailSegmentLength = .2f;
    [SerializeField] float tailSegmentWidth = .01f;
    [SerializeField] float spriteRotaionOffset = 0f;
    [SerializeField] float rotationSpeed = 15f;
    [SerializeField] Transform needleSpriteOriginal;

    [Header("Growth (harder control over time)")]
    [SerializeField] int maxTailNodes = 90;
    [SerializeField] int nodesGainedPerButton = 3;
    [SerializeField] float widthGainPerButton = 0.003f;
    [SerializeField] float lengthGainPerButton = 0.008f;
    [SerializeField] float maxTailSegmentWidth = 0.06f;
    [SerializeField] float maxTailSegmentLength = 0.45f;

    [Header("Button Spawning")]
    [SerializeField] Transform buttonParent;
    [SerializeField] List<ButtonSpawnOption> buttonTypes = new List<ButtonSpawnOption>();
    [SerializeField] float buttonDistanceThreshold = 2f;

    [System.Serializable]
    public class ButtonSpawnOption
    {
        public GameObject prefab;
        [Min(0f)] public float targetShare = 0.25f;
        public bool spawnAtStart;
        public Vector2 initialPosition;
    }

    private bool touchingString = false;
    private bool wasTouchingString = false;

    private struct Node
    {
        public Vector2 position;
    }

    private Node[] nodesArray;
    private bool isSewing = false;
    private Vector2 lastMouseWorldPos;
    private Vector2 lastNeedleDir;
    private float targetAngel;
    private NeedleData needleSprite;
    private int buttonsCollected;
    private readonly Dictionary<GameObject, ButtonCollector> buttons = new Dictionary<GameObject, ButtonCollector>();

    public int ButtonsCollected => buttonsCollected;

    public int AliveButtonCount
    {
        get
        {
            int count = 0;
            foreach (GameObject button in buttons.Keys)
            {
                if (button != null) count++;
            }
            return count;
        }
    }

    public int AliveButtonCountOf(string typeId)
    {
        int count = 0;
        foreach (KeyValuePair<GameObject, ButtonCollector> entry in buttons)
        {
            if (entry.Key == null || entry.Value == null) continue;
            if (entry.Value.ButtonTypeId == typeId) count++;
        }
        return count;
    }

    public class NeedleData
    {
        public Transform needleTransform;
        public int health = 3;
    }
   
    void Start()
    {
        nodesArray = new Node[tailNodes];
        Vector2 initialPosition = Vector2.zero;

        needleSprite = new NeedleData();
        needleSprite.needleTransform = needleSpriteOriginal;

        if (Mouse.current != null)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            float zDistance = Mathf.Abs(Camera.main.transform.position.z);
            mouseScreen.z = zDistance;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDistance));
            initialPosition = new Vector2(mouseWorld.x, mouseWorld.y);
            lastNeedleDir = Vector2.right;
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
        targetAngel = Mathf.Atan2(lastNeedleDir.y, lastNeedleDir.x) * Mathf.Rad2Deg;

        foreach (ButtonSpawnOption option in buttonTypes)
        {
            if (option != null && option.spawnAtStart)
            {
                SpawnButton(option.initialPosition, option);
            }
        }
    }

    void Update()
    {
        if (needleSprite == null || needleSprite.health <= 0)
        {
            return;
        }

        if (Mouse.current != null)
        {
            isSewing = true;
        }
        

        if (isSewing && Mouse.current != null)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            float zDistance = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDistance));
            Vector2 worldMousePos = new Vector2(mouseWorld.x, mouseWorld.y);

            Vector2 previousMouseWorldPos = lastMouseWorldPos;
            Vector2 movementDelta = worldMousePos - lastMouseWorldPos;
            if (movementDelta.magnitude >.00005f)
            {
                lastNeedleDir = movementDelta.normalized;
                targetAngel = Mathf.Atan2(lastNeedleDir.y, lastNeedleDir.x) * Mathf.Rad2Deg;
            }
            

            needleSprite.needleTransform.position = worldMousePos;
            if (sewingPoint != null)
            {
                sewingPoint.position = worldMousePos;
            }

            Vector2 needleDir = nodesArray[0].position - nodesArray[1].position;
            if (needleDir.magnitude > 0.0001f)
            {
                targetAngel = Mathf.Atan2(needleDir.y, needleDir.x) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.Euler(0, 0, targetAngel + spriteRotaionOffset); 
                needleSprite.needleTransform.rotation = Quaternion.Lerp(needleSprite.needleTransform.rotation, rotation, Time.deltaTime * rotationSpeed);
            }
            

            nodesArray[0] = new Node
            {
                position = worldMousePos
            };

            Vector3[] linePositions = new Vector3[tailNodes];
            linePositions[0] = new Vector3(worldMousePos.x, worldMousePos.y, 0);
            touchingString = false;

            for (int i = 1; i < tailNodes; i++)
            {
                float nodeAngle = Mathf.Atan2(nodesArray[i - 1].position.y - nodesArray[i].position.y, nodesArray[i - 1].position.x - nodesArray[i].position.x);
                linePositions[i] = new Vector3(nodesArray[i].position.x, nodesArray[i].position.y, 0);
                nodesArray[i] = new Node
                {
                    position = new Vector2(
                        nodesArray[i - 1].position.x - Mathf.Cos(nodeAngle) * tailSegmentLength, 
                        nodesArray[i - 1].position.y - Mathf.Sin(nodeAngle) * tailSegmentLength
                    )
                };

                if (!touchingString && i > 8 && i < tailNodes - 1)
                {
                    Vector2 a = nodesArray[0].position;
                    Vector2 b = nodesArray[1].position;
                    Vector2 c = nodesArray[i - 1].position;
                    Vector2 d = nodesArray[i].position;
                    touchingString = CheckIntersection(a, c, d) != CheckIntersection(b, c, d) && CheckIntersection(a, b ,c ) != CheckIntersection (a , b ,d);
                    
                }
                
                
                linePositions[i] = new Vector3(nodesArray[i].position.x, nodesArray[i].position.y, 0);
            }
            lineRenderer.SetPositions(linePositions);
            ProcessButtonCollection(
                previousMouseWorldPos,
                worldMousePos,
                touchingString && !wasTouchingString);
            wasTouchingString = touchingString;
            lastMouseWorldPos = worldMousePos;
        }
    }

    bool CheckIntersection(Vector2 a, Vector2 b, Vector2 c){
        return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
    }

    private void ProcessButtonCollection(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted)
    {
        if (buttons.Count == 0) return;

        List<GameObject> buttonKeys = new List<GameObject>(buttons.Keys);
        for (int i = 0; i < buttonKeys.Count; i++)
        {
            GameObject button = buttonKeys[i];
            if (button == null || !buttons.TryGetValue(button, out ButtonCollector collector) || collector == null)
            {
                buttons.Remove(button);
                continue;
            }

            if (!collector.TryCollect(
                    previousNeedlePosition,
                    currentNeedlePosition,
                    threadCrossingStarted,
                    buttonDistanceThreshold))
            {
                continue;
            }

            buttons.Remove(button);
            Destroy(button);
            OnButtonCollected();
            break;
        }
    }

    private void OnButtonCollected()
    {
        buttonsCollected++;
        GrowTail();
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
        List<ButtonSpawnOption> availableOptions = new List<ButtonSpawnOption>();
        float totalWeight = 0f;
        int aliveCount = Mathf.Max(1, AliveButtonCount);

        foreach (ButtonSpawnOption option in buttonTypes)
        {
            if (option == null || option.prefab == null || option.targetShare <= 0f) continue;
            ButtonCollector collector = option.prefab.GetComponent<ButtonCollector>();
            if (collector == null || string.IsNullOrWhiteSpace(collector.ButtonTypeId))
            {
                Debug.LogError($"Button prefab '{option.prefab.name}' needs a ButtonCollector with a type ID.", option.prefab);
                continue;
            }

            float share = AliveButtonCountOf(collector.ButtonTypeId) / (float)aliveCount;
            float weight = option.targetShare / (0.35f + share);
            if (weight <= 0f) continue;
            availableOptions.Add(option);
            totalWeight += weight;
        }

        if (availableOptions.Count == 0)
        {
            Debug.LogError("No valid button prefabs are configured on Sewing.", this);
            return;
        }

        float roll = Random.value * totalWeight;
        foreach (ButtonSpawnOption option in availableOptions)
        {
            ButtonCollector collector = option.prefab.GetComponent<ButtonCollector>();
            float share = AliveButtonCountOf(collector.ButtonTypeId) / (float)aliveCount;
            roll -= option.targetShare / (0.35f + share);
            if (roll <= 0f)
            {
                SpawnButton(position, option);
                return;
            }
        }

        SpawnButton(position, availableOptions[availableOptions.Count - 1]);
    }

    private void SpawnButton(Vector2 position, ButtonSpawnOption option)
    {
        if (option == null || option.prefab == null)
        {
            Debug.LogError("A button spawn option has no prefab assigned.", this);
            return;
        }

        Transform parent = buttonParent != null ? buttonParent : transform;
        GameObject newButton = Instantiate(option.prefab, position, Quaternion.identity, parent);
        ButtonCollector collector = newButton.GetComponent<ButtonCollector>();
        if (collector == null || string.IsNullOrWhiteSpace(collector.ButtonTypeId))
        {
            Debug.LogError($"Spawned button '{newButton.name}' needs a ButtonCollector with a type ID.", newButton);
            Destroy(newButton);
            return;
        }

        collector.Initialize();
        MonoBehaviour[] components = newButton.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component is IButtonSpawnInitializer initializer)
            {
                initializer.Initialize(this, GetNeedleTransform());
            }
        }

        buttons.Add(newButton, collector);
    }

    public void DespawnButtonsBelow(float worldY)
    {
        if (buttons == null || buttons.Count == 0) return;

        List<GameObject> buttonKeys = new List<GameObject>(buttons.Keys);
        for (int i = 0; i < buttonKeys.Count; i++)
        {
            GameObject button = buttonKeys[i];
            if (button == null)
            {
                buttons.Remove(button);
                continue;
            }

            if (button.transform.position.y < worldY)
            {
                buttons.Remove(button);
                Destroy(button);
            }
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
            Vector3 localHead = new Vector3(rect.rect.xMax, rect.rect.center.y, 0f);
            return needle.TransformPoint(localHead);
        }

        return needle.position;
    }

    public int GetNeedleHealth()
    {
        return needleSprite != null ? needleSprite.health : 0;
    }

    public bool IsNeedleAlive()
    {
        return needleSprite != null && needleSprite.health > 0;
    }

    public void DamageNeedle(int amount, Vector2 knockback)
    {
        if (needleSprite == null || needleSprite.health <= 0) return;

        needleSprite.health = Mathf.Max(0, needleSprite.health - amount);

        if (needleSprite.needleTransform != null && knockback.sqrMagnitude > 0.0001f)
        {
            needleSprite.needleTransform.position += (Vector3)knockback;
            if (sewingPoint != null)
            {
                sewingPoint.position = needleSprite.needleTransform.position;
            }
            if (nodesArray != null && nodesArray.Length > 0)
            {
                nodesArray[0].position = needleSprite.needleTransform.position;
            }
        }

        if (needleSprite.health <= 0)
        {
            Debug.Log("Needle broke! Buttons collected: " + buttonsCollected);
        }
    }

    
}
