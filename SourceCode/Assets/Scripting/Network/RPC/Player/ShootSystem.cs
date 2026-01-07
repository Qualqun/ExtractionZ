using System.Numerics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public struct UnifiedShootRPC : IRpcCommand
{
    public float3 origin;
    public float3 direction;
    public float3 hitPosition;

    public Entity shooterEntity;
    public Entity hitEntity;

    public int shooterNetworkId;
    public int hitNetworkId;

    public UnifiedShootTargetType targetType;
    public bool headshot;
}

public enum UnifiedShootTargetType : byte
{
    None = 0,
    Player = 1,
    Zombie = 2
}

public struct SpawnOtherPlayerBullet : IRpcCommand
{
    public float3 origin;
    public float3 direction;
    public Entity playerShooter;
}


public struct ShootIrrelevantPed : IRpcCommand
{
    public float3 origin;
    public float3 direction;
    public float3 hitPosition;
    public Entity pedHited;
}
public struct PedShotedAnim : IRpcCommand
{
    public bool headshot;
    public Entity pedHited;
}

public struct SpawnBloodFx : IRpcCommand
{
    public float3 shootOriginPosition;
    public float3 position;
}

public struct SpawnBulletHole : IRpcCommand
{
    public float3 shootOriginPosition;
    public float3 position;
}

public struct SpawnBulletImpact : IRpcCommand
{
    public float3 shootOriginPosition;
    public float3 position;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct HandleUnifiedShootRPCSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, rpcReq, entityRpc) in SystemAPI.Query<RefRO<UnifiedShootRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            var shoot = rpc.ValueRO;

            foreach (var (networkId, target) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
            {
                if (networkId.ValueRO.Value != shoot.shooterNetworkId)
                {
                    var bulletEntity = ecb.CreateEntity();
                    ecb.AddComponent(bulletEntity, new SpawnOtherPlayerBullet
                    {
                        origin = shoot.origin,
                        direction = shoot.direction,
                        playerShooter = shoot.shooterEntity
                    });
                    ecb.AddComponent(bulletEntity, new SendRpcCommandRequest { TargetConnection = target });
                }
            }

            if (shoot.targetType == UnifiedShootTargetType.Player || shoot.targetType == UnifiedShootTargetType.Zombie)
            {
                foreach (var (_, target) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
                {
                    var fxEntity = ecb.CreateEntity();
                    ecb.AddComponent(fxEntity, new SpawnBloodFx
                    {
                        position = shoot.hitPosition,
                        shootOriginPosition = shoot.origin
                    });
                    ecb.AddComponent(fxEntity, new SendRpcCommandRequest { TargetConnection = target });
                }
            }
            else
            {
                foreach (var (_, target) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
                {
                    Entity fxEntity = ecb.CreateEntity();
                    ecb.AddComponent(fxEntity, new SpawnBulletHole
                    {
                        position = shoot.hitPosition,
                        shootOriginPosition = shoot.origin
                    });

                    ecb.AddComponent(fxEntity, new SpawnBulletImpact
                    {
                        position = shoot.hitPosition,
                        shootOriginPosition = shoot.origin
                    });

                    ecb.AddComponent(fxEntity, new SendRpcCommandRequest { TargetConnection = target });
                }
            }

            if (shoot.targetType == UnifiedShootTargetType.Player)
            {
                Entity connection = Entity.Null;

                foreach (var (networkId, target) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
                {
                    if (networkId.ValueRO.Value == shoot.hitNetworkId)
                    {
                        connection = target;
                        break;
                    }
                }

                if (connection != Entity.Null)
                {
                    Entity sendToPlayer = ecb.CreateEntity();

                    ecb.AddComponent(sendToPlayer, shoot);
                    ecb.AddComponent(sendToPlayer, new SendRpcCommandRequest { TargetConnection = connection });
                }
            }

            else if (shoot.targetType == UnifiedShootTargetType.Zombie)
            {
                if (state.EntityManager.Exists(shoot.hitEntity))
                {
                    Entity animEntity = ecb.CreateEntity();
                    AIPedSynced ped = state.EntityManager.GetComponentData<AIPedSynced>(shoot.hitEntity);

                    ped.hp -= rpc.ValueRO.headshot ? 50 : 25;

                    if (ped.hp <= 0)
                    {
                        ecb.DestroyEntity(shoot.hitEntity);
                    }
                    else
                    {
                        ecb.SetComponent(shoot.hitEntity, ped);
                    }

                    ecb.AddComponent(animEntity, new PedShotedAnim
                    {
                        headshot = shoot.headshot,
                        pedHited = shoot.hitEntity
                    });

                    ecb.AddComponent(animEntity, new SendRpcCommandRequest());
                }
            }

            ecb.DestroyEntity(entityRpc);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}




[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct ShootClientSystem : ISystem
{
#if !UNITY_SERVER
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }
#endif

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
#if !UNITY_SERVER
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, entity) in SystemAPI.Query<RefRO<UnifiedShootRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            var shoot = rpc.ValueRO;

            if (shoot.targetType == UnifiedShootTargetType.Player &&
                Game.Instance.entityManager.Exists(shoot.hitEntity))
            {
                PlayerSyncedData playerInfo = Game.Instance.entityManager.GetComponentData<PlayerSyncedData>(shoot.hitEntity);

                playerInfo.hp -= 10;

                if (playerInfo.hp <= 0)
                {
                    playerInfo.playerSpectateNetworkId = shoot.hitNetworkId;
                }
                if (Game.Instance.entityManager.HasComponent<GhostOwnerIsLocal>(shoot.hitEntity))
                {
                    BloodScreenEffect.Instance.ShowBloodStain();
                }

                ecb.SetComponent(shoot.hitEntity, playerInfo);
            }

            ecb.DestroyEntity(entity);
        }

        // Animation de zombie touché
        foreach (var (simpleRpc, entity) in SystemAPI.Query<RefRO<PedShotedAnim>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            if (state.EntityManager.Exists(simpleRpc.ValueRO.pedHited))
            {
                AnimatorReference animatorRef = state.EntityManager.GetComponentData<AnimatorReference>(simpleRpc.ValueRO.pedHited);

                animatorRef.Animator.GetComponentInParent<AIPedMonobehaviour>(true).Triggered();
                animatorRef.Animator.SetTrigger(simpleRpc.ValueRO.headshot ? "Headshoot" : "Hit");

            }
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
#endif
    }
}




[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct SpawnBloodFxSystem : ISystem
{
#if !UNITY_SERVER
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }
#endif

    public void OnUpdate(ref SystemState state)
    {
#if !UNITY_SERVER
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (fxData, entity) in SystemAPI
            .Query<RefRO<SpawnBloodFx>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {
            var fx = fxData.ValueRO;

            float3 direction = math.normalize(fx.position - fx.shootOriginPosition);
            UnityEngine.Quaternion rotation = UnityEngine.Quaternion.LookRotation(direction);
            UnityEngine.Quaternion decalRotation = UnityEngine.Quaternion.Euler(50f, 0f, -90f);

            if (Game.Instance != null && Game.Instance.bloodPrefab != null)
            {
                GameObject.Instantiate(
                    Game.Instance.bloodPrefab,
                    fx.position,
                    rotation
                );
            }

            if (Game.Instance != null && Game.Instance.bloodDecalePrefab != null)
            {
                GameObject.Instantiate(
                    Game.Instance.bloodDecalePrefab,
                    fx.position + direction,
                    rotation * decalRotation
                );
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
#endif
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct SpawnBulletHoleSystem : ISystem
{
#if !UNITY_SERVER
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }
#endif

    public void OnUpdate(ref SystemState state)
    {
#if !UNITY_SERVER
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (fxData, entity) in SystemAPI
            .Query<RefRO<SpawnBulletHole>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {


            var fx = fxData.ValueRO;

            float3 direction = math.normalize(fx.position - fx.shootOriginPosition);
            UnityEngine.Quaternion lookRotation = UnityEngine.Quaternion.LookRotation(direction);

            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            UnityEngine.Quaternion randomRotation = UnityEngine.Quaternion.AngleAxis(randomAngle, direction);

            UnityEngine.Quaternion finalRotation = lookRotation * randomRotation;

            if (Game.Instance != null && Game.Instance.bloodPrefab != null)
            {
                float3 offsetPosition = fx.position + new float3(0f, 0f, 0.05f);
                GameObject.Instantiate(
                    Game.Instance.bulletHole,
                    offsetPosition,
                    finalRotation
                );
            }




            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
#endif
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct SpawnBulletImpactSystem : ISystem
{
#if !UNITY_SERVER
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }
#endif

    public void OnUpdate(ref SystemState state)
    {
#if !UNITY_SERVER
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (fxData, entity) in SystemAPI
            .Query<RefRO<SpawnBulletImpact>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {


            var fx = fxData.ValueRO;

            float3 direction = math.normalize(fx.position - fx.shootOriginPosition);
            UnityEngine.Quaternion lookRotation = UnityEngine.Quaternion.LookRotation(direction);

            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            UnityEngine.Quaternion randomRotation = UnityEngine.Quaternion.AngleAxis(randomAngle, direction);

            UnityEngine.Quaternion finalRotation = lookRotation * randomRotation;

            if (Game.Instance != null && Game.Instance.bloodPrefab != null)
            {
                float3 offsetPosition = fx.position + new float3(0f, 0f, 0.05f);
                GameObject.Instantiate(
                    Game.Instance.bulletImpact,
                    offsetPosition,
                    finalRotation
                );
            }




            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
#endif
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct SpawnOtherPlayerBulletSystem : ISystem
{
#if !UNITY_SERVER
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }
#endif
    public void OnUpdate(ref SystemState state)
    {
#if !UNITY_SERVER
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, entity) in SystemAPI.Query<RefRO<SpawnOtherPlayerBullet>>().WithEntityAccess())
        {
            var shooterEntity = rpc.ValueRO.playerShooter;

            if (!state.EntityManager.HasComponent<AnimatorReference>(shooterEntity))
                continue;


            var animatorRef = state.EntityManager.GetComponentData<AnimatorReference>(shooterEntity);
            var shooterGO = animatorRef.Animator.gameObject;

            var playerWeapon = shooterGO.GetComponentInParent<PlayerWeapon>();
            if (playerWeapon != null)
            {
                playerWeapon.EquipedWeaponGameObject.GetComponent<WeaponController>().NetworkShoot();
            }
            else
            {
                UnityEngine.Debug.LogError($"[SpawnOtherPlayerBulletSystem] PlayerWeapon not found in parents of GameObject '{shooterGO.name}'");
            }



            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
#endif
    }
}


