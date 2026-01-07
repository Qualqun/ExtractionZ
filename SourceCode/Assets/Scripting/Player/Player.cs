
#if !UNITY_SERVER
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Windows;
public enum AnimatorLayerState
{
    Locomotion,
    Armed,
}
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;

    [SerializeField] private PlayerCamera playerCamera;

    [SerializeField] private CameraSpring cameraSpring;

    [SerializeField] private PlayerCustomRig playerCustomRig;

    [SerializeField] private PlayerWeapon playerWeapon;

    [SerializeField] private Animator animator;

    public bool CanMove = true;

    public bool isSpectate = false;
    private PedMonobehaviour pedMonobehaviour;
    public Camera fpsCamera; // à assigner dans l'inspector
    private Camera mainCamera;
    private UniversalAdditionalCameraData mainCameraData;

    public Transform TempPlayer;
    private PlayerInputs inputActions;
    public Animator Animator => animator;

    public PlayerCamera PlayerCamera => playerCamera;

    public PlayerCharacter PlayerCharacter => playerCharacter;

    private bool isMoving;
    private bool isRunning;

    public bool IsRunning => isRunning;
    public bool IsMoving => isMoving;


    [SerializeField]

    public PedMonobehaviour PedMonobehaviour => pedMonobehaviour;

    public PlayerInputs GetPlayerInputs { get { return inputActions; } }

    [Header("Layer Configurations")]
    [SerializeField] private LayerBoneConfiguration[] layerConfigurations = new LayerBoneConfiguration[System.Enum.GetValues(typeof(AnimatorLayerState)).Length];
    [System.Serializable]
    public class LayerBoneConfiguration
    {
        [HideInInspector] public AnimatorLayerState layerState;
        public List<HumanBone> bones = new List<HumanBone>();
    }

    [SerializeField]
    private AnimatorLayerState layerState = AnimatorLayerState.Locomotion;
    public AnimatorLayerState LayerState { get { return layerState; } set { layerState = value; } }
    private void Awake()
    {
        pedMonobehaviour = playerCharacter.gameObject.GetComponent<PedMonobehaviour>();
    }
    void Start()
    {
        if (pedMonobehaviour.hasControl)
        {
            Game.Instance.playerList.Add(this);

            Cursor.lockState = CursorLockMode.Locked;

            inputActions = new PlayerInputs();
            inputActions.Enable();

            playerCharacter.Initialize();
            playerCamera.Initialize(playerCharacter.CameraTarget);
            cameraSpring.Initialize();

            //print("find camera");
            fpsCamera = GameObject.Find("FPSCamera")?.GetComponent<Camera>();

            mainCamera = Camera.main;
            mainCameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();

            SetLayerRecursively(playerCharacter.gameObject, 8);
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

    void Update()
    {
        // temp
        //Animator animator = playerCharacter.gameObject.GetComponent<Animator>();
        //if (animator != null)
        //{
        //    GameObject.Destroy(animator);
        //}

        if (pedMonobehaviour.hasControl)
        {
            if (CanMove)
            {
                var input = inputActions.Gameplay;

                // Update camera and player rotation
                var cameraInput = new CameraInput { look = input.Look.ReadValue<Vector2>() };

                var characterInput = new CharacterInput
                {
                    rotation = playerCamera.transform.rotation,
                    move = input.Move.ReadValue<Vector2>(),
                    jump = input.Jump.WasPressedThisFrame(),
                    crouch = input.Crouch.WasPressedThisFrame() ? CrouchInput.Toggle : CrouchInput.None,
                    jumpSustain = input.Jump.IsPressed(),
                    run = input.Run.IsPressed(),

                };

                PlayerSyncedData replicatedSyncedData = Game.Instance.entityManager.GetComponentData<PlayerSyncedData>(pedMonobehaviour.entity);

                var characterWeaponInput = new CharacterWeaponInput
                {
                    equipWeapon = true,
                    shoot = input.Shoot.IsPressed(),
                    aim = input.Aim.IsPressed(),
                    reload = input.Reload.WasPressedThisFrame()

                };

                playerCharacter.IsAiming = characterWeaponInput.aim;
                PlayerCamera.IsAiming = characterWeaponInput.aim;



                animator.SetFloat("Vertical", -characterInput.move.y);
                animator.SetFloat("Horizontal", -characterInput.move.x);

                if (characterInput.move.y != 0 || characterInput.move.x != 0)
                {
                    isMoving = true;
                    animator.SetBool("IsMoving", isMoving);
                }
                else
                {
                    isMoving = false;
                    animator.SetBool("IsMoving", isMoving);
                }

                isRunning = characterInput.run;

                animator.SetBool("IsRunning", characterInput.run);
                animator.SetBool("IsCrouching", playerCharacter.State.stance == Stance.Crouch);
                animator.SetBool("IsSliding", playerCharacter.State.stance == Stance.Slide);

                playerCharacter.UpdateInput(characterInput);
                playerWeapon.UpdateWeaponInput(characterWeaponInput);

                var cameraTarget = playerCharacter.CameraTarget;
                //playerCamera.UpdatePosition(cameraTarget);
                //cameraSpring.UpdateSpring(Time.deltaTime, cameraTarget.up);



                playerCamera.UpdateRotation(cameraInput);

                replicatedSyncedData.vertical = -characterInput.move.y;
                replicatedSyncedData.horizontal = -characterInput.move.x;

                replicatedSyncedData.isMoving = isMoving;
                replicatedSyncedData.isRunning = isRunning;
                replicatedSyncedData.isCrouched = playerCharacter.State.stance == Stance.Crouch;
                replicatedSyncedData.isSliding = playerCharacter.State.stance == Stance.Slide;

                Game.Instance.entityManager.SetComponentData(pedMonobehaviour.entity, replicatedSyncedData);

                ReplicatedPlayerSyncedData replicateddSyncedData = Game.Instance.entityManager.GetComponentData<ReplicatedPlayerSyncedData>(pedMonobehaviour.entity);
                string capacityText = string.Format("{0} / {1}", replicatedSyncedData.hp, 100);
                UIManager.Instance.SetElementText(UIElementEnum.HEALTH_POINT_TEXT, capacityText);
            }
        }
        else
        {
            ReplicatedPlayerSyncedData replicatedSyncedData = Game.Instance.entityManager.GetComponentData<ReplicatedPlayerSyncedData>(pedMonobehaviour.entity);

            // Valeurs actuelles
            float currentVertical = animator.GetFloat("Vertical");
            float currentHorizontal = animator.GetFloat("Horizontal");

            // Valeurs cibles depuis les données synchronisées
            float targetVertical = replicatedSyncedData.vertical;
            float targetHorizontal = replicatedSyncedData.horizontal;

            // Interpolation
            float lerpedVertical = Mathf.Lerp(currentVertical, targetVertical, Time.deltaTime * 10f);
            float lerpedHorizontal = Mathf.Lerp(currentHorizontal, targetHorizontal, Time.deltaTime * 10f);

            // Mise à jour de l'Animator
            animator.SetFloat("Vertical", lerpedVertical);
            animator.SetFloat("Horizontal", lerpedHorizontal);

            // print("animator float " + animator.GetFloat("Vertical"));
            // print("network float " + replicatedSyncedData.vertical);
            animator.SetBool("IsMoving", replicatedSyncedData.isMoving);
            animator.SetBool("IsRunning", replicatedSyncedData.isRunning);
            animator.SetBool("IsCrouching", replicatedSyncedData.isCrouched);
            animator.SetBool("IsSliding", replicatedSyncedData.isSliding);
        }
    }
    private void LateUpdate()
    {
        if (!isSpectate && CanMove)
        {
            var cameraTarget = playerCharacter.CameraTarget;
            playerCamera.UpdatePosition(cameraTarget);
            cameraSpring.UpdateSpring(Time.deltaTime, cameraTarget.up);
            playerCustomRig.UpdateFBA();
        }
    }
    public ref List<HumanBone> GetBonesForLayer(AnimatorLayerState state)
    {
        return ref layerConfigurations[(int)state].bones;
    }

    public void ShowOverlay()
    {
        if (!mainCameraData.cameraStack.Contains(fpsCamera))
        {
            mainCameraData.cameraStack.Add(fpsCamera);
        }
        fpsCamera.enabled = true;
    }

    public void HideOverlay()
    {
        if (mainCameraData.cameraStack.Contains(fpsCamera))
        {
            mainCameraData.cameraStack.Remove(fpsCamera);
        }
        fpsCamera.enabled = false;
    }

    public void InitializeNetworkPlayer()
    {
        playerCharacter.SetAllyMaterial();

        Debug.Log($"Disabling all player controlled scripts for ped: {this}");

        if (fpsCamera != null)
            fpsCamera.gameObject.SetActive(false);
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        if (playerCharacter.Motor != null)
            playerCharacter.Motor.enabled = false;

        AkSpatialAudioListener spatialAudioListener = playerCharacter.GetComponent<AkSpatialAudioListener>();

        if (spatialAudioListener != null)
            spatialAudioListener.enabled = false;

        AkAudioListener audioListener = playerCharacter.GetComponent<AkAudioListener>();

        if (audioListener != null)
            audioListener.enabled = false;

        //if (playerCustomRig != null)
        //    playerCustomRig.enabled = false;

        //playerCharacter.enabled = false;
    }


    private void OnDestroy()
    {
        if (pedMonobehaviour.hasControl)
        {
            inputActions.Gameplay.Disable();
        }
    }
}

[System.Serializable]
public class HumanBone
{
    public HumanBodyBones m_bone;
    [Range(0f, 1f)] public float m_weight;
    public float m_pitchMin;
    public float m_pitchMax;
    public float m_yawMin;
    public float m_yawMax;
}
#endif