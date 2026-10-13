using UnityEngine;


public struct tAttackInfo
{
    public Vector3 AttackDirection;
    public float Damage;

    public tAttackInfo(Vector3 _vAttackDirection , float _fDamage)
    {
        AttackDirection = _vAttackDirection;
        Damage = _fDamage;
    }
}
public interface ITakeDamgeable
{
    void TakeDamage(tAttackInfo _tInfo);
}

public class ObjectInfo 
{
    public float CurrentHP = 100.0f;
    public float CurrentSpeed;

}


