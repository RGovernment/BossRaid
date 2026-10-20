using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(AudioSource))]
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
    [Header("사운드")]
    [SerializeField] private AudioClip fallSound;
    [SerializeField] private AudioClip impactSound;

    [Range(0f, 1f)]
    [SerializeField] private float fallVolume = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 0.4f;

    private bool hasImpacted;

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        SphereCollider meteorCollider = GetComponent<SphereCollider>();
        AudioSource audioSource = GetComponent<AudioSource>();

        meteorCollider.isTrigger = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.down * fallSpeed;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 10f;
        audioSource.maxDistance = 100f;
        audioSource.dopplerLevel = 0f;

        if (fallSound != null)
            audioSource.PlayOneShot(fallSound, fallVolume);

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

        PlayImpactSound();
        Destroy(gameObject);
    }
    private void PlayImpactSound()
    {
        if (impactSound == null)
            return;

        GameObject soundObject = new GameObject("MeteorImpactSound");
        soundObject.transform.position = transform.position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = 10f;
        source.maxDistance = 100f;
        source.dopplerLevel = 0f;
        source.volume = impactVolume;
        source.clip = impactSound;
        source.Play();

        Destroy(soundObject, impactSound.length);
    }
}