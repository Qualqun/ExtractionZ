#if !UNITY_SERVER
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
public struct CharacterWeaponInput
{
    public bool aim;
    public bool shoot;
    public bool equipWeapon;
    public bool reload;
}
public class PlayerWeapon : MonoBehaviour
{

    [SerializeField]
    private Player player;
    public List<WeaponData> weapons = new List<WeaponData>();
    private WeaponData runtimeWeapon;
    private int equipedWeaponId = -1;

    private GameObject equipedWeaponGameObject;
    private GameObject equipedFPSWeaponGameObject;

    private GameObject fpsArms;

    private bool canReload = true;
    private bool canShoot = true;

    [SerializeField] private float transitionSpeed = 5.0f;
    private Transform m_originalTransform;
    private Vector3 m_originalPosition;
    private Quaternion m_originalRotation;
    private bool m_cameraAttachedToWeapon = false;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    public WeaponData RuntimeWeapon => runtimeWeapon;

    private bool requestedAim;
    private bool requestedShoot;
    public bool requestedEquipWeapon;
    private bool requestedReload;

    public GameObject EquipedWeaponGameObject => equipedWeaponGameObject;

    [SerializeField]
    private Transform shootingFPSPosition;

    [SerializeField]
    private Transform fpsObjects;

    [SerializeField]
    private Transform aimFPSPosition;

    private Quaternion originalShootingRotation;

    private Quaternion currentRecoilRotation;
    private float recoilTimer = 0f;
    private bool isReloading = false;

    [SerializeField] private float recoilAngle = 5f;
    [SerializeField] private float recoilDuration = 0.1f;
    [SerializeField] private float recoilRecoverySpeed = 5f;

    public Animator playerFPSAnimator;
    public Animator weaponAnimator;
    private Transform currentTarget;


    public GameObject GetArms => fpsArms;
    public void UpdateWeaponInput(CharacterWeaponInput characterWeaponInput)
    {
        requestedAim = characterWeaponInput.aim;
        requestedShoot = characterWeaponInput.shoot;
        requestedEquipWeapon = characterWeaponInput.equipWeapon;
        requestedReload = characterWeaponInput.reload;
    }
    public void ApplyRecoil()
    {
        recoilTimer = recoilDuration;

        // Applique un petit angle vers le haut
        currentRecoilRotation = Quaternion.Euler(-recoilAngle, 0f, 0f);

        player.fpsCamera.transform.localRotation = originalShootingRotation * currentRecoilRotation;
    }


    private void Start()
    {

        player = transform.parent.GetComponent<Player>();
        PedMonobehaviour pedMonobehaviour = GetComponent<PedMonobehaviour>();
        if (pedMonobehaviour.hasControl)
        {

            m_originalPosition = Camera.main.transform.localPosition;
            m_originalRotation = Camera.main.transform.localRotation;
            m_originalTransform = Camera.main.transform.parent;

            UIManager.Instance.AddElement(UIElementEnum.HIT_MARKER, GameObject.Find("HitMarker"));
            UIManager.Instance.AddElement(UIElementEnum.CROSS_HAIR, GameObject.Find("Crosshair"));
            UIManager.Instance.AddElement(UIElementEnum.WEAPON_CAPACITY_TEXT, GameObject.Find("MagCapacity"));
            UIManager.Instance.AddElement(UIElementEnum.HEALTH_POINT_TEXT, GameObject.Find("LifePoint"));
            
        }
    }

    void Update()
    {
        PedMonobehaviour pedMonobehaviour = GetComponent<PedMonobehaviour>();
        if (pedMonobehaviour.hasControl)
        {
            if (player.fpsCamera != null && shootingFPSPosition == null && aimFPSPosition == null)
            {
                shootingFPSPosition = player.fpsCamera.transform.Find("ShootingPosition");
                aimFPSPosition = player.fpsCamera.transform.Find("AimPosition");
                fpsObjects = shootingFPSPosition.Find("FPSObjects");

                originalShootingRotation = player.fpsCamera.transform.localRotation;
            }

            if (equipedWeaponGameObject != null && equipedWeaponId != -1)
            {
                targetPosition = requestedAim ? runtimeWeapon.m_aimingStancePosition : weapons[equipedWeaponId].m_defaultPosition;
                targetRotation = requestedAim ? runtimeWeapon.m_aimingStanceRotation : weapons[equipedWeaponId].m_defaultRotation;
            }


            if (player.IsMoving && equipedFPSWeaponGameObject != null && !requestedAim)
            {
                equipedFPSWeaponGameObject.GetComponent<WeaponController>().SetWalkingParameters(player.IsRunning);

            }
            if (equipedFPSWeaponGameObject != null)
                equipedFPSWeaponGameObject.GetComponent<WeaponController>().IsAiming = requestedAim;

            if (equipedFPSWeaponGameObject == null)
                Swap();

            if (requestedShoot && canShoot)
            {
                if (requestedAim)
                    Shooting();
                else
                {
                    if (!player.IsRunning)
                        Shooting();
                }
            }
               

            if (requestedReload)
            {
                Reload();
            }



            if (equipedFPSWeaponGameObject != null)
            {
                currentTarget = requestedAim ? aimFPSPosition : shootingFPSPosition;

                fpsObjects.position = Vector3.Lerp(
                 fpsObjects.position,
                 currentTarget.position,
                 Time.deltaTime * 10f
                );


                fpsObjects.rotation = Quaternion.Slerp(
                fpsObjects.rotation,
                currentTarget.rotation,
                Time.deltaTime * 10f
            );

                //if (recoilTimer > 0)
                //{
                //    recoilTimer -= Time.deltaTime;

                //    // Lerp vers la rotation de base (retour doux)
                //    player.fpsCamera.transform.localRotation = Quaternion.Lerp(
                //         player.fpsCamera.transform.localRotation,
                //        originalShootingRotation,
                //        Time.deltaTime * recoilRecoverySpeed
                //    );

                //    Debug.Log(player.fpsCamera.transform.localRotation);

                //}

            }


            if (weaponAnimator != null && playerFPSAnimator != null)
            {
                //print(equipedFPSWeaponGameObject.GetComponent<WeaponController>().CurrentBulletAmount);
                if (player.PlayerCharacter.IsClimbing)
                {
                    weaponAnimator.SetBool("IsClimbing", true);
                    playerFPSAnimator.SetBool("IsClimbing", true);
                }
                else
                {
                    weaponAnimator.SetBool("IsClimbing", false);
                    playerFPSAnimator.SetBool("IsClimbing", false);
                }

                if (player.IsRunning)
                {
                    //Debug.Log(weaponAnimator.GetBool("IsRunning"));
                    weaponAnimator.SetBool("IsRunning", true);
                    playerFPSAnimator.SetBool("IsRunning", true);
                }
                else
                {
                    weaponAnimator.SetBool("IsRunning", false);
                    playerFPSAnimator.SetBool("IsRunning", false);
                }

                if (requestedAim)
                {
                    weaponAnimator.SetBool("IsAiming", true);
                    playerFPSAnimator.SetBool("IsAiming", true);
                }
                else
                {
                    weaponAnimator.SetBool("IsAiming", false);
                    playerFPSAnimator.SetBool("IsAiming", false);
                }

                if (isReloading)
                {
                    weaponAnimator.SetBool("IsReloading", true);
                    playerFPSAnimator.SetBool("IsReloading", true);
                }
                else
                {
                    weaponAnimator.SetBool("IsReloading", false);
                    playerFPSAnimator.SetBool("IsReloading", false);
                }

                if (player.PlayerCharacter.State.stance == Stance.Slide)
                {
                    weaponAnimator.SetBool("IsSliding", true);
                    playerFPSAnimator.SetBool("IsSliding", true);
                }
                else
                {
                    weaponAnimator.SetBool("IsSliding", false);
                    playerFPSAnimator.SetBool("IsSliding", false);
                }
            }





            if (requestedAim && equipedFPSWeaponGameObject != null)
            {
                UIManager.Instance.SetActiveElement(UIElementEnum.CROSS_HAIR, false);
            }
            else
            {
                UIManager.Instance.SetActiveElement(UIElementEnum.CROSS_HAIR, true);
            }

        }
    }
    private void LateUpdate()
    {
        if (equipedWeaponGameObject != null && equipedWeaponId != -1 && runtimeWeapon != null)
        {

            foreach (var boneOverride in runtimeWeapon.m_bonesOverrides)
            {
                {
                    ref var realTargetPosition = ref (requestedAim)
                                    ? ref boneOverride.m_targetAimStancePosition
                                    : ref boneOverride.m_targetDefaultPosition;

                    ref var realTargetRotation = ref (requestedAim)
                        ? ref boneOverride.m_targetAimStanceRotation
                        : ref boneOverride.m_targetDefaultRotation;

                    Vector3 worldPosition = equipedWeaponGameObject.transform.TransformPoint(realTargetPosition);
                    Quaternion worldRotation = equipedWeaponGameObject.transform.rotation * realTargetRotation;


                    if (boneOverride == null)
                    {
                        Debug.LogWarning("boneOverride est null.");
                    }
                    else if (boneOverride.m_Target == null)
                    {
                        Debug.LogWarning("boneOverride.m_Target est null.");
                    }
                    else if (boneOverride.m_Target.transform == null)
                    {
                        Debug.LogWarning("boneOverride.m_Target.transform est null.");
                    }
                    else
                    {
                        boneOverride.m_Target.transform.position = worldPosition;
                        boneOverride.m_Target.transform.rotation = worldRotation;
                    }

                }
                {
                    ref var realHintPosition = ref (requestedAim)
                          ? ref boneOverride.m_hintAimStancePosition
                          : ref boneOverride.m_hintDefaultPosition;

                    ref var realHintRotation = ref (requestedAim)
                        ? ref boneOverride.m_hintAimStanceRotation
                        : ref boneOverride.m_hintDefaultRotation;

                    Vector3 worldPosition = equipedWeaponGameObject.transform.TransformPoint(realHintPosition);
                    Quaternion worldRotation = equipedWeaponGameObject.transform.rotation * realHintRotation;

                    if (boneOverride == null)
                    {
                        Debug.LogWarning("boneOverride est null.");
                    }
                    else if (boneOverride.m_Hint == null)
                    {
                        Debug.LogWarning("boneOverride.m_Hint est null.");
                    }
                    else if (boneOverride.m_Hint.transform == null)
                    {
                        Debug.LogWarning("boneOverride.m_Hint.transform est null.");
                    }
                    else
                    {
                        boneOverride.m_Hint.transform.position = worldPosition;
                        boneOverride.m_Hint.transform.rotation = worldRotation;
                    }

                }
                boneOverride.SolveTwoBoneIK(Vector3.zero, Quaternion.identity);
            }
        }
    }



    public void Swap(bool createFPSWeapon = true)
    {
        PedMonobehaviour pedMonobehaviour = GetComponent<PedMonobehaviour>();
        if (requestedEquipWeapon)
        {
            int previousWeapon = equipedWeaponId;
            equipedWeaponId++;
            if (equipedWeaponId >= weapons.Count)
            {
                equipedWeaponId = -1;
            }
            if (equipedWeaponId != previousWeapon)
            {
                if (equipedWeaponGameObject)
                {
                    Object.DestroyImmediate(equipedWeaponGameObject);
                    Object.DestroyImmediate(equipedFPSWeaponGameObject);
                }
                if (equipedWeaponId == -1)
                {
                    if (pedMonobehaviour.hasControl)
                    {
                        player.HideOverlay();
                    }
                    player.LayerState = AnimatorLayerState.Locomotion;
                }
                else if (equipedWeaponId < weapons.Count && weapons[equipedWeaponId] != null)
                {
                    if (runtimeWeapon != null)
                        DestroyImmediate(runtimeWeapon);

                    runtimeWeapon = Instantiate(weapons[equipedWeaponId]);
                    equipedWeaponGameObject = Instantiate(runtimeWeapon.m_model);
                    equipedWeaponGameObject.GetComponent<Animator>().enabled = false;
                    equipedWeaponGameObject.GetComponent<WeaponController>().pedMonobehaviour = pedMonobehaviour;
                    if (pedMonobehaviour.hasControl)
                    {

                        SetLayerRecursively(equipedWeaponGameObject, 8);
                    }


                    Transform[] boneTransforms = this.GetComponentsInChildren<Transform>();

                    for (int i = 0; i < boneTransforms.Length; i++)
                    {
                        if (boneTransforms[i].name == runtimeWeapon.m_bone.name)
                        {
                            equipedWeaponGameObject.transform.SetParent(boneTransforms[i], false);
                            break;
                        }
                    }
                    equipedWeaponGameObject.transform.localPosition = runtimeWeapon.m_defaultPosition;
                    equipedWeaponGameObject.transform.localRotation = runtimeWeapon.m_defaultRotation;


                    Transform[] m_boneTransforms = this.GetComponentsInChildren<Transform>();
                    string[] m_boneNames = new string[m_boneTransforms.Length];

                    for (int i = 0; i < m_boneTransforms.Length; i++)
                    {
                        m_boneNames[i] = m_boneTransforms[i].name;
                    }
                    // WIP
                    foreach (var boneOverride in runtimeWeapon.m_bonesOverrides)
                    {
                        int transformIndex = 0;
                        for (int boneId = 0; boneId < m_boneTransforms.Length; boneId++)
                        {
                            if (m_boneTransforms[boneId].name == boneOverride.m_Name)
                            {
                                transformIndex = boneId;
                                break;
                            }
                        }
                        boneOverride.m_Tip = m_boneTransforms[transformIndex];
                        if (boneOverride == null || boneOverride.m_Target == null || boneOverride.m_Hint == null || boneOverride.m_Tip == null)
                        {
                            boneOverride.TwoBoneIKAutoSetup(/*player.TempPlayer.gameObject*/);
                            if (boneOverride == null || boneOverride.m_Target == null || boneOverride.m_Hint == null || boneOverride.m_Tip == null)
                            {
                                Debug.LogWarning("Unable to correctly auto setup the constraint, action aborted.");
                                return;
                            }
                        }
                    }


                    if (pedMonobehaviour.hasControl && createFPSWeapon)
                    {
                        player.LayerState = AnimatorLayerState.Armed;
                        equipedFPSWeaponGameObject = Instantiate(runtimeWeapon.m_model);
                        equipedFPSWeaponGameObject.transform.SetParent(fpsObjects, false);
                        equipedFPSWeaponGameObject.transform.localPosition = Vector3.zero;
                        equipedFPSWeaponGameObject.transform.localRotation = Quaternion.identity;

                        weaponAnimator = equipedFPSWeaponGameObject.GetComponent<Animator>();

                        equipedFPSWeaponGameObject.GetComponent<WeaponController>().pedMonobehaviour = pedMonobehaviour;
                        SetLayerRecursively(equipedFPSWeaponGameObject, 7);
                        player.ShowOverlay();

                        fpsArms = Instantiate(runtimeWeapon.fpsArmModel);
                        fpsArms.transform.SetParent(fpsObjects, false);
                        fpsArms.transform.localPosition = Vector3.zero;
                        fpsArms.transform.localRotation = Quaternion.identity;

                        playerFPSAnimator = fpsArms.GetComponent<Animator>();
                        SetLayerRecursively(fpsArms, 7);

                    }
                }
            }
            requestedEquipWeapon = false;

        }
    }

    private void Shooting()
    {
        PedMonobehaviour pedMonobehaviour = GetComponent<PedMonobehaviour>();
        if (equipedWeaponGameObject != null && equipedWeaponId != -1)
        {
            WeaponController weaponController = equipedWeaponGameObject.GetComponent<WeaponController>();
            WeaponController weaponFPSController = equipedFPSWeaponGameObject.GetComponent<WeaponController>();
            if (weaponController.HaveAmmo)
            {
                weaponFPSController.Shoot(requestedAim);
                weaponFPSController.HandleMuzzleFlash();
                ApplyRecoil();
                player.PlayerCamera.ApplyRecoil(new Vector2(Random.Range(-0.2f, 0.2f), -0.2f)); // Caméra monte légèrement



                //if (weaponAnimator != null)
                //    weaponAnimator.SetTrigger("IsShooting");
            }
            else
            {
                weaponFPSController.MuzzleFlash.Stop();
                //weaponFPSController.BulletCasing.Stop();
            }




          

        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void Reload()
    {
        if (equipedFPSWeaponGameObject != null && equipedWeaponId != -1 && canReload && equipedFPSWeaponGameObject.GetComponent<WeaponController>().CanReload)
        {
            canReload = false;
            canShoot = false;
            isReloading = true;
            StartCoroutine(ReloadCooldown());
        }
    }

    private IEnumerator ReloadCooldown()
    {
        //yield return new WaitForSeconds(0.5f);
       
        yield return new WaitForSeconds(1.5f);
        canReload = true;
        canShoot = true;
        isReloading = false;
        equipedFPSWeaponGameObject.GetComponent<WeaponController>().Reload();
    }

}

#endif
