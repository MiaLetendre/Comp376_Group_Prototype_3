using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
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
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] Transform buttonParent;
    [SerializeField] Dictionary<GameObject, ButtonData> buttons = new Dictionary<GameObject, ButtonData>();
    [SerializeField] float buttonDistanceThreshold = 2f;

    [Header("Combat")]
    [SerializeField] float attackerContactRadius = 1.1f;
    [SerializeField] float attackerDamageCooldown = 0.9f;

    private bool touchingString = false;

    //the sewing mechanic is jsut a lijked set of nodes, slowly influincing the one behind it
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
    private float attackerLastHitTime = -999f;

    public int ButtonsCollected => buttonsCollected;

    public enum ButtonType
    {
        Regular,
        Sheild,
        Attacker,
        Dodger
    }

    public class ButtonData
    {
        public ButtonType buttonType;
        public int wrapsRequired;
        public int currentWraps = 0;
        public Color buttonColor;
        public float lastWrapTime =  -999f;

        public ButtonData(ButtonType type)
        {
            buttonType = type;
            wrapsRequired = (buttonType == ButtonType.Sheild) ? 2 : 1;
            if (buttonType == ButtonType.Regular)
            {
                buttonColor = Color.black;
            }
            else if (buttonType == ButtonType.Sheild)
            {
                buttonColor = Color.blue;
            }
            else if (buttonType == ButtonType.Attacker)
            {
                buttonColor = Color.red;
            }
            else if (buttonType == ButtonType.Dodger)
            {
                buttonColor = Color.green;
            }
        }
    }

    public class NeedleData
    {
        public Transform needleTransform;
        public int health = 3;
    }
   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        Vector2 initialButtonPos = Vector2.zero;
        SpawnButton(initialButtonPos, ButtonType.Regular);
        SpawnButton(initialButtonPos + new Vector2(4f, 0f), ButtonType.Sheild);
        SpawnButton(initialButtonPos + new Vector2(2f, 2.5f), ButtonType.Dodger);


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




    }

    // Update is called once per frame
    void Update()
    {
        if (needleSprite == null || needleSprite.health <= 0)
        {
            return;
        }

        Vector3 mouseScreen = Vector3.zero;
        float zDist = 0f;
        if (Mouse.current != null)
        {
            isSewing = true;
            mouseScreen = Mouse.current.position.ReadValue();
            zDist = -Camera.main.transform.position.z;
            lastMouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDist));
        }
        

        if (isSewing && Mouse.current != null)
        {
            mouseScreen = Mouse.current.position.ReadValue();
            float zDistance = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDistance));
            Vector2 worldMousePos = new Vector2(mouseWorld.x, mouseWorld.y);

            Vector2 movementDelta = worldMousePos - lastMouseWorldPos;
            if (movementDelta.magnitude >.00005f)
            {
                lastNeedleDir = movementDelta.normalized;
                targetAngel = Mathf.Atan2(lastNeedleDir.y, lastNeedleDir.x) * Mathf.Rad2Deg;
            }
            
            //record last frame mous position, then track new one and make needle rotate to face the direction of movement

            lastMouseWorldPos = mouseWorld;
            needleSprite.needleTransform.position = worldMousePos;
            if (sewingPoint != null)
            {
                sewingPoint.position = worldMousePos;
            }
            //rotate the needle sprite to face the direction of movement

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

                //OVERLAPPING LOGIC
                if (!touchingString && i > 8 && i < tailNodes - 1)
                {
                    Vector2 a = nodesArray[0].position;
                    Vector2 b = nodesArray[1].position;
                    Vector2 c = nodesArray[i - 1].position;
                    Vector2 d = nodesArray[i].position;
                    touchingString = CheckIntersection(a, c, d) != CheckIntersection(b, c, d) && CheckIntersection(a, b ,c ) != CheckIntersection (a , b ,d);
                    if (touchingString)
                    {
                        KillButton(touchingString);
                    }
                    
                }
                
                
                linePositions[i] = new Vector3(nodesArray[i].position.x, nodesArray[i].position.y, 0);
            }
            lineRenderer.SetPositions(linePositions);

            ProcessAttackerContact(worldMousePos);
        }
    }

    bool CheckIntersection(Vector2 a, Vector2 b, Vector2 c){
        return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
    }

    void KillButton(bool touchingString)
    {
        if (buttons == null || buttons.Count == 0)
        {
            return;
        }
    
        List<GameObject> buttonKeys = new List<GameObject>(buttons.Keys);
        for (int i = buttonKeys.Count - 1; i >= 0; i--)
        {
            GameObject button = buttonKeys[i];
            ButtonData buttonData = buttons[button];
            if (buttonData == null) continue;
            if (button != null)
            {
                Vector3 buttonWorldPos = button.transform.position;
            
                float distance = Vector2.Distance(needleSprite.needleTransform.position, buttonWorldPos);


                if (distance < buttonDistanceThreshold)
                {
                    if (Time.time - buttonData.lastWrapTime > 1f)
                    {
                        buttonData.lastWrapTime = Time.time;
                        buttonData.currentWraps++;
                        if (buttonData.currentWraps >= buttonData.wrapsRequired)
                        {
                            buttons.Remove(button);
                            Destroy(button);
                            OnButtonCollected(buttonData.buttonType);
                        }
                        break;
                    }
                    
                }
            }
            else
            {
                //saftey incase null buttons in buttons
                buttons.Remove(button);

            }
        }
    
       
    }

    void OnButtonCollected(ButtonType type)
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

    void ProcessAttackerContact(Vector2 needlePos)
    {
        if (Time.time - attackerLastHitTime < attackerDamageCooldown) return;

        foreach (KeyValuePair<GameObject, ButtonData> pair in buttons)
        {
            if (pair.Key == null || pair.Value == null) continue;
            if (pair.Value.buttonType != ButtonType.Attacker) continue;

            float dist = Vector2.Distance(needlePos, pair.Key.transform.position);
            if (dist <= attackerContactRadius)
            {
                attackerLastHitTime = Time.time;
                DamageNeedle(1, (needlePos - (Vector2)pair.Key.transform.position).normalized);
                break;
            }
        }
    }

    public void SpawnButton(Vector2 position, ButtonType buttonType)
    {
        if (buttonPrefab != null && buttonParent != null)
        {
            GameObject newButton = Instantiate(buttonPrefab, position, Quaternion.identity, buttonParent);
            newButton.SetActive(true);
            ButtonData buttonData = new ButtonData(buttonType);

            Image img = newButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = buttonData.buttonColor;
            }
            else
            {
                SpriteRenderer sr = newButton.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = buttonData.buttonColor;
                }
                
            }

            if (buttonType == ButtonType.Dodger && needleSprite != null && needleSprite.needleTransform != null)
            {
                DodgeButton dodge = newButton.GetComponent<DodgeButton>();
                if (dodge == null)
                {
                    dodge = newButton.AddComponent<DodgeButton>();
                }
                dodge.Initialize(needleSprite.needleTransform);
            }

            buttons.Add(newButton, buttonData);
        }
    }

    public void DespawnButtonsLeftOf(float worldX)
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

            if (button.transform.position.x < worldX)
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
