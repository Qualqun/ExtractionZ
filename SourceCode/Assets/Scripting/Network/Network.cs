using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;
public class Network : MonoBehaviour
{

    private Game game;
    public bool connected { get; private set; } = false;
    public bool inGame { get; private set; } = false;


    NetworkStreamDriver networkDriver;
    public static Network Instance { get; private set; }
#if !UNITY_SERVER
    private void Awake()
    {
#if !UNITY_SERVER
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        game = FindAnyObjectByType<Game>();
#endif

    }


    void Update()
    {
        EntityQuery query = game.entityManager.CreateEntityQuery(typeof(NetworkStreamConnection));

        if (query.CalculateEntityCount() <= 0)
        {
            Debug.LogError("[Network::Update] - NetworkStreamConnection entity not found, operation skipped this frame.");
            return;
        }

        connected = false;
        Entity connectionEntity = query.GetSingletonEntity();

        if (!query.IsEmpty && connectionEntity != null && game.entityManager.GetComponentData<NetworkStreamConnection>(connectionEntity).CurrentState == ConnectionState.State.Connected)
        {
            connected = true;
        }


        // Automatic IG State
        if (connected && game.entityManager.CreateEntityQuery(typeof(NetworkStreamInGame)).CalculateEntityCount() == 0 && !inGame)
        {
            query = game.entityManager.CreateEntityQuery(typeof(NetworkId));
            if (query.CalculateEntityCount() <= 0)
            {
                Debug.LogWarning("[Network::Update] - NetworkId entity not found, operation skipped this frame.");
                return;
            }

            Entity netEntity = query.GetSingletonEntity();
            if (netEntity == null)
            {
                Debug.LogWarning("[Network::Update] - netEntity is null, operation skipped this frame.");

                return;
            }

            //Debug.Log("[Network::Update] - Going InGame via MonoBehaviour");
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.AddComponent<NetworkStreamInGame>(netEntity);

            Entity req = ecb.CreateEntity();
            ecb.AddComponent(req, new InGameStateRequest());
            ecb.AddComponent(req, new SendRpcCommandRequest());

            ecb.Playback(game.entityManager);
            ecb.Dispose();
            inGame = true;

        }
    }
#endif
}