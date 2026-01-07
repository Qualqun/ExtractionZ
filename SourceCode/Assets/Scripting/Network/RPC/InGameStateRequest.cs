using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct InGameStateRequest : IRpcCommand
{
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct InGameStateServerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InGameStateRequest>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, request, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<InGameStateRequest>>().WithEntityAccess())
        {
            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(rpc.ValueRO.SourceConnection);
            Entity NetIdEntity = rpc.ValueRO.SourceConnection;

            ecb.AddComponent(NetIdEntity, new NetworkStreamInGame());
            UnityEngine.Debug.Log("[InGameStateServerSystem::OnUpdate] - New player has joined IG state");

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
