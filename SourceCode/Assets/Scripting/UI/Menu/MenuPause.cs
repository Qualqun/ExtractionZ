using UnityEngine;

public class MenuPause : MonoBehaviour
{
#if !UNITY_SERVER
    LoadingScript loadingScript;
#endif
    bool state = false;
    #if !UNITY_SERVER
    Player mainPlayer;
#endif
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if !UNITY_SERVER
        loadingScript = FindAnyObjectByType<LoadingScript>();
#endif
        transform.GetChild(0).gameObject.SetActive(state);
    }

    // Update is called once per frame
    void Update()
    {
        #if !UNITY_SERVER
        if (mainPlayer == null)
        { 

            foreach (Player ped in Game.Instance.playerList)
            {
                if (ped.gameObject.GetComponent<NetworkCloneTag>() == null)
                {
                    mainPlayer = ped;
                    break;
                }
            }

        }
        else
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                mainPlayer.CanMove = state;
                state = !state;
                transform.GetChild(0).gameObject.SetActive(state);

                Cursor.lockState = state ? CursorLockMode.Confined : CursorLockMode.Locked;
                Cursor.visible = state;
            }
        }
#endif
    }

    public void BackMenu()
    {
#if !UNITY_SERVER
        loadingScript.LoadScene("MainMenus");
#endif
        Game.Instance.connectMainServ = true;
    }
}
