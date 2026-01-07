using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;



public struct ExtractionZone : IComponentData
{
    public float3 positionPoint;
    public float sizeZones;
    public int teamId;
}

public struct WinInfo : IRpcCommand
{
    public int teamWinId;
}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ExtractionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<FlagComponent>(out _))
        {
            Entity flagEntity = SystemAPI.GetSingletonEntity<FlagComponent>();
            FlagComponent flagComponent = state.EntityManager.GetComponentData<FlagComponent>(flagEntity);
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var extractionZone in SystemAPI.Query<RefRO<ExtractionZone>>())
            {
                if (math.distance(flagComponent.position, extractionZone.ValueRO.positionPoint) < extractionZone.ValueRO.sizeZones)
                {
                    ReplicatedPlayerSyncedData playerInfo = state.EntityManager.GetComponentData<ReplicatedPlayerSyncedData>(flagComponent.owner);

                    if (playerInfo.teamId == extractionZone.ValueRO.teamId)
                    {
                        Entity rpcWin = ecb.CreateEntity();

                        Debug.Log("[Flag:ExtractionSystem] Team " + playerInfo.teamId + " have win");

                        ecb.AddComponent(rpcWin, new WinInfo { teamWinId = playerInfo.teamId });
                        ecb.AddComponent(rpcWin, new SendRpcCommandRequest());

                        ecb.DestroyEntity(flagEntity);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
