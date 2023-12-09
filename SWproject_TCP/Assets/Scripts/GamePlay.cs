using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GamePlay : MonoBehaviour
{
    public GameObject m_serverPlayerPrefab; // 서버 측 플레이어 캐릭터
    public GameObject m_clientPlayerPrefab; // 클라이언트 측 플레이어 캐릭터

    //public TextMeshProUGUI m_countdownText; // 카운트다운 표시하는 GUI 텍스트

    public GameObject m_actionSelect; // 액션 선택 관리하는 오브젝트

    public GameObject m_damageTextPrefab; // 데미지 표시

    // 결과 이미지
    public GameObject m_finalResultObject;
    public Sprite m_winImage;
    public Sprite m_loseImage;


    const int PLAYER_NUM = 2;
    GameObject m_serverPlayer; // 자주 사용하므로 확보
    GameObject m_clientPlayer; // 자주 사용하므로 확보
    GameObject m_myPlayer; // 내 플레이어 오브젝트
    GameObject m_opponentPlayer; // 상대방 플레이어 오브젝트
    Player m_myPlayerScript; // 내 플레이어의 스크립트
    Player m_opponentPlayerScript; // 상대방 플레이어의 스크립트

    GameState m_gameState = GameState.None;
    InputData[] m_inputData = new InputData[PLAYER_NUM];
    NetworkController m_networkController = null;
    ActionSelect actionSelect = null;
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
    public GameObject countdownObject;
    public Sprite countdown_3;
    public Sprite countdown_2;
    public Sprite countdown_1;
    public Sprite countdown_Start;
    //IEnumerator m_startCountdownCoroutine;

    // 캐릭터 사망
    bool isDead;

    // 자신이 승자인지 아닌지
    bool isWinner;

    // 결과 발표 코루틴용
    bool m_isResulted;


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
        m_isResulted = false;

        isDead = false;

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
                transport.RegisterEventHandler(EventCallback);
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

        actionSelect = m_actionSelect.GetComponent<ActionSelect>();
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
                m_myPlayer = (m_playerId == 0) ? m_serverPlayer : m_clientPlayer;
                m_opponentPlayer = (m_playerId == 1) ? m_serverPlayer : m_clientPlayer;
                m_myPlayerScript = m_myPlayer.GetComponent<Player>();
                m_opponentPlayerScript = m_opponentPlayer.GetComponent<Player>();
                UpdateAction();
                break;

            case GameState.Result:
                //UpdateResult();
                if (!m_isResulted)
                {
                    m_isResulted = true;
                    StartCoroutine(DisPlayResult());
                }

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
            style.normal.textColor = Color.white;
            GUI.Label(labelRect, "상대방 IP 주소", style);
            m_serverAddress = GUI.TextField(new Rect(px, py + 95, 200, 30), m_serverAddress);

            if (GUI.Button(new Rect(px, py + 40, 200, 30), "대전 상대와 접속합니다"))
            {
                Debug.Log("입력한 서버 IP: " + m_serverAddress);
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

        // 다음 상태(카운트다운)로.
        m_gameState = GameState.Countdown;
    }


    // 게임 시작 전 카운트다운 띄우는 코루틴
    private IEnumerator CountdownCoroutine()
    {
        Image presentCount = countdownObject.GetComponent<Image>();
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 0f; // 플레이어 멈춤

        presentCount.enabled = true;
        presentCount.sprite = countdown_3;
        //m_countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1.5f);

        presentCount.sprite = countdown_2;
        //m_countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1.5f);

        presentCount.sprite = countdown_1;
        //m_countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1.5f);

        presentCount.sprite = countdown_Start;
        //m_countdownText.text = "Start!";
        Time.timeScale = 1f; // 플레이어 움직임 시작
        m_gameState = GameState.Action; // 얘 위치를 마지막으로 빼야 하는지?
        yield return new WaitForSecondsRealtime(1.5f);

        presentCount.enabled = false;
        //m_countdownText.text = "";
    }


    // 공격/회피 선택(실행)
    void UpdateAction()
    {
        m_isSendAction = false;
        m_isReceiveAction = false;
        short send_validDamage = 0;
        

        //ActionSelect actionSelect = m_actionSelect.GetComponent<ActionSelect>();


        // 상대방의 액션 수신부터 함
        if (m_isReceiveAction == false)
        {
            // 수신 체크: 상대의 공격/방어를 체크
            bool isReceived = m_networkController.ReceiveActionData(
                ref m_inputData[m_playerId ^ 1].attackInfo.actionKind,
                ref m_inputData[m_playerId ^ 1].attackInfo.playerState,
                ref m_inputData[m_playerId ^ 1].attackInfo.damageValue,
                ref m_inputData[m_playerId ^ 1].attackInfo.validDamage);

            if (isReceived)
            {
                // 상대방이 보낸 공격 종류와 데미지
                ActionKind action = m_inputData[m_playerId ^ 1].attackInfo.actionKind;
                State state = m_inputData[m_playerId ^ 1].attackInfo.playerState;
                short damage = m_inputData[m_playerId ^ 1].attackInfo.damageValue;
                short validDamage = m_inputData[m_playerId ^ 1].attackInfo.validDamage;

                if (validDamage > 100)
                {
                    // validDamage가 100 이상의 값이라고 왔다면, 사망한 상태임을 알리는 것.
                    m_opponentPlayerScript.getHit(validDamage);
                    isWinner = true;
                    StartCoroutine(m_opponentPlayerScript.PlayerDied()); // 플레이어 사망 코루틴 실행
                    m_gameState = GameState.Result;
                    m_isGameOver = true;
                }
                else if (validDamage > 0)
                {
                    // 내 공격이 유효타였다면, 상대방 오브젝트의 hp를 깎는다
                    m_opponentPlayerScript.getHit(validDamage);
                }

                // 여기도 마찬가지로 수정 자유롭게...
                // 상대방의 action이 ActionKind.None이어도, state가 None이 아니라면 그것도 모션으로 반영해주어야 함.
                // Player.cs에서 state를 인풋으로 해서 애니메이션을 바꾸어주는 ChangeAnimationState(state)를 만들어주고, 여기에 실행하자...
                m_opponentPlayerScript.ChangeAnimationAction(action); // 상대방 플레이어 오브젝트 액션 모션을 반영

                m_isReceiveAction = true; // 수신 성공

                // 상대방의 액션이 Attack일 경우 데미지를 체력바에 반영하도록 함.
                // 실제로는 공격/회피 성공 판정을 해서 반영되어야 함.
                // A가 공격하고, B가 회피해서 이것이 성공했다면, B는 회피 성공을 알리는 패킷을 A에게 전송해야
                // A도 자신의 공격 데미지를 B의 체력바에 반영하지 않을 수 있음.
                // -> AttackInfo(전송하는 정보 구조체)에 자신이 깎인 데미지 값(damageValue와 별개로, validDamage 변수를 추가했습니다)을 추가해
                // A가 10의 데미지로 공격 -> B가 그것을 받아 공격 성공/실패를 판정 -> 공격 성공 시 validDamage=10으로 상대방에게 전송,
                // 공격 실패 시 validDamage=0으로 전송 -> A는 패킷을 받아 상대방 체력바에서 10을 깎음
                // 이렇게 구현하면 어떨까 합니다
                // 즉, Attack을 받은 쪽에서 공격 성공/실패를 판정하자! (나중에 서버가 모두 판정하는 식으로 바꿀 수도 있을 것 같음)
                if (action == ActionKind.Attack) // 상대방 액션이 공격이면
                {
                    State myState = actionSelect.GetState(); // 내 상태를 가져옴
                    if (myState == State.Dodging) // 내가 회피 중이라면 공격을 무효 처리함
                    {
                        send_validDamage = 0;
                    }
                    else
                    {
                        isDead = m_myPlayerScript.getHit(damage);
                        send_validDamage = damage; // damage 값만큼의 유효타가 들어갔음을 전송해서 알림

                        if (isDead) // 내가 사망하면
                        {
                            send_validDamage = 101; // send_validDamage가 100보다 크면, 사망한 상태라고 알리는 것.
                            isWinner = false;
                            StartCoroutine(m_myPlayerScript.PlayerDied()); // 플레이어 사망 코루틴 실행
                            m_gameState = GameState.Result; // 게임을 끝내기 전 승패를 발표하는 단계로 넘어감.
                            m_isGameOver = true;
                        }

                    }
                    
                }

            }
            else
            {
                // 상대방 입력이 없는 상태
                m_inputData[m_playerId ^ 1].attackInfo.actionKind = ActionKind.None;
                m_inputData[m_playerId ^ 1].attackInfo.playerState = State.None;
                m_inputData[m_playerId ^ 1].attackInfo.damageValue = 0;
                m_inputData[m_playerId ^ 1].attackInfo.validDamage = 0;
                m_isReceiveAction = true; // 수신 성공으로 침
            }
        }


        // 수신받은 정보 토대로 판정 후 송신
        if (m_isSendAction == false)
        {
            actionSelect.UpdateSelectAction();

            // 입력키(마우스클릭)에 따른 액션 선택을 가져옴
            ActionKind action = actionSelect.GetActionKind();
            State state = actionSelect.GetState(); // 이 부분은 달라지도록 할 수 있음. 마우스 클릭을 하지 않아도 스턴이나 공격/회피중 상태는 몇 초간 유지되어야 함. 코드 수정해주세요!
            short damage = actionSelect.GetDamage();

            // 스턴 상태거나, 공격/회피중인 상태라면 action과 damage가 캔슬되어야 합니다.
            // 즉 inputData에 정보값을 담기 전, action = ActionKind.None, damage = 0으로 설정 후 값을 담아야 합니다. 코드 수정해주세요!
            m_inputData[m_playerId].attackInfo.actionKind = action;
            m_inputData[m_playerId].attackInfo.playerState = state;
            m_inputData[m_playerId].attackInfo.damageValue = damage;

            // 상대방에게 전송
            m_networkController.SendActionData(action, state, damage, send_validDamage);

            // 자신의 애니메이션을 공격/회피에 맞게 변형
            // 이 부분도 스턴 or 공격/회피중 상태라면 그 애니메이션을 유지하고, 아래 코드는 캔슬되어야 합니다.
            m_myPlayerScript.ChangeAnimationAction(action);

            m_isSendAction = true; // 송신 성공
        }

        

        // 공격/회피 시에만 로그 찍도록
        if (m_inputData[m_playerId].attackInfo.actionKind == ActionKind.Attack ||
            m_inputData[m_playerId].attackInfo.actionKind == ActionKind.Dodge)
        {
            Debug.Log("Own Action:" + m_inputData[m_playerId].attackInfo.actionKind.ToString() +
                      ",  Damage:" + m_inputData[m_playerId].attackInfo.damageValue);
        }

        if (m_inputData[m_playerId ^ 1].attackInfo.actionKind == ActionKind.Attack ||
            m_inputData[m_playerId ^ 1].attackInfo.actionKind == ActionKind.Dodge)
        {
            Debug.Log("Opponent Action:" + m_inputData[m_playerId ^ 1].attackInfo.actionKind.ToString() +
                      ",  Damage:" + m_inputData[m_playerId ^ 1].attackInfo.damageValue);
        }
        

        /*
        Debug.Log("Own Action:" + m_inputData[m_playerId].attackInfo.actionKind.ToString() +
          ",  Damage:" + m_inputData[m_playerId].attackInfo.damageValue);
        Debug.Log("Opponent Action:" + m_inputData[m_playerId ^ 1].attackInfo.actionKind.ToString() +
          ",  Damage:" + m_inputData[m_playerId ^ 1].attackInfo.damageValue);
        */
    }

    // 상대방의 공격/회피 통신 대기
    /*
    void UpdateWaitAction()
    {
        // 수신대기
    }
    */


    // 게임 종료 체크
    public bool IsGameOver()
    {
        return m_isGameOver;
    }


    // 게임 결과 표시
    // 이건 코루틴으로?
    void UpdateResult()
    {

    }

    IEnumerator DisPlayResult()
    {
        Image result = countdownObject.GetComponent<Image>();

        if (isWinner)
        {
            result.enabled = true;
            result.sprite = m_winImage;
        }
        else
        {
            result.enabled = true;
            result.sprite = m_loseImage;
        }

        yield return new WaitForSecondsRealtime(1f);

        m_gameState = GameState.EndGame;
    }


    // 게임 종료
    void UpdateEndGame()
    {
        OnGUIEndGame();
    }


    // 게임 종료 시 화면
    void OnGUIEndGame()
    {
        // 종료 버튼 표시
        //GameObject obj = GameObject.Find("FinalResult");
        //if (obj == null) { return; }

        Rect r = new Rect(Screen.width / 2 - 50, Screen.height - 60, 100, 50);
        if (GUI.Button(r, "Retry"))
        {
            // 씬 변환 코드 아래에 추가
            SceneManager.LoadScene(0);

            // 현재 LoadScene만으로는 재시작을 완벽하게 구현할 수 없다.
            // 이렇게 테스트해보니, 상대방이 아직 접속하지 않았는데 상대방 플레이어 오브젝트가 생기기도 하고,
            // 네트워크에서 싱크로가 맞지 않고 차이나게 된다.
            // (LoadScene을 하면 데이터가 모두 날아가는 것은 맞다.)
            // 아마 Awake()를 사용하든가,
            // 생성한 오브젝트를 Destroy하는 코드를 추가한 후 LoadScene을 마지막에 해야 할 것 같다. Initiate의 문제일 수 있다는 듯.
            // 다른 스크립트에서 Start()를 사용하는지도 찾아보자.
        }
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

        if (m_playerId == 0)
        {
            m_networkController.CloseServer();
        }

        if (GUI.Button(new Rect(px, py, sx, sy), message, style))
        {
            // 게임 종료
            m_isGameOver = true;
            // 씬 변환하는 코드 아래에 추가
            SceneManager.LoadScene(0);
        }
    }
}
