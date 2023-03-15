using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates{ GUARD, PATROL, CHASE, DEAD}
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
public class EnemyController : MonoBehaviour, IEndGameObserver
{
    private EnemyStates enemyStates;
    private NavMeshAgent agent;
    private Animator animator;
    protected CharacterStats characterStats;

    [Header("Base Settings")]
    public float sightRadius;
    protected GameObject attackTarget;
    public bool isGuard;
    private float speed;
    public float lookAtTime;
    private float remainLookAtTime;
    private float lastAttackTime;

    [Header("Patrol State")]
    public float patrolRange;
    private Vector3 wayPoint;
    private Vector3 guardPos;
    private Quaternion guardRotation;

    // game ���
    private bool isPlayerDead;

    bool isWalk;
    bool isChase;
    bool isFollow;
    bool isDeath;
    bool isWin;

    // ���bug
    // ���ﲻ��Ŀ���
    public float stayYuanDiTimer = 2.0f;
    public float stayYuanDiTime = 0f;
    private Vector3 lastPos = new Vector3(0,0,0);

    private void Awake()
    {
        //Debug.Log("Awake()");
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();
        speed = agent.speed;
        guardPos = transform.position;
        guardRotation = transform.rotation;
        remainLookAtTime = lookAtTime;
    }
    // ����������GameManager.Instance�Ż����
    //private void OnEnable()
    //{
    //    Debug.Log(GameManager.Instance);
    //    GameManager.Instance.AddEndGameObserver(this);
    //}
    private void OnDisable()
    {
        if (!GameManager.IsInitialized) return;
        GameManager.Instance.RemoveEndGameObserver(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        if (isGuard)
        {
            enemyStates = EnemyStates.GUARD;
        }
        else
        {
            enemyStates = EnemyStates.PATROL;
            GetNextWayPoint();
        }
        GameManager.Instance.AddEndGameObserver(this);
    }

    // Update is called once per frame
    void Update()
    {
        isDeath = characterStats.CurrentHealth <= 0;
        if (!isPlayerDead)
        {
            SwitchStates();
            SwitchAnimations();
            lastAttackTime -= Time.deltaTime;
        }
    }
    // �л�����
    void SwitchAnimations()
    {
        animator.SetBool("Walk", isWalk);
        animator.SetBool("Chase", isChase);
        animator.SetBool("Follow", isFollow);
        animator.SetBool("Critical", characterStats.isCritical); // �ֱ汩�����
        animator.SetBool("Death", isDeath); // ����
    }
    void SwitchStates()
    {
        if(characterStats.CurrentHealth <= 0) {
            //Debug.Log(isDeath);
            enemyStates = EnemyStates.DEAD;
        }else if (FoundPlayer())
        {
            enemyStates = EnemyStates.CHASE;
        }
        switch (enemyStates)
        {
            case EnemyStates.GUARD:
                isChase = false;
                if (transform.position != guardPos)
                {
                    isWalk = true;
                    agent.isStopped = false;
                    agent.destination = guardPos;
                    if(Vector3.SqrMagnitude(guardPos - transform.position) <= agent.stoppingDistance)
                    {
                        isWalk=false;
                        transform.rotation = Quaternion.Lerp(transform.rotation, guardRotation, 0.01f);
                    }

                }
                break;
            case EnemyStates.PATROL:
                isChase = false;
                agent.speed = speed * 0.5f;

                // ����Ŀ���
                if (Vector3.Distance(transform.position, wayPoint) <= agent.stoppingDistance)
                {
                    isWalk = false;
                    // ͣ��
                    if (remainLookAtTime > 0)
                    {
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else
                    {
                        GetNextWayPoint();
                    }
                }
                else
                {
                    isWalk = true;
                    agent.destination = wayPoint;

                    // ͣ��
                    if (lastPos == transform.position)
                    {
                        if (stayYuanDiTime > stayYuanDiTimer)
                        {
                            stayYuanDiTime = 0f;
                            GetNextWayPoint();
                        }
                        else
                        {
                            stayYuanDiTime += Time.deltaTime;
                        }
                    }
                    lastPos = transform.position;
                }
                break;
            case EnemyStates.CHASE:
                //TODO:��϶���
                isWalk = false;
                isChase = true;
                agent.speed = speed;
                if (!FoundPlayer())
                {
                    //TODO:���ѻ���һ��״̬
                    isFollow = false;
                    // ͣ��
                    if (remainLookAtTime > 0)
                    {
                        remainLookAtTime -= Time.deltaTime;
                        agent.destination = transform.position;
                    } else if (isGuard)
                    {
                        enemyStates = EnemyStates.GUARD;
                    }
                    else
                    {
                        enemyStates = EnemyStates.PATROL;
                    }
                }
                else
                {
                    agent.isStopped = false;
                    isFollow = true;
                    agent.destination = attackTarget.transform.position;
                }

                //TODO:�ڹ�����Χ�ڲ๥��
                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    isFollow = false;
                    agent.isStopped = true;
                    if (lastAttackTime < 0)
                    {
                        lastAttackTime = characterStats.attackData.coolDown;
                        // �Ƿ񱩻�
                        characterStats.isCritical = Random.value < characterStats.attackData.criticalChance;
                        Attack();
                    }
                }
                break;
            case EnemyStates.DEAD:
                //agent.enabled = false;
                agent.radius = 0;
                GetComponent<Collider>().enabled = false;
                // ͣ��2s����
                Destroy(gameObject, 2);
                break;
        }
    }
    // ִ�й���
    void Attack()
    {
        // ��������������
        isDeath = characterStats.CurrentHealth <= 0;
        if (isDeath)
        {
            return;
        }
        transform.LookAt(attackTarget.transform);// ��������
        if (TargetInSkillRange())// ����
        {
            animator.SetTrigger("Skill");
        }
        if (TargetInAttackRange()) // ����
        {
            // 
            animator.SetTrigger("Attack");
        }
        
    }
    // �Ƿ��ڹ�����Χ��
    bool TargetInAttackRange()
    {
        // ��������������
        isDeath = characterStats.CurrentHealth <= 0;
        if (isDeath)
        {
            return false;
        }
        if (attackTarget != null)
        {
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.attackRange;
        }
        return false;
    }
    bool TargetInSkillRange()
    {
        // ��������������
        isDeath = characterStats.CurrentHealth <= 0;
        if (isDeath)
        {
            return false;
        }
        if (attackTarget != null)
        {
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.skillRange;
        }
        return false;
    }
    bool FoundPlayer()
    {
        var colliders = Physics.OverlapSphere(transform.position, sightRadius);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag(Tags.Player))
            {
                attackTarget = collider.gameObject;
                return true; 
            }
        }
        return false;
    }
    NavMeshHit hit;
    void GetNextWayPoint()
    {
        remainLookAtTime = lookAtTime;
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);
        Vector3 newPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z + randomZ);
        // �ж��µ㣬�ڵ��εķ�Χ�ڣ��Ƿ�Ϊ���ߵĵ�
        if (NavMesh.SamplePosition(newPoint, out hit, patrolRange, 1))
        {
            wayPoint = newPoint;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius); 
    }
    // ִ�й�������
    void Hit()
    {
        if (attackTarget == null || !transform.IsFacingTarget(attackTarget.transform))
            return;
        var targetStats = attackTarget.GetComponent<CharacterStats>();
        targetStats.TakeDamage(characterStats, targetStats);
    }
    // ������ִ�й������������Բ������������Ч
    void HitPlayHurtAudio()
    {
        if (attackTarget == null || !transform.IsFacingTarget(attackTarget.transform))
            return;
        var targetStats = attackTarget.GetComponent<CharacterStats>();
        targetStats.TakeDamage(characterStats, targetStats, true);
    }
    public void EndNotify()
    {
        // ���Ż���
        // ֹͣ�ƶ�
        // û�й���Ŀ��
        isPlayerDead = true; // �������
        animator.SetBool("Win", true);
        isChase = false; 
        isWalk = false;
        attackTarget = null; 
    }
}
