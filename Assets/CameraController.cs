using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 1f;
    public float tiltAmount = 5f;
    public float tiltSmoothness = 5f;
    public float zoomFOV = 45f;
    public float zoomSpeed = 10f;
    public Transform playerBody;

    public KeyCode flashlightKey = KeyCode.F;
    private Light flashlight;
    private bool isFlashlightOn = false;

    private float xRotation = 0f;
    private float currentTilt = 0f; 
    private float targetTilt = 0f; 
    private Camera playerCamera;
    private float defaultFOV;
    private float targetFOV;
    private bool isZooming = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerCamera = GetComponent<Camera>();
        
        if (playerCamera != null)
        {
            defaultFOV = playerCamera.fieldOfView;
            targetFOV = defaultFOV;
        }

        flashlight = GetComponentInChildren<Light>();
        if (flashlight != null)
        {
            flashlight.enabled = isFlashlightOn;
        }
    }

    void Update()
    {
        HandleFlashlight();
        HandleZoom();
        HandleMouseLook();
    }

    void HandleFlashlight()
    {
        if (flashlight == null) return;

        if (Input.GetKeyDown(flashlightKey))
        {
            isFlashlightOn = !isFlashlightOn;
            flashlight.enabled = isFlashlightOn;
        }
    }

    void HandleZoom()
    {
        if (playerCamera == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            isZooming = true;
            targetFOV = zoomFOV;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isZooming = false;
            targetFOV = defaultFOV;
        }

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        if (isZooming)
        {
            mouseX *= 0.7f;
            mouseY *= 0.7f;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        targetTilt = -mouseX * tiltAmount;
        
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothness * Time.deltaTime);
        
        transform.localRotation *= Quaternion.Euler(0f, 0f, currentTilt);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else
        {
            transform.Rotate(Vector3.up * mouseX);
        }
    }
}