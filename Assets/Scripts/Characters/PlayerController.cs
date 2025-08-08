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
    private float stopDistance; // 停止距离


    protected virtual void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();

        stopDistance = agent.stoppingDistance;

        rb = this.GetComponent<Rigidbody>();
        cam = Camera.main.transform;
        // 设置相机的目标为这个
        CameraController camCC = Camera.main.GetComponent<CameraController>();
        if (camCC != null)
        {
            camCC.target = this.transform;
        }
    }
    // 切换场景时, 游戏体是新生成的，而MOusemanger没有删除，鼠标监听的一直是以前的，所以当游戏体新生成时注册，关闭时撤销
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
            // 切换死亡
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
        // 检测角色是否死亡（当前生命值 ≤ 0）
        isDeath = characterStats.CurrentHealth <= 0;

        // 如果角色已死亡，直接退出更新
        if (isDeath)
        {
            return;
        }

        // 获取键盘输入（水平方向：A/D键，垂直方向：W/S键）
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        // 创建归一化移动方向向量（忽略Y轴）
        Vector3 dir = new Vector3(horizontal, 0f, vertical).normalized;

        // 当输入方向有效时（输入量 ≥ 0.1）
        if ((dir.magnitude >= 0.1f))
        {
            // 停止导航代理的自动寻路
            agent.isStopped = true;

            // 计算目标角度：将输入方向转换为世界空间中的旋转角度（考虑摄像机偏转）
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            // 使用平滑阻尼过渡当前旋转角度
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            // 应用旋转到角色（仅绕Y轴旋转）
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // 将目标角度转换为移动方向
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // 设置刚体速度（保持垂直速度，水平方向按指定速度移动）
            this.rb.velocity = this.rb.velocity.y * Vector3.up + moveDir * speed;
        }
        else
        {
            // 无输入时：水平速度归零，保留垂直速度（如重力）
            this.rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }

        // 根据移动方向播放动画
        playAni(dir);
    }
    // 
    private void SwitchAnimation()
    {
        //anim.SetFloat("Speed", agent.velocity.sqrMagnitude);// 根据速度，判定是走还是跑
        anim.SetBool("Death", isDeath); // 死亡
    }
    void playAni(Vector3 vec)
    {
        //anim.SetFloat("horizontal", Mathf.Abs(horizontal));
        //anim.SetFloat("vertical", Mathf.Abs(vertical));
        if (agent.isStopped)
        {
            anim.SetFloat("Speed", vec.magnitude);// 根据速度，判定是走还是跑
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

        // 改回stopdistance
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
        agent.stoppingDistance = characterStats.attackData.attackRange;// aget的停止距离为攻击距离
        // 移动
        transform.LookAt(this.attackTarget.transform);
        while (Vector3.Distance(transform.position, attackTarget.transform.position) > characterStats.attackData.attackRange)
        {
            agent.destination = attackTarget.transform.position;
            yield return null;
        }
        agent.isStopped = true;
        // 攻击
        if (lastAttackTime < 0)
        {
            anim.SetBool("Critical", characterStats.isCritical);
            anim.SetTrigger("Attack");
            lastAttackTime = characterStats.attackData.coolDown;
        }
    }
    // 执行反击石头人
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
