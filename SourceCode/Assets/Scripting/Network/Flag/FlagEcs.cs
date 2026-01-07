using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;


public struct FlagComponent : IComponentData
{
    public Entity owner;
    public float3 position;
    public quaternion rotation;
    public bool isTake;
}

public struct FlagRPC : IRpcCommand
{
    public bool isTaken;
}
public struct FlagOwnerRPC : IRpcCommand
{
    public Entity flagOwner;
}

public struct FlagPositionRPC : IRpcCommand
{
    public Vector3 position;
    public quaternion rotation;
    public Entity oldOwner;
}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct FlagSystem : ISystem
{

    float timeDrop;
    float timerDrop;
    public void OnCreate(ref SystemState state)
    {
        //Flag Creation
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity flagEntity = ecb.CreateEntity();

        ecb.AddComponent(flagEntity, new FlagComponent
        {
            owner = Entity.Null,
            isTake = false
        });

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        state.RequireForUpdate<FlagComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<FlagComponent>(out _))
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity flagEntity = SystemAPI.GetSingletonEntity<FlagComponent>();
            FlagComponent flagComponent = state.EntityManager.GetComponentData<FlagComponent>(flagEntity);



            foreach (var (flagRPC, rpcCommandRequest, entityRpc) in SystemAPI.Query<RefRO<FlagRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
            {
                if (flagComponent.isTake != flagRPC.ValueRO.isTaken)
                {
                    flagComponent.isTake = flagRPC.ValueRO.isTaken;

                    if (flagComponent.isTake)
                    {
                        // take flag
                        Entity rpcGetFlag = ecb.CreateEntity();

                        foreach (var (ghostOwner, player) in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<PlayerSyncedData>().WithEntityAccess())
                        {
                            NetworkId playerTakerId = state.EntityManager.GetComponentData<NetworkId>(rpcCommandRequest.ValueRO.SourceConnection);

                            if (ghostOwner.ValueRO.NetworkId == playerTakerId.Value)
                            {
                                flagComponent.owner = player;
                                ecb.AddComponent(rpcGetFlag, new FlagOwnerRPC { flagOwner = player });
                            }
                        }

                        ecb.AddComponent(rpcGetFlag, new SendRpcCommandRequest());

                    }
                    else
                    {
                        //Drop flag
                        NetworkId playerCommand = state.EntityManager.GetComponentData<NetworkId>(rpcCommandRequest.ValueRO.SourceConnection);
                        GhostOwner flagOwner = state.EntityManager.GetComponentData<GhostOwner>(flagComponent.owner);

                        if (playerCommand.Value == flagOwner.NetworkId)
                        {
                            Entity rpcDropFlag = ecb.CreateEntity();

                            ecb.AddComponent(rpcDropFlag, new FlagPositionRPC
                            {
                                position = flagComponent.position,
                                rotation = flagComponent.rotation,
                            });

                            ecb.AddComponent(rpcDropFlag, new SendRpcCommandRequest());


                            flagComponent.owner = Entity.Null;
                        }
                        else
                        {
                            flagComponent.isTake = !flagRPC.ValueRO.isTaken;
                        }
                    }
                }

                SystemAPI.SetSingleton<FlagComponent>(flagComponent);
                ecb.DestroyEntity(entityRpc);
            }

            if (flagComponent.owner != Entity.Null && flagComponent.isTake)
            {
                LocalTransform playerTransform = state.EntityManager.GetComponentData<LocalTransform>(flagComponent.owner);

                flagComponent.position = playerTransform.Position;
                flagComponent.rotation = playerTransform.Rotation;
                SystemAPI.SetSingleton<FlagComponent>(flagComponent);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DropFlagSystem : SystemBase
{
#if !UNITY_SERVER
    FlagScript flagScript;
    PlayerCharacter playerFlagOwner;
#endif
    protected override void OnUpdate()
    {
#if !UNITY_SERVER
        if (flagScript != null)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (flagRPC, entityRpc) in SystemAPI.Query<RefRO<FlagPositionRPC>>().WithEntityAccess().WithAll<ReceiveRpcCommandRequest>())
            {
                ExtractionInfoUi extractionInfo = GameObject.FindAnyObjectByType<ExtractionInfoUi>();

                extractionInfo.StopHud();

                flagScript.DropFlag(flagRPC.ValueRO.position, flagRPC.ValueRO.rotation);

                playerFlagOwner.DropFlag();

                Game.Instance.flagOwner = null;

                ecb.DestroyEntity(entityRpc);
            }

            foreach (var (flagRPC, entityRpc) in SystemAPI.Query<RefRO<FlagOwnerRPC>>().WithEntityAccess().WithAll<ReceiveRpcCommandRequest>())
            {
                AnimatorReference animatorRef = EntityManager.GetComponentData<AnimatorReference>(flagRPC.ValueRO.flagOwner);
                PlayerCharacter playerCharacter = animatorRef.Animator.gameObject.GetComponentInParent<PlayerCharacter>(true);
                
                ExtractionInfoUi extractionInfo = GameObject.FindAnyObjectByType<ExtractionInfoUi>();
                ReplicatedPlayerSyncedData playerInfo = EntityManager.GetComponentData<ReplicatedPlayerSyncedData>(flagRPC.ValueRO.flagOwner);
                
                extractionInfo.ActiveExtractionHUD(Game.Instance.playerTeam == playerInfo.teamId);
                

                playerFlagOwner = playerCharacter;

                playerFlagOwner.TakeFlag();
                flagScript.TakeFlag();

                Game.Instance.flagOwner = playerFlagOwner.transform;

                ecb.DestroyEntity(entityRpc);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        else
        {
            flagScript = GameObject.FindAnyObjectByType<FlagScript>();
        }
#endif
    }

}
