using TMPro;
using UnityEngine;

public class ConnectionStatus : MonoBehaviour
{
    TMP_Text statusTmp;
    void Start()
    {
        statusTmp = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Game.Instance.connected)
        {
            statusTmp.text = "Connected";
            statusTmp.color = Color.green;
        }
        else
        {
            statusTmp.text = "Connection...";
            statusTmp.color = Color.yellow;
        }

    }
}
