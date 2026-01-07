using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public struct PedCreationRequest : IRpcCommand
{
    public PedType pedType;
    public float3 position;
    public quaternion rotation;
}

#region ServerSide
[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct PedCreationRequestServerSystem : ISystem
{
    private ComponentLookup<NetworkId> networkIdFromEntity;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AISpawner>();
        state.RequireForUpdate<PlayerSpawner>();

        state.RequireForUpdate<PedCreationRequest>();
        networkIdFromEntity = state.GetComponentLookup<NetworkId>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityManager entityManager = state.EntityManager;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        PlayerSpawner playerSpawner = SystemAPI.GetSingleton<PlayerSpawner>();
        AISpawner aiSpawner = SystemAPI.GetSingleton<AISpawner>();

        networkIdFromEntity.Update(ref state);

        foreach (var (requestPed, rpcData, rpcEntity) in SystemAPI.Query<RefRO<PedCreationRequest>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            NetworkId networkId = networkIdFromEntity[rpcData.ValueRO.SourceConnection];
            Entity connectionEntity = rpcData.ValueRO.SourceConnection;
            Entity prefab = Entity.Null;
            Entity ped;

            PedType pedType = requestPed.ValueRO.pedType;

            switch (pedType)
            {
                case PedType.PLAYER:
                    prefab = playerSpawner.playerEmptyContainer;
                    break;

                case PedType.AI:
                    prefab = aiSpawner.aiEmptyContainer;
                    break;

                default:
                    Debug.LogError("[PedCreationRequestServerSystem] Bad pedType request, look your switch / enum");
                    break;
            }

            ped = ecb.Instantiate(prefab);

            ecb.SetComponent(ped, new Ped { type = pedType });
            ecb.SetComponent(ped, new GhostOwner { NetworkId = networkId.Value });
            ecb.SetComponent(ped, new PredictedGhost());
            ecb.SetComponent(ped, new ClientAuthTransform { Transform = LocalTransform.FromPosition(requestPed.ValueRO.position) });
            ecb.SetComponent(ped, new LocalTransform
            {
                Position = requestPed.ValueRO.position,
                Rotation = requestPed.ValueRO.rotation,
                Scale = 1
            });

            ecb.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = ped });


            ecb.DestroyEntity(rpcEntity);
            //UnityEngine.Debug.Log($"[PedCreationRequestServerSystem::OnUpdate] - A new ped has been created with type: {pedType} owner is: {networkId.Value} at position: {requestPed.ValueRO.position}");
        }

        ecb.Playback(entityManager);
    }

}
#endregion
