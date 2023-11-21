using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using TMPro;
using System.Linq;

public class GamePlay : MonoBehaviour
{
    public GameObject m_serverPlayerPrefab; // 서버 측 플레이어 캐릭터
    public GameObject m_clientPlayerPrefab; // 클라이언트 측 플레이어 캐릭터

    public GameObject m_CountdownObject; // 카운트다운 표시 이미지 모음

    public GameObject m_damageTextPrefab; // 데미지 표시

    public GameObject m_finalResultWinPrefab; // 최종 결과 승리
    public GameObject m_finalResultLosePrefab; // 최종 결과 패배


    const int PLAYER_NUM = 2;
    GameObject m_serverPlayer; // 자주 사용하므로 확보
    GameObject m_clientPlayer; // 자주 사용하므로 확보

    GameState m_gameState = GameState.None;
    InputData[] m_inputData = new InputData[PLAYER_NUM];
    NetworkController m_networkController = null;
    string m_serverAddress;

    int m_playerId = 0;
    int[] m_hp = new int[PLAYER_NUM]; // 서로의 체력
    Winner m_actionWinner = Winner.None;
    bool m_isGameOver = false;

    // 공격/회피 송수신 대기용
    float m_timer;
    bool m_isSendAction;
    bool m_isReceiveAction;

    // 카운트다운용
    private float startTime;


    // 게임 진행 상황
    enum GameState
    {
        None = 0,
        Ready, // 게임 상대의 로그인 대기
        Countdown, // 카운트다운 시작
        Action, // (게임 시작) 공격/회피 선택, 수신 대기
        EndAction, // 공격/회피 연출
        Result, // 결과 발표
        EndGame, // 끝
        Disconnect, // 오류 (연결 안 됨)
    }


    // Start is called before the first frame update (Initialize용)
    void Start()
    {
        m_serverPlayer = null;
        m_clientPlayer = null;

        m_timer = 0;
        m_isSendAction = false;
        m_isReceiveAction = false;

        // 초기화
        for (int i = 0; i < m_inputData.Length; ++i)
        {
            m_inputData[i].attackInfo.actionKind = ActionKind.None;
            m_inputData[i].attackInfo.actionTime = 0.0f;
        }

        // 아직 동작시키지 않음
        m_gameState = GameState.None;

        for (int i = 0; i < m_hp.Length; ++i)
        {
            m_hp[i] = 100; // 체력바 우선 100으로 통일
        }

        // 통신 모듈 작성
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

        // 호스트명 가져오기
        string hostname = Dns.GetHostName();
        // 호스트명에서 IP주소를 가져옴
        //IPAddress[] adrList = Dns.GetHostAddresses(hostname);
        IPHostEntry host = Dns.GetHostEntry(hostname);
        //m_serverAddress = adrList[0].ToString();
        m_serverAddress = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();


    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(m_gameState); // 나중에 없애기

        switch (m_gameState)
        {
            case GameState.None:
                break;

            case GameState.Ready:
                UpdateReady();
                break;

            case GameState.Countdown:
                UpdateCountdown();
                break;

            case GameState.Action:
                UpdateAction();
                break;

            case GameState.Result:
                UpdateResult();
                break;

            case GameState.EndGame:
                UpdateEndGame();
                break;

            case GameState.Disconnect:
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

        // 미접속일 때의 GUI 표시
        if (m_networkController == null)
        {
            if (GUI.Button(new Rect(px, py, 200, 30), "대전 상대를 기다립니다."))
            {
                m_networkController = new NetworkController(); // 서버
                m_playerId = 0;
                m_gameState = GameState.Ready;
                m_isGameOver = false;

                // 호스트 플레이어 생성
                m_serverPlayer = Instantiate(m_serverPlayerPrefab) as GameObject;
                m_serverPlayer.name = m_serverPlayerPrefab.name;

                GameObject.Find("Title").SetActive(false); // 타이틀 표시 OFF

                Debug.Log("서버 생성, 접속");
            }

            // 클라이언트를 선택했을 때 접속할 서버의 주소 입력
            Rect labelRect = new Rect(px, py + 80, 200, 30);
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.black;
            GUI.Label(labelRect, "상대방 IP 주소", style);
            m_serverAddress = GUI.TextField(new Rect(px, py + 95, 200, 30), m_serverAddress);

            if (GUI.Button(new Rect(px, py + 40, 200, 30), "대전 상대와 접속합니다"))
            {
                m_networkController = new NetworkController(m_serverAddress); // 서버
                m_playerId = 1;
                m_gameState = GameState.Ready;
                m_isGameOver = false;

                // 클라이언트 플레이어 생성
                m_clientPlayer = Instantiate(m_clientPlayerPrefab) as GameObject;
                m_clientPlayer.name = m_clientPlayerPrefab.name;

                GameObject.Find("Title").SetActive(false);

                Debug.Log("클라이언트 접속");
            }
        }
    }


    // 로그인 대기
    void UpdateReady()
    {
        // 접속 확인
        if (m_networkController.IsConnected() == false)
        {
            return; // 접속 상태 아니면 종료
        }

        // 플레이어 캐릭터가 만들어지지 않았으면 생성함
        if (m_serverPlayer == null)
        {
            m_serverPlayer = Instantiate(m_serverPlayerPrefab) as GameObject;
            // Instantiate as GameObject 설명: https://codingmania.tistory.com/248
            m_serverPlayer.name = m_serverPlayerPrefab.name;
        }
        if (m_clientPlayer == null)
        {
            m_clientPlayer = Instantiate(m_clientPlayerPrefab) as GameObject;
            m_clientPlayer.name = m_clientPlayerPrefab.name;
        }

        // 모션이 Idle이 될 때까지 대기
        if (m_serverPlayer.GetComponent<Player>().IsIdleAnimation() == false)
        {
            return;
        }
        if (m_clientPlayer.GetComponent<Player>().IsIdleAnimation() == false)
        {
            return;
        }

        // 대기 통과 후 다음 상태(카운트다운)로.
        m_gameState = GameState.Countdown;
    }


    // 게임 시작 전 카운트다운
    void UpdateCountdown()
    {
        GameObject countObj = GameObject.Find("CountdownObject");
        if (countObj == null)
        {
            // 연출용 오브젝트가 없다면 생성. 초기 동작임
            countObj = Instantiate(m_CountdownObject) as GameObject;
            countObj.name = "CountdownObject";
            Time.timeScale = 0f;
            StartCoroutine("StartCountdown", countObj); // 카운트다운 코루틴 호출
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

        Time.timeScale = 1f; // 게임 시작
        m_gameState = GameState.Action;
    }


    // 공격/회피 선택(실행)
    void UpdateAction()
    {

    }

    // 상대방의 공격/회피 통신 대기
    void UpdateWaitAction()
    {
        // 수신대기
    }


    // 체력바 반영
    void UpdateHP()
    {

    }


    // 게임 종료
    void UpdateEndGame()
    {

    }


    // 게임 종료 시 화면
    void OnGUIEndGame()
    {

    }


    // 게임 종료 체크
    public bool IsGameOver()
    {
        return m_isGameOver;
    }


    // 게임 결과 표시
    void UpdateResult()
    {

    }


    // 이벤트 발생 시 콜백 함수
    public void EventCallback(NetEventState state)
    {
        switch (state.type)
        {
            case NetEventType.Disconnect: // 연결이 끊어진 이벤트가 들어오면
                if (m_gameState < GameState.EndGame && m_isGameOver == false)
                {
                    m_gameState = GameState.Disconnect; // 게임 상태를 Diconnect로 변경
                }
                break;
        }
    }


    // 연결 끊김 알림
    void NotifyDisconnection()
    {

    }
}
