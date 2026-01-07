#if !UNITY_SERVER
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapon")]
public class Weapon : ScriptableObject
{
    public GameObject m_model;

    [Header("Attachment details")]
    public Vector3 m_aimCameraPosition = Vector3.zero;
    public Quaternion m_aimCameraRotation = Quaternion.identity;

    [Header("Weapon Positions")]
    public Vector3 m_defaultPosition = Vector3.zero;
    public Quaternion m_defaultRotation = Quaternion.identity;

    public Vector3 m_aimingStancePosition = Vector3.zero;
    public Quaternion m_aimingStanceRotation = Quaternion.identity;

    //public List<BoneOverride> m_defaultBonesOverrides;
    //public List<BoneOverride> m_aimBonesOverrides;
    public List<BoneOverride> m_bonesOverrides;

    [Header("Recoil")]
    [Range(0.05f, 0.2f)] public float horizontalFactor = 0.08f;
    [Range(0.05f, 0.1f)] public float verticalFactor = 0.08f;

    [Header("Preview Settings"), Description("The model to which the weapon will be attached")]
    public GameObject m_character;
    [Description("Bone on which weapon will be attached")]
    public Transform m_bone;

    public int baseCapacity = 30;
    public float fireRate = 10f;
    public GameObject bulletPrefab;
}
#endif