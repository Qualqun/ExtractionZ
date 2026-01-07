
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct WinStateSystem : ISystem
{
#if !UNITY_SERVER
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WinInfo>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (rpcInfoWin, rpcEntity) in SystemAPI.Query<RefRO<WinInfo>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            Game.Instance.teamWin = rpcInfoWin.ValueRO.teamWinId;

            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
#endif
}