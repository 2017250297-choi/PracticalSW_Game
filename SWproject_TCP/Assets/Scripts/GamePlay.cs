using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using TMPro;

public class GamePlay : MonoBehaviour
{
    public GameObject m_serverPlayerPrefab; // ���� �� �÷��̾� ĳ����
    public GameObject m_clientPlayerPrefab; // Ŭ���̾�Ʈ �� �÷��̾� ĳ����

    public GameObject m_CountdownObject; // ī��Ʈ�ٿ� ǥ�� �̹��� ����

    public GameObject m_damageTextPrefab; // ������ ǥ��

    public GameObject m_finalResultWinPrefab; // ���� ��� �¸�
    public GameObject m_finalResultLosePrefab; // ���� ��� �й�


    const int PLAYER_NUM = 2;
    GameObject m_serverPlayer; // ���� ����ϹǷ� Ȯ��
    GameObject m_clientPlayer; // ���� ����ϹǷ� Ȯ��

    GameState m_gameState = GameState.None;
    InputData[] m_inputData = new InputData[PLAYER_NUM];
    NetworkController m_networkController = null;
    string m_serverAddress;

    int m_playerId = 0;
    int[] m_hp = new int[PLAYER_NUM]; // ������ ü��
    Winner m_actionWinner = Winner.None;
    bool m_isGameOver = false;

    // ����/ȸ�� �ۼ��� ����
    float m_timer;
    bool m_isSendAction;
    bool m_isReceiveAction;

    // ī��Ʈ�ٿ��
    private float startTime;


    // ���� ���� ��Ȳ
    enum GameState
    {
        None = 0,
        Ready, // ���� ����� �α��� ���
        Countdown, // ī��Ʈ�ٿ� ����
        Action, // (���� ����) ����/ȸ�� ����, ���� ���
        EndAction, // ����/ȸ�� ����
        Result, // ��� ��ǥ
        EndGame, // ��
        Disconnect, // ���� (���� �� ��)
    }


    // Start is called before the first frame update (Initialize��)
    void Start()
    {
        m_serverPlayer = null;
        m_clientPlayer = null;

        m_timer = 0;
        m_isSendAction = false;
        m_isReceiveAction = false;

        // �ʱ�ȭ
        for (int i = 0; i < m_inputData.Length; ++i)
        {
            m_inputData[i].attackInfo.actionKind = ActionKind.None;
            m_inputData[i].attackInfo.actionTime = 0.0f;
        }

        // ���� ���۽�Ű�� ����
        m_gameState = GameState.None;

        for (int i = 0; i < m_hp.Length; ++i)
        {
            m_hp[i] = 100; // ü�¹� �켱 100���� ����
        }

        // ��� ��� �ۼ�
        GameObject go = new GameObject("Network");
        if (go != null)
        {
            TransportTCP transport = go.AddComponent<TransportTCP>();
            if (transport != null)
            {
                transport.RegiserEventHandler(EventCallback);
            }
            DontDestroyOnLoad(go);
        }

        // ȣ��Ʈ�� ��������
        string hostname = Dns.GetHostName();
        // ȣ��Ʈ���� IP�ּҸ� ������
        IPAddress[] adrList = Dns.GetHostAddresses(hostname);
        m_serverAddress = adrList[0].ToString();

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(m_gameState); // ���߿� ���ֱ�

        switch(m_gameState)
        {
            case GameState.None:
                break;
            case GameState.Ready:
                UpdateReady();
                break;
            case GameState.Countdown:
                UpdateCountdown();
                break;
        }

    }


    // �̺�Ʈ �߻� �� �ݹ� �Լ�
    public void EventCallback(NetEventState state)
    {
        switch (state.type)
        {
            case NetEventType.Disconnect: // ������ ������ �̺�Ʈ�� ������
                if (m_gameState < GameState.EndGame && m_isGameOver == false)
                {
                    m_gameState = GameState.Disconnect; // ���� ���¸� Diconnect�� ����
                }
                break;
        }
    }


    private void OnGUI()
    {
        switch (m_gameState)
        {
            case GameState.EndGame:
                OnGUIEndGame();
                break;

            case GameState.Disconnect:
                NotifyDisconnection();
                break;
        }

        float px = Screen.width * 0.5f - 100.0f;
        float py = Screen.height * 0.5f;

        // �������� ���� GUI ǥ��
        if (m_networkController == null)
        {
            if (GUI.Button(new Rect(px, py, 200, 30), "���� ��븦 ��ٸ��ϴ�."))
            {
                m_networkController = new NetworkController(); // ����
                m_playerId = 0;
                m_gameState = GameState.Ready;
                m_isGameOver = false;

                // ȣ��Ʈ �÷��̾� ����
                m_serverPlayer = Instantiate(m_serverPlayerPrefab) as GameObject;
                m_serverPlayer.name = m_serverPlayerPrefab.name;

                GameObject.Find("Title").SetActive(false); // Ÿ��Ʋ ǥ�� OFF
            }

            // Ŭ���̾�Ʈ�� �������� �� ������ ������ �ּ� �Է�
            Rect labelRect = new Rect(px, py + 80, 200, 30);
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.black;
            GUI.Label(labelRect, "���� IP �ּ�", style);
            m_serverAddress = GUI.TextField(new Rect(px, py + 95, 200, 30), m_serverAddress);

            if (GUI.Button(new Rect(px, py + 40, 200, 30), "���� ���� �����մϴ�"))
            {
                m_networkController = new NetworkController(m_serverAddress); // ����
                m_playerId = 1;
                m_gameState = GameState.Ready;
                m_isGameOver = false;

                // Ŭ���̾�Ʈ �÷��̾� ����
                m_clientPlayer = Instantiate(m_clientPlayerPrefab) as GameObject;
                m_clientPlayer.name = m_clientPlayerPrefab.name;

                GameObject.Find("Title").SetActive(false);

            }
        }
    }


    // �α��� ���
    void UpdateReady()
    {
        // ���� Ȯ��
        if (m_networkController.IsConnected() == false)
        {
            return; // ���� ���� �ƴϸ� ����
        }

        // �÷��̾� ĳ���Ͱ� ��������� �ʾ����� ������
        if (m_serverPlayer == null)
        {
            m_serverPlayer = Instantiate(m_serverPlayerPrefab) as GameObject;
            // Instantiate as GameObject ����: https://codingmania.tistory.com/248
            m_serverPlayer.name = m_serverPlayerPrefab.name;
        }
        if (m_clientPlayer == null)
        {
            m_clientPlayer = Instantiate(m_clientPlayerPrefab) as GameObject;
            m_clientPlayer.name = m_clientPlayerPrefab.name;
        }

        // ����� Idle�� �� ������ ���
        if (m_serverPlayer.GetComponent<Player>().IsIdleAnimation() == false)
        {
            return;
        }
        if (m_clientPlayer.GetComponent<Player>().IsIdleAnimation() == false)
        {
            return;
        }

        // ��� ��� �� ���� ����(ī��Ʈ�ٿ�)��.
        m_gameState = GameState.Countdown;
    }


    // ���� ���� �� ī��Ʈ�ٿ�
    void UpdateCountdown()
    {
        GameObject countObj = GameObject.Find("CountdownObject");
        if (countObj == null) 
        {
            // ����� ������Ʈ�� ���ٸ� ����. �ʱ� ������
            countObj = Instantiate(m_CountdownObject) as GameObject;
            countObj.name = "CountdownObject";
            Time.timeScale = 0f;
            StartCoroutine("StartCountdown", countObj); // ī��Ʈ�ٿ� �ڷ�ƾ ȣ��
            return;
        }
    }

    private IEnumerable StartCountdown(GameObject countObj)
    {
        countObj.GetComponent<TextMeshPro>().text = "3";
        startTime = Time.realtimeSinceStartup;
        yield return new WaitForSecondsRealtime(1.5f);
        countObj.GetComponent<TextMeshPro>().text = "2";
        yield return new WaitForSecondsRealtime(1.5f);
        countObj.GetComponent<TextMeshPro>().text = "1";
        yield return new WaitForSecondsRealtime(1.5f);
        countObj.GetComponent<TextMeshPro>().text = "Start!";
        yield return new WaitForSecondsRealtime(1.5f);
        countObj.SetActive(false);

        Time.timeScale = 1f; // ���� ����
        m_gameState = GameState.Action;
    }


    // ���� ���� �� ȭ��
    void OnGUIEndGame()
    {

    }


    // ���� ���� �˸�
    void NotifyDisconnection()
    {

    }
}
