using Unity.NetCode;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;


//[GhostComponent]
public struct PlayerSyncedData : IInputComponentData
{
    /*[GhostField]*/ public float3 targetPos;
    /*[GhostField]*/ public float3 targetRotation;
    /*[GhostField]*/ public int stepSoundState;

    public int hp;
    public int playerSpectateNetworkId;
    public bool isMoving;
    public int teamId;
    public bool isRunning;
    public float blend;
    public float vertical;
    public float horizontal;
    public bool isCrouched;
    public bool Jump;
    public bool isSliding;
}

[GhostComponent]
public struct ReplicatedPlayerSyncedData : IComponentData
{
    [GhostField] public float3 targetPos;
    [GhostField] public float3 targetRotation;
    [GhostField] public int stepSoundState;
    [GhostField] public int hp;
    [GhostField] public int teamId;
    [GhostField] public bool isMoving;
    [GhostField] public bool isRunning;
    [GhostField] public float blend;
    [GhostField] public float vertical;
    [GhostField] public float horizontal;
    [GhostField] public bool isCrouched;
    [GhostField] public bool Jump;
    [GhostField] public bool isSliding;
}

#region ServerSide
[UpdateInGroup(typeof(PredictedSimulationSystemGroup)), WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ReplicatedPlayerSyncedDataSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (original, replicated) in SystemAPI.Query<RefRW<PlayerSyncedData>, RefRW<ReplicatedPlayerSyncedData>>().WithAll<Simulate>())
        {
            replicated.ValueRW.targetPos = original.ValueRO.targetPos;
            replicated.ValueRW.targetRotation = original.ValueRO.targetRotation;
            replicated.ValueRW.stepSoundState = original.ValueRO.stepSoundState;
            replicated.ValueRW.hp = original.ValueRO.hp;
            replicated.ValueRW.teamId = original.ValueRO.teamId;
            replicated.ValueRW.isMoving = original.ValueRO.isMoving;
            replicated.ValueRW.isRunning = original.ValueRO.isRunning;
            replicated.ValueRW.blend = original.ValueRO.blend;
            replicated.ValueRW.vertical = original.ValueRO.vertical;
            replicated.ValueRW.horizontal = original.ValueRO.horizontal;
            replicated.ValueRW.isCrouched = original.ValueRO.isCrouched;
            replicated.ValueRW.Jump = original.ValueRO.Jump;
            replicated.ValueRW.isSliding = original.ValueRO.isSliding;
        }
    }
}
#endregion