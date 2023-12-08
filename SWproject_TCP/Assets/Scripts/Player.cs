using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // public AudioClip m_attackSE; // 공격 시 음향
    // public AudioClip m_avoidSE; // 회피 시 음향 
    // 등등... 음향도 여기서 정의

    // 애니메이션 정의
    public enum Motion
    {
        Idle, // 대기 동작
        Attack, // 공격
        Avoid, // 회피
        Hurt, // 데미지 입음
    };

    Motion m_currentMotion;
    Animation m_anim;
    int m_damage;

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

    public void Attack()
    {
        //animation
        m_currentAttack=(m_currentAttack+1)%3 + 1;
        m_animator.SetTrigger("Attack"+m_currentAttack);
        //set cooltime

        //motion

        //do real Deal



    }

    public void Dodge()
    {
        //animation
        m_animator.SetTrigger("Dodge");
        //set cooltime

        //motion

        //do real Deal
    }

    public void Jump()
    {
        //animation
        m_animator.SetTrigger("Jump");
        //set cooltime

        //motion

        //do real Deal
    }

    public void getHit(short damage)
    {
        m_animator.SetTrigger("Hurt");
        healthSystem.TakeDamage((float)damage);
        Debug.Log(healthSystem.hitPoint);
    }

    private void Awake()
    {
        
        m_currentMotion = Motion.Idle;
        m_anim = GetComponentInChildren<Animation>();

        m_damage = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();
        PlayerHealth = Instantiate(PlayerHealthPrefab, GameObject.Find("Canvas").transform) as GameObject;
        //PlayerHealth.name = this.name;
        healthSystem = PlayerHealth.GetComponent<PlayerHealthSystem>();
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


    public void ChangeAnimation(Motion motion)
    {
        m_currentMotion = motion;
        m_anim.Play(m_currentMotion.ToString());
    }

    public void ChangeAnimationAction(ActionKind action)
    {
        // 서버 클라이언트에서의 판정만 할 수 있으므로
        // Winner.serverPlayer면 자신의 승리로 다룬다

        // case Winner.ServerPlayer: // 공격이 자신의 승
        // if (action == ActionKind.Attack) {
        // ChangeAnimationAttack();
        // }

        if (action == ActionKind.Attack) 
        {
            Attack();
        }
        else if (action == ActionKind.Avoid)
        {
            Dodge();
        }
    }


    // 애니메이션이 끝나면 true 리턴
    public bool IsCurrentAnimationEnd()
    {
        return (m_anim.isPlaying == false);
    }


    // 대기 애니메이션 중이면 true 리턴
    public bool IsIdleAnimation()
    {
        return (m_currentMotion == Motion.Idle);
    }
}
