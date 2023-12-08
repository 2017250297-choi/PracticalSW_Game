using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum State // 캐릭터 상태
{
    //SelectWait, // 선택 대기
    //Selected, // 선택 종료
    None, // 기본 상태
    Attacking, // 공격 중
    Dodging, // 회피 중
    Stun, // 회피 실패(=피격, 스턴)
    Dead, // 사망
}

public class ActionSelect : MonoBehaviour
{
    ActionKind m_selected; // 공격할지 회피할지 선택
    short m_damage;
    State m_state;

    // Start is called before the first frame update
    void Start()
    {
        m_selected = ActionKind.None;
        m_state = State.None;
        m_damage = 0;
    }

    /*
    // Update is called once per frame
    void Update()
    {
        switch (m_state)
        {
            case State.SelectWait:
                UpdateSelectWait();
                break;
            case State.Selected:
                UpdateSelected();
                break;
        }
    }
    */

    // 아직 액션을 취하기 전
    public void UpdateSelectAction()
    {
        if (Input.GetMouseButtonDown(0)) // 좌클릭 시 공격
        {
            m_selected = ActionKind.Attack;
            m_state = State.Attacking;
            m_damage = 10; // 우선 10으로 둔 것. 나중에 랜덤값을 얻도록 수정하기!
        }
        else if (Input.GetMouseButtonDown(1)) // 우클릭 시 회피
        {
            m_selected = ActionKind.Dodge;
            m_state = State.Dodging;
            m_damage = 0;
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

    // 액션을 선택한 후
    void UpdateSelected()
    {
        m_selected = ActionKind.None;
        //m_state = State.SelectWait;
        m_damage = 0;
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

    // 선택 종료면 true
    /*
    public bool IsSelected()
    {
        if (m_state == State.Selected)
        {
            return true;
        }
        return false;
    }
    */
}
