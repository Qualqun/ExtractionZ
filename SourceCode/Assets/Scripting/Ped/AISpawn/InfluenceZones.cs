using System;
using UnityEngine;

public class InfluenceZones : MonoBehaviour
{
    [Serializable]
    public struct InfoInfluence
    {
        public float sizeInfluence;
        public float spawnRate;
#if UNITY_EDITOR
        public Color colorZone;
#endif
    }


    [SerializeField]
    InfoInfluence[] allInfluence = new InfoInfluence[3];
#if !UNITY_SERVER

    void Reset()
    {
        allInfluence = new InfoInfluence[3];

#if UNITY_EDITOR
        allInfluence[0].colorZone = Color.white;
        allInfluence[1].colorZone = Color.yellow;
        allInfluence[2].colorZone = Color.red;
#endif
        allInfluence[0].spawnRate = 33.3f;

        allInfluence[1].spawnRate = 33.3f;

        allInfluence[2].spawnRate = 33.4f;
    }

    public Vector3 GetRandomPosition()
    {
        int zoneSpawn = 0;
        float random = UnityEngine.Random.Range(0f, 100f);
        Vector3 spawnPoint = UnityEngine.Random.insideUnitSphere;

        //Select zone of spawn
        for (int i = 0; i < allInfluence.Length; i++)
        {
            if (random < allInfluence[i].spawnRate)
            {
                zoneSpawn = i;
            }
            else
            {
                random -= allInfluence[i].spawnRate;
            }
        }

        spawnPoint *= allInfluence[zoneSpawn].sizeInfluence;
        spawnPoint += transform.position;

        //Tortion testiculaire pour faire en sorte que ça spawn dans la partie exterieur de la zone 
        if (zoneSpawn > 0 && (spawnPoint - transform.position).magnitude < allInfluence[zoneSpawn - 1].sizeInfluence)
        {
            float distRemaining = allInfluence[zoneSpawn - 1].sizeInfluence - (spawnPoint - transform.position).magnitude;
            Vector3 dirSpawn = spawnPoint - transform.position;

            spawnPoint += dirSpawn.normalized * distRemaining;
        }
        return spawnPoint /*new Vector3(spawnPoint.x, transform.position.y, spawnPoint.z)*/;
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        foreach (InfoInfluence influence in allInfluence)
        {
            Gizmos.color = influence.colorZone;
            Gizmos.DrawWireSphere(transform.position, influence.sizeInfluence);
        }
    }
#endif
#endif
}