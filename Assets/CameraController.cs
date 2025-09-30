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

    [Header("Flashlight Sound")]
    public AudioSource flashlightAudioSource;
    public AudioClip flashlightClickSound;
    [Range(0.0f, 1.0f)]
    public float flashlightVolume = 0.8f;
    [Range(0.5f, 2.0f)]
    public float flashlightPitch = 1.0f;

    private float xRotation = 0f;
    private float currentTilt = 0f; 
    private float targetTilt = 0f; 
    private Camera playerCamera;
    private float defaultFOV;
    private float targetFOV;
    private bool isZooming = false;

    // Reference to PlayerController for slide tilt
    private PlayerController playerController;
    private float externalTilt = 0f;

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

        // Get reference to PlayerController
        playerController = GetComponentInParent<PlayerController>();

        // Auto-find AudioSource if not assigned
        if (flashlightAudioSource == null)
        {
            flashlightAudioSource = GetComponent<AudioSource>();
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
            
            // Play flashlight sound
            PlayFlashlightSound();
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

        // Get external tilt from PlayerController (slide tilt)
        if (playerController != null)
        {
            externalTilt = playerController.GetSlideTilt();
        }

        // Combine mouse-based tilt with external slide tilt
        targetTilt = (-mouseX * tiltAmount) + externalTilt;
        
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

    void PlayFlashlightSound()
    {
        if (flashlightAudioSource == null || flashlightClickSound == null) return;

        flashlightAudioSource.clip = flashlightClickSound;
        flashlightAudioSource.volume = flashlightVolume;
        flashlightAudioSource.pitch = flashlightPitch;
        flashlightAudioSource.loop = false;
        flashlightAudioSource.Play();
    }

    // Public method to set external tilt (for PlayerController)
    public void SetExternalTilt(float tilt)
    {
        externalTilt = tilt;
    }
}