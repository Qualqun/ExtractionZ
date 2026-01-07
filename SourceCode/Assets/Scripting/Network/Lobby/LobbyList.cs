#if !UNITY_SERVER

using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyList : MonoBehaviour
{
    [SerializeField]
    MenuManager menuManager;

    [SerializeField]
    GameObject lobbySelectPrefab;

    [SerializeField]
    Transform lobbySelectParent;

    [SerializeField]
    Button refreshButton;

    [SerializeField]
    Button joinButton;

    [SerializeField]
    LobbyPlayerList lobbyPlayerList;

    public string idSessionSelected;

    private void Start()
    {
        refreshButton.onClick.AddListener(RefreshLobbyList);
        joinButton.onClick.AddListener(JoinLobby);

        joinButton.interactable = false;
    }

    async void RefreshLobbyList()
    {
        refreshButton.interactable = false; // pour empecher le spam

        QuerySessionsOptions queryOptions = new QuerySessionsOptions();
        
        QueryResponse results = await LobbyService.Instance.QueryLobbiesAsync();
        // Destroy sessions
        LobbySelectable[] allSessionSelect = lobbySelectParent.GetComponentsInChildren<LobbySelectable>();

        foreach (LobbySelectable session in allSessionSelect)
        {
            Destroy(session.gameObject);
        }
        
        foreach (Lobby lobby in results.Results)
        {
            GameObject lobbySelect = Instantiate(lobbySelectPrefab, lobbySelectParent);
            LobbySelectable lobbySelectScript = lobbySelect.GetComponent<LobbySelectable>();
            
            lobbySelectScript.nameLobbyText.text = lobby.Name;
            lobbySelectScript.playersNbText.text = lobby.Players.Count.ToString() + "/" + lobby.MaxPlayers.ToString();
            lobbySelectScript.lobbyId = lobby.Id;

            lobbySelectScript.selectableActive += SessionSelected;
        }

        await Task.Delay(1000);

        refreshButton.interactable = true;
    }

    void SessionSelected(string idSession)
    {
        joinButton.interactable = true;
        idSessionSelected = idSession;

        Debug.Log("Selectionée");
    }

    void SessionDeselected()
    {
        if (EventSystem.current.currentSelectedGameObject != joinButton.gameObject)
        {
            joinButton.interactable = false;
            idSessionSelected = null;
        }
    }

    async void JoinLobby()
    {
        joinButton.interactable = false; // evite le spam


        //Create players info
        JoinLobbyByIdOptions options = new JoinLobbyByIdOptions();
        Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();

        playerData["Team"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, ((PlayerTeam)Game.Instance.playerTeam).ToString());
        playerData["Name"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, Game.Instance.pseudo);
        
        options.Player = new Unity.Services.Lobbies.Models.Player();
        options.Player.Data = playerData;
        //

        Debug.Log($"[Netcode] Trying to connect to join{idSessionSelected}");

        lobbyPlayerList.lobbyInfo = await LobbyService.Instance.JoinLobbyByIdAsync(idSessionSelected, options);        

        Game.Instance.lobbyId = lobbyPlayerList.lobbyInfo.Id;
        
        //Request
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity req = ecb.CreateEntity();

        ecb.AddComponent(req, new CreateOrJoinNewServerWorld { sessionId = lobbyPlayerList.lobbyInfo.Id });
        ecb.AddComponent(req, new SendRpcCommandRequest());

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
        //
        menuManager.ActiveNode("Lobby");
    }
}




#endif