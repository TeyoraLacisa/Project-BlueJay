using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;
    public float jumpSpeed = 8f;
    public float gravity = 20.0f;
    public float speedTransitionSharpness = 10f;
    public float airControlMultiplier = 0.5f;
    public float jumpCooldown = 0.3f;
    public float crouchCooldown = 0.2f;
    public float groundAcceleration = 12f;
    public float groundDeceleration = 16f;
    public float airAcceleration = 8f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 8f;
    
    public float slideSpeed = 15f;
    public float slideDuration = 1f;
    public float slideCooldown = 1.5f;
    public float slideHeight = 0.5f;
    
    [Header("Slide Decay Settings")]
    [Tooltip("How quickly sliding speed decreases (higher = faster decay)")]
    [Range(0.1f, 2.0f)]
    public float slideDecayRate = 0.8f;
    [Tooltip("Minimum speed when sliding ends")]
    [Range(0f, 10f)]
    public float minSlideSpeed = 3f;
    
    public float standingCameraHeight = 1.6f;
    public float crouchingCameraHeight = 0.8f;
    public float slidingCameraHeight = 0.5f;
    
    // Camera tilt properties - compatible with CameraController
    public float slideTiltAngle = 10f;
    public float tiltTransitionSpeed = 5f;
    
    [Header("Footstep Sounds")]
    public AudioSource footstepAudioSource;
    public AudioClip walkFootstepSound;
    public AudioClip slideSound;
    public AudioClip jumpSound;
    public AudioClip landingSound;
    
    [Header("Footstep Timing")]
    [Tooltip("Time between footsteps when walking")]
    [Range(0.1f, 1.0f)]
    public float walkStepInterval = 0.5f;
    
    [Tooltip("Time between footsteps when running")]
    [Range(0.1f, 1.0f)]
    public float runStepInterval = 0.3f;
    
    [Tooltip("Time between footsteps when crouching")]
    [Range(0.1f, 1.0f)]
    public float crouchStepInterval = 0.7f;
    
    [Header("Footstep Audio Settings")]
    [Tooltip("Random pitch variation for footsteps")]
    [Range(0.0f, 0.3f)]
    public float pitchRandomness = 0.1f;
    
    [Tooltip("Base volume for walking footsteps")]
    [Range(0.0f, 1.0f)]
    public float walkVolume = 0.8f;
    
    [Tooltip("Base volume for running footsteps")]
    [Range(0.0f, 1.0f)]
    public float runVolume = 1.0f;
    
    [Tooltip("Base volume for crouching footsteps")]
    [Range(0.0f, 1.0f)]
    public float crouchVolume = 0.6f;
    
    [Tooltip("Pitch for walking footsteps")]
    [Range(0.5f, 2.0f)]
    public float walkPitch = 1.0f;
    
    [Tooltip("Pitch for running footsteps")]
    [Range(0.5f, 2.0f)]
    public float runPitch = 1.3f;
    
    [Tooltip("Pitch for crouching footsteps")]
    [Range(0.5f, 2.0f)]
    public float crouchPitch = 0.8f;
    
    [Header("Jump Sound Settings")]
    [Tooltip("Volume for jump sound")]
    [Range(0.0f, 1.0f)]
    public float jumpVolume = 0.8f;
    
    [Tooltip("Pitch for jump sound")]
    [Range(0.5f, 2.0f)]
    public float jumpPitch = 1.0f;
    
    [Header("Landing Sound Settings")]
    [Tooltip("Minimum falling speed to trigger landing sound")]
    [Range(0.5f, 5.0f)]
    public float landingThreshold = 2.0f;
    
    [Tooltip("Base volume for landing sound")]
    [Range(0.0f, 1.0f)]
    public float landingBaseVolume = 0.5f;
    
    [Tooltip("Additional volume based on landing speed")]
    [Range(0.0f, 1.0f)]
    public float landingSpeedVolume = 0.5f;
    
    [Tooltip("Pitch range for landing sound (min)")]
    [Range(0.5f, 1.5f)]
    public float landingPitchMin = 0.9f;
    
    [Tooltip("Pitch range for landing sound (max)")]
    [Range(0.5f, 1.5f)]
    public float landingPitchMax = 1.1f;
    
    [Tooltip("Maximum landing velocity for volume calculation")]
    [Range(5.0f, 20.0f)]
    public float maxLandingVelocity = 10f;
    
    [Header("Slide Sound Settings")]
    [Tooltip("Volume for slide sound")]
    [Range(0.0f, 1.0f)]
    public float slideVolume = 1.0f;
    
    [Tooltip("Pitch for slide sound")]
    [Range(0.5f, 2.0f)]
    public float slidePitch = 1.0f;
    
    private CharacterController controller;
    private CapsuleCollider capsuleCollider;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool isSliding = false;
    private bool wantsToCrouch = false;
    private float currentSpeed;
    private float jumpCooldownTimer = 0f;
    private float crouchCooldownTimer = 0f;
    private float standingHeight;
    private float currentHeight;
    private float targetHeight;
    private Vector3 standingCenter;

    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;
    private Vector3 slideDirection;
    private float currentSlideSpeed;

    // Reference to CameraController for tilt coordination
    private CameraController cameraController;
    private float currentSlideTilt = 0f;
    private float targetSlideTilt = 0f;

    // Footstep variables
    private float stepTimer = 0f;
    private bool wasGrounded = false;
    private bool hasLanded = false;

    public Transform cameraTransform;
    public Transform groundCheck;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        cameraController = GetComponentInChildren<CameraController>();
        
        currentSpeed = speed;
        currentVelocity = Vector3.zero;
        
        standingHeight = controller.height;
        standingCenter = controller.center;
        
        currentHeight = standingHeight;
        targetHeight = standingHeight;

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }
        
        UpdateCameraHeight();
    }

    void Update()
    {
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }
        
        if (crouchCooldownTimer > 0f)
        {
            crouchCooldownTimer -= Time.deltaTime;
        }

        HandleCrouchInput();
        HandleSliding();
        HandleCrouch();
        HandleMovement();
        HandleSlideTilt();
        HandleFootsteps();
        
        if (isCrouching && !isSliding && !wantsToCrouch && CanStandUp())
        {
            AutoStandUp();
        }
    }

    void HandleCrouchInput()
    {
        if ((Input.GetKeyDown(KeyCode.LeftControl)) && crouchCooldownTimer <= 0f)
        {
            wantsToCrouch = true;
            
            if (isSprinting && IsSlidingAllowed())
            {
                StartSlide();
                crouchCooldownTimer = crouchCooldown;
            }
            else if (!isCrouching)
            {
                isCrouching = true;
                targetHeight = crouchHeight;
                crouchCooldownTimer = crouchCooldown;
            }
        }
        
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            wantsToCrouch = false;
            
            if (isCrouching && !isSliding && CanStandUp())
            {
                AutoStandUp();
            }
        }
    }

    void AutoStandUp()
    {
        if (isCrouching && !isSliding && CanStandUp() && crouchCooldownTimer <= 0f)
        {
            isCrouching = false;
            targetHeight = standingHeight;
            crouchCooldownTimer = crouchCooldown;
        }
    }

    void HandleSliding()
    {
        if (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            // Calculate speed decay based on time and decay rate
            float normalizedTime = 1f - (slideTimer / slideDuration);
            float speedMultiplier = Mathf.Lerp(1f, 0f, normalizedTime * slideDecayRate);
            currentSlideSpeed = Mathf.Lerp(slideSpeed, minSlideSpeed, normalizedTime * slideDecayRate);
            
            currentVelocity = slideDirection * currentSlideSpeed;
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            if ((slideTimer <= 0f || Input.GetButton("Jump")) && crouchCooldownTimer <= 0f)
            {
                EndSlide();
                crouchCooldownTimer = crouchCooldown;
            }
            else
            {
                Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                if (input.magnitude > 0.1f)
                {
                    Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
                    desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
                    
                    Vector3 steerDirection = Vector3.Lerp(slideDirection, desiredMoveDirection, 0.3f * Time.deltaTime);
                    slideDirection = steerDirection.normalized;
                }
            }
        }
    }

    bool IsSlidingAllowed()
    {
        return isSprinting && 
               controller.isGrounded && 
               slideCooldownTimer <= 0f && 
               crouchCooldownTimer <= 0f &&
               !isSliding &&
               currentVelocity.magnitude > sprintSpeed * 0.7f;
    }

    void StartSlide()
    {
        isSliding = true;
        isCrouching = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        
        slideDirection = new Vector3(currentVelocity.x, 0, currentVelocity.z).normalized;
        if (slideDirection.magnitude < 0.1f)
        {
            slideDirection = transform.forward;
        }

        targetHeight = slideHeight;
        currentSlideSpeed = slideSpeed;
        
        // Set slide tilt
        targetSlideTilt = -slideTiltAngle; // Negative for left tilt
    }

    void EndSlide()
    {
        isSliding = false;
        
        // Reset slide tilt
        targetSlideTilt = 0f;
        
        if (wantsToCrouch)
        {
            targetHeight = crouchHeight;
            isCrouching = true;
        }
        else
        {
            if (CanStandUp())
            {
                AutoStandUp();
            }
            else
            {
                targetHeight = crouchHeight;
                isCrouching = true;
            }
        }
    }

    void HandleSlideTilt()
    {
        // Smoothly interpolate the slide tilt
        currentSlideTilt = Mathf.Lerp(currentSlideTilt, targetSlideTilt, tiltTransitionSpeed * Time.deltaTime);
        
        // Apply slide tilt through CameraController if available
        if (cameraController != null)
        {
            // We'll use a public method or property to communicate the slide tilt
            ApplySlideTiltToCamera();
        }
        else
        {
            // Fallback: apply directly to camera transform
            ApplySlideTiltDirect();
        }
    }

    void ApplySlideTiltToCamera()
    {
        // If CameraController has a way to receive external tilt, use it
        // For now, we'll use reflection as a fallback
        try
        {
            // Try to set a public field or property on CameraController
            var slideTiltField = cameraController.GetType().GetField("externalTilt");
            if (slideTiltField != null)
            {
                slideTiltField.SetValue(cameraController, currentSlideTilt);
            }
        }
        catch
        {
            // If reflection fails, apply directly
            ApplySlideTiltDirect();
        }
    }

    void ApplySlideTiltDirect()
    {
        if (cameraTransform != null)
        {
            Vector3 currentRotation = cameraTransform.localEulerAngles;
            // Only affect Z rotation for tilt, preserve X and Y
            cameraTransform.localEulerAngles = new Vector3(
                currentRotation.x, 
                currentRotation.y, 
                currentSlideTilt
            );
        }
    }

    void HandleCrouch()
    {
        if (isSliding)
        {
            targetHeight = slideHeight;
        }
        else if (isCrouching)
        {
            targetHeight = crouchHeight;
        }
        else
        {
            targetHeight = standingHeight;
        }

        UpdateHeight();
        UpdateCameraHeight();
    }

    void UpdateHeight()
    {
        float previousHeight = currentHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        if (capsuleCollider != null)
        {
            capsuleCollider.height = currentHeight;
            capsuleCollider.center = new Vector3(0, currentHeight / 2f, 0);
        }

        if (currentHeight < previousHeight)
        {
            float heightDifference = previousHeight - currentHeight;
            transform.position += new Vector3(0, heightDifference / 2f, 0);
        }

        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(0, 0.1f, 0);
        }
    }

    void UpdateCameraHeight()
    {
        if (cameraTransform != null)
        {
            float targetCameraHeight;
            
            if (isSliding)
            {
                targetCameraHeight = slidingCameraHeight;
            }
            else if (isCrouching)
            {
                targetCameraHeight = crouchingCameraHeight;
            }
            else
            {
                targetCameraHeight = standingCameraHeight;
            }
            
            Vector3 currentCameraPos = cameraTransform.localPosition;
            Vector3 targetCameraPos = new Vector3(0, targetCameraHeight, 0);
            cameraTransform.localPosition = Vector3.Lerp(currentCameraPos, targetCameraPos, crouchTransitionSpeed * Time.deltaTime);
        }
    }

    bool CanStandUp()
    {
        if (!isCrouching || isSliding) return false;
        
        float checkDistance = standingHeight - currentHeight + 0.2f; 
        Vector3 rayStart = transform.position + Vector3.up * (currentHeight + 0.1f);
        
        Debug.DrawRay(rayStart, Vector3.up * checkDistance, Color.red);
        
        float checkRadius = capsuleCollider != null ? capsuleCollider.radius * 0.8f : 0.3f;
        
        if (Physics.Raycast(rayStart, Vector3.up, out RaycastHit hit, checkDistance))
        {
            return false;
        }
        
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };
        
        foreach (Vector3 dir in directions)
        {
            Vector3 offsetStart = rayStart + dir * checkRadius;
            if (Physics.Raycast(offsetStart, Vector3.up, out RaycastHit offsetHit, checkDistance))
            {
                return false;
            }
        }
        
        if (capsuleCollider != null)
        {
            Vector3 point1 = transform.position + Vector3.up * capsuleCollider.radius;
            Vector3 point2 = transform.position + Vector3.up * (standingHeight - capsuleCollider.radius);
            float radius = capsuleCollider.radius * 0.9f;
            
            if (Physics.CapsuleCast(point1, point2, radius, Vector3.up, out RaycastHit capsuleHit, checkDistance))
            {
                return false;
            }
        }
        
        return true;
    }

    void HandleMovement()
    {
        if (isSliding)
        {
            moveDirection.y -= gravity * Time.deltaTime;
            controller.Move(moveDirection * Time.deltaTime);
            return;
        }

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        if (input.magnitude > 1f)
        {
            input.Normalize();
        }
        
        Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
        desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
        
        isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && 
                     !isCrouching && 
                     !isSliding && 
                     controller.isGrounded &&
                     input.magnitude > 0.1f;
        
        float targetMovementSpeed;
        if (isCrouching)
        {
            targetMovementSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            targetMovementSpeed = sprintSpeed;
        }
        else
        {
            targetMovementSpeed = speed;
        }
        
        currentSpeed = Mathf.Lerp(currentSpeed, targetMovementSpeed, speedTransitionSharpness * Time.deltaTime);

        if (controller.isGrounded)
        {
            Vector3 targetVelocity = desiredMoveDirection * currentSpeed;
            
            if (input.magnitude > 0.1f)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, groundAcceleration * Time.deltaTime);
            }
            else
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, groundDeceleration * Time.deltaTime);
            }
            
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            if (Input.GetButton("Jump") && jumpCooldownTimer <= 0f && !isCrouching)
            {
                // Play jump sound
                PlayJumpSound();
                moveDirection.y = jumpSpeed;
                jumpCooldownTimer = jumpCooldown;
            }
            else
            {
                moveDirection.y = -0.1f;
            }
        }
        else
        {
            Vector3 targetAirVelocity = desiredMoveDirection * currentSpeed;
            
            if (input.magnitude > 0.1f)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, targetAirVelocity, airAcceleration * Time.deltaTime);
            }
            
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;
        }

        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    void HandleFootsteps()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;

        bool isGrounded = controller.isGrounded;
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        // SIMPLIFIED: Use input and velocity to detect movement instead of position tracking
        bool hasMovementInput = input.magnitude > 0.1f;
        bool isMoving = currentVelocity.magnitude > 0.1f;
        
        // Only play footsteps if moving with input AND grounded
        bool shouldPlayFootsteps = isGrounded && hasMovementInput && isMoving && !isSliding;

        // Play landing sound - FIXED for high speed landings
        if (isGrounded && !wasGrounded)
        {
            // Calculate landing velocity (how fast we were falling)
            float landingVelocity = Mathf.Abs(moveDirection.y);
            
            // Only play landing sound if we fell from a significant height
            if (landingVelocity > landingThreshold)
            {
                PlayLandingSound(landingVelocity);
                hasLanded = true;
            }
        }

        // Handle footsteps while moving
        if (shouldPlayFootsteps)
        {
            float stepInterval = GetStepInterval();
            
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                PlayFootstepSound();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f; // Reset timer when not moving
            
            // Stop any currently playing footstep sound if we're not supposed to be moving
            if (footstepAudioSource.isPlaying && footstepAudioSource.clip == walkFootstepSound)
            {
                footstepAudioSource.Stop();
            }
        }

        // Handle slide sound
        if (isSliding)
        {
            if (!footstepAudioSource.isPlaying || footstepAudioSource.clip != slideSound)
            {
                PlaySlideSound();
            }
        }
        else if (footstepAudioSource.clip == slideSound && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
        }

        wasGrounded = isGrounded;
    }

    float GetStepInterval()
    {
        if (isCrouching)
            return crouchStepInterval;
        else if (isSprinting)
            return runStepInterval;
        else
            return walkStepInterval;
    }

    void PlayFootstepSound()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;

        footstepAudioSource.clip = walkFootstepSound;
        
        float basePitch = walkPitch;
        float baseVolume = walkVolume;

        if (isSprinting)
        {
            basePitch = runPitch;
            baseVolume = runVolume;
        }
        else if (isCrouching)
        {
            basePitch = crouchPitch;
            baseVolume = crouchVolume;
        }

        // Add slight randomness to make it more natural
        footstepAudioSource.pitch = basePitch + Random.Range(-pitchRandomness, pitchRandomness);
        footstepAudioSource.volume = baseVolume;
        footstepAudioSource.loop = false;

        footstepAudioSource.Play();
    }

    void PlaySlideSound()
    {
        if (footstepAudioSource == null || slideSound == null) return;

        footstepAudioSource.clip = slideSound;
        footstepAudioSource.pitch = slidePitch;
        footstepAudioSource.volume = slideVolume;
        footstepAudioSource.loop = true; // Slide sound can loop
        footstepAudioSource.Play();
    }

    void PlayJumpSound()
    {
        if (footstepAudioSource == null || jumpSound == null) return;

        footstepAudioSource.clip = jumpSound;
        footstepAudioSource.pitch = jumpPitch;
        footstepAudioSource.volume = jumpVolume;
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    void PlayLandingSound(float landingVelocity)
    {
        if (footstepAudioSource == null || landingSound == null) return;

        // Calculate landing intensity based on vertical velocity
        float landingIntensity = Mathf.Clamp01(landingVelocity / maxLandingVelocity); // Normalize to 0-1 range
        
        footstepAudioSource.clip = landingSound;
        footstepAudioSource.pitch = Random.Range(landingPitchMin, landingPitchMax);
        
        // Volume increases with landing speed
        footstepAudioSource.volume = landingBaseVolume + (landingIntensity * landingSpeedVolume);
        
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    // Public method to get current slide tilt for CameraController
    public float GetSlideTilt()
    {
        return currentSlideTilt;
    }

    // Public method to check if sliding for CameraController
    public bool IsSliding()
    {
        return isSliding;
    }

    public bool IsGrounded()
    {
        return controller.isGrounded;
    }

    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public bool IsSprinting()
    {
        return isSprinting;
    }

    public float GetSprintSpeed()
    {
        return sprintSpeed;
    }
}