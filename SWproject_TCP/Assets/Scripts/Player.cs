using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public enum State // 캐릭터 상태
{
    None, // 기본 상태
    Attacking, // 공격 중
    Dodging, // 회피 중
    Reloading,//회피 후 무방비한상
    Stun, // 회피 실패(=피격, 스턴==공격실패)
    Dead, // 사망
}

public enum MotionState
{
    None,
    Charge,
    Attack,
    Dodge,
    Stun

}
public class Player : MonoBehaviour
{
    // 등등... 음향도 여기서 정의

    // 애니메이션 정의

    ActionKind m_selected; // 공격할지 회피할지 선택
    short m_damage;
    State m_state;
    MotionState m_motionState=MotionState.None;

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

    private bool m_grounded = false;
    private bool m_rolling = false;
    private int m_facingDirection = 1;
    private int m_currentAttack = 0;
    private float m_delayToIdle = 0.0f;
    private float m_rollDuration = 0.1f;
    private float m_rollCurrentTime;
    private float globalCoolDown = 0.0f;
    private float originPos;
    private Vector2 moveforce;
    private int animCount=0;
    private int totalFail=0;

    private float enemyPos;

    private float attackDelay = 0.8f;
    public float attackSpeed = 1.0f;
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


    IEnumerator StunCoroutine()
    {
        GameObject damageText = Instantiate(hitDamageText); // 텍스트 생성
        damageText.transform.position = damagePos.transform.position; // 표시될 위치
        Vector3 offset = damageText.transform.position;
        offset.y -= 2;
        damageText.transform.position = offset;
        damageText.GetComponent<DamageText>().cases = 2;
        float stunTime = 0.5f;
        float temp = 0f;
        m_state = State.Stun;
        m_motionState = MotionState.Stun;

        while(temp < stunTime)
        {
            temp += Time.deltaTime;
            yield return null;
        }
        m_state = State.None;
        m_motionState = MotionState.None;
    }

    IEnumerator AttackCoroutine()
    {
        healthSystem.UseMana(10);
        float temp = 0.0f;
        globalCoolDown = 1.0f;
        m_state = State.Attacking;
        m_motionState = MotionState.Charge;
        m_damage = 0;
        Debug.Log("Start Attack");

        Vector3 origin = transform.position;
        Vector3 newPos = origin;
        moveforce = new Vector2(m_opponentPlayer.transform.position.x - transform.position.x, 0);
        enemyPos = m_opponentPlayer.transform.position.x;
        float dist = enemyPos - transform.position.x;
        while (temp < attackDelay && m_state==State.Attacking)
        {
            temp += Time.deltaTime;

            if (m_state != State.Attacking)
            {
                healthSystem.UseMana(20);
                yield break;
            }
            yield return null;
        }
        if(m_state !=State.Attacking)
        {
            healthSystem.UseMana(20);
            yield break; 
        }
        Debug.Log("Attack!");
        m_currentAttack = (m_currentAttack + 1) % 3 + 1;
        m_motionState = MotionState.Attack;
        healthSystem.UseMana(30);
        m_animator.SetTrigger("Attack" + m_currentAttack);
        m_damage = (short)Random.Range(5, 21); // 공격 데미지 랜덤하게
        if (m_opponentPlayerScript.m_state == State.Dodging)
            totalFail += 1;
        yield return null;
        m_damage = 0;
        yield return new WaitForSeconds(0.6f-Time.deltaTime);
        if (m_opponentPlayerScript.m_state == State.Dodging)
            totalFail += 1;
        if(totalFail==2)
        {
            totalFail = 0;
            StartCoroutine(StunCoroutine());
            yield break;

        }
        temp = 0.0f;
        m_motionState = MotionState.None;
        while (globalCoolDown > temp)
        {
            temp += Time.deltaTime * reloadSpeed;
            yield return null;
        }
        globalCoolDown = 0.0f;
        Debug.Log("End Attack");

        m_state = State.None; // 돌아가기 전에 Attacking 상태를 해제해야 하나?
        totalFail = 0;
    }



    IEnumerator DodgeCoroutine()
    {
        healthSystem.UseMana(25);
        float invincibleTime = 0.8f;
        globalCoolDown = 0.5f;
        m_state = State.Dodging;
        m_motionState = MotionState.Dodge;
        m_damage = 0;
        float temp = 0.0f;
        enemyPos = m_opponentPlayer.transform.position.x;
        m_rolling = true;
        m_animator.SetTrigger("Roll");
        Debug.Log("Start Dodge");
        float dist = Mathf.Sign(transform.position.x - enemyPos);
        moveforce = new Vector2(dist,0);
        Debug.Log(dist / (invincibleTime / Time.deltaTime));
        yield return new WaitForSeconds(invincibleTime);
        m_rolling = false;
        m_state = State.Reloading;
        m_motionState = MotionState.None;
        temp = 0.0f;

        while (globalCoolDown > temp)
        {
            temp += Time.deltaTime * reloadSpeed;
            yield return null;
        }
        Debug.Log("End Dodge");
        globalCoolDown = 0.0f;

        m_state = State.None;
    }
    public bool getHit(short damage)
    {

            GameObject damageText = Instantiate(hitDamageText); // 텍스트 생성
            damageText.transform.position = damagePos.transform.position; // 표시될 위치
            damageText.GetComponent<DamageText>().cases = 0;
            damageText.GetComponent<DamageText>().damage = damage; // 데미지 전
            if (damage==-1)
            {

                damageText.GetComponent<DamageText>().cases = 1;
                isDead = false;
            }
            else
            {
                m_animator.SetTrigger("Hurt");
                StartCoroutine(StunCoroutine());
                isDead = healthSystem.TakeDamage((float)damage);
            }
                
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
    }

    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();
        PlayerHealth = Instantiate(PlayerHealthPrefab, GameObject.Find("Canvas").transform) as GameObject;
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
        
    }
    private void FixedUpdate()
    {
        if (m_motionState == MotionState.None)
        {
            moveforce = new Vector2(originPos - transform.position.x, 0);
            m_body2d.AddForce(moveforce*1.5f);
            
        }
        else
        {
            switch (m_motionState)
            {
                case MotionState.Charge:
                    m_body2d.AddForce(moveforce*5);
                    break;
                case MotionState.Dodge:
                    m_body2d.AddForce(moveforce*20);
                    break;
                case MotionState.Stun:
                    m_body2d.velocity = new Vector2(0, 0);
                    break;
                default:
                    break;
            }

        }
        if (-1 < m_body2d.velocity.x && m_body2d.velocity.x < 1 && (m_motionState == MotionState.None|| m_motionState == MotionState.Attack))
        {
            m_animator.SetInteger("AnimState", 0);
        }
        else
        {
            m_animator.SetInteger("AnimState", 1);
            m_animator.SetBool("Grounded", true);

        }
        
         
        Debug.Log(m_motionState);
    }


    public void ChangeAnimationAction(ActionKind action)
    {
        if (action == ActionKind.Attack) 
        {
            StartCoroutine(AttackCoroutine());
        }
        else if (action == ActionKind.Dodge)
        {
            StartCoroutine(DodgeCoroutine());
        }
    }

    // 아직 액션을 취하기 전
    public void UpdateSelectAction()
    {
        if (m_selected == ActionKind.None) // 현재 선택해서 실행 중인 액션이 없을 때만 키 입력 값 받음
        {
            if((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))&&m_state==State.Stun)
            {
                Debug.Log("stuned");
                GameObject stuned = Instantiate(hitDamageText); // 텍스트 생성
                stuned.transform.position = damagePos.transform.position; // 표시될 위치
                stuned.GetComponent<DamageText>().cases = 2;

            }
            if (Input.GetMouseButtonDown(0) && m_state == State.None) // 좌클릭 시 공격
            {
                if(healthSystem.isEnoughMana(40))
                {
                    
                    m_selected = ActionKind.Attack;
                }
                else
                {
                    //mana not enough
                }
                
            }
            else if (Input.GetMouseButtonDown(1) && m_state == State.None) // 우클릭 시 회피
            {
                if (healthSystem.isEnoughMana(25))
                {

                m_selected = ActionKind.Dodge;
                }
                else
                {
                    //mana not enough
                }
            }
            else{}
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
