using Unity.Entities;
using UnityEngine;

public struct AISpawner : IComponentData
{
    public Entity aiEmptyContainer;
}

[DisallowMultipleComponent]
public class AISpawnerAuthoring : MonoBehaviour
{

    public GameObject aiEmptyContainer;

    class Baker : Baker<AISpawnerAuthoring>
    {
        public override void Bake(AISpawnerAuthoring authoring)
        {
            AISpawner component = default(AISpawner);
            component.aiEmptyContainer = GetEntity(authoring.aiEmptyContainer, TransformUsageFlags.Dynamic);
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        
        }
    }
}