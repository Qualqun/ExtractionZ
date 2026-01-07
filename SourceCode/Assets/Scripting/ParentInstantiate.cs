using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;

public class ParentInstantiate : MonoBehaviour
{
    [SerializeField] bool isTuto = false;

    private async void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#if !UNITY_SERVER
        if (!isTuto)
        {
            await LobbyService.Instance.RemovePlayerAsync(Game.Instance.lobbyId, AuthenticationService.Instance.PlayerId);
        }
#endif

        await Task.Delay(0);
    }
}
