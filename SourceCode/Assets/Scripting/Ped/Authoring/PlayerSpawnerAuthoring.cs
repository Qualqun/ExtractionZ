using Unity.Entities;
using UnityEngine;

public struct PlayerSpawner : IComponentData
{
    public Entity playerEmptyContainer;
}

[DisallowMultipleComponent]
public class PlayerSpawnerAuthoring : MonoBehaviour
{

    public GameObject playerEmptyContainer;
    class Baker : Baker<PlayerSpawnerAuthoring>
    {
        public override void Bake(PlayerSpawnerAuthoring authoring)
        {
            PlayerSpawner playerSpawnerComp = default(PlayerSpawner);
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            playerSpawnerComp.playerEmptyContainer = GetEntity(authoring.playerEmptyContainer, TransformUsageFlags.Dynamic);
            AddComponent(entity, playerSpawnerComp);
        }
    }
}
