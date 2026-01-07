using System.Globalization;
using TMPro;
using UnityEngine;

public class PlayerItem : MonoBehaviour
{
    public TMP_Text playerName;
    public string playerId;
    void Awake()
    {
        playerName = GetComponentInChildren<TMP_Text>();
    }

}
