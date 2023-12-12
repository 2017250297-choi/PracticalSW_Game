using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;



public class ActionSelect : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {

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


    // 액션을 선택한 후
    void UpdateSelected()
    {
        //m_selected = ActionKind.None;
        //m_state = State.SelectWait;
        //m_damage = 0;
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
