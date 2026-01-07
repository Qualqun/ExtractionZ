using System;
using UnityEngine;

public class LightDistance : MonoBehaviour
{
    [Serializable]
    public struct LightOptions
    {
        public Transform transform;
        public float distance;
    };

    [SerializeField] LightOptions[] lights;

    [SerializeField] Transform mainPlayerPos;

    // Update is called once per frame
    void Update()
    {
        if (mainPlayerPos == null)
        {
#if !UNITY_SERVER
            foreach (Player ped in Game.Instance.playerList)
            {
                if (ped.gameObject.GetComponent<NetworkCloneTag>() == null)
                {
                    mainPlayerPos = ped.transform.GetChild(0);
                    break;
                }
            }
#endif
        }
        else
        {
            foreach (LightOptions light in lights)
            {
                light.transform.gameObject.SetActive(Vector3.Distance(mainPlayerPos.position, light.transform.position) < light.distance);
            }
        }

        
    }
}
