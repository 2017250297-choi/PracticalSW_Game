using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ����/ȸ�� ����
public enum ActionKind
{
    None = 0,
    Attack, // ����
    Avoid, // ȸ��
};

// ����/ȸ�� ���� ����ü
public struct AttackInfo
{
    public ActionKind actionKind;
    public float actionTime; // ��� �ð�. ������ ����/ȸ�� Ÿ�̹��� ���ϴ� �뵵
    // ������ ���� ���⿡ ������ �� �� ����?

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


// ���� �ĺ�
public enum Winner
{
    None = 0,
    ServerPlayer, // ���� ��(1P) �¸� (���� ����)
    ClientPlayer, // Ŭ���̾�Ʈ ��(2P) �¸� (���� ����)
    Draw,
};

class ResultChecker
{
    // ����/ȸ�ǿ��� ���� ���ϱ�
    public static Winner GetActionWinner(AttackInfo server, AttackInfo client)
    {
        string debugStr = "server.actionKind: " + server.actionKind.ToString() + " time: " + server.actionTime.ToString();
        debugStr += "\nclient.actionKind: " + client.actionKind.ToString() + " time: " + client.actionTime.ToString();
        Debug.Log(debugStr);

        ActionKind serverAction = server.actionKind;
        ActionKind clientAction = client.actionKind;

        // ����/ȸ�ǰ� �ٸ��� �̷�������� ����
        if (serverAction != ActionKind.Attack && clientAction != ActionKind.Attack)
        {
            // ������ �ƹ��͵� ���� �ʰų�/ȸ���ϰų� �� �� �ϳ��� �� (���� �� �������� ����)
            return Winner.None;
        }


        // �ð� ��� (����/ȸ�� Ÿ�̹� ����)
        float serverTime = server.actionTime;
        float clientTime = client.actionTime;

        /* ����!!
           ȸ�� Ű�� ������ �� ȸ�ǰ� ��ȿ�� �ð��� �����ؾ� ��!
           ȸ�� ���� ������ ��밡 ������ ������ ��,
           �� ȸ�ǰ� �󸶳� ���� ��ȿ������ ���ϰ� �Ʒ� �ڵ忡 �ݿ�������!!

           ���� �������� ������ �ڵ�� ���� �׼� �����̰�,
           ���� �׳� ���� ����/ȸ�Ǹ� ���� ����� ������.

           �츮 ������ ����ؼ� ����/ȸ�� ��ư�� ���� �� �ֱ� ������,
           �� ��ư�� ������ �� ���� �ð� ���� �Է��� �ٷ� �ݿ����� �ʵ���
           ���� �ð��� ���ؾ� �ϰ�, �� �ð� ������ �Է��� �����ؾ� ��.
           ���� �ð��� ���� ���� ���� �Է¿� ���ؼ��� �������� �������� ��.
           �׷��� �� ���� �ð� ���� ������ �Է��� ����/ȸ�� ���� ���ؼ���
           �������� �޾Ƽ� ó���ؾ� ��.

           serverTime�� clientTime�� �񱳵� �Ŀ��� �ٽ� ���µǰ�,
           �װ��� ���� ��ġ�ؾ� �Ѵٴ� �͵� �����ؼ� �׽�Ʈ����!
        */

        if (serverAction == ActionKind.Attack)
        {
            // ����-�����̰ų� ����-ȸ���� ���
            // 1P�� 2P���� ���� ��, ���� ����
            if (serverTime < clientTime)
            {
                return Winner.ServerPlayer;
            }
            else if (clientAction == ActionKind.Attack)
            {
                // ����-�����ε� 1P�� �ʾ��� ��, 2P�� ���� ����
                return Winner.ClientPlayer;
            }
            else
            {
                // ����-ȸ���ε� 1P�� �ʾ��� ��, 2P�� ȸ�� ����
                return Winner.Draw;
            }
        }
        else
        {
            // ȸ��-������ ���
            // 2P���� ������ ȸ�� ����
            if (serverTime > clientTime)
            {
                return Winner.ClientPlayer;
            }
            else
            {
                // ȸ��-�����ε� 2P�� �ʾ��� ��, 1P�� ȸ�� ����
                return Winner.Draw;
            }
        }

        // �ð��� ������ ��, ���º�
        // return Winner.Draw;
    }
}