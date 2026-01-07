#if !UNITY_SERVER
using System.Numerics;

public interface IWeapon
{
    void Shoot(bool isAiming);
}
#endif