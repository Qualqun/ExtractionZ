#if !UNITY_SERVER
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;


public class CreateLobby : MonoBehaviour
{
    [SerializeField]
    MenuManager menuManager;

    [SerializeField]
    LobbyPlayerList lobbyPlayerList;

    TMP_InputField lobbyName;
    Button createButton;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        createButton = GetComponentInChildren<Button>();
        lobbyName = GetComponentInChildren<TMP_InputField>();

        createButton.onClick.AddListener(CreateJoinSession);
        createButton.interactable = false;

        lobbyName.onValueChanged.AddListener(value =>
        {
            createButton.interactable = !string.IsNullOrEmpty(value);
        });
    }

    async void CreateJoinSession()
    {
        //Empeche le spam
        createButton.interactable = false;

        //Create players info
        CreateLobbyOptions options = new CreateLobbyOptions();
        Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();

        playerData["Team"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, ((PlayerTeam)Game.Instance.playerTeam).ToString());
        playerData["Name"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, Game.Instance.pseudo);

        options.Player = new Unity.Services.Lobbies.Models.Player();
        options.Player.Data = playerData;
        //

        lobbyPlayerList.lobbyInfo = await LobbyService.Instance.CreateLobbyAsync(lobbyName.text, 7, options);

        Game.Instance.lobbyId = lobbyPlayerList.lobbyInfo.Id;

        // à executer après l'await 
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity req = ecb.CreateEntity();

        ecb.AddComponent(req, new CreateOrJoinNewServerWorld { sessionId = lobbyPlayerList.lobbyInfo.Id });
        ecb.AddComponent(req, new SendRpcCommandRequest());

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
        //
        menuManager.ActiveNode("Lobby");
        Debug.Log("Lobby created with success");

        lobbyName.text = null;
    }

}

#endif