#if !UNITY_SERVER
using UnityEngine;

public class CameraLean : MonoBehaviour
{
    [SerializeField] private float attackDamping = 0.5f;

    [SerializeField] private float decayDamping = 0.3f;

    [SerializeField] private float walkStrength = 0.075f;

    [SerializeField] private float slideStrength = 0.2f;

    [SerializeField] private float strenghtResponse = 5f;

    private Vector3 dampedAcceleration;
    private Vector3 dampedAccelerationVelocity;

    private float smoothStrenght;

    public void Initialize()
    {
        smoothStrenght = walkStrength;
    }

    public void UpdateLean(float deltaTime, bool sliding, Vector3 acceleration, Vector3 up)
    {
        //var planarAcceleration = Vector3.ProjectOnPlane(acceleration, up);
        //var damping = planarAcceleration.magnitude > dampedAcceleration.magnitude
        //    ? attackDamping
        //    : decayDamping;

        //dampedAcceleration = Vector3.SmoothDamp(
        //    current: dampedAcceleration,
        //    target: planarAcceleration,
        //    currentVelocity: ref dampedAccelerationVelocity,
        //    smoothTime: damping,
        //    maxSpeed: float.PositiveInfinity,
        //    deltaTime: deltaTime
        //);

        //// Get the rotation axis based on the acceleration vector.
        //var leanAxis = Vector3.Cross(dampedAcceleration.normalized, up).normalized;

        //// Reset the rotation to that of its parent.
        //transform.localRotation = Quaternion.identity;

        //// Rotate around the lean axis.
        //var targetStrength = sliding
        //    ? slideStrength
        //    : walkStrength;

        //smoothStrenght = Mathf.Lerp(smoothStrenght, targetStrength, 1f - Mathf.Exp(-strenghtResponse * deltaTime));

        //transform.rotation = Quaternion.AngleAxis(-dampedAcceleration.magnitude * smoothStrenght, leanAxis) * transform.rotation;
    }

}
#endif