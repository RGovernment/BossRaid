using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using SF = UnityEngine.SerializeField;
[RequireComponent(typeof(ParticleSystem))]
public class SkillParticleTrigger : MonoBehaviour
{
    [SF] private SkillBase skillRoot;

    [SF] private ParticleSystem ps;
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void OnParticleSystemStopped()
    {
        if (skillRoot != null)
            skillRoot.OnParticleSystemStoppedByChild();
    }

}
