using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class MeteorProjectile : MonoBehaviour
{
    [Header("낙하")]
    [Min(0f)]
    [SerializeField] private float fallSpeed = 30f;
    [Header("충돌 대상")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask playerLayers;
    [Header("삭제")]
    [Min(0.01f)]
    [SerializeField] private float maxLifetime = 5f;

    private bool hasImpacted;

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        SphereCollider meteorCollider = GetComponent<SphereCollider>();

        meteorCollider.isTrigger = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.down * fallSpeed;

        Destroy(gameObject, maxLifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasImpacted)
            return;

        int collidedLayerMask = 1 << other.gameObject.layer;
        bool hitGround = (groundLayers.value & collidedLayerMask) != 0;
        bool hitPlayer = (playerLayers.value & collidedLayerMask) != 0;

        if (!hitGround && !hitPlayer)
            return;

        hasImpacted = true;
        Destroy(gameObject);
    }
}