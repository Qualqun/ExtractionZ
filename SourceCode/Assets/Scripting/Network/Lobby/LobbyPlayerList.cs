using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplayer;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerList : MonoBehaviour
{
    [SerializeField]
    Transform contentRedParent;

    [SerializeField]
    Transform contentBlueParent;

    [SerializeField]
    GameObject playerItemPrefab;

    [SerializeField]
    Button swapTeam;

    [HideInInspector] public Lobby lobbyInfo;
   

    List<PlayerItem> playersItems = new List<PlayerItem>();

   


    private void Start()
    {
        swapTeam.onClick.AddListener(SwapTeam);
    }
    private async void OnEnable()
    {
        if (lobbyInfo != null)
        {  
            LobbyEventCallbacks lobbyEvt = new LobbyEventCallbacks();
          
            lobbyEvt.PlayerDataChanged += OnPlayerDataChanged;
            lobbyEvt.PlayerJoined += PlayerJoin;

            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyInfo.Id, lobbyEvt);

            InitPlayersItem();
        }
    }

    private void OnDisable()
    {
        //while (contentRedParent.childCount > 0)
        //{
        //    Destroy(contentRedParent.GetChild(0).gameObject);
        //}

        //while (contentBlueParent.childCount > 0)
        //{
        //    Destroy(contentRedParent.GetChild(0).gameObject);
        //}
    }


    void InitPlayersItem()
    {
        foreach (var player in lobbyInfo.Players)
        {
            player.Data.TryGetValue("Team", out PlayerDataObject teamInfo);
            player.Data.TryGetValue("Name", out PlayerDataObject nameInfo);

            SpawnItem(teamInfo.Value, player.Id, nameInfo.Value);  
        }
    }

    void PlayerJoin(List<LobbyPlayerJoined> listPlayerJoined)
    {

        foreach (LobbyPlayerJoined playerJoined in listPlayerJoined)
        {
            if(playerJoined.Player.Data == null)
            {
                Debug.Log("Oh c'est null ");
                SpawnItem(PlayerTeam.RED.ToString(), playerJoined.Player.Id, "");

            }
            else
            {
                playerJoined.Player.Data.TryGetValue("Team", out PlayerDataObject teamInfo);
                playerJoined.Player.Data.TryGetValue("Name", out PlayerDataObject nameInfo);

                SpawnItem(teamInfo.Value, playerJoined.Player.Id, nameInfo.Value);
            }
            
        }
    }

    void OnPlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
    {
        foreach (var playerChange in changes)
        {
            int playerIndex = playerChange.Key; // Index du joueur dans la liste du lobby
            var changedData = playerChange.Value;

            foreach (var kvp in changedData)
            {
                string key = kvp.Key;
                ChangedOrRemovedLobbyValue<PlayerDataObject> valueChange = kvp.Value;

                if (valueChange.Value != null)
                {
                    Debug.Log($"[Player {playerIndex}] Donnée '{key}' mise à jour : {valueChange.Value.Value}");
                }

                SwapPlayerItem(lobbyInfo.Players[playerChange.Key].Id, valueChange.Value.Value );
            }
        }
    }

    async void SwapTeam()
    {
        Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();
        UpdatePlayerOptions updateRequest;

        swapTeam.interactable = false;

        Game.Instance.playerTeam = Game.Instance.playerTeam == 0 ? 1 : 0;

        playerData["Team"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, ((PlayerTeam)Game.Instance.playerTeam).ToString());
        updateRequest = new UpdatePlayerOptions { Data = playerData };

        await LobbyService.Instance.UpdatePlayerAsync(lobbyInfo.Id, AuthenticationService.Instance.PlayerId, updateRequest);

        SwapPlayerItem(AuthenticationService.Instance.PlayerId, ((PlayerTeam)Game.Instance.playerTeam).ToString());

        await Task.Delay(1000);

        swapTeam.interactable = true;


        Debug.Log("Swap");
    }

    void SwapPlayerItem(string playerId, string team)
    {
        string playerName = null;
        PlayerItem playerDelet = null;

        foreach(PlayerItem item in playersItems)
        {
            if (item.playerId == playerId)
            {
                playerName = item.playerName.text;
                Destroy(item.gameObject);

                playerDelet = item;
            }
        }
        
        if(playerDelet != null)
        {
            playersItems.Remove(playerDelet);
        }

        SpawnItem(team, playerId, playerName);

    }

    void SpawnItem(string team, string playerId, string playerName)
    {
        if (team == PlayerTeam.RED.ToString())
        {
            PlayerItem playerItem = Instantiate(playerItemPrefab, contentRedParent).GetComponent<PlayerItem>();

            playerItem.playerName.text = playerName;
            playerItem.playerId = playerId;

            playersItems.Add(playerItem);
        }
        else if (team == PlayerTeam.BLUE.ToString())
        {
            PlayerItem playerItem = Instantiate(playerItemPrefab, contentBlueParent).GetComponent<PlayerItem>();

            playerItem.playerName.text = playerName;
            playerItem.playerId = playerId;

            playersItems.Add(playerItem);
        }
    }

}
