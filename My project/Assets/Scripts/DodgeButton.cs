using UnityEngine;

/// <summary>
/// Green dodger button: flees from the needle when it gets close,
/// making it harder to wrap and collect.
/// </summary>
[DisallowMultipleComponent]
public class DodgeButton : MonoBehaviour
{
    [SerializeField] float detectionRadius = 3.5f;
    [SerializeField] float fleeSpeed = 4.5f;
    [SerializeField] float wanderSpeed = 1.2f;
    [SerializeField] float wanderChangeInterval = 1.1f;
    [SerializeField] float verticalPadding = 1.5f;

    private Transform needle;
    private Camera cam;
    private Vector2 wanderDirection;
    private float nextWanderTime;
    private float halfHeight;

    public void Initialize(Transform needleTransform)
    {
        needle = needleTransform;
        cam = Camera.main;
        PickNewWanderDirection();
    }

    private void Update()
    {
        if (needle == null || cam == null) return;

        halfHeight = cam.orthographicSize - verticalPadding;
        Vector2 pos = transform.position;
        Vector2 needlePos = needle.position;
        float distance = Vector2.Distance(pos, needlePos);

        Vector2 velocity;
        if (distance < detectionRadius && distance > 0.001f)
        {
            // Sprint away from the needle tip.
            velocity = (pos - needlePos).normalized * fleeSpeed;
        }
        else
        {
            if (Time.time >= nextWanderTime)
            {
                PickNewWanderDirection();
            }
            velocity = wanderDirection * wanderSpeed;
        }

        pos += velocity * Time.deltaTime;

        // Stay inside the visible vertical band of the scrolling camera.
        float camY = cam.transform.position.y;
        pos.y = Mathf.Clamp(pos.y, camY - halfHeight, camY + halfHeight);

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    private void PickNewWanderDirection()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        nextWanderTime = Time.time + wanderChangeInterval;
    }
}
