using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public enum State // 캐릭터 상태
{
    //SelectWait, // 선택 대기
    //Selected, // 선택 종료
    None, // 기본 상태
    Attacking, // 공격 중
    Dodging, // 회피 중
    Stun, // 회피 실패(=피격, 스턴==공격실패)
    Dead, // 사망
}


public class Player : MonoBehaviour
{
    // public AudioClip m_attackSE; // 공격 시 음향
    // public AudioClip m_avoidSE; // 회피 시 음향 
    // 등등... 음향도 여기서 정의

    // 애니메이션 정의

    /*
    public enum Motion
    {
        Idle, // 대기 동작
        Attack, // 공격
        Avoid, // 회피
        Hurt, // 데미지 입음
    };

    Motion m_currentMotion;
    Animation m_anim; // 이거 안 쓰는 애 같은데? 나중에 코드 읽고 삭제하기
    */
    ActionKind m_selected; // 공격할지 회피할지 선택
    short m_damage;
    State m_state;


    [SerializeField] float m_speed = 4.0f;
    [SerializeField] float m_jumpForce = 7.5f;
    [SerializeField] float m_rollForce = 6.0f;
    [SerializeField] bool m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    public GameObject PlayerHealthPrefab;
    public GameObject PlayerHealth;
    private PlayerHealthSystem healthSystem;

    private Animator m_animator;
    private Rigidbody2D m_body2d;
    private Sensor_HeroKnight m_groundSensor;
    private Sensor_HeroKnight m_wallSensorR1;
    private Sensor_HeroKnight m_wallSensorR2;
    private Sensor_HeroKnight m_wallSensorL1;
    private Sensor_HeroKnight m_wallSensorL2;

    //private bool m_isWallSliding = false;
    private bool m_grounded = false;
    private bool m_rolling = false;
    private int m_facingDirection = 1;
    private int m_currentAttack = 0;
    //private float m_timeSinceAttack = 0.0f;
    private float m_delayToIdle = 0.0f;
    private float m_rollDuration = 0.1f;
    private float m_rollCurrentTime;
    private float globalCoolDown = 0.0f;
    private float originPos;
    private float enemyPos;

    private float attackDelay = 2.0f;
    public float attackSpeed = 2.0f;
    public float reloadSpeed = 2.0f;
    public float dodgeSpeed = 2.0f;

    public bool isDead; // 사망 판정

    GameObject m_opponentPlayer; // 상대방 플레이어 오브젝트
    Player m_opponentPlayerScript; // 상대방 플레이어의 스크립트

    // 데미지 텍스트
    public GameObject hitDamageText;
    public GameObject damagePos;


    public void GetOpponentPlayer(int m_playerId)
    {
        if (m_playerId == 0)
        {
            m_opponentPlayer = GameObject.Find("client_HeroKnight");
            if (m_opponentPlayer == null)
            {
                Debug.Log("client_HeroKnight had not found");
            }

        }
        else
        {
            m_opponentPlayer = GameObject.Find("host_HeroKnight");
            if (m_opponentPlayer == null)
            {
                Debug.Log("host_HeroKnight had not found");
            }
        }
        m_opponentPlayerScript = m_opponentPlayer.GetComponent<Player>();
    }


    public void Attack()
    {
        //animation
        m_currentAttack=(m_currentAttack+1)%3 + 1;
        m_animator.SetTrigger("Attack"+m_currentAttack);
        //set cooltime

        //motion

        //do real Deal




    }

    IEnumerator AttackCoroutine()
    {
        float temp = 0.0f;
        globalCoolDown = 3.0f;
        //m_state = State.Attacking;
        m_damage = 0;
        Debug.Log("Start Attack");

        Vector3 origin = transform.position;
        Vector3 newPos = origin;
        while (temp < attackDelay)
        {
            temp += Time.deltaTime * attackSpeed;
            
            enemyPos = m_opponentPlayer.transform.position.x;

            // 여기서 그냥 enemyPos - transform.position.x 해줘도 왼쪽/오른쪽 상관 없나?
            // 상관 있는 듯... 아래 코드 수정하기
            m_body2d.velocity = new Vector2((enemyPos - transform.position.x) * m_speed, m_body2d.velocity.y);
            yield return null;
        }

        Debug.Log("Attack!");
        m_state = State.Attacking;
        m_currentAttack = (m_currentAttack + 1) % 3 + 1;
        m_animator.SetTrigger("Attack" + m_currentAttack);
        m_damage = (short)Random.Range(5, 21); // 공격 데미지 랜덤하게

        yield return new WaitForSeconds(0.06f);

        temp = 0.0f;
        while (globalCoolDown > temp)
        {
            temp += Time.deltaTime * reloadSpeed;
            yield return null;
        }
        globalCoolDown = 0.0f;
        Debug.Log("End Attack");

        m_state = State.None; // 돌아가기 전에 Attacking 상태를 해제해야 하나?
        //m_selected = ActionKind.None; // 상태 선택 중 해제
    }

    public void Dodge()
    {
        //animation
        //m_animator.SetTrigger("Dodge");
        //set cooltime

        //motion

        //do real Deal

    }


    IEnumerator DodgeCoroutine()
    {
        float invincibleTime = 0.04f;
        globalCoolDown = 1.0f;
        m_state = State.Dodging;
        m_damage = 0;
        float temp = 0.0f;

        m_rolling = true;
        m_rollCurrentTime = 0.0f;
        m_animator.SetTrigger("Roll");
        Vector3 origin = transform.position;
        Vector3 newPos = origin;
        Debug.Log("Start Dodge");

        while (temp < invincibleTime)
        {
            temp += Time.deltaTime;
            if (m_rolling)
            {
                m_body2d.velocity = new Vector2(-10f * m_speed, m_body2d.velocity.y);
            }
            yield return null;
        }

        temp = 0.0f;

        while (globalCoolDown > temp)
        {
            temp += Time.deltaTime * reloadSpeed;
            yield return null;
        }
        Debug.Log("End Dodge");
        globalCoolDown = 0.0f;

        m_state = State.None;
        //m_selected = ActionKind.None;
    }


    public void Jump()
    {
        //animation
        m_animator.SetTrigger("Jump");
        //set cooltime

        //motion

        //do real Deal
    }

    public bool getHit(short damage)
    {
        GameObject damageText = Instantiate(hitDamageText); // 텍스트 생성
        damageText.transform.position = damagePos.transform.position; // 표시될 위치
        damageText.GetComponent<DamageText>().damage = damage; // 데미지 전달

        m_animator.SetTrigger("Hurt");
        isDead = healthSystem.TakeDamage((float)damage);
        Debug.Log(healthSystem.hitPoint);

        return isDead;
    }


    public IEnumerator PlayerDied()
    {
        // 플레이어 사망 시 실행되는 코루틴

        m_state = State.Dead;
        Time.timeScale = 0.5f; // 느리게 연출하기 위함
        m_animator.SetTrigger("Death");

        yield return new WaitForSecondsRealtime(2.0f);
        Time.timeScale = 1f;
    }


    private void Awake()
    {
        /*
        m_currentMotion = Motion.Idle;
        m_anim = GetComponentInChildren<Animation>();

        m_damage = 0;
        */
    }

    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();
        PlayerHealth = Instantiate(PlayerHealthPrefab, GameObject.Find("Canvas").transform) as GameObject;
        //PlayerHealth.name = this.name;
        healthSystem = PlayerHealth.GetComponent<PlayerHealthSystem>();
        m_body2d = GetComponent<Rigidbody2D>();

        m_selected = ActionKind.None;
        m_state = State.None;
        m_damage = 0;

        originPos = transform.position.x;
        enemyPos = m_opponentPlayer.transform.position.x;

        damagePos.transform.position = transform.GetChild(0).transform.position; // 데미지 텍스트가 표시될 위치
    }

    // Update is called once per frame
    void Update()
    {
        
        /*
        switch (m_currentMotion)
        {
            case Motion.Idle: // 대기 모션
                break;
            case Motion.Attack:
                //Attack();
                break;
            case Motion.Avoid:
                //Dodge();
                break;
            case Motion.Hurt:
                break;
        }
        */
    }


    public void ChangeAnimationAction(ActionKind action)
    {
        if (action == ActionKind.Attack) 
        {
            //Attack();
            StartCoroutine(AttackCoroutine());
            //m_selected = ActionKind.None;
        }
        else if (action == ActionKind.Dodge)
        {
            //Dodge();
            StartCoroutine(DodgeCoroutine());
            //m_selected = ActionKind.None;
        }
    }



    // 아직 액션을 취하기 전
    public void UpdateSelectAction()
    {
        if (m_selected == ActionKind.None) // 현재 선택해서 실행 중인 액션이 없을 때만 키 입력 값 받음
        {

            if (Input.GetMouseButtonDown(0) && m_state == State.None) // 좌클릭 시 공격
            {
                m_selected = ActionKind.Attack;
                //m_state = State.None; // Attacking이 아닌 이유: 타이밍을 맞추기 위해 정확히 칼을 휘두르는 시점에 State.Attacking을 적용하고, 그것을 비교하도록 함
                m_damage = 0;

                //StartCoroutine(AttackCoroutine());
            }
            else if (Input.GetMouseButtonDown(1) && m_state == State.None) // 우클릭 시 회피
            {
                m_selected = ActionKind.Dodge;
                //m_state = State.None;
                m_damage = 0;

                //StartCoroutine(DodgeCoroutine());
            }
            else
            {
                m_selected = ActionKind.None;
                m_state = State.None; // 이 부분은 ActionKind를 선택하지 않아도 스턴 or 사망 상태일 수 있음. 적절하게 수정되어야 함.
                                      // m_stuned 같은 bool 변수를 추가해서, GamePlay에서 그 값을 넘겨받고 그에 따라 m_state를 변경해주면 좋을 것 같음
                                      // 아니면 코루틴으로 시간 지연
                m_damage = 0;
            }
        }
        else
            m_selected =ActionKind.None;

    }


    // 선택된 액션 반환
    public ActionKind GetActionKind()
    {
        return m_selected;
    }

    public short GetDamage()
    {
        return m_damage;
    }

    // 현재 스테이트 반환
    public State GetState()
    {
        return m_state;
    }
}
