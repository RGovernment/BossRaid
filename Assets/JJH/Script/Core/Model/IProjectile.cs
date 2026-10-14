using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IProjectile
{
    public UniTask LifeTimeEnd();
    public void Move(Vector3 direction, float speed);
    public void Shoot(Vector3 direction);
    public void Stop();
}
