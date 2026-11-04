using UnityEngine;

[DisallowMultipleComponent]
public class DodgeButton : MonoBehaviour
{
    [SerializeField] float detectionRadius = 3.5f;
    [SerializeField] float fleeSpeed = 4.5f;
    [SerializeField] float wanderSpeed = 1.2f;
    [SerializeField] float wanderChangeInterval = 1.1f;
    [SerializeField] float edgePadding = 0.8f;

    private Transform needle;
    private Camera cam;
    private Vector2 wanderDirection;
    private float nextWanderTime;

    public void Initialize(Transform needleTransform)
    {
        needle = needleTransform;
        cam = Camera.main;
        PickNewWanderDirection();
    }

    private void Update()
    {
        if (needle == null || cam == null) return;

        Vector2 pos = transform.position;
        Vector2 needlePos = needle.position;
        float distance = Vector2.Distance(pos, needlePos);

        Vector2 velocity;
        if (distance < detectionRadius && distance > 0.001f)
        {
            velocity = (pos - needlePos).normalized * fleeSpeed;
            velocity.y *= 0.35f;
        }
        else
        {
            if (Time.time >= nextWanderTime)
            {
                PickNewWanderDirection();
            }
            velocity = wanderDirection * wanderSpeed;
            velocity.y *= 0.35f;
        }

        pos += velocity * Time.deltaTime;
        pos.x = ClampX(pos.x);

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    private float ClampX(float x)
    {
        float halfWidth = cam.orthographicSize * cam.aspect - edgePadding;
        float camX = cam.transform.position.x;
        return Mathf.Clamp(x, camX - halfWidth, camX + halfWidth);
    }

    private void PickNewWanderDirection()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        nextWanderTime = Time.time + wanderChangeInterval;
    }
}
