using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class TestPlayerMovement : MonoBehaviour
{
    [Header("이동")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 5f;

    [Header("점프")]
    [Min(0f)]
    [SerializeField] private float jumpHeight = 2f;

    [Header("바닥 감지")]
    [SerializeField] private LayerMask groundLayers;

    [Min(0.001f)]
    [SerializeField] private float groundCheckDistance = 0.1f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (Input.GetKeyDown(KeyCode.Space))
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveInput.x * moveSpeed;
        velocity.z = moveInput.y * moveSpeed;

        if (jumpRequested && IsGrounded())
        {
            float gravityStrength = -Physics.gravity.y;
            velocity.y = Mathf.Sqrt(2f * gravityStrength * jumpHeight);
        }

        jumpRequested = false;
        rb.linearVelocity = velocity;
    }

    private bool IsGrounded()
    {
        Bounds bounds = capsule.bounds;
        Vector3 origin = bounds.center;
        float checkDistance = bounds.extents.y + groundCheckDistance;

        return Physics.Raycast(origin, Vector3.down, checkDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        jumpRequested = false;

        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }
    }
}