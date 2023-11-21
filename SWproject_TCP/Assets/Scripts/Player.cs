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
                    // 대기 모션으로 전환할 때 플레이어 표기를 낸다.
                    GameObject board = GameObject.Find("BoardYou");
                    board.GetComponent<BoardYou>().Run();
                }
                break;
            */
            case Motion.Idle: // 대기 모션
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
        // 서버 클라이언트에서의 판정만 할 수 있으므로
        // Winner.serverPlayer면 자신의 승리로 다룬다

        // case Winner.ServerPlayer: // 공격이 자신의 승
        // if (action == ActionKind.Attack) {
        // ChangeAnimationAttack();
        // }

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
