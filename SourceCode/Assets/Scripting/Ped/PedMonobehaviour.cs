
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public class PedMonobehaviour : MonoBehaviour
{
#if !UNITY_SERVER


    private Game game;
    PlayerCharacter playerCharacter;

    [HideInInspector] public Entity entity;
    [HideInInspector] public bool hasControl = false;
    bool xRayDeleted = false;

    private void Awake()
    {
        game = FindFirstObjectByType<Game>();
        playerCharacter = GetComponent<PlayerCharacter>();
    }

    private void Update()
    {
        if (!game || Game.Instance.playerList.Count == 0) return;

        if (game.entityManager.Exists(entity) && game.entityManager.HasComponent<ClientAuthTransform>(entity) && game.entityManager.HasComponent<Ped>(entity))
        {
            Ped ped = game.entityManager.GetComponentData<Ped>(entity);

            GhostOwner ghostOwner = game.entityManager.GetComponentData<GhostOwner>(entity);
            int networkId = GetNetworkId();
            
            //delete xray & add material team
            if (ped.type == PedType.PLAYER )
            {
                ReplicatedPlayerSyncedData playerInfo = game.entityManager.GetComponentData<ReplicatedPlayerSyncedData>(entity);

                if ( game.playerTeam != playerInfo.teamId && !xRayDeleted)
                {
                    playerCharacter.UnsetAllyMaterial();
                    xRayDeleted = true;
                }
            }
            //

            if (hasControl && game.entityManager.HasComponent<GhostOwnerIsLocal>(entity))
            {
                ClientAuthTransform clientAuthTransform = game.entityManager.GetComponentData<ClientAuthTransform>(entity);
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

                //Update transform position
                clientAuthTransform.Transform.Position = transform.position;
                clientAuthTransform.Transform.Rotation = transform.rotation;

                ecb.SetComponent(entity, clientAuthTransform);

                ecb.Playback(game.entityManager);
                ecb.Dispose();
            }

        }
    }

    private void OnDestroy()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        ecb.DestroyEntity(entity);

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
    }

    public int GetNetworkId()
    {
        var query = game.entityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
        if (query.IsEmpty)
        {
            Debug.LogError("NetworkId singleton not found!");
            return -1;
        }

        var networkIdEntity = query.GetSingletonEntity();
        return game.entityManager.GetComponentData<NetworkId>(networkIdEntity).Value;
    }
#endif
}
