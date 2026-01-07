
#if !UNITY_SERVER
using System;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.Animations;
using UnityEditor;
using System.Collections;


[System.Serializable]
public class BoneOverride
{
    public string m_Name;
    [HideInInspector] public GameObject gameObject;

    #region Target & Hint data
    /*[HideInInspector]*/
    public Vector3 m_targetDefaultPosition;
    /*[HideInInspector]*/
    public Quaternion m_targetDefaultRotation;
    /*[HideInInspector]*/
    public Vector3 m_targetAimStancePosition;
    /*[HideInInspector]*/
    public Quaternion m_targetAimStanceRotation;
    /*[HideInInspector]*/
    public Vector3 m_hintDefaultPosition;
    /*[HideInInspector]*/
    public Quaternion m_hintDefaultRotation;
    /*[HideInInspector]*/
    public Vector3 m_hintAimStancePosition;
    /*[HideInInspector]*/
    public Quaternion m_hintAimStanceRotation;
    #endregion
    [HideInInspector] public Transform m_Root;
    [HideInInspector] public Transform m_Mid;
    [HideInInspector] public Transform m_Tip;

    [HideInInspector, SyncSceneToStream, SerializeField] public Transform m_Target;
    [HideInInspector, SyncSceneToStream, SerializeField] public Transform m_Hint;
    [SyncSceneToStream, SerializeField, Range(0f, 1f)] public float m_TargetPositionWeight;
    [SyncSceneToStream, SerializeField, Range(0f, 1f)] public float m_TargetRotationWeight;
    [SyncSceneToStream, SerializeField, Range(0f, 1f)] public float m_HintWeight;

    [NotKeyable, SerializeField] public bool m_MaintainTargetPositionOffset;
    [NotKeyable, SerializeField] public bool m_MaintainTargetRotationOffset;


    public float targetPositionWeight { get => m_TargetPositionWeight; set => m_TargetPositionWeight = Mathf.Clamp01(value); }
    public float targetRotationWeight { get => m_TargetRotationWeight; set => m_TargetRotationWeight = Mathf.Clamp01(value); }
    public float hintWeight { get => m_HintWeight; set => m_HintWeight = Mathf.Clamp01(value); }


    public bool maintainTargetPositionOffset { get => m_MaintainTargetPositionOffset; set => m_MaintainTargetPositionOffset = value; }

    public bool maintainTargetRotationOffset { get => m_MaintainTargetRotationOffset; set => m_MaintainTargetRotationOffset = value; }

    public void TwoBoneIKAutoSetup(GameObject parent = null)
    {
        if (!this.m_Tip) { Debug.LogWarning("Not tip transform has been set, action aborted."); return; }
        var tip = m_Tip;
       
        this.gameObject = new GameObject(tip.name + "_Rig");
        if (parent != null)
            this.gameObject.transform.SetParent(parent.transform, true);
            //    this.gameObject.transform.SetParent(FPSManager.Instance.FPSWeaponManager.m_equipedWeapon.transform.Find("Mag").transform, true);
            var animator = gameObject.GetComponentInParent<Animator>()?.transform;
#if UNITY_EDITOR
        if (!tip)
        {
            var selection = Selection.transforms;
            var constraintInSelection = false;

            if (animator)
            {
                for (int i = 0; i < selection.Length; i++)
                {
                    if (selection[i].IsChildOf(animator))
                    {
                        if (selection[i] != gameObject.transform)
                        {
                            tip = selection[i];
                            break;
                        }
                        else
                        {
                            constraintInSelection = true;
                        }
                    }
                }
            }

            if (!tip && constraintInSelection)
                tip = gameObject.transform;

            if (!tip)
            {
                Debug.LogWarning("Please provide a tip before running auto setup!");
                return;
            }
        }
#endif
        if (!m_Mid)
        {
            m_Mid = tip.parent;
        }

        if (!m_Root)
        {
            m_Root = tip.parent.parent;
        }

        if (!m_Target)
        {
            //var target = gameObject.transform.Find(gameObject.name + "_target");
            Transform target;
            //if (target == null)
            {
                var t = new GameObject();
                t.name = gameObject.name + "_target";
                //t.transform.localScale = .1f * t.transform.localScale;
                t.transform.SetParent(gameObject.transform, false);
                target = t.transform;
            }
            m_Target = target;
        }

        if (!m_Hint)
        {
            //var hint = gameObject.transform.Find(gameObject.name + "_hint");
            Transform hint;
            //if (hint == null)
            {
                var t = new GameObject();
                t.name = gameObject.name + "_hint";
                //t.transform.localScale = .1f * t.transform.localScale;
                t.transform.SetParent(gameObject.transform, false);
                hint = t.transform;
            }
            m_Hint = hint;
        }

        Vector3 rootPosition = m_Root.position;
        Vector3 midPosition = m_Mid.position;
        Vector3 tipPosition = tip.position;
        Quaternion tipRotation = tip.rotation;
        Vector3 targetPosition = m_Target.position;
        Quaternion targetRotation = m_Target.rotation;
        Vector3 hintPosition = m_Hint.position;
        float posWeight = 1.0f;
        float rotWeight = 1.0f;
        float hintWeight = 1.0f;
        AffineTransform targetOffset = new AffineTransform(Vector3.zero, Quaternion.identity);

        AnimationRuntimeUtils.InverseSolveTwoBoneIK(rootPosition, midPosition, tipPosition, tipRotation,
            ref targetPosition, ref targetRotation, ref hintPosition, true, posWeight, rotWeight, hintWeight, targetOffset);

        m_Target.position = targetPosition;
        m_Target.rotation = targetRotation;
        m_Hint.position = hintPosition;
    }

    static float TriangleAngle(float aLen, float aLen1, float aLen2)
    {
        float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
        return Mathf.Acos(c);
    }

    const float k_SqrEpsilon = 1e-8f;

    public void SolveTwoBoneIK(
     Vector3 targetOffsetTranslation,
     Quaternion targetOffsetRotation
 )
    {
        if (!m_Root || !m_Mid || !m_Tip || !m_Target)
        {
            Debug.LogWarning("Transforms not assigned");
            return;
        }

        const float positionTolerance = 0.001f; // Threshold for position convergence
        const float rotationTolerance = 0.1f;   // Threshold for rotation convergence (degrees)
        const int maxIterations = 10;           // Maximum iterations to avoid infinite loops

        int iteration = 0;
        bool hasConverged = false;

        while (!hasConverged && iteration < maxIterations)
        {
            hasConverged = true; // Assume convergence unless proven otherwise

            // Cache previous positions and rotations for comparison
            Vector3 previousTipPosition = m_Tip.position;
            Quaternion previousTipRotation = m_Tip.rotation;

            // Extract positions and rotations
            Vector3 aPosition = m_Root.position;
            Vector3 bPosition = m_Mid.position;
            Vector3 cPosition = m_Tip.position;

            Vector3 targetPos = m_Target.position + targetOffsetTranslation;
            Quaternion targetRot = m_Target.rotation * targetOffsetRotation;

            Vector3 tPosition = Vector3.Lerp(cPosition, targetPos, m_TargetPositionWeight);
            Quaternion tRotation = Quaternion.Lerp(m_Tip.rotation, targetRot, m_TargetRotationWeight);

            bool hasHint = m_Hint != null && hintWeight > 0f;

            // Compute vectors and lengths
            Vector3 ab = bPosition - aPosition;
            Vector3 bc = cPosition - bPosition;
            Vector3 ac = cPosition - aPosition;
            Vector3 at = tPosition - aPosition;

            float abLen = ab.magnitude;
            float bcLen = bc.magnitude;
            float acLen = ac.magnitude;
            float atLen = at.magnitude;

            float oldAbcAngle = TriangleAngle(acLen, abLen, bcLen);
            float newAbcAngle = TriangleAngle(atLen, abLen, bcLen);

            // Calculate bend axis
            Vector3 axis = Vector3.Cross(ab, bc);
            if (axis.sqrMagnitude < k_SqrEpsilon)
            {
                axis = hasHint ? Vector3.Cross(m_Hint.position - aPosition, bc) : Vector3.zero;

                if (axis.sqrMagnitude < k_SqrEpsilon)
                    axis = Vector3.Cross(at, bc);

                if (axis.sqrMagnitude < k_SqrEpsilon)
                    axis = Vector3.up;
            }
            axis.Normalize();

            // Mid-joint rotation
            float angleDelta = 0.5f * (oldAbcAngle - newAbcAngle);
            Quaternion deltaRotation = Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, axis);
            m_Mid.rotation = deltaRotation * m_Mid.rotation;

            // Root rotation to align AC with AT
            cPosition = m_Tip.position; // Update tip position after mid rotation
            ac = cPosition - aPosition;
            m_Root.rotation = Quaternion.FromToRotation(ac, at) * m_Root.rotation;

            // Apply hint adjustment
            if (hasHint)
            {
                float acSqrMag = ac.sqrMagnitude;
                if (acSqrMag > 0f)
                {
                    bPosition = m_Mid.position;
                    cPosition = m_Tip.position;
                    ab = bPosition - aPosition;
                    ac = cPosition - aPosition;

                    Vector3 acNorm = ac / Mathf.Sqrt(acSqrMag);
                    Vector3 ah = m_Hint.position - aPosition;
                    Vector3 abProj = ab - acNorm * Vector3.Dot(ab, acNorm);
                    Vector3 ahProj = ah - acNorm * Vector3.Dot(ah, acNorm);

                    float maxReach = abLen + bcLen;
                    if (abProj.sqrMagnitude > (maxReach * maxReach * 0.001f) && ahProj.sqrMagnitude > 0f)
                    {
                        Quaternion hintRotation = Quaternion.FromToRotation(abProj, ahProj);
                        hintRotation = Quaternion.Lerp(Quaternion.identity, hintRotation, hintWeight);
                        m_Root.rotation = hintRotation * m_Root.rotation;
                    }
                }
            }

            // Set final tip rotation
            m_Tip.rotation = tRotation;

            // Check for convergence
            if (Vector3.Distance(previousTipPosition, m_Tip.position) > positionTolerance ||
                Quaternion.Angle(previousTipRotation, m_Tip.rotation) > rotationTolerance)
            {
                hasConverged = false;
            }

            iteration++;
        }

        if (iteration == maxIterations)
        {
            Debug.LogWarning($"SolveTwoBoneIK did not converge within {maxIterations} iterations.");
        }
    }

    public void SmoothResetWeights()
    {
        m_TargetPositionWeight = 0f;
        m_TargetRotationWeight = 0f;
        m_HintWeight = 0f;
    }

    public void SmoothWeights()
    {
        m_TargetPositionWeight = 1f;
        m_TargetRotationWeight = 1f;
        m_HintWeight = 1f;
    }

}
#endif