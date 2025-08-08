using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController :  SingleTon<PlayerController>
{
    private Transform cam;
    public float speed = 4f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    float horizontal;
    float vertical;

    private Animator anim;
    private Rigidbody rb;

    private NavMeshAgent agent;
    public CharacterStats characterStats;

    GameObject attackTarget;
    private float lastAttackTime;
    bool isDeath;
    private float stopDistance; // ֹͣ����


    protected virtual void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();

        stopDistance = agent.stoppingDistance;

        rb = this.GetComponent<Rigidbody>();
        cam = Camera.main.transform;
        // ���������Ŀ��Ϊ���
        CameraController camCC = Camera.main.GetComponent<CameraController>();
        if (camCC != null)
        {
            camCC.target = this.transform;
        }
    }
    // �л�����ʱ, ��Ϸ���������ɵģ���MOusemangerû��ɾ������������һֱ����ǰ�ģ����Ե���Ϸ��������ʱע�ᣬ�ر�ʱ����
    private void OnEnable()
    {
        MouseManager.Instance.OnMouseClicked += MoveToTarget;
        MouseManager.Instance.OnEnemyClicked += EnemyAttack;
        GameManager.Instance.RigisterPlayer(characterStats);
    }
    // Start is called before the first frame update
    void Start()
    {
        SaveManager.Instance.LoadPlayerData();
    }
    private void OnDisable()
    {
        MouseManager.Instance.OnMouseClicked -= MoveToTarget;
        MouseManager.Instance.OnEnemyClicked -= EnemyAttack;
    }

    // Update is called once per frame
    void Update()
    {
        isDeath = characterStats.CurrentHealth <= 0;
        if (isDeath)
        {
            // �л�����
            SwitchAnimation();
            GameManager.Instance.isPlayerDeath = true;
            //Debug.Log("isDeath");
            GameManager.Instance.NotifyObservers();
            return;
        }
        SwitchAnimation();
        MoveToAttackTarget();
        lastAttackTime -= Time.deltaTime;
    }
    void FixedUpdate()
    {
        // ����ɫ�Ƿ���������ǰ����ֵ �� 0��
        isDeath = characterStats.CurrentHealth <= 0;

        // �����ɫ��������ֱ���˳�����
        if (isDeath)
        {
            return;
        }

        // ��ȡ�������루ˮƽ����A/D������ֱ����W/S����
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        // ������һ���ƶ���������������Y�ᣩ
        Vector3 dir = new Vector3(horizontal, 0f, vertical).normalized;

        // �����뷽����Чʱ�������� �� 0.1��
        if ((dir.magnitude >= 0.1f))
        {
            // ֹͣ����������Զ�Ѱ·
            agent.isStopped = true;

            // ����Ŀ��Ƕȣ������뷽��ת��Ϊ����ռ��е���ת�Ƕȣ����������ƫת��
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            // ʹ��ƽ��������ɵ�ǰ��ת�Ƕ�
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            // Ӧ����ת����ɫ������Y����ת��
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // ��Ŀ��Ƕ�ת��Ϊ�ƶ�����
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // ���ø����ٶȣ����ִ�ֱ�ٶȣ�ˮƽ����ָ���ٶ��ƶ���
            this.rb.velocity = this.rb.velocity.y * Vector3.up + moveDir * speed;
        }
        else
        {
            // ������ʱ��ˮƽ�ٶȹ��㣬������ֱ�ٶȣ���������
            this.rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }

        // �����ƶ����򲥷Ŷ���
        playAni(dir);
    }
    // 
    private void SwitchAnimation()
    {
        //anim.SetFloat("Speed", agent.velocity.sqrMagnitude);// �����ٶȣ��ж����߻�����
        anim.SetBool("Death", isDeath); // ����
    }
    void playAni(Vector3 vec)
    {
        //anim.SetFloat("horizontal", Mathf.Abs(horizontal));
        //anim.SetFloat("vertical", Mathf.Abs(vertical));
        if (agent.isStopped)
        {
            anim.SetFloat("Speed", vec.magnitude);// �����ٶȣ��ж����߻�����
        }
        else
        {
            anim.SetFloat("Speed", agent.velocity.sqrMagnitude);
        }
    }
    public void MoveToTarget(Vector3 target)
    {
        StopAllCoroutines();
        if (isDeath) return;

        // �Ļ�stopdistance
        agent.stoppingDistance = stopDistance;
        agent.isStopped = false;
        agent.destination = target;
    }
    private void EnemyAttack(GameObject target)
    {
        if (isDeath) return;
        if (target != null)
        {
            characterStats.isCritical = UnityEngine.Random.value < characterStats.attackData.criticalChance;
            this.attackTarget = target;
            StartCoroutine(MoveToAttackTarget());
        }
    }
    IEnumerator MoveToAttackTarget()
    {
        agent.isStopped = false;
        agent.stoppingDistance = characterStats.attackData.attackRange;// aget��ֹͣ����Ϊ��������
        // �ƶ�
        transform.LookAt(this.attackTarget.transform);
        while (Vector3.Distance(transform.position, attackTarget.transform.position) > characterStats.attackData.attackRange)
        {
            agent.destination = attackTarget.transform.position;
            yield return null;
        }
        agent.isStopped = true;
        // ����
        if (lastAttackTime < 0)
        {
            anim.SetBool("Critical", characterStats.isCritical);
            anim.SetTrigger("Attack");
            lastAttackTime = characterStats.attackData.coolDown;
        }
    }
    // ִ�з���ʯͷ��
    void Hit()
    {
        if (attackTarget.CompareTag(Tags.AttackAble))
        {
            //if (attackTarget.GetComponent<Rock>() && attackTarget.GetComponent<Rock>().rockStates == Rock.RockStates.HitNothing)
            if (attackTarget.GetComponent<Rock>())
                {
                attackTarget.GetComponent<Rigidbody>().velocity = Vector3.one;
                attackTarget.GetComponent<Rock>().rockStates = Rock.RockStates.HitEnemy;
                attackTarget.GetComponent<Rigidbody>().velocity = Vector3.one;
                attackTarget.GetComponent<Rigidbody>().AddForce(transform.forward * 20.0f, ForceMode.Impulse);
            }
        }
        else
        {
            var targetStats = attackTarget.GetComponent<CharacterStats>();
            targetStats.TakeDamage(characterStats, targetStats);
        }
    }
}
