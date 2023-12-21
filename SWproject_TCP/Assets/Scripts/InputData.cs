using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 공격/회피 설정
public enum ActionKind
{
    None = 0,
    Attack, // 공격
    Dodge, // 회피
};

// 공격/회피 정보 구조체
public struct AttackInfo
{
    public ActionKind actionKind;
    public State playerState;
    public short damageValue; // 내 공격값
    public short validDamage; // 내가 당한(유효타 먹은) 공격값

    public AttackInfo(ActionKind kind, State state, short myDamage, short hittedDamage)
    {
        actionKind = kind;
        playerState = state;
        damageValue = myDamage;
        validDamage = hittedDamage;
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

