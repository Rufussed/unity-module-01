using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] private bool doubleJump = false;
    [SerializeField] private float maxTiltDegrees = 10f;
    [SerializeField] private float horizontalSpeedCap = 10f;

    private Rigidbody rb;
    private float moveInput;
    private bool isGrounded;
    private bool canDoubleJump;
    private bool canControl;
    private Quaternion baseRotation;

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
            return;
        }

        var moveForce = new Vector3(moveInput * moveSpeed, 0f, 0f);
        rb.AddForce(moveForce, ForceMode.Force);
        ClampHorizontalSpeed();
        ApplyTilt();
    }

    private void ReadMovement()
    {
        moveInput = 0f;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            moveInput -= 1f;
        }

        if (Keyboard.current.dKey.isPressed)
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

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryJump();
        }
    }

    private void TryJump()
    {
        if (isGrounded)
        {
            PerformJump();
            canDoubleJump = doubleJump;
        }
        else if (doubleJump && canDoubleJump)
        {
            PerformJump();
            canDoubleJump = false;
        }
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
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = grounded;
                if (grounded)
                {
                    canDoubleJump = doubleJump;
                }
                return;
            }
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
