using UnityEngine;

[DisallowMultipleComponent]
public class BackgroundCameraHeightFollow : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private float cameraStartY;
    private float backgroundStartY;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("Assign a target camera", this);
            enabled = false;
            return;
        }

        cameraStartY = targetCamera.transform.position.y;
        backgroundStartY = transform.position.y;
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        position.y = backgroundStartY + targetCamera.transform.position.y - cameraStartY;
        transform.position = position;
    }
}
