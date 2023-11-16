using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 공격/회피 설정
public enum ActionKind
{
    None = 0,
    Attack, // 공격
    Avoid, // 회피
};

// 공격/회피 정보 구조체
public struct AttackInfo
{
    public ActionKind actionKind;
    public float actionTime; // 경과 시간. 서로의 공격/회피 타이밍을 비교하는 용도
    // 데미지 값도 여기에 넣으면 될 것 같은?

    public AttackInfo(ActionKind kind, float time)
    {
        actionKind = kind;
        actionTime = time;
    }
};


public struct InputData
{
    public AttackInfo attackInfo;
}


// 승자 식별
public enum Winner
{
    None = 0,
    ServerPlayer, // 서버 쪽(1P) 승리 (공격 성공)
    ClientPlayer, // 클라이언트 쪽(2P) 승리 (공격 성공)
    Draw,
};

class ResultChecker
{
    // 공격/회피에서 승패 구하기
    public static Winner GetActionWinner(AttackInfo server, AttackInfo client)
    {
        string debugStr = "server.actionKind: " + server.actionKind.ToString() + " time: " + server.actionTime.ToString();
        debugStr += "\nclient.actionKind: " + client.actionKind.ToString() + " time: " + client.actionTime.ToString();
        Debug.Log(debugStr);

        ActionKind serverAction = server.actionKind;
        ActionKind clientAction = client.actionKind;

        // 공격/회피가 바르게 이루어졌는지 판정
        if (serverAction != ActionKind.Attack && clientAction != ActionKind.Attack)
        {
            // 양측이 아무것도 하지 않거나/회피하거나 둘 중 하나를 함 (양측 다 공격하지 않음)
            return Winner.None;
        }


        // 시간 대결 (공격/회피 타이밍 판정)
        float serverTime = server.actionTime;
        float clientTime = client.actionTime;

        /* 주의!!
           회피 키를 눌렀을 때 회피가 유효한 시간을 설정해야 함!
           회피 먼저 누르고 상대가 공격을 눌렀을 때,
           이 회피가 얼마나 오래 유효한지를 정하고 아래 코드에 반영해주자!!

           현재 바탕으로 참고한 코드는 단판 액션 게임이고,
           따라서 그냥 먼저 공격/회피를 누른 사람이 성공임.

           우리 게임은 계속해서 공격/회피 버튼을 누를 수 있기 때문에,
           한 버튼을 눌렀을 때 일정 시간 다음 입력이 바로 반영되지 않도록
           지연 시간을 정해야 하고, 그 시간 동안의 입력은 무시해야 함.
           지연 시간이 지난 다음 들어온 입력에 대해서만 소켓으로 보내도록 함.
           그러나 내 지연 시간 동안 상대방이 입력한 공격/회피 값에 대해서도
           소켓으로 받아서 처리해야 함.

           serverTime과 clientTime이 비교된 후에는 다시 리셋되고,
           그것이 서로 일치해야 한다는 것도 유의해서 테스트하자!
        */

        if (serverAction == ActionKind.Attack)
        {
            // 공격-공격이거나 공격-회피인 경우
            // 1P가 2P보다 빠를 때, 공격 성공
            if (serverTime < clientTime)
            {
                return Winner.ServerPlayer;
            }
            else if (clientAction == ActionKind.Attack)
            {
                // 공격-공격인데 1P가 늦었을 때, 2P의 공격 성공
                return Winner.ClientPlayer;
            }
            else
            {
                // 공격-회피인데 1P가 늦었을 때, 2P의 회피 성공
                return Winner.Draw;
            }
        }
        else
        {
            // 회피-공격인 경우
            // 2P보다 느리면 회피 실패
            if (serverTime > clientTime)
            {
                return Winner.ClientPlayer;
            }
            else
            {
                // 회피-공격인데 2P가 늦었을 때, 1P의 회피 성공
                return Winner.Draw;
            }
        }

        // 시간이 동일할 때, 무승부
        // return Winner.Draw;
    }
}