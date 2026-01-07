using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


public struct EnviroSoundRPC : IRpcCommand
{
    public int objectNumber;
    public int typeObject;
    public uint eventSoundId;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class EnviroSoundsSystem : SystemBase
{
    uint[][] allEvent;
    int[] nbGameObject;

    float timeSound;
    float timerSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void OnCreate()
    {
        // Liste générée par Wwise ici -> MainProject\Assets\StreamingAssets\Audio\GeneratedSoundBanks
        // Demandez à ChatGPT de vous faire la liste

        allEvent = new uint[][]
        {
            // Megaphone
            new uint[]
            {
                2897240517U, // PLAY_ALL_SYSTEM
                2156880018U, // PLAY_CAUTION_MU
                1132459130U, // PLAY_CONTAMINAT
                899357187U,  // PLAY_POWER_RESE
                865591102U,  // PLAY_REACTOR_TE
                215589731U,  // PLAY_VENTILATIO
                1155376864U  // PLAY_WARNING_UN
            },

            // Other
            new uint[]
            {
                6771957U,    // PLAY_ALIEN_TRIPOD_PNEUMATIC_CHEMICAL_WEAPON_BLACK_SMOKE
                4065313899U, // PLAY_CRUNCHY_BASS_DIVE_DISSONANT_BOLT
                3791449522U, // PLAY_ELECTRICITY_ELECTRONIC_LIGHTER_BUTTON_CLICK_ON_OFF_002_02
                1650636570U, // PLAY_ELECTRICITY_SURGE_DISCHARGE_ELECTRICAL_ARC_CRACKLING
                4041939007U, // PLAY_ELECTRICITY_TEXTURE_SIZZLING_CRACKLING
                2235592238U, // PLAY_ELECTRONIC_MOVEMENT_27
                1689077303U, // PLAY_GLITCH_03
                586603559U,  // PLAY_RATTLE___SHUDDER_CONSTANT
                432796728U,  // PLAY_RETROFUTURISTIC_COMPUTER_ALARM
                2819617421U  // PLAY_RETROFUTURISTIC_COMPUTER_DATA_PROCESSING
            }
        };

        nbGameObject = new int[(int)TypeSoundObject.LENGHT];

        nbGameObject[(int)TypeSoundObject.MEGAPHONE] = 21;
        nbGameObject[(int)TypeSoundObject.OTHER] = 30;

        timeSound = 1f;
        timerSound = timeSound;

        RequireForUpdate<ReplicatedPlayerSyncedData>();
        RequireForUpdate<NetworkStreamInGame>();
    }
    // Update is called once per frame
    protected override void OnUpdate()
    {
        timerSound -= SystemAPI.Time.DeltaTime;

        if (timerSound < 0f)
        {
            var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

            int randomType = random.NextInt(0, (int)TypeSoundObject.LENGHT);
            int randomObject = random.NextInt(0, nbGameObject[randomType]);
            int randomSound = random.NextInt(0, allEvent[randomType].Length);


            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity soundRpc = ecb.CreateEntity();


            ecb.AddComponent(soundRpc, new EnviroSoundRPC
            {
                typeObject = randomType,
                objectNumber = randomObject,
                eventSoundId = allEvent[randomType][randomSound]
            });

            ecb.AddComponent(soundRpc, new SendRpcCommandRequest());

            ecb.Playback(EntityManager);
            ecb.Dispose();

            timerSound = timeSound;
        }

    }
}



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class EnviroClientSoundsSystem : SystemBase
{

    PlayEvironementSounds playEvironementSounds;
    protected override void OnCreate()
    {

        RequireForUpdate<EnviroSoundRPC>();
        RequireForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {
        if (playEvironementSounds == null)
        {
            playEvironementSounds = Object.FindAnyObjectByType<PlayEvironementSounds>();
        }
        else
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (soundRpc, rpcEntity) in SystemAPI.Query<RefRO<EnviroSoundRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                playEvironementSounds.PlaySound(soundRpc.ValueRO.typeObject, soundRpc.ValueRO.objectNumber, soundRpc.ValueRO.eventSoundId);

                ecb.DestroyEntity(rpcEntity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

    }
}