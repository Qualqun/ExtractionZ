
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
#if !UNITY_SERVER
    void Start()
    {
        PedMonobehaviour mainPlayer = new PedMonobehaviour();


        PedMonobehaviour[] allPed = FindObjectsByType<PedMonobehaviour>(FindObjectsSortMode.InstanceID);
        foreach (PedMonobehaviour ped in allPed)
        {
            if (ped.GetComponent<NetworkCloneTag>() == null)
            {
                Debug.Log("Found ped");
                mainPlayer = ped;
                break;
            }
        }

        mainPlayer.gameObject.transform.position = new Vector3(2.85f, 4.74f, 2.58f);

    }

#endif
}
