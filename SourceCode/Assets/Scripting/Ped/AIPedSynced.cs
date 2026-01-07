
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

//[GhostComponent]
public partial struct AIPedSynced : IComponentData
{
    public int hp;

}

public partial struct AIPedAnimAttackSynced : IRpcCommand
{
    public Entity pedEntity;
    public int networkId;
}


public partial struct AIPedAnimSynced : IInputComponentData
{
    public bool isMove;
    public bool jumping;
}

[GhostComponent]
public struct ReplicatedAIPedAnimSynced : IComponentData
{
    [GhostField] public bool isMove;
    [GhostField] public bool jumping;
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup)), WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ReplicatedAIPedAnimSyncedSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (original, replicated) in SystemAPI.Query<RefRO<AIPedAnimSynced>, RefRW<ReplicatedAIPedAnimSynced>>().WithAll<Simulate>())
        {
            replicated.ValueRW.isMove = original.ValueRO.isMove;
            replicated.ValueRW.jumping = original.ValueRO.jumping;
        }


        foreach (var (rpc, rpcEntity) in SystemAPI.Query<RefRO<AIPedAnimAttackSynced>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            Entity rpcToAll = ecb.CreateEntity();

            ecb.AddComponent(rpcToAll, rpc.ValueRO);
            ecb.AddComponent(rpcToAll, new SendRpcCommandRequest());

            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ReplicatedAIPedAnimSyncedClientSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (rpc, rpcEntity) in SystemAPI.Query<RefRO<AIPedAnimAttackSynced>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            NetworkId networkId = SystemAPI.GetSingleton<NetworkId>();

            if(rpc.ValueRO.networkId != networkId.Value)
            {
                AnimatorReference animatorRef = state.EntityManager.GetComponentData<AnimatorReference>(rpc.ValueRO.pedEntity);

                animatorRef.Animator.SetTrigger("Attack");
            }

            ecb.DestroyEntity(rpcEntity);
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}