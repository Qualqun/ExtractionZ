
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public class AIPedMonobehaviour : MonoBehaviour
{

    enum AIState
    {
        IDLE,
        FOLLOW,
        ATTACK
    }

    enum AttackSync
    {
        NONE = -1,
        FIRST_ATTACK = 0,
        SECOND_ATTACK = 1
    }



    public bool tutorial = false;

#if !UNITY_SERVER
    [SerializeField] AK.Wwise.Event soundsIdle;
    [SerializeField] AK.Wwise.Event soundsAttack;

    [SerializeField] float detectionRange = 10f;
    [SerializeField] float disengageRange = 30f;
    [SerializeField] float detectionAngle = 40.0f;
    [SerializeField] float attackRange = 4.0f;

    [SerializeField] int[] damageHit = new int[2];
    [SerializeField] float[] timeAttack = new float[2];

    [SerializeField] float timeMaxGrowl = 10f;
    [SerializeField] float timeDisparition = 2f;

    PedMonobehaviour pedMono;
    NavMeshAgent agent;
    PlayerCharacter[] allPlayerPed = new PlayerCharacter[0];
    AIPedSpawn aiPedSpawn;

    int indexCurrentTarget = -1;
    Animator animator;

    AttackSync attackync = AttackSync.NONE;
    AIState actualState = AIState.IDLE;

    float timerSound;
    float timerDisparition;
    float timerAttack = 0;

    SkinnedMeshRenderer[] skinnedMeshRenderers;


    void Start()
    {
        aiPedSpawn = FindAnyObjectByType<AIPedSpawn>();

        pedMono = GetComponent<PedMonobehaviour>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        agent.enabled = true;

        timeMaxGrowl = timeMaxGrowl < 2f ? 2f : timeMaxGrowl;
        timerSound = Random.Range(0.25f, timeMaxGrowl);
        timerDisparition = timeDisparition;

        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }


    void Update()
    {
        SoundZombie();


        if (!Game.Instance.entityManager.HasComponent<LocalTransform>(pedMono.entity))
        {
            if (timerDisparition == timeDisparition)
            {
                animator.SetTrigger("Death" + Random.Range(1, 3));
                animator.SetBool("IsDeath", true);


                foreach(Collider collider in GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }

            agent.isStopped = true;
            timerDisparition -= Time.deltaTime;

            foreach (var renderer in skinnedMeshRenderers)
            {
                renderer.material.SetFloat("_Amount", 1 - timerDisparition / timeDisparition);
            }

            if (timerDisparition < 0)
            {
                Destroy(gameObject);
            }

            return;
        }

       

        if (pedMono != null && pedMono.hasControl && !tutorial)
        {
           
            if (allPlayerPed.Length < Game.Instance.playerList.Count)
            {
                allPlayerPed = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
            }
            else
            {
                if (!agent.isOnNavMesh)
                {
                    // Ensure the agent is placed on the NavMesh
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        transform.position = hit.position;  // Move it to the nearest valid NavMesh position
                    }
                    //Debug.Log("[AIPedMonoBehaviour::Update] - Going back to the navmesh");
                    return;
                }

                switch (actualState)
                {
                    case AIState.FOLLOW:
                        // dans cet ordre la 
                        FollowTarget(); // 1
                        FindClosestPed(); // 2
                        break;

                    case AIState.ATTACK:
                        AttackTarget();
                        animator.SetBool("IsMove", false);
                        break;

                    default:
                        FindClosestPed();
                        animator.SetBool("IsMove", false);
                        break;
                }

                animator.SetBool("Jump", agent.isOnOffMeshLink);
                SyncAnimation();
            }
        }
        else
        {
            ReplicatedAIPedAnimSynced replicateAnim = Game.Instance.entityManager.GetComponentData<ReplicatedAIPedAnimSynced>(pedMono.entity);

            animator.SetBool("IsMove", replicateAnim.isMove);
            animator.SetBool("Jump", replicateAnim.jumping);
        }
    }

    private void OnDestroy()
    {
        if (!(pedMono == null || !pedMono.hasControl))
        {
            aiPedSpawn.nbIAPedSpawned--;
        }
    }

    
    void SoundZombie()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Take Zombie Attack"))
        {
            soundsAttack.Post(gameObject);
        }
        else
        {
            timerSound -= Time.deltaTime;

            if (timerSound <= 0)
            {
                soundsIdle.Post(gameObject);

                timerSound = Random.Range(2f, timeMaxGrowl);
            }
        }
    }

    void FindClosestPed()
    {
        int closestPed = -1;
        float closestDistance = detectionRange;

        for (int i = 0; i < allPlayerPed.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, allPlayerPed[i].transform.position);

            if (distance < closestDistance)
            {
                closestPed = i;
                closestDistance = distance;
            }
        }

        if (actualState != AIState.IDLE && closestDistance < attackRange)
        {
            indexCurrentTarget = closestPed;
            actualState = AIState.ATTACK;
            agent.isStopped = true;
            return;
        }


        if (Game.Instance.flagOwner != null)
        {
            for (int i = 0; i < allPlayerPed.Length; i++)
            {
                if (allPlayerPed[i].transform == Game.Instance.flagOwner)
                {
                    indexCurrentTarget = i;
                    actualState = AIState.FOLLOW;
                }
            }

        }
        else if (closestPed != -1)
        {
            //Look if the target are in visuel 
            Vector3 directionToPlayer = (allPlayerPed[closestPed].transform.position + Vector3.up) - (transform.position + Vector3.up * 1.5f);
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle < detectionAngle)
            {
                int layerMask = ~(1 << LayerMask.NameToLayer("Zombie")); // Collision with all 

                Debug.DrawRay(transform.position + Vector3.up * 1.5f, directionToPlayer  * closestDistance, Color.red);

                //Debug.Log("Collider test " + Physics.Raycast(transform.position + Vector3.up, directionToPlayer, float.MaxValue, layerMask));

                if (Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer, out RaycastHit hit, float.MaxValue, layerMask))
                {
                    //Debug.Log("Collider hit " + hit.collider.tag);

                    if (hit.collider.CompareTag("Player"))
                    {
                        indexCurrentTarget = closestPed;
                        actualState = AIState.FOLLOW;
                    }
                }

            }
        }
    }

    void FollowTarget()
    {
        if (Game.Instance.flagOwner != null)
        {
            agent.SetDestination(Game.Instance.flagOwner.position);

            if(!animator.GetBool("isMove"))
            {
                animator.SetBool("IsMove", true);
                animator.speed = Random.Range(1f, 1.1f);
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, allPlayerPed[indexCurrentTarget].transform.position) > disengageRange)
            {
                actualState = AIState.IDLE;
                indexCurrentTarget = -1;
                agent.isStopped = true;
                return;
            }

            animator.speed = 1;
            animator.SetBool("IsMove", true);


            agent.isStopped = false;
            agent.SetDestination(allPlayerPed[indexCurrentTarget].transform.position);
        }
    }

    void AttackTarget()
    {
        if (attackync == AttackSync.NONE)
        {
            animator.SetTrigger("Attack");
            attackync = AttackSync.FIRST_ATTACK;
            SyncTriggerAnimationRPC();
        }

        timerAttack += Time.deltaTime;

        if (timerAttack > timeAttack[(int)attackync])
        {
            SystemRpcAttack(damageHit[(int)attackync]);
            timerAttack = 0;

            if (attackync == AttackSync.SECOND_ATTACK)
            {
                attackync = AttackSync.NONE;
                actualState = AIState.FOLLOW;
                agent.isStopped = false;
            }
            else
            {
                attackync = AttackSync.SECOND_ATTACK;
            }
        }
    }


    void SystemRpcAttack(int damage)
    {
        Entity playerEntity = allPlayerPed[indexCurrentTarget].GetComponent<PedMonobehaviour>().entity;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        // Check if the player are the owner 
        GhostOwner playerGhostOwner = Game.Instance.entityManager.GetComponentData<GhostOwner>(playerEntity);
        GhostOwner pedIAGhostOwner = Game.Instance.entityManager.GetComponentData<GhostOwner>(pedMono.entity);

        if (playerGhostOwner.NetworkId == pedIAGhostOwner.NetworkId)
        {
            PlayerSyncedData playerData = Game.Instance.entityManager.GetComponentData<PlayerSyncedData>(playerEntity);

            playerData.hp -= damage;
            BloodScreenEffect.Instance.ShowBloodStain();

            ecb.SetComponent(playerEntity, playerData);
        }
        else
        {
            //Send Rpc attack
            Entity rpcInfo = ecb.CreateEntity();

            ecb.AddComponent(rpcInfo, new AIPedHitRPC
            {
                damage = damage,
                playerNetworkId = playerGhostOwner.NetworkId
            });

            ecb.AddComponent(rpcInfo, new SendRpcCommandRequest());
        }

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
    }

    void SyncAnimation()
    {
        AIPedAnimSynced animSynced = Game.Instance.entityManager.GetComponentData<AIPedAnimSynced>(pedMono.entity);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        animSynced.isMove = animator.GetBool("IsMove");
        animSynced.jumping = animator.GetBool("Jump");

        ecb.SetComponent(pedMono.entity, animSynced);

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
    }

    void SyncTriggerAnimationRPC()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity rpcAnim = ecb.CreateEntity();

        GhostOwner ghostOwner = Game.Instance.entityManager.GetComponentData<GhostOwner>(pedMono.entity);

        ecb.AddComponent(rpcAnim, new AIPedAnimAttackSynced
        {
            pedEntity = pedMono.entity,
            networkId = ghostOwner.NetworkId,
        });

        ecb.AddComponent(rpcAnim, new SendRpcCommandRequest());

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
    }

    public void Triggered()
    {
        if (actualState == AIState.IDLE)
        {
            int closestPed = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < allPlayerPed.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, allPlayerPed[i].transform.position);

                if (distance < closestDistance)
                {
                    closestPed = i;
                    closestDistance = distance;
                }
            }

            indexCurrentTarget = closestPed;
            actualState = AIState.FOLLOW;
        }
    }


#if UNITY_EDITOR

    [ExecuteInEditMode]
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 dir = transform.forward * detectionRange;
        Vector3 dirRight = Quaternion.Euler(Vector3.up * detectionAngle) * dir;
        Vector3 dirLeft = Quaternion.Euler(-Vector3.up * detectionAngle) * dir;
        Gizmos.DrawLine(transform.position, transform.position + dirRight);
        Gizmos.DrawLine(transform.position, transform.position + dirLeft);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, disengageRange);

    }
#endif
#endif
}

