using UnityEngine;

public class DisableAiPed : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        AIPedMonobehaviour aiped = GetComponentInChildren<AIPedMonobehaviour>();

        if(aiped != null)
        {
            aiped.tutorial = true;
            aiped.transform.rotation = Quaternion.identity;
        }
        
    }
}
