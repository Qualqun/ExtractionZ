using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;


[GhostComponent]
partial struct Ped : IComponentData
{
    [GhostField] public PedType type;
}

[DisallowMultipleComponent]
public class PedAuthoring : MonoBehaviour
{
    class Baker : Baker<PedAuthoring>
    {
        public override void Bake(PedAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Ped>(entity);
            AddComponent<ClientAuthTransform>(entity);
            //AddComponent<AnimatorReference>(entity);
        }
    }
}
