using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;

struct WorldInfo
{
    public FixedString128Bytes sessionId;
    public ushort port;
}

public struct CreateOrJoinNewServerWorld : IRpcCommand
{
    public FixedString128Bytes sessionId;
}

public struct InfoPortConnection : IRpcCommand
{
    public ushort connexionPort;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class NetworkConnectionServerSystem : SystemBase
{
    private ushort mainServPort = 7979;
    private ushort nextServPort;

    private List<WorldInfo> allServWorlds = new List<WorldInfo>();

    bool isInitialized = false;
    protected override void OnCreate()
    {

        if (World.Name == "MainServerWorld")
        {
            nextServPort = mainServPort;
            nextServPort++;
        }

        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        if (!isInitialized)
        {
            NetworkStreamDriver driver = SystemAPI.GetSingleton<NetworkStreamDriver>();

#if UNITY_EDITOR
            driver.Listen(NetworkEndpoint.Parse("127.0.0.1", mainServPort));
#else
            driver.Listen(NetworkEndpoint.Parse("51.210.104.120", mainServPort));
#endif

            isInitialized = true;
            Debug.Log("[MAIN SERVER] LISTEN");

        }
        else if (World.Name == "MainServerWorld")
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (rpc, receiveCommand, entityRpc) in SystemAPI.Query<RefRO<CreateOrJoinNewServerWorld>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
            {
                Entity rpcToClient = ecb.CreateEntity();

                bool worldExist = false;
                ushort port = nextServPort;

                Debug.Log(" -- Receive rpc create world");

                if (rpc.ValueRO.sessionId != "Tutorial")
                {
                    foreach (WorldInfo world in allServWorlds)
                    {
                        if (world.sessionId == rpc.ValueRO.sessionId)
                        {
                            worldExist = true;
                            port = world.port;
                        }
                    }
                }

                if (!worldExist)
                {
                    WorldInfo newWorld = new WorldInfo();

                    newWorld.port = nextServPort;
                    newWorld.sessionId = rpc.ValueRO.sessionId;

                    CreateServerSession(newWorld.port);

                    allServWorlds.Add(newWorld);
                    nextServPort++;
                }

                ecb.AddComponent(rpcToClient, new InfoPortConnection { connexionPort = port });
                ecb.AddComponent(rpcToClient, new SendRpcCommandRequest { TargetConnection = receiveCommand.ValueRO.SourceConnection });

                ecb.DestroyEntity(entityRpc);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }


    private World CreateServerSession(ushort port)
    {
        World servWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld " + allServWorlds.Count);
        ServerSessionSystem sessionSystem = servWorld.GetExistingSystemManaged<ServerSessionSystem>();

        sessionSystem.sessionPort = port;

        Debug.Log(" Create a new world port " + port);

        return servWorld;
    }

}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerSessionSystem : SystemBase
{
    public ushort sessionPort;
    bool isInitialized = false;
    protected override void OnUpdate()
    {
        if (!isInitialized && sessionPort != 0 && World.Name != "MainServerWorld")
        {
            NetworkStreamDriver driver = SystemAPI.GetSingleton<NetworkStreamDriver>();

#if UNITY_EDITOR
            driver.Listen(NetworkEndpoint.Parse("127.0.0.1", sessionPort));
#else
            driver.Listen(NetworkEndpoint.Parse("51.210.104.120", sessionPort));
#endif

            isInitialized = true;

            //Remplace par le GUID de ta subscene
            var sceneGuid = new Unity.Entities.Hash128("3ff0f8acaa208c34aa2308dd8ea7a45f");

            // Charge la subscene (asynchrone)
            SceneSystem.LoadSceneAsync(World.Unmanaged, sceneGuid, new SceneSystem.LoadParameters
            {
                AutoLoad = true
            });
        }
    }
}

#if !UNITY_SERVER
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientConnectionSystem : SystemBase
{
    public static ushort sessionPort;
    private ushort mainServPort = 7979;

    bool isInitialized = false;
    bool test = true;

    float timerTimeout = 10f;
    float timeTimeout = 10f;

    protected override void OnUpdate()
    {
        if (!isInitialized)
        {
            NetworkStreamDriver driver = SystemAPI.GetSingleton<NetworkStreamDriver>();

#if UNITY_EDITOR
            driver.Connect(EntityManager, NetworkEndpoint.Parse("127.0.0.1", mainServPort));
#else
            driver.Connect(EntityManager, NetworkEndpoint.Parse("51.210.104.120", mainServPort));
#endif
            sessionPort = mainServPort;

            isInitialized = true;
        }
        else
        {
            if (!SystemAPI.TryGetSingleton<NetworkStreamConnection>(out _))
            {
                NetworkStreamDriver driver = SystemAPI.GetSingleton<NetworkStreamDriver>();

#if UNITY_EDITOR
                driver.Connect(EntityManager, NetworkEndpoint.Parse("127.0.0.1", sessionPort));
#else
                driver.Connect(EntityManager, NetworkEndpoint.Parse("51.210.104.120", sessionPort));
#endif
                Debug.LogError("[ClientConnectionSystem::Update] - NetworkStreamConnection entity not found try to reconect port " + sessionPort);

                Game.Instance.connected = false;
            }
            else if (!SystemAPI.TryGetSingleton<NetworkId>(out _))
            {
                Debug.LogWarning("[ClientConnectionSystem::Update] - NetworkId entity not found time out in " + timerTimeout);

                timerTimeout -= SystemAPI.Time.DeltaTime;

                if (timerTimeout < 0)
                {
                    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                    Entity connectionEntity = SystemAPI.GetSingletonEntity<NetworkStreamConnection>();

                    ecb.AddComponent<NetworkStreamRequestDisconnect>(connectionEntity);
                    timerTimeout = timeTimeout;

                    ecb.Playback(EntityManager);
                    ecb.Dispose();
                }

                Game.Instance.connected = false;
            }
            else
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                bool hasConnection = EntityManager.CreateEntityQuery(typeof(NetworkStreamInGame)).CalculateEntityCount() > 0;

                //automatic go in game
                if (/*mainServPort != sessionPort && */!hasConnection)
                {
                    Entity connectionEntity = SystemAPI.GetSingletonEntity<NetworkStreamConnection>();
                    Entity rpcEntity = ecb.CreateEntity();

                    ecb.AddComponent<NetworkStreamInGame>(connectionEntity);

                    ecb.AddComponent(rpcEntity, new InGameStateRequest());
                    ecb.AddComponent(rpcEntity, new SendRpcCommandRequest());
                    Game.Instance.connected = true;

                }

                foreach (var (rpc, entityRpc) in SystemAPI.Query<RefRO<InfoPortConnection>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
                {
                    Entity connectionEntity = SystemAPI.GetSingletonEntity<NetworkStreamConnection>();

                    ecb.AddComponent<NetworkStreamRequestDisconnect>(connectionEntity);
                    sessionPort = rpc.ValueRO.connexionPort;

                    ecb.DestroyEntity(entityRpc);
                }

                if (Game.Instance.connectMainServ)
                {
                    Entity connectionEntity = SystemAPI.GetSingletonEntity<NetworkStreamConnection>();

                    Game.Instance.teamWin = -1;
                    Game.Instance.connectMainServ = false;
                    Game.Instance.playerTeam = 0;

                    Game.Instance.flagOwner = null;
                    Game.Instance.playerList.Clear();

                   sessionPort = mainServPort;

                    ecb.AddComponent<NetworkStreamRequestDisconnect>(connectionEntity);
                }

                timerTimeout = timeTimeout;

                ecb.Playback(EntityManager);
                ecb.Dispose();
            }


        }
    }

}
#endif