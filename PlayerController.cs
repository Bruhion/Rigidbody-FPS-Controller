using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    
    [SerializeField] Transform orientation;
    [SerializeField] Transform playerCapsule;
    [SerializeField] Transform cameraPosition;
    
    [Header("Movement")]
    
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float airMultiplier = 0.4f;
    [SerializeField] float movementMultiplier = 10f;

    [Header("Sprinting")]
    
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 6f;
    [SerializeField] float acceleration = 10f;
    
    [Header("Jumping")]
    
    [SerializeField] float jumpForce = 5f;
    
    [Header("Wallrunning Detection")]
    
    [SerializeField] private float wallDistance = .6f;
    [SerializeField] private float minimumJumpHeight = 2f;

    [Header("Wallrunning Tuning")]
    
    [SerializeField] private float wallRunningGravity = 12f;
    [SerializeField] private float wallRunningJumpForce = 4f;
    
    [Header("Wallrunning Camera Movement")]
    
    [SerializeField] private Camera wallrunCamera;
    [SerializeField] private float camTilt = 10f;
    [SerializeField] private float camTiltTime =7.66f;
    public float tilt { get; private set; }
    
    [Header("Dynamic Field of View")]
    
    [SerializeField] Camera Camera;
    [SerializeField] private float dynamicFOVTime;
    [SerializeField] private float dynamicFOVLimit;

    [Header("Keybinds")]
    
    [SerializeField] KeyCode jumpKey;
    [SerializeField] KeyCode sprintKey;

    [Header("Drag")]
    
    [SerializeField] float groundDrag = 6f;
    [SerializeField] float airDrag = 2f;

    [Header("Ground Detection")]
    
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.2f;
    public bool isGrounded { get; private set; }

    [Header("Camera Controller")]
    [SerializeField] PlayerController playerController;
    [SerializeField] private float sensitivityX = 100f;
    [SerializeField] private float sensitivityY = 100f;
    [SerializeField] Transform cam = null;

    float playerHeight = 2f;
    float horizontalMovement;
    float verticalMovement;
    float mouseX;
    float mouseY;
    float multiplier = 0.01f;
    float xRotation;
    float yRotation;

    private bool wallLeft = false;
    private bool wallRight = false;
    
    Vector3 moveDirection;
    Vector3 slopeMoveDirection;
    Rigidbody rb;
    RaycastHit slopeHit;
    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    
    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight / 2 + 0.5f))
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }
    
    bool CanWallrun()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumJumpHeight);
    }
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        MyInput();
        ControlDrag();
        ControlSpeed();
        SlopeMovementDetection();
        GroundChecking();
        JumpCheck();
        CameraMovement();
        Crouch();
        DynamicFOV();
    }
    
    private void FixedUpdate()
    {
        MovePlayer(); 
        Wallrunning();
    }
    
    void MyInput()
    {
        horizontalMovement = Input.GetAxisRaw("Horizontal");
        verticalMovement = Input.GetAxisRaw("Vertical");
        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    void ControlSpeed()
    {
        if (Input.GetKey(sprintKey) && isGrounded)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
        }
    }

    void ControlDrag()
    {
        if (isGrounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = airDrag;
        }
    }

    void MovePlayer()
    {
        if (isGrounded && !OnSlope())
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        }
        else if (isGrounded && OnSlope())
        {
            rb.AddForce(slopeMoveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        }
        else if (!isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier * airMultiplier, ForceMode.Acceleration);
        }
    }
    
    void DynamicFOV()
    {
        if(Input.GetKey(KeyCode.LeftShift))
        {
            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView,100, dynamicFOVTime * Time.deltaTime);
        }
        else
        {
            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView,80,dynamicFOVTime * Time.deltaTime);
        }
    }

    void Crouch()
    {
        if(Input.GetKey(KeyCode.LeftControl))
        {
            playerCapsule.localScale = new Vector3(1f,0.7f,1f);
            moveSpeed = Mathf.Lerp(moveSpeed, 2f, acceleration * Time.deltaTime);
        }
        else if(Input.GetKey(KeyCode.LeftControl) && (Input.GetKey(KeyCode.LeftShift)))
        {
            playerCapsule.localScale = new Vector3(1f,0.7f,1f);
            moveSpeed = Mathf.Lerp(moveSpeed, 2f, acceleration * Time.deltaTime);
        }else
        {
           playerCapsule.localScale = new Vector3(1f,1f,1f);   
        } 
    }
    
    void SlopeMovementDetection()
    {
        slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
    }
    
    void GroundChecking()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }
    
    void JumpCheck()
    {
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            Jump();
        }
    }
        
    void CameraMovement()
    {
        mouseX = Input.GetAxisRaw("Mouse X");
        mouseY = Input.GetAxisRaw("Mouse Y");
        yRotation += mouseX * sensitivityX * multiplier;
        xRotation -= mouseY * sensitivityY * multiplier;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cam.transform.rotation = Quaternion.Euler(xRotation, yRotation, playerController.tilt);
        orientation.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    void Wallrunning()
    {
        Checkwall();

        if (CanWallrun())
        {
            if (wallLeft)
            {
                StartWallrun();
            }
            else if (wallRight)
            {
                StartWallrun();
            }
            else
            {
                StopWallrun();
            }
        }
        else
        {
            StopWallrun();
        }
    }

    void Checkwall()
    {
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance);
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance);
    }

    void StartWallrun()
    {
        rb.useGravity = false;

        rb.AddForce(Vector3.down * wallRunningGravity, ForceMode.Force);
        if (wallLeft)
            tilt = Mathf.Lerp(tilt, -camTilt, camTiltTime * Time.fixedDeltaTime);
        else if (wallRight)
            tilt = Mathf.Lerp(tilt, camTilt, camTiltTime * Time.fixedDeltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (wallLeft)
            {
                Vector3 wallRunJumpDirection = transform.up + leftWallHit.normal;
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(wallRunJumpDirection * wallRunningJumpForce * 100, ForceMode.Force);
            }
            else if (wallRight)
            {
                Vector3 wallRunJumpDirection = transform.up + rightWallHit.normal;
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); 
                rb.AddForce(wallRunJumpDirection * wallRunningJumpForce * 100, ForceMode.Force);
            }
        }
    }

    void StopWallrun()
    {
        rb.useGravity = true;
        tilt = Mathf.Lerp(tilt, 0, camTiltTime * Time.fixedDeltaTime);
    }
}
