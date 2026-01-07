#if !UNITY_SERVER
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbySelectable : MonoBehaviour, ISelectHandler
{
    [HideInInspector] public delegate void IdDelegate(string value);

    [HideInInspector] public TMP_Text nameLobbyText;
    [HideInInspector] public TMP_Text playersNbText;
    [HideInInspector] public string lobbyId;

    [HideInInspector] public IdDelegate selectableActive;
    [HideInInspector] public Action selectableDisable;


    bool isSelected = false;

    private void Awake()
    {
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>();

        nameLobbyText = allTexts[0];
        playersNbText = allTexts[1];
    }

    public void OnSelect(BaseEventData eventData)
    {
        selectableActive?.Invoke(lobbyId);
        isSelected = true;
    }


    private void Update()
    {
        if (isSelected && EventSystem.current.currentSelectedGameObject != gameObject)
        {
            isSelected = false;
            selectableDisable?.Invoke();
        }
    }

}
#endif