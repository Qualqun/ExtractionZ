
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct AIPedHitRPC : IRpcCommand
{
    public int damage;
    public int playerNetworkId;
}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct IAPedSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (hitAI, entityRpc) in SystemAPI.Query<RefRO<AIPedHitRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            Entity rpcToPlayer = ecb.CreateEntity();
            Entity playerNetworkConnection = Entity.Null;

            foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
            {
                if (networkId.ValueRO.Value == hitAI.ValueRO.playerNetworkId)
                {
                    playerNetworkConnection = entity;
                    break;
                }
            }

            if (playerNetworkConnection != Entity.Null)
            {
                ecb.AddComponent(rpcToPlayer, hitAI.ValueRO);

                ecb.AddComponent(rpcToPlayer, new SendRpcCommandRequest
                {
                    TargetConnection = playerNetworkConnection,
                });

                Debug.Log("Send rpc attack from serv");

            }


            ecb.DestroyEntity(entityRpc);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

}


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct IAPedClientSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }


    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (playerHitedInfo, entityRpc) in SystemAPI.Query<RefRO<AIPedHitRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            foreach (var playerInfo in SystemAPI.Query<RefRW<PlayerSyncedData>>().WithAll<GhostOwnerIsLocal>())
            {
                playerInfo.ValueRW.hp -= playerHitedInfo.ValueRO.damage;
                BloodScreenEffect.Instance.ShowBloodStain();
            }

            ecb.DestroyEntity(entityRpc);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}


