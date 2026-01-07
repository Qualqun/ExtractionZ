using Unity.NetCode;
using Unity.Networking.Transport;

[UnityEngine.Scripting.Preserve]
public class Bootstrap : ClientServerBootstrap
{
    //bootstrap ne gere plus la connexion se referer au NetworkConnectionystem
    public override bool Initialize(string defaultWorldName)
    {
        PlayType requestedPlayType = RequestedPlayType;

        if (!DetermineIfBootstrappingEnabled())
        {
            return false;
        }

        if (requestedPlayType != PlayType.Client)
        {
            CreateServerWorld("MainServerWorld");
        }

        if (requestedPlayType != PlayType.Server)
        {
            CreateClientWorld("ClientWorld");
            AutomaticThinClientWorldsUtility.BootstrapThinClientWorlds();
        }

        return true;
    }

}

