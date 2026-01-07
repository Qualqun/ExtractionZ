#if !UNITY_SERVER
using UnityEngine;
using UnityEngine.UI;

public class WinHud : MonoBehaviour
{
    [SerializeField]
    Sprite[] winLoseSprite = new Sprite[2];

    [SerializeField]
    float slowDiv = 4f;

    Image imageHud;
    LoadingScript loadingScript;

    bool stateWinDisplayed = false;
    float timerChangeScene;
    float timeChangeScene = 5f;

    void Start()
    {
        imageHud = GetComponent<Image>();
        loadingScript = FindAnyObjectByType<LoadingScript>();

        Time.timeScale = 1f;
        Game.Instance.teamWin = -1;

        timerChangeScene = timeChangeScene;
    }
    private void Update()
    {
        if(Game.Instance.teamWin != -1 && !stateWinDisplayed)
        {
            DisplayResult(Game.Instance.teamWin);
            Time.timeScale = 1 / slowDiv;
         
            stateWinDisplayed = true;
        }

        if (Game.Instance.teamWin != -1)
        {
            Color alpha = imageHud.color;
            alpha.a += (Time.deltaTime * slowDiv) / 2;

            imageHud.color = alpha;
            timerChangeScene -= Time.deltaTime * slowDiv;

            if(timerChangeScene < 0)
            {
                loadingScript.LoadScene("MainMenus");
                Game.Instance.connectMainServ = true;
            }

        }
    }

    public void OnDestroy()
    {
        Time.timeScale = 1;
    }

    public void DisplayResult(int teamId)
    {
        imageHud.sprite = teamId == Game.Instance.playerTeam ? winLoseSprite[0] : winLoseSprite[1];
    }
}
#endif