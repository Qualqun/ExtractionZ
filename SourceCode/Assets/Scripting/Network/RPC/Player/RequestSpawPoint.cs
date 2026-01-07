using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct SpawnPoint : IComponentData
{
    public int nbTeams;
    public FixedList32Bytes<int> countSpawn;
}

public struct SpawnPointRequest : IRpcCommand
{
    public int playerTeam;
}

public struct SpawnPointResponse : IRpcCommand
{
    public int spawnPointId;
    public Entity responseId;

}

public struct TempDeleteRPC : IComponentData
{
}


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial class RequestSpawPoint : SystemBase
{

    PlayerSpawn playerSpawn;
    bool requestSended = false;

    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        //Debug.Log("[Spawn] update");
        if (playerSpawn == null)
        {
            playerSpawn = GameObject.FindAnyObjectByType<PlayerSpawn>();

            //Debug.Log("[Spawn] Search");
        }
        else if (!requestSended)
        {
            Entity rpcSpawnPoint = ecb.CreateEntity();

            requestSended = true;

            ecb.AddComponent(rpcSpawnPoint, new SpawnPointRequest
            {
                playerTeam = Game.Instance.playerTeam
            });


            ecb.AddComponent(rpcSpawnPoint, new SendRpcCommandRequest());

            //UnityEngine.Debug.Log("[RequestSpawPoint::OnUpdate] - Requested spawn points");
        }

        foreach (var (rpc, spawnTeam, rpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnPointResponse>>().WithEntityAccess())
        {
            playerSpawn.spawnNumber = spawnTeam.ValueRO.spawnPointId;

            ecb.DestroyEntity(rpcEntity);
        }

        if(Game.Instance.connectMainServ)
        {
            requestSended = false;
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ResponseSpawPoint : ISystem
{

    public void OnCreate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity spawnPointEntity = ecb.CreateEntity();
        SpawnPoint spawnPoint = new SpawnPoint();


        //change nb of teams
        spawnPoint.nbTeams = 2;

        for (int i = 0; i < spawnPoint.nbTeams; i++)
        {
            spawnPoint.countSpawn.Add(0);
        }

        ecb.AddComponent(spawnPointEntity, spawnPoint);

        state.RequireForUpdate<SpawnPoint>();
        state.RequireForUpdate<SpawnPointRequest>();
        state.RequireForUpdate<NetworkStreamInGame>();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }


    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawnPoint, rpcSPawn) in SystemAPI.Query<RefRO<TempDeleteRPC>>().WithEntityAccess())
        {
            ecb.DestroyEntity(rpcSPawn);
            //Debug.Log("[Spawn] destroy entity");
        }

        foreach (var (rpc, spawnTeam, rpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnPointRequest>>().WithEntityAccess().WithNone<TempDeleteRPC>())
        {
            Entity rpcSpawn = ecb.CreateEntity();
            Entity sourceConnection = rpc.ValueRO.SourceConnection;
            SpawnPoint spawnPoint = SystemAPI.GetSingleton<SpawnPoint>();


            ecb.AddComponent(rpcSpawn, new SpawnPointResponse
            {
                spawnPointId = spawnPoint.countSpawn[spawnTeam.ValueRO.playerTeam],
                responseId = sourceConnection

            });

            spawnPoint.countSpawn[spawnTeam.ValueRO.playerTeam]++;
            SystemAPI.SetSingleton(spawnPoint);

            ecb.AddComponent(rpcSpawn, new SendRpcCommandRequest { TargetConnection = sourceConnection });
            ecb.AddComponent(rpcEntity, new TempDeleteRPC());


            //UnityEngine.Debug.Log("[ResponseSpawPoint::OnUpdate] - Spawn point was requested");

        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

}