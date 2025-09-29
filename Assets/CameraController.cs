using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 1f;
    public float tiltAmount = 5f; // How much the camera tilts when rotating
    public float tiltSmoothness = 5f; // How smooth the tilt transition is
    public float zoomFOV = 45f; // Field of view when zoomed
    public float zoomSpeed = 10f; // How fast the zoom transitions
    public Transform playerBody;

    // Flashlight
    public KeyCode flashlightKey = KeyCode.F;
    private Light flashlight;
    private bool isFlashlightOn = false;

    private float xRotation = 0f;
    private float currentTilt = 0f; // Current tilt angle
    private float targetTilt = 0f; // Target tilt angle
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

        // Find flashlight in children
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

        // Toggle flashlight with F key
        if (Input.GetKeyDown(flashlightKey))
        {
            isFlashlightOn = !isFlashlightOn;
            flashlight.enabled = isFlashlightOn;
        }
    }

    void HandleZoom()
    {
        if (playerCamera == null) return;

        // Check for right mouse button
        if (Input.GetMouseButtonDown(1)) // Right mouse button pressed
        {
            isZooming = true;
            targetFOV = zoomFOV;
        }
        if (Input.GetMouseButtonUp(1)) // Right mouse button released
        {
            isZooming = false;
            targetFOV = defaultFOV;
        }

        // Smoothly transition FOV
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // Optional: Reduce sensitivity while zooming for more precise aiming
        if (isZooming)
        {
            mouseX *= 0.7f; // 70% sensitivity when zoomed
            mouseY *= 0.7f;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Calculate tilt based on horizontal mouse movement
        targetTilt = -mouseX * tiltAmount;
        
        // Smoothly interpolate to the target tilt
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothness * Time.deltaTime);
        
        // Apply the tilt to the camera's Z rotation
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