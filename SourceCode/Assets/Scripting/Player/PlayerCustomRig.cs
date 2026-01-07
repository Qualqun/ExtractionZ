#if !UNITY_SERVER
using JetBrains.Annotations;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerCustomRig : MonoBehaviour
{
    private Player player;
    [HideInInspector] private RigBuilder m_rigBuilder;
    [HideInInspector] private GameObject m_rigGameObject;
    [HideInInspector] private Rig m_rig;
    #region Pickups
    [HideInInspector] private GameObject m_pickupRightArmRig;
    [HideInInspector] public ChainIKConstraint m_pickupConstraint;
    #endregion

    #region Hands
    [HideInInspector] private GameObject m_leftHandRig;
    [HideInInspector] public TwoBoneIKConstraint m_leftHandConstraint;
    [HideInInspector] private GameObject m_rightHandRig;
    [HideInInspector] public TwoBoneIKConstraint m_rightHandConstraint;
    #endregion
    //public Transform m_targetTransform;
    [Header("FBA Settings")]
    [Range(1f, 10f)] public float m_rotationSpeed = 5f;
    [Range(0f, 1f)] public float m_weight = 1.0f;

    private float m_yaw = 0f;
    private float m_pitch = 0f;
    private int m_interations = 10;

    private Transform[] m_bonesTransform;

    private Vector2 recoilOffset = Vector2.zero;

    private bool disabled;

    public bool Disabled
    {
        get { return disabled; }
        set { disabled = value; }
    }

    void Start()
    {
        player = transform.parent.GetComponent<Player>();
        m_rigBuilder = transform.AddComponent<RigBuilder>();


        m_rigGameObject = new GameObject("Rig");
        m_rigGameObject.transform.parent = this.transform;

        m_rig = m_rigGameObject.AddComponent<Rig>();

        m_rigBuilder.layers.Add(new RigLayer(m_rig));
        ConfigureHands();

        m_rigBuilder.Build();

        InitializeBones();
    }


    private void ConfigureHands()
    {
        m_leftHandRig = new GameObject("LeftHand");
        m_leftHandRig.transform.SetParent(m_rigGameObject.transform, false);
        m_leftHandConstraint = m_leftHandRig.AddComponent<TwoBoneIKConstraint>();
        m_leftHandConstraint.weight = 0.0f;

        m_rightHandRig = new GameObject("RightHand");
        m_rightHandRig.transform.SetParent(m_rigGameObject.transform, false);
        m_rightHandConstraint = m_rightHandRig.AddComponent<TwoBoneIKConstraint>();
        m_rightHandConstraint.weight = 0.0f;
    }

    public void UpdateFBA()
    {    
        if (player != null && Game.Instance.playerList.Count != 0)
        {
            PedMonobehaviour pedMonobehaviour = GetComponent<PedMonobehaviour>();
            for (int i = 0; i < m_interations; i++)
            {
                List<HumanBone> bones = player.GetBonesForLayer(player.LayerState);
                for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
                {
                    HumanBone bone = bones[boneIndex];
                    Transform boneTransform = player.Animator.GetBoneTransform(bone.m_bone);
                    if (boneTransform != null)
                    {
                        if (pedMonobehaviour.hasControl)
                            AimInCameraDirection(boneTransform,bone.m_weight,bone.m_pitchMin, bone.m_pitchMax, bone.m_yawMin, bone.m_yawMax);
                        else
                            NetworkAimInTargetDirection(boneTransform, bone.m_weight, bone.m_pitchMin, bone.m_pitchMax, bone.m_yawMin, bone.m_yawMax);
                    }
                }
            }

         
            if (pedMonobehaviour.hasControl)
            {
                PlayerSyncedData replicatedSyncedData = Game.Instance.entityManager.GetComponentData<PlayerSyncedData>(pedMonobehaviour.entity);
                replicatedSyncedData.targetPos = lastTargetDirection;
                Game.Instance.entityManager.SetComponentData(pedMonobehaviour.entity, replicatedSyncedData);
            }
            else
            {
                ReplicatedPlayerSyncedData replicatedSyncedData = Game.Instance.entityManager.GetComponentData<ReplicatedPlayerSyncedData>(pedMonobehaviour.entity);
                lastTargetDirection = replicatedSyncedData.targetPos;
            }
        }
    }


    private void InitializeBones()
    {
        List<HumanBone> bones = player.GetBonesForLayer(player.LayerState);
        m_bonesTransform = new Transform[bones.Count];
        for (int i = 0; i < m_bonesTransform.Length; i++)
        {
            m_bonesTransform[i] = player.Animator.GetBoneTransform(bones[i].m_bone);
        }
    }
    public void ApplyRecoilOffset(Vector3 offset)
    {
        recoilOffset = offset;
    }

    public Vector3 lastTargetDirection = Vector3.forward;

    public Vector3 LastTargetDirection
    {
        get { return lastTargetDirection; }
        set { lastTargetDirection = value; }
    }

    private void AimInCameraDirection(Transform boneTransform, float weight, float pitchMin, float pitchMax, float yawMin, float yawMax)
    {
        Vector3 targetDirection = Camera.main.transform.forward;
        lastTargetDirection = targetDirection; // network Data
        if (!disabled)
        {
            PedMonobehaviour pedMonobehaviour = GetComponent<PedMonobehaviour>();
            if (pedMonobehaviour.hasControl)
            {
                PlayerSyncedData syncedData = Game.Instance.entityManager.GetComponentData<PlayerSyncedData>(pedMonobehaviour.entity);
                syncedData.targetPos = lastTargetDirection;
                Game.Instance.entityManager.SetComponentData(pedMonobehaviour.entity, syncedData);
            }
        }

        Vector3 localTargetDirection = boneTransform.parent.InverseTransformDirection(targetDirection);

        Vector3 euler = Quaternion.LookRotation(localTargetDirection).eulerAngles;

        float pitch = NormalizeAngle(euler.x);
        float yaw = NormalizeAngle(euler.y);

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        yaw = Mathf.Clamp(yaw, yawMin, yawMax);

        Quaternion clampedLocalRotation = Quaternion.Euler(pitch, yaw, 0);

        Quaternion targetWorldRotation = boneTransform.parent.rotation * clampedLocalRotation;

        Quaternion blendedRotation = Quaternion.Slerp(boneTransform.rotation, targetWorldRotation, weight);
        boneTransform.rotation = blendedRotation;
    }

    private void NetworkAimInTargetDirection(Transform boneTransform, float weight, float pitchMin, float pitchMax, float yawMin, float yawMax)
    {
        Vector3 localTargetDirection = boneTransform.parent.InverseTransformDirection(lastTargetDirection);

        Vector3 euler = Quaternion.LookRotation(localTargetDirection).eulerAngles;

        float pitch = NormalizeAngle(euler.x);
        float yaw = NormalizeAngle(euler.y);

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        yaw = Mathf.Clamp(yaw, yawMin, yawMax);

        Quaternion clampedLocalRotation = Quaternion.Euler(pitch, yaw, 0);

        Quaternion targetWorldRotation = boneTransform.parent.rotation * clampedLocalRotation;

        Quaternion blendedRotation = Quaternion.Slerp(boneTransform.rotation, targetWorldRotation, weight);
        boneTransform.rotation = blendedRotation;
    }

    // Helper to normalize angle from 0-360 to -180 to 180
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }


}


#endif