using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActionSelect : MonoBehaviour
{
    ActionKind m_selected; // 공격할지 회피할지 선택
    short m_damage;

    enum State // 캐릭터 상태
    {
        SelectWait, // 선택 대기
        Selected, // 선택 종료
        //None, // 기본 상태
        
    }
    State m_state;

    // Start is called before the first frame update
    void Start()
    {
        m_selected = ActionKind.None;
        m_state = State.SelectWait;
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
    public void UpdateSelectWait()
    {
        if (Input.GetMouseButtonDown(0)) // 좌클릭 시 공격
        {
            m_selected = ActionKind.Attack;
            m_state = State.Selected;
            m_damage = 10; // 우선 10으로 둔 것. 나중에 랜덤값을 얻도록 수정하기!
        }
        else if (Input.GetMouseButtonDown(1)) // 우클릭 시 회피
        {
            m_selected = ActionKind.Avoid;
            m_state = State.Selected;
            m_damage = 0;
        }
        else
        {
            m_selected = ActionKind.None;
            m_state = State.SelectWait;
            m_damage = 0;
        }
    }

    // 액션을 선택한 후
    void UpdateSelected()
    {
        m_selected = ActionKind.None;
        m_state = State.SelectWait;
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

    // 선택 종료면 true
    public bool IsSelected()
    {
        if (m_state == State.Selected)
        {
            return true;
        }
        return false;
    }
}
