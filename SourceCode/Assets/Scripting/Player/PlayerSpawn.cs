using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Collections;
using System;
using UnityEngine.UIElements;

public enum PlayerTeam
{
    RED,
    BLUE,
    ALL_TEAM
}

public class PlayerSpawn : MonoBehaviour
{
    public int spawnNumber = -1;

    [SerializeField] float timeSpawn = 2f;
    [SerializeField] GameObject inGameCanvas;
    [SerializeField] GameObject spectateCameraPrefab;
    [SerializeField] FlagScript flagScript;

    Transform[] team = new Transform[(int)PlayerTeam.ALL_TEAM];
    float timerSpawn;

    bool firstSpawn = true;
    bool firstSpawnDebug = true;
    bool spectate = false;

#if !UNITY_SERVER
    PedMonobehaviour mainPlayer;

    float timer = 2f;
    [SerializeField] bool isInTutorial = false;
    void Start()
    {
        //Debug.Log("[PlayerSpawn::Start] - Spawn Mono is available");
        timerSpawn = timeSpawn;


        for (int i = 0; i < team.Length; i++)
        {
            PlayerTeam playerTeam = (PlayerTeam)i;

            team[i] = transform.Find(playerTeam.ToString());

            if (team[i] == null)
            {
                Debug.Log("Team " + playerTeam.ToString() + " are not loaded");
            }
        }


    }

    private void Update()
    {
        if (!Game.Instance.connected) return;
        //foreach (var script in newCompanionGameObject.GetComponentsInChildren<KinematicCharacterController.KinematicCharacterMotor>(true))

        if(isInTutorial && spawnNumber == -1)
        {
            timer -= Time.deltaTime;

            if (timer < 0)
            {
                spawnNumber = 0;
                isInTutorial = false;
            }
        }

        if (spawnNumber != -1)
        {
            if (firstSpawn)
            {
                //Debug.Log("[PlayerSpawn::Update] - Spawning player for the first time");
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Entity playerCreationRq = ecb.CreateEntity();
                Transform spawnPos = team[Game.Instance.playerTeam].Find((spawnNumber + 1).ToString());

                ecb.AddComponent(playerCreationRq, new SendRpcCommandRequest());
                ecb.AddComponent(playerCreationRq, new PedCreationRequest
                {
                    pedType = PedType.PLAYER,
                    position = new Vector3(0, 0, 0),
                    rotation = spawnPos.rotation
                });

                ecb.Playback(Game.Instance.entityManager);
                ecb.Dispose();

                firstSpawn = false;

            }
            else if (mainPlayer == null)
            {
                foreach (Player ped in Game.Instance.playerList)
                {
                    if (ped.gameObject.GetComponent<NetworkCloneTag>() == null)
                    {
                        Transform spawnPos = team[Game.Instance.playerTeam].Find((spawnNumber + 1).ToString());
                        mainPlayer = ped.GetComponentInChildren<PedMonobehaviour>();

                        mainPlayer.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>().SetPosition(spawnPos.position);
                        mainPlayer.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>().SetRotation(spawnPos.rotation);

                        //Debug.Log("[PlayerSpawn::Update] - Trouvé");

                        break;
                    }
                }
            }
            else if (!spectate)
            {
                PlayerSyncedData playerInfo = Game.Instance.entityManager.GetComponentData<PlayerSyncedData>(mainPlayer.entity);

                if (playerInfo.hp <= 0 || mainPlayer.transform.position.y < -8f)
                {
                    flagScript.DropFlagNetwork();

                    if (timerSpawn == timeSpawn)
                    {
                        //For the future Disable Camera
                        mainPlayer.transform.position = Vector3.one * -10000;
                        timerSpawn -= Time.deltaTime;
                    }
                    else
                    {
                        timerSpawn -= Time.deltaTime;
                        mainPlayer.gameObject.SetActive(false);

                        if (timerSpawn < 0)
                        {
                            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                            Transform spawnPos = team[Game.Instance.playerTeam].Find((spawnNumber + 1).ToString());

                            playerInfo.hp = 100;

                            ecb.SetComponent(mainPlayer.entity, playerInfo);

                            mainPlayer.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>().SetPosition(spawnPos.position);
                            mainPlayer.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>().SetRotation(spawnPos.rotation);

                            mainPlayer.gameObject.SetActive(true);
                            timerSpawn = timeSpawn;

                            ecb.Playback(Game.Instance.entityManager);
                            ecb.Dispose();
                        }
                    }

                    //Debug.Log("[PlayerSpawn::Update] - Needs to respawn ASAP");
                }

                //debug camera mod

                if (Input.GetKeyDown(KeyCode.M))
                {
                    Player playerScript = mainPlayer.GetComponentInParent<Player>();
                    playerScript.InitializeNetworkPlayer();
                    playerScript.isSpectate = true;

                    Instantiate(spectateCameraPrefab, transform);

                    mainPlayer.transform.GetChild(0).gameObject.SetActive(false);
                    inGameCanvas.SetActive(false);
                    spectate = true;
                }

                //
            }
        }
    }
#endif
}
