#if !UNITY_SERVER
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    public GameObject m_model;
    public GameObject fpsArmModel;

    public GameObject bullet;
    public GameObject shootEffectPrefab;
    [Header("Aim Camera Position")]
    public Vector3 m_aimCameraPosition = Vector3.zero;
    public Quaternion m_aimCameraRotation = Quaternion.identity;

    public int baseCapacity = 30;
    public float fireRate = 10f;

    [Header("Weapon Positions")]
    public Vector3 m_defaultPosition = Vector3.zero;
    public Quaternion m_defaultRotation = Quaternion.identity;

    public Vector3 m_aimingStancePosition = Vector3.zero;
    public Quaternion m_aimingStanceRotation = Quaternion.identity;

    public List<BoneOverride> m_bonesOverrides;

    [Header("Preview Settings"), Description("The model to which the weapon will be attached")]
    public GameObject m_character;
    [Description("Bone on which weapon will be attached")]
    public Transform m_bone;

}
#endif