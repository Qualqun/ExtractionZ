
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct AnimationSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
#if !UNITY_SERVER
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        // Init
        foreach (var (gameObjectPrefab, localTransform, controller, entity) in SystemAPI.Query<GameObjectPrefab, LocalTransform, Ped>().WithNone<AnimatorReference>().WithEntityAccess())
        {
            Transform parentTransform = GameObject.FindAnyObjectByType<ParentInstantiate>().transform;
            GameObject newCompanionGameObject = Object.Instantiate(gameObjectPrefab.Prefab, parentTransform);

            PedMonobehaviour pedMono = newCompanionGameObject.GetComponentInChildren<PedMonobehaviour>();
            Animator animator = newCompanionGameObject.GetComponentInChildren<Animator>();


            newCompanionGameObject.transform.position = localTransform.Position;
            newCompanionGameObject.transform.rotation = localTransform.Rotation;

            //Debug.Log("[AnimationSystem::OnUpdate] - New ped instanciated with pos " + localTransform.Position);

            ecb.AddComponent(entity, new AnimatorReference
            {
                Animator = animator
            });

            if (pedMono != null)
            {
                GhostOwner ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(entity);
                int networkId = SystemAPI.GetSingleton<NetworkId>().Value;

                pedMono.entity = entity;

                //Debug.Log("Ped type " + controller.type);
                if (ghostOwner.NetworkId == networkId)
                {
                    //Cursor.lockState = CursorLockMode.Locked;
                    if (controller.type == PedType.PLAYER)
                    {
                        PlayerSyncedData playerTest = state.EntityManager.GetComponentData<PlayerSyncedData>(entity);

                        playerTest.teamId = Game.Instance.playerTeam;

                        Game.Instance.entityManager.SetComponentData(entity, playerTest);

                        newCompanionGameObject.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>().enabled = true;
                        newCompanionGameObject.GetComponentInChildren<PlayerCharacter>().enabled = true;

                    }

                    pedMono.hasControl = true;
                    //Debug.Log("[AnimationSystem::OnUpdate] - Gaining control of entity");
                }
                else
                {
                    if (controller.type == PedType.PLAYER)
                    {
                        //Debug.Log($"Disabling all player controlled scripts for ped: {newCompanionGameObject}");

                        newCompanionGameObject.GetComponentInChildren<Camera>().gameObject.SetActive(false);

                        newCompanionGameObject.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>().enabled = false;
                        newCompanionGameObject.GetComponentInChildren<AkSpatialAudioListener>().enabled = false;
                        newCompanionGameObject.GetComponentInChildren<AkAudioListener>().enabled = false;

                        newCompanionGameObject.GetComponentInChildren<PlayerCustomRig>().Disabled = true;
                        newCompanionGameObject.GetComponent<Player>().InitializeNetworkPlayer();

                        //TPSCamera
                        int targetLayer = LayerMask.NameToLayer("TPSCamera");
                        foreach (Transform child in newCompanionGameObject.GetComponentsInChildren<Transform>(true)) // 'true' includes inactive ones
                        {
                            if (child.gameObject.layer == targetLayer)
                            {
                                child.gameObject.layer = 0;
                            }
                        }

                        // !Do not delete will be used for the milestone
                        if (controller.type == PedType.PLAYER)
                        {
                            PlayerWeapon weaponController = newCompanionGameObject.GetComponentInChildren<PlayerWeapon>();

                            if (weaponController != null)
                            {
                                weaponController.requestedEquipWeapon = true;
                                weaponController.Swap(false);
                            }

                            newCompanionGameObject.GetComponent<Player>().LayerState = AnimatorLayerState.Armed;
                        }

                        //Debug.Log($"[AnimationSystem::OnUpdate] - Loosing Control of entity at position: {localTransform.Position}");

                        newCompanionGameObject.AddComponent<NetworkCloneTag>();
                    }
                    
                    pedMono.hasControl = false;
                }

            }
            else
            {
                Debug.LogError("[AnimationSystem::OnUpdate] - PedMonobehaviour not found, bind it to the prefab.");
            }
        }


        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        foreach (var (animatorReference, localTransform) in SystemAPI.Query<AnimatorReference, LocalTransform>().WithAll<AnimatorReference>().WithNone<GhostOwnerIsLocal>())
        {
            if (animatorReference.Animator != null)
            {
                Player player = animatorReference.Animator.GetComponentInParent<Player>();

                if (player != null)
                {

                    player.transform.position = localTransform.Position;
                    player.transform.rotation = localTransform.Rotation;
                }
                else
                {
                    AIPedMonobehaviour aiPed = animatorReference.Animator.GetComponentInParent<AIPedMonobehaviour>();

                    aiPed.transform.position = localTransform.Position;
                    aiPed.transform.rotation = localTransform.Rotation;
                }


                //Fonctionel mais le son ne va  pas suivre
                //animatorReference.Animator.transform.position = localTransform.Position;
                //animatorReference.Animator.transform.rotation = localTransform.Rotation;

            }
        }
#endif
    }

}

