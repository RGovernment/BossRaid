using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class HitEffect : MonoBehaviour
{
    public ParticleSystem rootParticle;
    public AudioSource hitSound;

    private void Awake()
    {
        if (rootParticle == null) rootParticle = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        if(hitSound != null)
            hitSound.PlayOneShot(hitSound.clip);
    }

    private void OnDisable()
    {
        // 스킬이 도중에 풀로 회수되거나 강제로 꺼질 때 잔상 제거용
        if (rootParticle != null)
        {
            rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
