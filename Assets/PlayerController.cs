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
    
    public float standingCameraHeight = 1.6f;
    public float crouchingCameraHeight = 0.8f;
    public float slidingCameraHeight = 0.5f;
    
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

    public Transform cameraTransform;
    public Transform groundCheck;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
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

            currentVelocity = slideDirection * slideSpeed;
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
    }

    void EndSlide()
    {
        isSliding = false;
        
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