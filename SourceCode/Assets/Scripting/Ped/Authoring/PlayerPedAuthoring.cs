using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPedAuthoring : MonoBehaviour
{
    public GameObject PlayerModel;
    class Baker : Baker<PlayerPedAuthoring>
    {
        public override void Bake(PlayerPedAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponentObject(entity, new GameObjectPrefab { Prefab = authoring.PlayerModel });

            AddComponent(entity, new PlayerSyncedData
            {
                teamId = Game.Instance == null ? 0 : Game.Instance.playerTeam,
                playerSpectateNetworkId = -1,
                hp = 100,
            });

            AddComponent(entity, new ReplicatedPlayerSyncedData{});
        }
    }
}
