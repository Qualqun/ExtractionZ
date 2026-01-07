using System.Net.NetworkInformation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct StartGameInfo : IRpcCommand
{
    public FixedString64Bytes sessionId;
}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct StartGameSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (rpc, rpcEntity) in SystemAPI.Query<RefRO<StartGameInfo>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {

            Entity rpcToAllPlayers = ecb.CreateEntity();

            ecb.AddComponent(rpcToAllPlayers, new StartGameInfo { sessionId = rpc.ValueRO.sessionId });
            ecb.AddComponent(rpcToAllPlayers, new SendRpcCommandRequest());

            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

#if !UNITY_SERVER
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct StartGameCliebtSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (rpc, rpcEntity) in SystemAPI.Query<RefRO<StartGameInfo>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {

            LoadingScript loadingScript = GameObject.FindAnyObjectByType<LoadingScript>();

            loadingScript.LoadScene("MainMap");

            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
#endif


