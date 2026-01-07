#if !UNITY_SERVER
using UnityEngine;

public struct CameraInput
{
    public Vector2 look;
}

public class PlayerCamera : MonoBehaviour
{
    private Vector3 eulerAngles;
    private Vector2 recoilOffset;          // Recul actuel
    private Vector2 currentRecoilVelocity; // Pour un retour fluide

    public Vector3 offset;

    [SerializeField] private float sensistivity = 0.1f;
    [SerializeField] private float verticalClampMin = -60f;
    [SerializeField] private float verticalClampMax = 60f;
    [SerializeField] private Player player;

    public Transform targetTransform;
    public bool isAiming = false;

    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float aimFOV = 50f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float recoilReturnSpeed = 8f;

    [SerializeField] private Camera camera;

    public bool IsAiming
    {
        get { return isAiming; }
        set { isAiming = value; }
    }

    private void Update()
    {
        // Gérer le FOV pour le zoom
        float targetFOV = isAiming ? aimFOV : defaultFOV;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);

        // Retour progressif du recul à zéro
        recoilOffset = Vector2.SmoothDamp(recoilOffset, Vector2.zero, ref currentRecoilVelocity, 1f / recoilReturnSpeed);

        // Appliquer le recul à la rotation actuelle
        Vector3 totalRotation = eulerAngles;
        totalRotation.x += recoilOffset.y;
        totalRotation.y += recoilOffset.x;

        totalRotation.x = Mathf.Clamp(totalRotation.x, verticalClampMin, verticalClampMax);
        transform.eulerAngles = totalRotation;
    }

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = eulerAngles = target.eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        if (player.CanMove)
            transform.position = targetTransform.position;
    }

    public void UpdateRotation(CameraInput input)
    {
        if (player.CanMove)
        {
            eulerAngles += new Vector3(-input.look.y, input.look.x) * sensistivity;
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, verticalClampMin, verticalClampMax);
        }
        // Recul est appliqué dans Update

    }

    /// <summary>
    /// Applique un recul à la caméra (x = horizontal, y = vertical)
    /// </summary>
    public void ApplyRecoil(Vector2 recoil)
    {
        recoilOffset += recoil;
    }
}
#endif
