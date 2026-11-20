using UnityEngine;
using UnityEngine.InputSystem;

public class NeedleManager : MonoBehaviour
{
    private const int MaxNeedleHealth = 3;

    
    [Header("Movement")]
   // [SerializeField] private float baseMovementSpeed = .02f;
    [SerializeField] private float sensitivity = 1f;
    private Vector2 virtualNeedlePos;

    public int ButtonsCollected => buttonManager != null ? buttonManager.ButtonsCollected : 0;
    public int Score => buttonManager != null ? buttonManager.Score : 0;
    public int ScoreMultiplier => buttonManager != null ? buttonManager.ScoreMultiplier : 1;
    public int ActiveButtonCount => buttonManager != null ? buttonManager.ActiveButtonCount : 0;
    public event System.Action OnDamageTaken;

    private NeedleView needleView;
    private ButtonManager buttonManager;
    private int needleHealth = MaxNeedleHealth;
    private Vector2 lastMouseWorldPosition;
    private const string SensitivityPrefKey = "MouseSensitivity";

    public event System.Action<Vector2> OnDynamiteCaptured // forward the event from the button manager    
    {
        add
        {
            if (buttonManager != null)
            {
                buttonManager.DynamiteCaptured += value;
            }
        }
        remove
        {
            if (buttonManager != null)
            {
                buttonManager.DynamiteCaptured -= value;
            }
        }
    }

    private void Awake()
    {
        needleView = GetComponent<NeedleView>();
        if (needleView == null)
        {
            Debug.LogError("NeedleView must be added to the NeedleManager GameObject.", this);
        }

        buttonManager = GetComponent<ButtonManager>();
        if (buttonManager == null)
        {
            Debug.LogError("ButtonManager must be added and configured on the NeedleManager GameObject.", this);
        }
        else
        {
            buttonManager.Initialize(this);
        }
        sensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, 1f);
        if (needleView != null)
        {
            virtualNeedlePos = transform.position;
        }
    }

    private void Start()
    {
        if (needleView == null)
        {
            return;
        }

        needleView.Initialize();

        // Initialize the needle position so it does not start at origin
        if (Mouse.current != null && Camera.main != null)
        {
            lastMouseWorldPosition = needleView.GetMouseWorldPosition(Mouse.current.position.ReadValue());
        }
    }

    private void Update()
    {
        if (needleHealth <= 0)
        {
            return;
        }

        if (needleView == null || Mouse.current == null || Camera.main == null)
        {
            return;
        }



        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensitivity ;//* baseMovementSpeed;
        virtualNeedlePos += mouseDelta;

    

        virtualNeedlePos.x = Mathf.Clamp(virtualNeedlePos.x, 0f, Screen.width);
        virtualNeedlePos.y = Mathf.Clamp(virtualNeedlePos.y, 0f, Screen.height);

        Vector2 currentMouseWorldPosition = needleView.GetMouseWorldPosition(virtualNeedlePos);
        
        // Update the needle view
        needleView.UpdateView(virtualNeedlePos, Time.deltaTime);

        // Check if the needle's movement crossed the thread and notify the button manager.
        bool threadCrossingStarted = TryFindNeedleThreadCrossing(lastMouseWorldPosition, currentMouseWorldPosition);
        if (buttonManager != null)
        {
            buttonManager.ProcessButtonCollection(lastMouseWorldPosition, currentMouseWorldPosition, threadCrossingStarted);
        }

        lastMouseWorldPosition = currentMouseWorldPosition;
    }

    private bool TryFindNeedleThreadCrossing(Vector2 previousHeadPosition, Vector2 currentHeadPosition)
    {
        // Ignore if the needle view is not available or if the needle has not moved significantly
        if (needleView == null ||
            (currentHeadPosition - previousHeadPosition).sqrMagnitude <= 0.00000001f)
        {
            return false;
        }

        // Treat the needle's movement this frame as a segment and test it against each thread segment
        Vector2 needleDirection = currentHeadPosition - previousHeadPosition;
        for (int i = 9; i < needleView.ThreadNodeCount; i++)
        {
            // Get the thread segment's start and direction, and compute the cross product to determine if they are parallel.
            Vector2 threadStart = needleView.GetThreadNodePosition(i - 1);
            Vector2 threadDirection = needleView.GetThreadNodePosition(i) - threadStart;
            float denominator = CrossProduct(needleDirection, threadDirection);

            if (Mathf.Abs(denominator) < 0.000001f)
            {
                // If parallel, skip
                continue;
            }

            // Solve for each segment's normalized intersection position (between 0-1 means within that segment)
            Vector2 offset = threadStart - previousHeadPosition;
            float needleAmount = CrossProduct(offset, threadDirection) / denominator;
            float threadAmount = CrossProduct(offset, needleDirection) / denominator;

            // A crossing occurs only if the intersection lies on both segments.
            if (needleAmount >= 0f && needleAmount <= 1f &&
                threadAmount >= 0f && threadAmount <= 1f)
            {
                return true;
            }
        }

        return false;
    }

    private static float CrossProduct(Vector2 first, Vector2 second)
    {
        return first.x * second.y - first.y * second.x;
    }

    public void RestoreNeedleHealth(int amount)
    {
        if (needleHealth <= 0)
        {
            return;
        }

        needleHealth = Mathf.Min(MaxNeedleHealth, needleHealth + amount);
    }

    public void DamageNeedle(int amount, Vector2 knockback)
    {
        if (needleHealth <= 0)
        {
            return;
        }

        int previousHealth = needleHealth;
        needleHealth = Mathf.Max(0, needleHealth - amount);
        if (needleHealth < previousHealth)
        {
            OnDamageTaken?.Invoke();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNailDamagedSound();
            }
        }

        Transform needleTransform = GetNeedleTransform();
        if (needleTransform != null && knockback.sqrMagnitude > 0.0001f)
        {
            needleTransform.position += (Vector3)knockback;
            if (needleView != null)
            {
                needleView.SynchronizeNeedlePosition();
            }
        }

        if (needleHealth <= 0)
        {
            Debug.Log("Needle broke! Buttons collected: " + ButtonsCollected);
        }
    }

    public Transform GetNeedleTransform()
    {
        return needleView != null ? needleView.GetNeedleTransform() : null;
    }

    public Vector2 GetNeedleHeadPosition()
    {
        return needleView != null ? needleView.GetNeedleHeadPosition() : Vector2.zero;
    }

    public int GetNeedleHealth()
    {
        return needleHealth;
    }

    public void IncreaseScoreMultiplier()
    {
        if (buttonManager != null)
        {
            buttonManager.IncreaseScoreMultiplier();
        }
    }

    public bool IsNeedleAlive()
    {
        return needleHealth > 0;
    }

    public void SetSensitivity(float sens)
    {
        sensitivity = sens;
        //sets player preference for sensitivity
        PlayerPrefs.SetFloat(SensitivityPrefKey, sens); 
    }

    public float GetSensitivity()
    {
        return sensitivity;
    }

    private void ResetVirtualCursorToCenter()
    {
        virtualNeedlePos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (needleView != null && Camera.main != null)
        {
            lastMouseWorldPosition = needleView.GetMouseWorldPosition(virtualNeedlePos);
        }
    }



}
