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
    
    // Sliding variables
    public float slideSpeed = 15f;
    public float slideDuration = 1f;
    public float slideCooldown = 1.5f;
    public float slideHeight = 0.5f;
    
    // Camera height adjustments
    public float standingCameraHeight = 1.6f; // Camera height when standing
    public float crouchingCameraHeight = 0.8f; // Camera height when crouching
    public float slidingCameraHeight = 0.5f; // Camera height when sliding
    
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

    // Sliding timers
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;
    private Vector3 slideDirection;

    // Reference to child objects that need adjustment
    public Transform cameraTransform;
    public Transform groundCheck;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        currentSpeed = speed;
        currentVelocity = Vector3.zero;
        
        // Store original dimensions
        standingHeight = controller.height;
        standingCenter = controller.center;
        
        currentHeight = standingHeight;
        targetHeight = standingHeight;

        // If no camera assigned, try to find it
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }
        
        // Set initial camera height
        UpdateCameraHeight();
    }

    void Update()
    {
        // Update cooldown timers
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
        
        // Auto stand up check
        if (isCrouching && !isSliding && !wantsToCrouch && CanStandUp())
        {
            AutoStandUp();
        }
    }

    void HandleCrouchInput()
    {
        // Check for crouch button (LEFT CTRL only) and ensure cooldown has expired
        if ((Input.GetKeyDown(KeyCode.LeftControl)) && crouchCooldownTimer <= 0f)
        {
            wantsToCrouch = true;
            
            // If sprinting and allowed, start slide (which includes crouching)
            if (isSprinting && IsSlidingAllowed())
            {
                StartSlide();
                crouchCooldownTimer = crouchCooldown;
            }
            // Otherwise just start normal crouch
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
            
            // Auto stand up if possible when releasing crouch
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
        // Update slide cooldown
        if (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
        }

        // Handle active slide
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            // Apply slide movement
            currentVelocity = slideDirection * slideSpeed;
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            // End slide when timer expires or player jumps (check cooldown for transition)
            if ((slideTimer <= 0f || Input.GetButton("Jump")) && crouchCooldownTimer <= 0f)
            {
                EndSlide();
                crouchCooldownTimer = crouchCooldown;
            }
            else
            {
                // Allow some steering during slide
                Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                if (input.magnitude > 0.1f)
                {
                    Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
                    desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
                    
                    // Add some steering influence
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
        
        // Store slide direction based on current movement
        slideDirection = new Vector3(currentVelocity.x, 0, currentVelocity.z).normalized;
        if (slideDirection.magnitude < 0.1f)
        {
            slideDirection = transform.forward;
        }

        targetHeight = slideHeight;
    }

    void EndSlide()
    {
        isSliding = false;
        
        // After slide ends, check if we should stay crouched or stand up
        if (wantsToCrouch)
        {
            // Player is still holding crouch button, transition to normal crouch
            targetHeight = crouchHeight;
            isCrouching = true;
        }
        else
        {
            // Player released crouch button, try to stand up automatically
            if (CanStandUp())
            {
                AutoStandUp();
            }
            else
            {
                // Can't stand up, stay crouched
                targetHeight = crouchHeight;
                isCrouching = true;
            }
        }
    }

    void HandleCrouch()
    {
        // Update height based on current state (crouch or slide)
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
        // Smoothly transition height
        float previousHeight = currentHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Update Character Controller
        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        // Update Capsule Collider
        if (capsuleCollider != null)
        {
            capsuleCollider.height = currentHeight;
            capsuleCollider.center = new Vector3(0, currentHeight / 2f, 0);
        }

        // Adjust position to prevent sinking into ground when getting shorter
        if (currentHeight < previousHeight)
        {
            float heightDifference = previousHeight - currentHeight;
            transform.position += new Vector3(0, heightDifference / 2f, 0);
        }

        // Adjust ground check position
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
            
            // Smoothly transition camera height
            Vector3 currentCameraPos = cameraTransform.localPosition;
            Vector3 targetCameraPos = new Vector3(0, targetCameraHeight, 0);
            cameraTransform.localPosition = Vector3.Lerp(currentCameraPos, targetCameraPos, crouchTransitionSpeed * Time.deltaTime);
        }
    }

    bool CanStandUp()
    {
        if (!isCrouching || isSliding) return false;
        
        float checkDistance = standingHeight - currentHeight + 0.2f; // Extra margin
        Vector3 rayStart = transform.position + Vector3.up * (currentHeight + 0.1f);
        
        // Debug visualization
        Debug.DrawRay(rayStart, Vector3.up * checkDistance, Color.red);
        
        // Check with multiple rays for better coverage
        float checkRadius = capsuleCollider != null ? capsuleCollider.radius * 0.8f : 0.3f;
        
        // Center ray
        if (Physics.Raycast(rayStart, Vector3.up, out RaycastHit hit, checkDistance))
        {
            return false;
        }
        
        // Additional rays in a circle pattern for better coverage
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
        
        // Final capsule cast for comprehensive check
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
        // Don't process normal movement input during slide
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
        
        // Handle sprinting (can't sprint while crouching)
        isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && 
                     !isCrouching && 
                     !isSliding && 
                     controller.isGrounded &&
                     input.magnitude > 0.1f;
        
        // Determine target speed based on movement state
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
}