﻿using System.Collections;
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
    string m_serverAddress;

    int m_playerId = 0;
    Winner m_actionWinner = Winner.None;
    float enemyPos = 0.0f;
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

    // 캐릭터 사망
    bool isDead;

    // 자신이 승자인지 아닌지
    bool isWinner;

    // 결과 발표 코루틴용
    bool m_isResulted;

    // Retry 버튼 프리팹
    public Button m_retryButtonPrefab;
    public Button retryButton;
    public Button m_connectionLostButtonPrefab;
    public Button connectionLostButton;
    public Button m_returnButtonPrefab;
    public Button returnButton;
    public Button m_exitButtonPrefab;
    public Button exitButton;

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

        /*
        for (int i = 0; i < m_hp.Length; ++i)
        {
            m_hp[i] = 100; // 체력바 우선 100으로 통일
        }
        */

        // 통신 모듈 작성
        GameObject go = new GameObject("Network");
        if (go != null)
        {
            TransportTCP transport = go.AddComponent<TransportTCP>();
            if (transport != null)
            {
                transport.RegisterEventHandler(EventCallback);
            }
            //DontDestroyOnLoad(go);
        }

        // 호스트명 가져오기
        string hostname = Dns.GetHostName();
        // 호스트명에서 IP주소를 가져옴
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
                //UpdateCountdown();
                if (!m_isCountdown)
                {
                    m_isCountdown = true;

                    m_myPlayer = (m_playerId == 0) ? m_serverPlayer : m_clientPlayer;
                    m_opponentPlayer = (m_playerId == 1) ? m_serverPlayer : m_clientPlayer;
                    m_myPlayerScript = m_myPlayer.GetComponent<Player>();
                    m_opponentPlayerScript = m_opponentPlayer.GetComponent<Player>();

                    m_myPlayerScript.GetOpponentPlayer(m_playerId); // 내 플레이어 오브젝트의 스크립트에서 상대방의 플레이어 오브젝트에 접근하게 해줌
                    m_opponentPlayerScript.GetOpponentPlayer(m_playerId ^ 1);
                    StartCoroutine(CountdownCoroutine());
                }
                break;

            case GameState.Action:
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
            case GameState.Ready:
                OnGUIReady();
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
        if (returnButton == null)
        {
            OnGUIReady();
        }
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
        if (returnButton != null)
        {
            returnButton.enabled = false;
            Debug.Log("START NO RETURN");
            Destroy(returnButton.gameObject);
        }
        presentCount.sprite = countdown_Start;
        //m_countdownText.text = "Start!";
        Time.timeScale = 1f; // 플레이어 움직임 시작
        yield return new WaitForSecondsRealtime(1.5f);

        presentCount.enabled = false;
        //m_countdownText.text = "";
        m_gameState = GameState.Action;
    }


    // 공격/회피 선택(실행)
    void UpdateAction()
    {
        //Debug.Log("NEWFRAME");
        m_isSendAction = false;
        m_isReceiveAction = false;
        short send_validDamage = 0;

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

                //Debug.Log(validDamage);
                if (validDamage > 100)
                {
                    // validDamage가 100 이상의 값이라고 왔다면, 사망한 상태임을 알리는 것.
                    m_isGameOver = m_opponentPlayerScript.getHit(validDamage);
                    isWinner = true;
                    StartCoroutine(m_opponentPlayerScript.PlayerDied()); // 플레이어 사망 코루틴 실행
                    m_gameState = GameState.Result;
                    //m_isGameOver = true;
                }
                else if (validDamage > 0)
                {
                    // 내 공격이 유효타였다면, 상대방 오브젝트의 hp를 깎는다
                    m_isGameOver = m_opponentPlayerScript.getHit(validDamage);
                }
                if(validDamage == -1)
                    m_isGameOver = m_opponentPlayerScript.getHit(-1);
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

                if (damage>0&&state==State.Attacking) // 상대방 액션이 공격이면
                {
                    State myState = m_myPlayerScript.GetState(); // 내 상태를 가져옴
                    if (myState == State.Dodging) // 내가 회피 중이라면 공격을 무효 처리함
                    {
                        isDead = m_myPlayerScript.getHit(-1);
                        send_validDamage = -1;
                    }
                    else // 나의 피격
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
                //m_inputData[m_playerId ^ 1].attackInfo.playerState = State.None;
                m_inputData[m_playerId ^ 1].attackInfo.damageValue = 0;
                m_inputData[m_playerId ^ 1].attackInfo.validDamage = 0;
                m_isReceiveAction = true; // 수신 성공으로 침


            }
        }


        // 수신받은 정보 토대로 판정 후 송신
        if (m_isSendAction == false)
        {
            m_myPlayerScript.UpdateSelectAction();

            // 입력키(마우스클릭)에 따른 액션 선택을 가져옴
            ActionKind action = m_myPlayerScript.GetActionKind();
            State state = m_myPlayerScript.GetState(); // 이 부분은 달라지도록 할 수 있음. 마우스 클릭을 하지 않아도 스턴이나 공격/회피중 상태는 몇 초간 유지되어야 함. 코드 수정해주세요!
            short damage = m_myPlayerScript.GetDamage();

            // 이전에 보낸 패킷과 내용이 동일할 시, 패킷을 보내지 않는다
            if (m_inputData[m_playerId].attackInfo.actionKind == action
                && m_inputData[m_playerId].attackInfo.playerState == state
                && m_inputData[m_playerId].attackInfo.damageValue == damage
                && m_inputData[m_playerId].attackInfo.validDamage == send_validDamage)
            {
                m_isSendAction = true; // 송신 성공으로 친다
            }
            else
            {
                m_inputData[m_playerId].attackInfo.actionKind = action;
                m_inputData[m_playerId].attackInfo.playerState = state;
                m_inputData[m_playerId].attackInfo.damageValue = damage;
                m_inputData[m_playerId].attackInfo.validDamage = send_validDamage;

                // 상대방에게 전송
                m_networkController.SendActionData(action, state, damage, send_validDamage);
                if (damage > 0)
                    Debug.Log(damage);
                // 자신의 애니메이션을 공격/회피에 맞게 변형
                // 이 부분도 스턴 or 공격/회피중 상태라면 그 애니메이션을 유지하고, 아래 코드는 캔슬되어야 합니다.
                m_myPlayerScript.ChangeAnimationAction(action);

                m_isSendAction = true; // 송신 성공
            }

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
    }


    // 게임 종료 체크
    public bool IsGameOver()
    {
        return m_isGameOver;
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
    void OnGUIReady()
    {
        if (returnButton == null)
        {
            returnButton = Instantiate(m_returnButtonPrefab, GameObject.Find("Canvas").transform);
            returnButton.onClick.AddListener(CancelGame);
        }
    }

    // 게임 종료 시 화면
    void OnGUIEndGame()
    {
     
        
        if (retryButton == null)
        {
            retryButton = Instantiate(m_retryButtonPrefab, GameObject.Find("Canvas").transform);
            retryButton.onClick.AddListener(retryClick);
        }
        if (exitButton == null)
        {
            exitButton = Instantiate(m_exitButtonPrefab, GameObject.Find("Canvas").transform);
            Vector3 origin = exitButton.GetComponent<RectTransform>().position;
            origin.y -= 100;
            exitButton.GetComponent<RectTransform>().position = new Vector3(origin.x,origin.y,origin.z);

            exitButton.onClick.AddListener(Application.Quit);
        }
        m_networkController.CloseServer();
    }
    void CancelGame()
    {
        m_networkController.CloseServer();
        SceneManager.LoadScene(0);
    }
    void retryClick()
    {
        
        m_isGameOver = true;
        SceneManager.LoadScene(0);
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
        m_networkController.CloseServer();
        if(connectionLostButton == null)
        {
            connectionLostButton = Instantiate(m_connectionLostButtonPrefab, GameObject.Find("Canvas").transform);
            connectionLostButton.onClick.AddListener(retryClick);
        }
        if(exitButton == null)
        {
            exitButton = Instantiate(m_exitButtonPrefab, GameObject.Find("Canvas").transform);
            exitButton.onClick.AddListener(Application.Quit);
        }
    }
}
