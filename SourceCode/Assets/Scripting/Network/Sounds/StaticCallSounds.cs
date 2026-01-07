using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;



static public class StaticCallSounds 
{
    static public void SyncSoundPlayer(GameObject gameObject, uint soundId)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity soundRpc = ecb.CreateEntity();


        ecb.AddComponent(soundRpc, new SoundRPC
        {
            eventSoundId = soundId
        });

        ecb.AddComponent(soundRpc, new SendRpcCommandRequest());

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
#if !UNITY_SERVER
        AkSoundEngine.PostEvent(soundId, gameObject);
#endif
    }

}
