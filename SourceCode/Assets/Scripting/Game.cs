
using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;

public class Game : MonoBehaviour
{

    public enum State
    {
        MAIN_MENU,
        IN_GAME,
    }

    public static Game Instance { get; private set; }
    public bool connected = true;
    public EntityManager entityManager { get; private set; }

    public World world { get; private set; }
    public State state { get; private set; }

    public GameObject bloodPrefab;
    public GameObject bloodDecalePrefab;
    public GameObject bulletHole;
    public GameObject bulletImpact;
    //MenuInfo
    public string pseudo;
    //IgInfo
    public string lobbyId = null;

    public Transform flagOwner;

    public int teamWin = -1;
    public bool connectMainServ = false;
    public int playerTeam = 0;

    //
    string[] defaultPseudo = { "LunaShadow", "PixelStorm", "NeoBlade", "EchoRider", "NovaPulse" };
    private bool inGameInitialized = false;
#if !UNITY_SERVER
    public List<Player> playerList = new List<Player>();

    private void Awake()
    {
        //Cursor.lockState = CursorLockMode.Confined;
        Application.runInBackground = true;

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        pseudo = defaultPseudo[Random.Range(0, defaultPseudo.Length)];

        world = null;
        foreach (var element in World.All)
        {
            if (element.Flags == WorldFlags.GameClient)
            {
                world = element;
                break;
            }
        }

        if (world == null)
        {
            Debug.LogError("Client world not found!");
            return;
        }

        entityManager = world.EntityManager;


        SetGameState(State.IN_GAME);
    }

//    void Update()
//    {
//#if !UNITY_SERVER
//        if (state == State.IN_GAME && !inGameInitialized)
//        {
//            if (!network.connected)
//                return; // Prevents message sending while connecting

//            var query = entityManager.CreateEntityQuery(
//                       ComponentType.ReadOnly<NetworkId>(),
//                       ComponentType.Exclude<NetworkStreamInGame>());

//            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.TempJob))
//            {
//                foreach (var entity in entities)
//                {
//                    entityManager.AddComponent<NetworkStreamInGame>(entity);
//                    Debug.Log("[Game::Update] - Going In game from MonoBehaviour");

//                    Entity newEntity = entityManager.CreateEntity();
//                    entityManager.AddComponent<InGameStateRequest>(newEntity);
//                    entityManager.AddComponent<SendRpcCommandRequest>(newEntity);
//                }
//            }

//            inGameInitialized = true;
//        }
//#endif
//    }


    public void SetGameState(State state)
    {
        this.state = state;
    }
#endif
}


