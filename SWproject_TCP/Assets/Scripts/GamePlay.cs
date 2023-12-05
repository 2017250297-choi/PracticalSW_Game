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

    public TextMeshProUGUI m_countdownText; // 카운트다운 표시하는 GUI 텍스트

    public GameObject m_actionSelect; // 액션 선택 관리하는 오브젝트

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
    bool m_isCountdown;
    //IEnumerator m_startCountdownCoroutine;


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
        m_isCountdown = false;

        // 초기화
        for (int i = 0; i < m_inputData.Length; ++i)
        {
            m_inputData[i].attackInfo.actionKind = ActionKind.None;
            m_inputData[i].attackInfo.damageValue = 0;
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

        //m_startCountdownCoroutine = CountdownCoroutine();
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
                //UpdateCountdown();
                if (!m_isCountdown)
                {
                    m_isCountdown = true;
                    StartCoroutine(CountdownCoroutine());
                }

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
        /*
        if (m_serverPlayer.GetComponent<Player>().IsIdleAnimation() == false)
        {
            return;
        }
        if (m_clientPlayer.GetComponent<Player>().IsIdleAnimation() == false)
        {
            return;
        }
        Player.cs에서 모션 다루는 거 수정하고 주석 해제하기
        */

        // 대기 통과 후 다음 상태(카운트다운)로.
        m_gameState = GameState.Countdown;
    }


    // 게임 시작 전 카운트다운 띄우는 코루틴
    private IEnumerator CountdownCoroutine()
    {
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 0f; // 플레이어 멈춤

        m_countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1.5f);
        m_countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1.5f);
        m_countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1.5f);
        m_countdownText.text = "Start!";
        Time.timeScale = 1f; // 플레이어 움직임 시작
        m_gameState = GameState.Action; // 얘 위치를 마지막으로 빼야 하는지?
        yield return new WaitForSecondsRealtime(1.5f);
        m_countdownText.text = "";

    }


    // 공격/회피 선택(실행)
    void UpdateAction()
    {
        m_isSendAction = false;
        m_isReceiveAction = false;

        ActionSelect actionSelect = m_actionSelect.GetComponent<ActionSelect>();
        if (actionSelect.IsSelected() && m_isSendAction == false)
        {
            // 입력키(마우스클릭)에 따른 액션 선택을 가져옴
            ActionKind action = actionSelect.GetActionKind();
            short damage = actionSelect.GetDamage();

            m_inputData[m_playerId].attackInfo.actionKind = action;
            m_inputData[m_playerId].attackInfo.damageValue = damage;

            // 상대방에게 전송
            m_networkController.SendActionData(action, damage);

            // 자신의 애니메이션을 공격/회피에 맞게 변형해주는 코드인데
            // 이건 꼭 이렇게 안 해도 되고... 적절히 수정해주시면 됩니다.
            // 아래처럼 하면, 클릭이 일어나고 전송까지 다 한 다음 플레이어의 공격 액션을 보여주는 것
            // 그러면 딜레이가 있을 수 있으니, 그냥 클릭 시 바로 액션을 취하도록 ActionSelec 혹은
            // Player 스크립트에서 액션을 취하도록 해주셔도 될 것 같습니다
            GameObject player = (m_playerId == 0) ? m_serverPlayer : m_clientPlayer;
            player.GetComponent<Player>().ChangeAnimationAction(action);

            m_isSendAction = true; // 송신 성공
            // 또 수시로 액션을 보내야 하니 이건 다시 false로 바꿔주어야 함! 코드 추가하기
            if(action == ActionKind.Attack)
            {
                if (m_playerId == 0)
                    m_serverPlayer.GetComponent<Player>().Attack();
                else
                    m_clientPlayer.GetComponent<Player>().Attack();
            }
        }

        // 상대방의 액션 수신 대기
        if (m_isReceiveAction == false)
        {
            // 수신 체크: 상대의 공격/방어를 체크
            bool isReceived = m_networkController.ReceiveActionData(
                ref m_inputData[m_playerId ^ 1].attackInfo.actionKind,
                ref m_inputData[m_playerId ^ 1].attackInfo.damageValue);

            if (isReceived)
            {
                // 애니메이션(대전 상대)
                ActionKind action = m_inputData[m_playerId ^ 1].attackInfo.actionKind;
                GameObject player = (m_playerId == 1) ? m_serverPlayer : m_clientPlayer;
                // 여기도 마찬가지로 수정 자유롭게...
                player.GetComponent<Player>().ChangeAnimationAction(action);

                m_isReceiveAction = true; // 수신 성공
                // 여기도 나중에 false로 다시 고쳐주는 코드가 필요
                if (action == ActionKind.Attack)
                {
                    if (m_playerId == 1)
                        m_serverPlayer.GetComponent<Player>().Attack();
                    else
                        m_clientPlayer.GetComponent<Player>().Attack();
                }
            }
            else
            {
                // 상대방 입력이 없는 상태
                m_inputData[m_playerId ^ 1].attackInfo.actionKind = ActionKind.None;
                m_inputData[m_playerId ^ 1].attackInfo.damageValue = 0;
                m_isReceiveAction = true; // 수신 성공으로 침
            }
        }

        Debug.Log("Own Action:" + m_inputData[m_playerId].attackInfo.actionKind.ToString() +
                      ",  Damage:" + m_inputData[m_playerId].attackInfo.damageValue);
        Debug.Log("Opponent Action:" + m_inputData[m_playerId ^ 1].attackInfo.actionKind.ToString() +
                  ",  Damage:" + m_inputData[m_playerId ^ 1].attackInfo.damageValue);
    }

    // 상대방의 공격/회피 통신 대기
    /*
    void UpdateWaitAction()
    {
        // 수신대기
    }
    */


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
        // 종료 버튼 표시
        GameObject obj = GameObject.Find("FinalResult"); // 이거 그냥 public으로 받자
        if (obj == null) { return; }

        Rect r = new Rect(Screen.width / 2 - 50, Screen.height - 60, 100, 50);
        if (GUI.Button(r, "RESET"))
        {
            // 씬 변환 코드 아래에 추가
        }
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
        GUISkin skin = GUI.skin;
        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
        style.normal.textColor = Color.white;
        style.fontSize = 25;

        float sx = 450;
        float sy = 200;
        float px = Screen.width / 2 - sx * 0.5f;
        float py = Screen.height / 2 - sy * 0.5f;

        string message = "연결이 끊어졌습니다.";

        if (GUI.Button(new Rect(px, py, sx, sy), message, style))
        {
            // 게임 종료
            m_isGameOver = true;
            // 씬 변환하는 코드 아래에 추가
        }
    }
}
