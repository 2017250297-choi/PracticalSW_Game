using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // public AudioClip m_attackSE; // ���� �� ����
    // public AudioClip m_avoidSE; // ȸ�� �� ���� 
    // ���... ���⵵ ���⼭ ����

    // �ִϸ��̼� ����
    public enum Motion
    {
        Idle, // ��� ����
        Attack, // ����
        Avoid, // ȸ��
        Hurt, // ������ ����
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
                    // ��� ������� ��ȯ�� �� �÷��̾� ǥ�⸦ ����.
                    GameObject board = GameObject.Find("BoardYou");
                    board.GetComponent<BoardYou>().Run();
                }
                break;
            */
            case Motion.Idle: // ��� ���
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
        // ���� Ŭ���̾�Ʈ������ ������ �� �� �����Ƿ�
        // Winner.serverPlayer�� �ڽ��� �¸��� �ٷ��

        // case Winner.ServerPlayer: // ������ �ڽ��� ��
        // if (action == ActionKind.Attack) {
        // ChangeAnimationAttack();
        // }

    }


    // �ִϸ��̼��� ������ true ����
    public bool IsCurrentAnimationEnd()
    {
        return (m_anim.isPlaying == false);
    }


    // ��� �ִϸ��̼� ���̸� true ����
    public bool IsIdleAnimation()
    {
        return (m_currentMotion == Motion.Idle);
    }
}
