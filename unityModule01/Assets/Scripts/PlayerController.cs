using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] private float maxTiltDegrees = 10f;
    [SerializeField] private float horizontalSpeedCap = 10f;
    [SerializeField] private float coyoteTime = 0.1f;

    private Rigidbody rb;
    private float moveInput;
    private bool isGrounded;
    private bool canControl;
    private Quaternion baseRotation;
    private float coyoteTimer;
    private Transform currentConveyor;
    private Vector3 conveyorPreviousPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component missing on player.");
            enabled = false;
            return;
        }

        baseRotation = transform.rotation;
        rb.constraints |= RigidbodyConstraints.FreezePositionZ;
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
    }

    private void Update()
    {
        CheckFallOutOfBounds();

        if (!canControl)
        {
            moveInput = 0f;
            return;
        }

        ReadMovement();
        ReadJump();
    }

    private void FixedUpdate()
    {
        if (!canControl)
        {
            ClampHorizontalSpeed();
            ApplyTilt();
            FixedUpdateConveyorFollow();
            return;
        }

        var currentMoveSpeed = isGrounded ? moveSpeed : moveSpeed * 0.4f;
        var moveForce = new Vector3(moveInput * currentMoveSpeed, 0f, 0f);
        rb.AddForce(moveForce, ForceMode.Force);
        ClampHorizontalSpeed();
        ApplyTilt();
        FixedUpdateConveyorFollow();
    }

    private void ReadMovement()
    {
        moveInput = 0f;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput += 1f;
        }

        moveInput = Mathf.Clamp(moveInput, -1f, 1f);
    }

    private void ReadJump()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if ((Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.upArrowKey.wasPressedThisFrame) &&
            (isGrounded || coyoteTimer > 0f))
        {
            TryJump();
        }
    }

    private void TryJump()
    {
        PerformJump();
        coyoteTimer = 0f;
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        UpdateGroundState(collision, true);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateGroundState(collision, true);
    }

    private void OnCollisionExit(Collision collision)
    {
        UpdateGroundState(collision, false);
    }

    private void UpdateGroundState(Collision collision, bool grounded)
    {
        if (!grounded)
        {
            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
            }

            isGrounded = false;
            DetachFromConveyor();
            return;
        }

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.4f)
            {
                isGrounded = true;
                coyoteTimer = coyoteTime;
                var conveyorTransform = ResolveConveyorTransform(collision, contact);
                if (conveyorTransform != null)
                {
                    AttachToConveyor(conveyorTransform);
                }
                else
                {
                    DetachFromConveyor();
                }

                return;
            }
        }
    }

    private void LateUpdate()
    {
        if (!isGrounded && coyoteTimer > 0f)
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    private void ClampHorizontalSpeed()
    {
        if (horizontalSpeedCap <= 0f)
        {
            return;
        }

        var velocity = rb.linearVelocity;
        velocity.x = Mathf.Clamp(velocity.x, -horizontalSpeedCap, horizontalSpeedCap);
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    private void ApplyTilt()
    {
        if (maxTiltDegrees <= 0f)
        {
            rb.MoveRotation(baseRotation);
            rb.angularVelocity = Vector3.zero;
            return;
        }

        var targetTilt = Mathf.Clamp(moveInput, -1f, 1f) * maxTiltDegrees;
        var targetRotation = baseRotation * Quaternion.Euler(0f, 0f, targetTilt);
        rb.MoveRotation(targetRotation);
        rb.angularVelocity = Vector3.zero;
    }

    private void CheckFallOutOfBounds()
    {
        var manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (transform.position.y < -15f) //fall out threshold
        {
            manager.ReloadActiveScene();
        }
    }

    private void AttachToConveyor(Transform conveyorTransform)
    {
        if (conveyorTransform == null || currentConveyor == conveyorTransform)
        {
            return;
        }

        currentConveyor = conveyorTransform;
        conveyorPreviousPosition = conveyorTransform.position;
    }

    private void DetachFromConveyor()
    {
        currentConveyor = null;
    }

    private Transform ResolveConveyorTransform(Collision collision, ContactPoint contact)
    {
        var conveyor = FindTaggedConveyor(contact.otherCollider != null ? contact.otherCollider.transform : null);
        return conveyor != null ? conveyor : FindTaggedConveyor(collision.transform);
    }

    private Transform FindTaggedConveyor(Transform start)
    {
        while (start != null)
        {
            if (start.CompareTag("Conveyor"))
            {
                return start;
            }

            start = start.parent;
        }

        return null;
    }

    private void FixedUpdateConveyorFollow()
    {
        if (currentConveyor == null)
        {
            return;
        }

        var deltaX = currentConveyor.position.x - conveyorPreviousPosition.x;
        if (!Mathf.Approximately(deltaX, 0f))
        {
            rb.MovePosition(rb.position + new Vector3(deltaX, 0f, 0f));
        }

        conveyorPreviousPosition = currentConveyor.position;
    }

    public void SetControlState(bool active)
    {
        canControl = active;
        if (!canControl)
        {
            moveInput = 0f;
        }
    }

    public bool IsControlEnabled => canControl;
}
