using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    [SerializeField, Min(0f)] private float duration = 0.35f;
    [SerializeField, Min(0f)] private float magnitude = 0.18f;

    private NeedleManager needleManager;
    private Transform shakeCamera;
    private Vector3 cameraShakeOffset;
    private float shakeTimer;

    private void OnEnable()
    {
        needleManager = GetComponent<NeedleManager>();
        if (needleManager == null)
        {
            Debug.LogError("ScreenShake requires NeedleManager on the same GameObject.", this);
            return;
        }

        needleManager.OnDamageTaken += StartShake;
    }

    private void OnDisable()
    {
        if (needleManager != null)
        {
            needleManager.OnDamageTaken -= StartShake;
        }

        StopShake();
    }

    private void LateUpdate()
    {
        if (needleManager == null || !needleManager.enabled)
        {
            StopShake();
            return;
        }

        if (!IsShakeActive() || !TryGetShakeCamera())
        {
            return;
        }

        Vector3 basePosition = shakeCamera.position - cameraShakeOffset;
        UpdateShakeOffset();
        shakeCamera.position = basePosition + cameraShakeOffset;
        ClearCameraWhenFinished();
    }

    private void StartShake()
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
        shakeTimer = duration;
    }

    private bool IsShakeActive()
    {
        return shakeCamera != null || shakeTimer > 0f;
    }

    private bool TryGetShakeCamera()
    {
        if (shakeCamera != null)
        {
            return true;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        shakeCamera = mainCamera.transform;
        return true;
    }

    private void UpdateShakeOffset()
    {
        shakeTimer = Mathf.Max(0f, shakeTimer - Time.unscaledDeltaTime);
        if (shakeTimer > 0f && duration > 0f)
        {
            float intensity = shakeTimer / duration;
            Vector2 randomOffset = Random.insideUnitCircle * (magnitude * intensity);
            cameraShakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
        }
        else
        {
            cameraShakeOffset = Vector3.zero;
        }
    }

    private void ClearCameraWhenFinished()
    {
        if (shakeTimer <= 0f)
        {
            shakeCamera = null;
        }
    }

    private void StopShake()
    {
        if (shakeCamera != null && cameraShakeOffset != Vector3.zero)
        {
            shakeCamera.position -= cameraShakeOffset;
        }

        shakeCamera = null;
        cameraShakeOffset = Vector3.zero;
        shakeTimer = 0f;
    }
}
