#if !UNITY_SERVER
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    AK.Wwise.Event menuMusic;

    [SerializeField]
    AK.Wwise.Event menuNavigation;

    [SerializeField]
    AK.Wwise.Event menuPressed;


    NodeMenu[] allMenuNode;
    LoadingScript loadingScript;

    private void Start()
    {
        Button[] allButton = GetComponentsInChildren<Button>(true);

        foreach (Button button in allButton)
        {
            EventTrigger eventTrigger = button.AddComponent<EventTrigger>();
            EventTrigger.Entry navicationEntry = new EventTrigger.Entry();

            navicationEntry.eventID = EventTriggerType.PointerEnter;
            navicationEntry.callback.AddListener((eventData)
            =>
            {
                if (button != null && button.interactable)
                {
                    menuNavigation.Post(gameObject);

                }
            });


            eventTrigger.triggers.Add(navicationEntry);

            button.onClick.AddListener(onClickSound);

        }
        
        menuMusic.Post(gameObject); 

        allMenuNode = GetComponentsInChildren<NodeMenu>();
        loadingScript = FindAnyObjectByType<LoadingScript>();
        ActiveNode("MainMenu");
    }

    void onClickSound()
    {
        menuPressed.Post(gameObject);

        
    }


    public void ActiveNode(string nameNode)
    {
        bool nodeExist = false;

        foreach (NodeMenu nodeMenu in allMenuNode)
        {

            if (nodeMenu.name == nameNode)
            {
                nodeMenu.gameObject.SetActive(true);
                nodeExist = true;
            }
            else
            {

                if (nodeMenu.name == "Session")
                {
                    /*SessionManager.DestroyAsync()*/;
                }

                nodeMenu.gameObject.SetActive(false);
            }

        }

        if (nodeExist)
        {
            Debug.Log("Node Menu " + nameNode + " don't exist");
        }

    }

    public void Quit()
    {
        Application.Quit();
    }
    public void LoadSceneGroup(string nameScene)
    {
        Debug.Log("inside");
        if (loadingScript != null)
        {
            loadingScript.LoadScene(nameScene);

            if(nameScene == "Tutorial")
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Entity req = ecb.CreateEntity();

                ecb.AddComponent(req, new CreateOrJoinNewServerWorld { sessionId = "Tutorial" });
                ecb.AddComponent(req, new SendRpcCommandRequest());

                ecb.Playback(Game.Instance.entityManager);
                ecb.Dispose();
               
            }

        }
        else
        {
            Debug.Log("Don't load loadingscript");
        }

    }

}
#endif