using UnityEngine;


public struct tAttackInfo
{
    public Vector3 Pos;
    public float Damage;

    public tAttackInfo(Vector3 _Pos , float _fDamage)
    {
        Pos = _Pos;
        Damage = _fDamage;
    }
}
public interface ITakeDamgeable
{
    void TakeDamage(tAttackInfo _tInfo);
}

public class ObjectInfo 
{
    float CurrentHP;
    float CurrentSpeed;

}


