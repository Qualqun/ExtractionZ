using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AI;


public class AIPedSpawn : MonoBehaviour
{
    [SerializeField]
    bool isTuto = false;

    [SerializeField]
    int nbIAPed = 50;

    [SerializeField]
    float[] spawnRate;

    public int nbIAPedSpawned = 0;
    public InfluenceZones[] allSpawns;

    float maxSpawnRate = 0;
    float timerTuto = 2f;

    bool noTimeSpawn = false;
    bool startRespawn = false;
    float timerSpawn;
    float timeSpawn = 15f;

    //Network networkInfo;
#if !UNITY_SERVER

    void Start()
    {
        //networkInfo = FindFirstObjectByType<Network>();

        timerSpawn = timeSpawn;

        foreach (float spawnRate in spawnRate)
        {
            maxSpawnRate += spawnRate;
        }
    }

    void Update()
    {
        if (isTuto)
        {
            timerTuto -= Time.deltaTime;

            if (timerTuto < 0)
            {
                SpawnPed();
                isTuto = false;
                noTimeSpawn = true;
            }

        }
        else if ((Game.Instance.connected || Game.Instance.playerList.Count > 0) && nbIAPedSpawned < nbIAPed)
        {
            timerSpawn -= Time.deltaTime;

            if (!startRespawn || timerSpawn <= 0 || noTimeSpawn)
            {
                SpawnPed();

            }
        }
        else if (nbIAPedSpawned >= nbIAPed)
        {
            startRespawn = true;

            Debug.Log("StartRespawn");
        }
    }

    void SpawnPed()
    {
        float randSpawn = Random.Range(0, maxSpawnRate);

        for (int i = 0; i < allSpawns.Length; i++)
        {
            if (randSpawn <= spawnRate[i])
            {
                Vector3 randomPoint = allSpawns[i].GetRandomPosition();


                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                    Entity req = ecb.CreateEntity();

                    ecb.AddComponent(req, new PedCreationRequest
                    {
                        pedType = PedType.AI,
                        position = hit.position,
                        rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0)
                    });

                    ecb.AddComponent(req, new SendRpcCommandRequest());

                    ecb.Playback(Game.Instance.entityManager);
                    ecb.Dispose();

                    nbIAPedSpawned++;
                    timerSpawn = timeSpawn;

                }

                break;
            }
            else
            {
                randSpawn -= spawnRate[i];
            }

        }
    }

#if UNITY_EDITOR
    public void UpdateInfoSpawn()
    {
        allSpawns = GetComponentsInChildren<InfluenceZones>();

        if (spawnRate.Length != allSpawns.Length)
        {
            spawnRate = new float[allSpawns.Length];
        }
    }
#endif
#endif

}

