#if !UNITY_SERVER
using TMPro;
using UnityEngine;

public class SwapTeam : MonoBehaviour
{
    TMP_Text buttonText;

    private void Start()
    {
        buttonText = GetComponentInChildren<TMP_Text>();
    }

    public void ChangeTeam()
    {
        Game.Instance.playerTeam = Game.Instance.playerTeam == 0 ? 1 : 0;
        buttonText.text = ((PlayerTeam)Game.Instance.playerTeam).ToString() + " team";
    }
}
#endif