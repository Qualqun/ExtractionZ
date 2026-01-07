using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;


#if !UNITY_SERVER

using System;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class LobbyScript : MonoBehaviour
{
    static public bool intialized = false;

    async void Awake()
    {
        QueryResponse results;
        
        if (!intialized)
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");

            intialized = true;
        }

        results = await LobbyService.Instance.QueryLobbiesAsync();

        Debug.Log($"TOTAL Session count : {results.Results.Count}.");
        foreach (var session in results.Results)
        {
            Debug.Log($"Session match found {session.Name}.");
        }


    }


}
#endif