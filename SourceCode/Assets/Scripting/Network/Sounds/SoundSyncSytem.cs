using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


public struct SoundRPC : IRpcCommand
{
    public int originNetworkId;
    public uint eventSoundId;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct SoundSyncSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SoundRPC>();
        state.RequireForUpdate<NetworkStreamInGame>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        int? networkId = null;
        uint? eventSoundId = null;

        foreach (var (soundRpc, receiveRpcInfo, rpcEntity) in SystemAPI.Query<RefRO<SoundRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            eventSoundId = soundRpc.ValueRO.eventSoundId;
            networkId = state.EntityManager.GetComponentData<NetworkId>(receiveRpcInfo.ValueRO.SourceConnection).Value;
            
            ecb.DestroyEntity(rpcEntity);
        }

        if (networkId.HasValue && eventSoundId.HasValue)
        {
            Entity soundRpc = ecb.CreateEntity();

            ecb.AddComponent(soundRpc, new SoundRPC
            {
                originNetworkId = networkId.Value,
                eventSoundId = eventSoundId.Value
            });

            ecb.AddComponent(soundRpc, new SendRpcCommandRequest());
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
#if !UNITY_SERVER
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct SoundReplicateClientSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SoundRPC>();
        state.RequireForUpdate<NetworkStreamInGame>();
    }


    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (soundRpc, rpcEntity) in SystemAPI.Query<RefRO<SoundRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            foreach(Player player in Game.Instance.playerList)
            {
                if(player.GetComponent<NetworkCloneTag>())
                {
                    int playerNetworkId = Game.Instance.entityManager.GetComponentData<GhostOwner>(player.PedMonobehaviour.entity).NetworkId;
                    Debug.Log("Player network id " + playerNetworkId + " PLayer network rpc " + soundRpc.ValueRO.originNetworkId);

                    if (playerNetworkId == soundRpc.ValueRO.originNetworkId)
                    {
                        AkSoundEngine.PostEvent(soundRpc.ValueRO.eventSoundId, player.gameObject);
                    }
                }
            }

            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
#endif