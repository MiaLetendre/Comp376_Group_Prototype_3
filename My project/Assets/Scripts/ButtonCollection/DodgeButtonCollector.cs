using UnityEngine;

[DisallowMultipleComponent]
public class DodgeButtonCollector : WrapButtonCollector
{
    [SerializeField] private float detectionRadius = 3.5f;
    [SerializeField] private float fleeSpeed = 4.5f;
    [SerializeField] private float wanderSpeed = 1.2f;
    [SerializeField] private float wanderChangeInterval = 1.1f;
    [SerializeField] private float edgePadding = 0.8f;

    private Camera cam;
    private Vector2 wanderDirection;
    private float nextWanderTime;

    protected override void OnButtonSpawned()
    {
        cam = Camera.main;
        PickNewWanderDirection();
    }

    protected override void OnButtonUpdate()
    {
        if (Needle == null || cam == null) return;

        Vector2 position = transform.position;
        Vector2 needlePosition = Needle.position;
        float distance = Vector2.Distance(position, needlePosition);

        Vector2 velocity;
        if (distance < detectionRadius && distance > 0.001f)
        {
            velocity = (position - needlePosition).normalized * fleeSpeed;
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

        position += velocity * Time.deltaTime;
        position.x = ClampX(position.x);
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private float ClampX(float x)
    {
        float halfWidth = cam.orthographicSize * cam.aspect - edgePadding;
        float cameraX = cam.transform.position.x;
        return Mathf.Clamp(x, cameraX - halfWidth, cameraX + halfWidth);
    }

    private void PickNewWanderDirection()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        nextWanderTime = Time.time + wanderChangeInterval;
    }
}
