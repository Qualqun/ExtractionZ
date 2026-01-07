using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;


// This Component allow Client authorative sync over the Transform
public struct ClientAuthTransform : IInputComponentData
{
    public LocalTransform Transform;
}


#region ClientSide
//#if !UNITY_SERVER
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class ClientAuthTransformClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {// Required to Set the position once
        foreach (var (clientAuthTransform, localTransform) in SystemAPI.Query<RefRW<ClientAuthTransform>, RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
        {
            if (clientAuthTransform.ValueRO.Transform.Scale == 1) continue;
            //Debug.Log($"[ClientAuthTransformClientSystem::OnUpdate] - {localTransform.ValueRO.Scale} {localTransform.ValueRO.Position} {localTransform.ValueRO.Rotation}");
            clientAuthTransform.ValueRW.Transform.Scale = localTransform.ValueRO.Scale;
            clientAuthTransform.ValueRW.Transform.Rotation = localTransform.ValueRO.Rotation;
            clientAuthTransform.ValueRW.Transform.Position = localTransform.ValueRO.Position;
        }
    }
}
//#endif
#endregion

#region ServerSide
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ClientAuthTransformServerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (localtransform, clientAuthTransform) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<ClientAuthTransform>>().WithAll<Simulate>())
        {
            if (clientAuthTransform.ValueRO.Transform.Scale != 1) continue;// Allow client to apply the correct position before it gets overriden

            localtransform.ValueRW = clientAuthTransform.ValueRO.Transform;
        }
    }
}
#endregion