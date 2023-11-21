using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // public AudioClip m_attackSE; // ???? ?? ????
    // public AudioClip m_avoidSE; // ???? ?? ???? 
    // ????... ?????? ?????? ????

    // ?????????? ????
    public enum Motion
    {
        Idle, // ???? ????
        Attack, // ????
        Avoid, // ????
        Hurt, // ?????? ????
    };
    Motion m_currentMotion;
    Animation m_anim;
    int m_damage;
    [SerializeField] float m_speed = 4.0f;
    [SerializeField] float m_jumpForce = 7.5f;
    [SerializeField] float m_rollForce = 6.0f;
    [SerializeField] bool m_noBlood = false;
    [SerializeField] GameObject m_slideDust;
    [SerializeField] GameObject my_healthObj;
    private Animator m_animator;
    private Rigidbody2D m_body2d;
    private Sensor_HeroKnight m_groundSensor;
    private Sensor_HeroKnight m_wallSensorR1;
    private Sensor_HeroKnight m_wallSensorR2;
    private Sensor_HeroKnight m_wallSensorL1;
    private Sensor_HeroKnight m_wallSensorL2;
    private bool m_isWallSliding = false;
    private bool m_grounded = false;
    private bool m_rolling = false;
    private int m_facingDirection = 1;
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private float m_delayToIdle = 0.0f;
    private float m_rollDuration = 0.1f;
    private float m_rollCurrentTime;
    private float globalCoolDown = 0.0f;
    private float originPos;
    private float enemyPos;
    private void Awake()
    {
        m_currentMotion = Motion.Idle;
        m_anim = GetComponentInChildren<Animation>();

        m_damage = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (m_currentMotion)
        {
            /*
            case Motion.In:
                if (m_anim.isPlaying == false)
                {
                    ChangeAnimation(Motion.Idle);
                    // ???? ???????? ?????? ?? ???????? ?????? ????.
                    GameObject board = GameObject.Find("BoardYou");
                    board.GetComponent<BoardYou>().Run();
                }
                break;
            */
            case Motion.Idle: // ???? ????
            case Motion.Attack:
            case Motion.Avoid:
            case Motion.Hurt:
                break;
        }
    }


    public void ChangeAnimation(Motion motion)
    {
        m_currentMotion = motion;
        m_anim.Play(m_currentMotion.ToString());
    }

    public void ChangeAnimationAction(ActionKind action)
    {
        // ???? ???????????????? ?????? ?? ?? ????????
        // Winner.serverPlayer?? ?????? ?????? ??????

        // case Winner.ServerPlayer: // ?????? ?????? ??
        // if (action == ActionKind.Attack) {
        // ChangeAnimationAttack();
        // }

    }


    // ???????????? ?????? true ????
    public bool IsCurrentAnimationEnd()
    {
        return (m_anim.isPlaying == false);
    }


    // ???? ?????????? ?????? true ????
    public bool IsIdleAnimation()
    {
        return (m_currentMotion == Motion.Idle);
    }
}
