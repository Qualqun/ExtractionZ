using KinematicCharacterController;
using System.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public struct CharacterInput
{
    public Quaternion rotation;
    public Vector2 move;
    public bool jump;
    public CrouchInput crouch;
    public bool jumpSustain;
    public bool run;
}

public enum CrouchInput
{
    None,
    Toggle
}

public enum Stance
{
    Stand,
    Crouch,
    Slide
}

public struct CharacterState
{
    public bool grounded;
    public Stance stance;
    public Vector3 velocity;
    public Vector3 acceleration;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    // private fields
    private CharacterState state;
    private CharacterState lastState;
    private CharacterState tempState;

    bool isPressingForward;

    // Request
    private Quaternion requestedRotation;
    private Vector3 requestedMovement;
    private bool requestedJump;
    private bool requestedCrouch;
    private bool requestedSustainedJump;
    private bool requestedRun;

    private bool ungroundedDueToJump;

    private float timeSinceUngrounded;
    private float timeSinceJumpRequest;

    private Collider[] uncrouchOverlapResults;
#if !UNITY_SERVER

    [Header("Motors")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;

    [Header("Movement Speed")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 15f;
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;

    [Header("Air")]
    [SerializeField] private float airSpeed = 7f;
    [SerializeField] private float airAcceleration = 35f;

    [SerializeField] private float runAirSpeed = 15f;
    [SerializeField] private float runAirAcceleration = 70f;

    [Header("Slide")]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.8f;
    [SerializeField] private float slideGravity = -90f;
    [SerializeField] private float slideSteerAcceleration = 5f;

    [Header("Jump")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90;
    //to jump even if the player leave the floor 
    [SerializeField] private float coyoteTime = 0.2f;

    private float walkResponse = 25f;
    private float crouchResponse = 20f;
    private float crouchHeightResponse = 80f;

    private float standHeight = 2f;
    private float crouchHeight = 1f;

    private bool waistHit;
    private bool headHit;
    private bool footHit;

    private RaycastHit waistHitInfo;
    private RaycastHit footHitInfo;
    private RaycastHit headHitInfo;

    [Header("Climb")]
    [SerializeField] float maxHangDuration = 1f;
    [SerializeField] float climbUpSpeed = 3f;
    [SerializeField] float climbDownSpeed = -3f;
    [SerializeField] float horizontalClimbSpeed = 2f;

    private bool isHanging = false;
    private bool climbRequested = false;
    private bool isClimbing;
    float hangingTimer = 0f;

    private Vector3 originalCameraLocalPosition;

    private Vector3 crouchPosition;
    private Vector3 standPosition;

    public bool IsClimbing => isHanging;

    [Header("Vault")]
    [SerializeField] private float vaultUpStrength = 3f;
    [SerializeField] private bool canVault;

    private bool requestedVault;
    private float timeSinceVaultRequest;

    [Header("Other")]
    [SerializeField] private GameObject flagObject;
    [SerializeField] private Material xRayMaterial;

    private bool isAiming;
    [SerializeField] Material[] playerMaterial = new Material[2];

    public bool IsAiming
    {
        get { return isAiming; }
        set { isAiming = value; }
    }
    private Vector3 GetFootOrigin()
    {
        return transform.position;
    }


    private Vector3 GetWaistOrigin()
    {
        return transform.position + Vector3.up * (motor.Capsule.height / 2f);
    }

    public KinematicCharacterMotor Motor => motor;

    private Vector3 GetHeadOrigin()
    {
        return transform.position + Vector3.up * motor.Capsule.height;
    }

    //getter 
    public Transform CameraTarget => cameraTarget;

    public void TakeFlag()
    {
        flagObject.SetActive(true);
        runSpeed /= 2;
        walkSpeed /= 2;

    }

    public void DropFlag()
    {
        flagObject.SetActive(false);

        runSpeed *= 2;
        walkSpeed *= 2;
    }

    public void SetAllyMaterial()
    {
        SkinnedMeshRenderer[] skinMesh = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer skin in skinMesh)
        {
            Material[] materials = new Material[skin.materials.Length + 1];

            for (int i = 0; i < skin.materials.Length; i++)
            {
                materials[i] = skin.materials[i];

                materials[i].renderQueue = xRayMaterial.renderQueue + 1;
            }

            materials[skin.materials.Length] = xRayMaterial;

            skin.materials = materials;
        }
    }

    public void SetPlayerSkin(int playerTeam)
    {
        Material material = null;
        SkinnedMeshRenderer[] skinMesh = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (playerTeam == (int)PlayerTeam.RED)
        {
            material = playerMaterial[0];
        }
        else
        {
            material = playerMaterial[1];
        }

        foreach (SkinnedMeshRenderer skin in skinMesh)
        {
            if (skin.gameObject.name != "RiffleShape")
            {

                Material[] materials = new Material[skin.materials.Length + 1];

                for (int i = 0; i < skin.materials.Length; i++)
                {
                    materials[i] = skin.materials[i];

                    materials[i].renderQueue = xRayMaterial.renderQueue + 1;
                }

                materials[skin.materials.Length] = material;

                skin.materials = materials;
            }
        }
    }

    public void UnsetAllyMaterial()
    {
        SkinnedMeshRenderer[] skinMesh = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer skin in skinMesh)
        {
            if(skin.gameObject.name != "RiffleShape")
            {
                skin.materials = new Material[0];

            }

        }

        SetPlayerSkin((int)PlayerTeam.BLUE);
    }
    private void Start()
    {
        originalCameraLocalPosition = cameraTarget.position;
        crouchPosition = originalCameraLocalPosition - new Vector3(0f, 0.7f, 0f);
        standPosition = originalCameraLocalPosition;
    }

    public void Initialize()
    {
        SetAllyMaterial();

        state.stance = Stance.Stand;
        lastState = state;
        motor.CharacterController = this;
        uncrouchOverlapResults = new Collider[8];
    }
#endif
    #region Update
    public void UpdateInput(CharacterInput input)
    {
#if !UNITY_SERVER
        requestedRotation = input.rotation;
        requestedMovement = new Vector3(input.move.x, 0f, input.move.y);

        // to prevent moving faster diagonally
        requestedMovement = Vector3.ClampMagnitude(requestedMovement, 1f);

        requestedMovement = input.rotation * requestedMovement;
        var wasRequestingJump = requestedJump;
        requestedJump = requestedJump || input.jump;
        if (requestedJump && !wasRequestingJump)
            timeSinceJumpRequest = 0f;

        requestedSustainedJump = input.jumpSustain;

        requestedRun = input.run;
        requestedVault = canVault && input.jump;
        requestedCrouch = input.crouch switch
        {
            CrouchInput.Toggle => !requestedCrouch,
            CrouchInput.None => requestedCrouch,
            _ => requestedCrouch
        };

        isPressingForward = input.move.y > 0.1f;

#endif
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
#if !UNITY_SERVER
        // Uncrouch.
        if (!requestedCrouch && state.stance != Stance.Stand)
        {
            state.stance = Stance.Stand;
            // Tentatively "standup" the character capsule.
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: 1f
            );
        }
        state.grounded = motor.GroundingStatus.IsStableOnGround;
        state.velocity = motor.Velocity;
        lastState = tempState;
#endif
    }


    public void BeforeCharacterUpdate(float deltaTime)
    {
#if !UNITY_SERVER
        tempState = state;

        if (requestedCrouch && state.stance is Stance.Stand)
        {

            state.stance = Stance.Crouch;

            motor.SetCapsuleDimensions(motor.Capsule.radius, crouchHeight, 0.5f);
        }
#endif
    }



    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
#if !UNITY_SERVER
        //Lisse la transition entre la rotation actuelle et la rotation demandée
        Quaternion smoothedRotation = Quaternion.Slerp(currentRotation, requestedRotation, deltaTime * 20f); // 10f est un facteur que tu peux ajuster

        // Calcule la direction "forward" projetée sur le plan du sol
        Vector3 forward = Vector3.ProjectOnPlane(smoothedRotation * Vector3.forward, motor.CharacterUp);

        if (forward.sqrMagnitude > 0f)
        {
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
        }
#endif
    }
    private Vector3 climbResultVelocity = Vector3.zero;
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
#if !UNITY_SERVER
        state.acceleration = Vector3.zero;
        if (motor.GroundingStatus.IsStableOnGround)
        {
            ungroundedDueToJump = false;
            timeSinceUngrounded = 0f;
            var groundedMovement = motor.GetDirectionTangentToSurface(
             requestedMovement, motor.GroundingStatus.GroundNormal) * requestedMovement.magnitude;

            // start sliding
            {
                var moving = groundedMovement.sqrMagnitude > 0f;
                var crouching = state.stance is Stance.Crouch;
                var running = requestedRun;
                var wasStanding = lastState.stance is Stance.Stand;
                var wasInAir = !lastState.grounded;


                if (moving && running && crouching && (wasStanding || wasInAir))
                {
                    state.stance = Stance.Slide;

                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane(lastState.velocity, motor.GroundingStatus.GroundNormal);
                    }

                    var slideSpeed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, motor.GroundingStatus.GroundNormal) * slideSpeed;
                }
            }

            // Réinitialiser l'accrochage lorsque tu es au sol
            if (isHanging)
            {
                isHanging = false;
            }

            if (state.stance is Stance.Stand or Stance.Crouch)
            {
                float speed = state.stance is Stance.Stand ? walkSpeed : crouchSpeed;

                if (state.stance is Stance.Stand && requestedRun)
                    speed = runSpeed;

                if (isAiming)
                    speed /= 2;

                float response = state.stance is Stance.Stand ? walkResponse : crouchResponse;

                var targetVelocity = groundedMovement * speed;
                var moveVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-response * deltaTime));

                state.acceleration = moveVelocity - currentVelocity;

                currentVelocity = moveVelocity;
            }
            // continue sliding
            else
            {
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);
                // Slope
                var force = Vector3.ProjectOnPlane(-motor.CharacterUp, motor.GroundingStatus.GroundNormal) * slideGravity;

                currentVelocity -= force * deltaTime;

                // Stop slide when velocity is low
                if (currentVelocity.magnitude < slideEndSpeed)
                {
                    requestedCrouch = false;
                }

            }

            // Vault detection
            Vector3 direction = transform.forward;
            float rayLength = 0.8f;

            Vector3 originWaist = GetWaistOrigin();
            Vector3 originHead = GetHeadOrigin();

            waistHit = Physics.Raycast(originWaist, direction, out waistHitInfo, rayLength);
            headHit = Physics.Raycast(originHead, direction, out headHitInfo, rayLength);

            if (waistHit && !headHit)
            {
                canVault = true;
            }
            else
            {
                canVault = false;
            }
        }
        else
        {
            // Handle air movement (while you're not grounded)
            HandleAirMovement(ref currentVelocity, deltaTime);

            // Reset hanging if you're in the air
            //if (isHanging)
            //{
            //    isHanging = false;
            //}
        }

        // Handle jump (vault or regular jump)
        if (requestedJump)
        {
            if (canVault)
                HandleVault(ref currentVelocity);
            else
                HandleJump(ref currentVelocity, deltaTime);
        }

        if (State.stance == Stance.Crouch || State.stance == Stance.Slide)
        {
            cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, crouchPosition, 0.1f);
        }
        else
        {
            cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, standPosition, 0.1f);
        }


#endif
    }


    #endregion

    #region Actions
#if !UNITY_SERVER
    private void HandleAirMovement(ref Vector3 currentVelocity, float deltaTime)
    {

        Vector3 direction = transform.forward;
        float rayLength = 0.8f;

        Vector3 originWaist = GetWaistOrigin();
        Vector3 originHead = GetHeadOrigin();
        Vector3 originFoot = GetFootOrigin();

        headHit = Physics.Raycast(originHead, direction, out headHitInfo, rayLength);
        waistHit = Physics.Raycast(originWaist, direction, out waistHitInfo, rayLength);
        footHit = Physics.Raycast(originFoot, direction, out footHitInfo, rayLength);

        timeSinceUngrounded += deltaTime;

        float targetAirAcceleration = requestedRun ? runAirAcceleration : airAcceleration;
        float targetAirSpeed = requestedRun ? runAirSpeed : airSpeed;

        if (requestedMovement.sqrMagnitude > 0f)
        {
            var planarMovement = Vector3.ProjectOnPlane(requestedMovement, motor.CharacterUp * requestedMovement.magnitude);
            var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

            var movementForce = planarMovement * targetAirAcceleration * deltaTime;


            if (currentPlanarVelocity.magnitude < targetAirSpeed)
            {
                var targetPlanarVeclocity = currentPlanarVelocity + movementForce;

                targetPlanarVeclocity = Vector3.ClampMagnitude(targetPlanarVeclocity, targetAirSpeed);

                movementForce = targetPlanarVeclocity - currentPlanarVelocity;
            }
            else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
            {
                var constrainedMovementForce = Vector3.ProjectOnPlane(movementForce, currentPlanarVelocity.normalized);

                movementForce = constrainedMovementForce;
            }

            //print(targetAirSpeed);
            // prevent air-climbing steep slopes

            if (motor.GroundingStatus.FoundAnyGround)
            {
                if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                {
                    var ostructionNormal = Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal).normalized;

                    movementForce = Vector3.ProjectOnPlane(movementForce, ostructionNormal);

                }


            }
            currentVelocity += movementForce;
        }
        // --- Accroché à un obstacle ---
        // Tu n'es PAS au sol
        // Système d'accrochage
        if (requestedJump && (waistHit || footHit))
        {
            isHanging = true;
            currentVelocity = Vector3.zero;
        }

        if (isHanging)
        {
            hangingTimer += deltaTime;

            if (isPressingForward)
            {
                if (!waistHit && !footHit)
                {

                    isHanging = false;
                }

                if (footHit || waistHit)
                {
                    if (hangingTimer < maxHangDuration)
                    {

                        currentVelocity = motor.CharacterUp * climbUpSpeed;
                    }
                    else
                    {

                        currentVelocity = motor.CharacterUp * climbDownSpeed;
                    }
                }
                else
                {

                    currentVelocity = transform.forward * horizontalClimbSpeed;
                }
            }
            else
            {

                currentVelocity = motor.CharacterUp * climbDownSpeed;
            }

            if (motor.GroundingStatus.IsStableOnGround)
            {
                isHanging = false;
            }
        }
        else
        {
            hangingTimer = 0f;
            // Pas accroché → appliquer la gravité
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);

            if (requestedSustainedJump && verticalSpeed > 0f)
                effectiveGravity *= jumpSustainGravity;

            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
        }


    }


    IEnumerator ClimbCoroutine()
    {
        climbResultVelocity = Vector3.zero;
        print("start c");
        isClimbing = true;

        float climbSpeed = 2.0f;

        while (footHit)
        {
            climbResultVelocity += motor.CharacterUp * climbSpeed * Time.deltaTime;
            yield return null;
        }

        Vector3 forwardImpulse = transform.forward * 2f;
        climbResultVelocity += forwardImpulse;

        print("end c");
        isClimbing = false;
    }


#endif


    private bool isVaultAscending;
    private bool hasVaultedVertically;

    private void HandleVault(ref Vector3 currentVelocity)
    {
#if !UNITY_SERVER
        if (motor.GroundingStatus.IsStableOnGround && (timeSinceUngrounded < coyoteTime) && !ungroundedDueToJump)
        {
            requestedVault = false;
            requestedJump = false;
            requestedCrouch = false;
            canVault = true;
            motor.ForceUnground(0f);

            ungroundedDueToJump = true;

            float currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            float targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, vaultUpStrength);

            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

            // animator.SetTrigger("IsVaulting");
        }
#endif
    }



    private void HandleJump(ref Vector3 currentVelocity, float deltaTime)
    {
#if !UNITY_SERVER
        if (motor.GroundingStatus.IsStableOnGround && (timeSinceUngrounded < coyoteTime) && !ungroundedDueToJump)
        {
            requestedJump = false;
            requestedCrouch = false;
            motor.ForceUnground(time: 0f);

            ungroundedDueToJump = true;

            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);

            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

            //animator.SetTrigger("IsJumping");
        }
        else
        {
            //animator.ResetTrigger("IsJumping");
            timeSinceJumpRequest += deltaTime;
            var canJumpLater = timeSinceJumpRequest < coyoteTime;
            requestedJump = canJumpLater;
        }
#endif
    }

    #endregion
#if !UNITY_SERVER

    private void OnDrawGizmosSelected()
    {
        if (motor == null) return;

        Vector3 direction = transform.forward;
        float rayLength = 0.5f;

        Vector3 originWaist = GetWaistOrigin();
        Vector3 originHead = GetHeadOrigin();
        Vector3 originFoot = GetFootOrigin(); // Ajout de l'origine du pied

        // Raycast pour la taille
        Gizmos.color = waistHit ? Color.red : Color.green;
        Gizmos.DrawLine(originWaist, originWaist + direction * rayLength);
        Gizmos.DrawSphere(originWaist + direction * rayLength, 0.025f);

        // Raycast pour la tête
        Gizmos.color = headHit ? Color.red : Color.cyan;
        Gizmos.DrawLine(originHead, originHead + direction * rayLength);
        Gizmos.DrawSphere(originHead + direction * rayLength, 0.025f);

        // Raycast pour le pied
        Gizmos.color = footHit ? Color.red : Color.yellow;
        Gizmos.DrawLine(originFoot, originFoot + direction * rayLength);
        Gizmos.DrawSphere(originFoot + direction * rayLength, 0.025f);
    }



    public CharacterState State => state;

#endif
    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
#if !UNITY_SERVER
        if (!motor.GroundingStatus.IsStableOnGround && state.stance is Stance.Slide)
            state.stance = Stance.Crouch;
#endif
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }
}