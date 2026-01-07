#if !UNITY_SERVER
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.VFX;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public class WeaponController : MonoBehaviour, IWeapon
{
    [SerializeField]
    private WeaponData weaponData;
    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private Transform aimFirePoint;

    [SerializeField]
    private int currentBulletAmount;
    private float nextFireTime = 0f;

    private bool isReloading = false;
    private bool isAiming;

    // simulate walk
    private float walkTime = 0f;

    private float walkAmplitudeX = 0.006f;
    private float walkAmplitudeY = 0.004f;
    private float walkFrequency = 6f;

    private float runAmplitudeX = 0.01f;
    private float runAmplitudeY = 0.006f;
    private float runFrequency = 9f;

    private float currentAmplitudeX;
    private float currentAmplitudeY;
    private float currentFrequency;
    private Vector3 initialLocalPosition;
#if !UNITY_SERVER
    [SerializeField]
    AK.Wwise.Event shootSound;
    [SerializeField]
    AK.Wwise.Event reloadSound;
#endif
    public PedMonobehaviour pedMonobehaviour;

    [SerializeField] private VisualEffect muzzleFlash;

    [SerializeField] private VisualEffect bulletCasing;

    [SerializeField] private Transform recoilBone;
    public VisualEffect MuzzleFlash
    {
        get { return muzzleFlash; }
    }
    public VisualEffect BulletCasing => bulletCasing;


    public bool CanShoot => HaveAmmo && Time.time >= nextFireTime;

    public bool HaveAmmo => currentBulletAmount > 0;

    public bool CanReload => !isReloading && currentBulletAmount < weaponData.baseCapacity;
    public bool IsAiming
    {
        get { return isAiming ; }
        set { isAiming = value ; }
    }

    private Vector3 recoilOffset;
    private float recoilStrength = 0.05f;
    private float recoilReturnSpeed = 8f;


    void Start()
    {
        if (weaponData != null)
        {
            currentBulletAmount = weaponData.baseCapacity;
        }

        //bulletCasing = transform.Find("BulletCasing").GetComponent<VisualEffect>();

        //muzzleFlash = transform.Find("MuzzleFlash").GetComponent<VisualEffect>();

        initialLocalPosition = transform.parent.localPosition;

        string capacityText = string.Format("{0} / {1}", currentBulletAmount, weaponData.baseCapacity);
        UIManager.Instance.SetElementText(UIElementEnum.WEAPON_CAPACITY_TEXT, capacityText);
    }
    #region Actions

    private bool isWalkingInitialized = false;
    public void SetWalkingParameters(bool isRunning)
    {
        if (isRunning)
        {
            currentAmplitudeX = runAmplitudeX;
            currentAmplitudeY = runAmplitudeY;
            currentFrequency = runFrequency;
        }
        else
        {
            currentAmplitudeX = walkAmplitudeX;
            currentAmplitudeY = walkAmplitudeY;
            currentFrequency = walkFrequency;
        }
    }
    public void SimulateWalkingMotion()
    {
        if (!isWalkingInitialized)
        {
            initialLocalPosition = transform.localPosition;
            isWalkingInitialized = true;
            walkTime = 0f;
        }

        walkTime += Time.deltaTime * currentFrequency;

        float offsetZ = Mathf.Sin(walkTime) * currentAmplitudeX;
        float offsetY = Mathf.Cos(walkTime * 2f) * currentAmplitudeY;

        Vector3 walkOffset = new Vector3(0f, offsetY, offsetZ);
        Vector3 targetLocalPos = initialLocalPosition + walkOffset;

        if (!isAiming)
            targetLocalPos += recoilOffset;

        
        // On interpole vers la position cible pour éviter les sauts brusques
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * 10f);
    }



    public void Shoot(bool isAiming)
    {
#if !UNITY_SERVER
        if (CanShoot)
        {
            this.isAiming = isAiming;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            Transform spawnPoint = isAiming ? aimFirePoint : firePoint;

            

            muzzleFlash.Play();
            bulletCasing.Play();
            nextFireTime = Time.time + (1f / weaponData.fireRate);
            shootSound.Post(this.gameObject);
            // Nouvelle logique ici
            Camera cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

            Vector3 targetPoint;

            // Ignore layer 10 (TPSCamera) and layer 11 (AvoidRaycast)
            int layerMask = ~(1 << LayerMask.NameToLayer("TPSCamera") | 1 << LayerMask.NameToLayer("AvoidRaycast"));

            if (Physics.Raycast(ray, out RaycastHit hitCamera, 20f, layerMask))
            {
                targetPoint = hitCamera.point + new Vector3(-20f, 0f, 0f); // ou: targetPoint.x -= 1f;
            }

            else
            {
                targetPoint = ray.GetPoint(100f);
            }

            Vector3 direction = (targetPoint - spawnPoint.position).normalized;

            //if (!isAiming)
            //{
                GameObject bullet = Instantiate(
                    weaponData.bullet,
                    spawnPoint.position,
                    spawnPoint.rotation
                );
                Bullet bulletScript = bullet.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(spawnPoint.forward, 600f);
                }
            //}

            currentBulletAmount -= 1;
            string capacityText = string.Format("{0} / {1}", currentBulletAmount, weaponData.baseCapacity);
            UIManager.Instance.SetElementText(UIElementEnum.WEAPON_CAPACITY_TEXT, capacityText);

            // Raycast for Network
            Ray rayFromGun = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(rayFromGun, out RaycastHit hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
            {
                Entity rpcEntity = ecb.CreateEntity();
                var unifiedShoot = new UnifiedShootRPC
                {
                    origin = rayFromGun.origin,
                    direction = rayFromGun.direction,
                    hitPosition = hit.point,
                    shooterEntity = pedMonobehaviour.entity,
                    shooterNetworkId = Game.Instance.entityManager.GetComponentData<GhostOwner>(pedMonobehaviour.entity).NetworkId,
                    targetType = UnifiedShootTargetType.None,
                    headshot = false
                };

                if (hit.collider.CompareTag("Player"))
                {
                    PedMonobehaviour targetMono = hit.collider.GetComponentInParent<PedMonobehaviour>();
                    unifiedShoot.targetType = UnifiedShootTargetType.Player;
                    unifiedShoot.hitEntity = targetMono.entity;
                    unifiedShoot.hitNetworkId = Game.Instance.entityManager.GetComponentData<GhostOwner>(targetMono.entity).NetworkId;
                    if (!isAiming)
                    {
                        var hitmarkerGO = UIManager.Instance.GetElement(UIElementEnum.HIT_MARKER);
                        var hitmarkerFade = hitmarkerGO.GetComponent<FadeOutSprite>();
                        hitmarkerFade?.Show(); // Lance le fondu
                    }

                }
                else if (hit.collider.CompareTag("Zombie"))
                {
                    PedMonobehaviour targetMono = hit.collider.GetComponentInParent<PedMonobehaviour>();
                    unifiedShoot.targetType = UnifiedShootTargetType.Zombie;
                    unifiedShoot.hitEntity = targetMono.entity;
                    unifiedShoot.headshot = hit.collider.name == "Head";
                    if (!isAiming)
                    {
                        var hitmarkerGO = UIManager.Instance.GetElement(UIElementEnum.HIT_MARKER);

                        if(hitmarkerGO != null)
                        {
                            var hitmarkerFade = hitmarkerGO.GetComponent<FadeOutSprite>();
                            hitmarkerFade?.Show(); // Lance le fondu
                        }
                       
                    }


                }



                ecb.AddComponent(rpcEntity, unifiedShoot);
                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest());
            }

            // Ajoute un recul vers l'arrière
            if (!isAiming)
                recoilOffset = new Vector3(0f, 0f, -recoilStrength);


            ecb.Playback(Game.Instance.entityManager);
            ecb.Dispose();
        }
#endif
    }

    public void NetworkShoot()
    {
#if !UNITY_SERVER
        Transform spawnPoint = firePoint;

        GameObject bullet = Instantiate(
            weaponData.bullet,
            spawnPoint.position,
            spawnPoint.rotation
        );

        SetLayerRecursively(bullet, LayerMask.NameToLayer("Default"));

        muzzleFlash.Play();
        bulletCasing.Play();

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(firePoint.forward, 600f);
        }
#endif
    }


    public void HandleMuzzleFlash()
    {
#if !UNITY_SERVER
        if (CanShoot)
        {
            muzzleFlash.Play();

            nextFireTime = Time.time + (1f / weaponData.fireRate);       
        }
#endif
    }
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }

    private void Update()
    {
      

        if (recoilOffset != Vector3.zero && !isAiming)
        {
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
            transform.parent.localPosition = initialLocalPosition + recoilOffset;
        }
    }

    void OnDrawGizmos()
    {
        if (firePoint == null || Camera.main == null) return;

        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;

        Gizmos.color = Color.red;

        // Ignore layers: TPSCamera (10) and AvoidRaycast (11)
        int layerMask = ~(1 << LayerMask.NameToLayer("TPSCamera") | 1 << LayerMask.NameToLayer("AvoidRaycast"));

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f, layerMask))
        {
            Gizmos.DrawRay(origin, direction * hit.distance);
            Gizmos.DrawSphere(hit.point, 0.1f);
        }
        else
        {
            Gizmos.DrawRay(origin, direction * 100f);
            Gizmos.DrawSphere(origin + direction * 100f, 0.1f);
        }
    }




    public void Reload()
    {

            //reloadSound.Post(this.gameObject);
             
            StaticCallSounds.SyncSoundPlayer(this.gameObject, reloadSound.Id);

            Debug.Log("Weapon Reloaded");
            currentBulletAmount = weaponData.baseCapacity;

            string capacityText = string.Format("{0} / {1}", currentBulletAmount, weaponData.baseCapacity);
            UIManager.Instance.SetElementText(UIElementEnum.WEAPON_CAPACITY_TEXT, capacityText);
        
    }

    #endregion

}
#endif