using UnityEngine;

public interface IProjectile
{
    public void Move(Vector3 direction, float speed);
    public void Shoot(Vector3 direction);
    public void Stop();
}
