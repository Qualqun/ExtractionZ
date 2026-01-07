using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


[DisallowMultipleComponent]
public class AIPedAuthoring : MonoBehaviour
{
    public GameObject ZombieModel;
    class Baker : Baker<AIPedAuthoring>
    {
        public override void Bake(AIPedAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new AIPedSynced { hp = 100 });

            AddComponent(entity, new ReplicatedAIPedAnimSynced());

            AddComponent(entity, new AIPedAnimSynced
            {
                isMove = false
            });

            AddComponentObject(entity, new GameObjectPrefab { Prefab = authoring.ZombieModel });
        }
    }
}

