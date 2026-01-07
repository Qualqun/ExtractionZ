using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGameMono : MonoBehaviour
{
    Button buttonStart;

    bool isStartingGame = false;
#if !UNITY_SERVER

    void Start()
    {
        buttonStart = GetComponent<Button>();
        buttonStart.onClick.AddListener(StartGameEvnt);
    }

    void StartGameEvnt()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        Entity rpcStartGame = ecb.CreateEntity();

        ecb.AddComponent(rpcStartGame, new StartGameInfo { sessionId = Game.Instance.lobbyId });
        ecb.AddComponent(rpcStartGame, new SendRpcCommandRequest());

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
        //LoadingScript loadingScript = GameObject.FindAnyObjectByType<LoadingScript>();

        //loadingScript.StartCoroutine(loadingScript.LoadSceneCoroutine("Game"));

        //SceneManager.UnloadSceneAsync("Menu");

        buttonStart.interactable = false;
        isStartingGame = true;
    }
#endif
}
