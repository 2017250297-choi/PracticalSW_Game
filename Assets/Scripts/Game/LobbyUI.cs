using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Game;
using Game.Events;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lobbyCodeText;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _startButton;

    private void OnEnable()
    {
        _readyButton.onClick.AddListener(OnReadyPressed);
        _startButton.onClick.AddListener(OnStartButtonClicked);

        if (GameLobbyManager.Instance.IsHost)
        {
            LobbyEvents.OnLobbyReady += OnLobbyReady;
        }

        //LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void OnDisable()
    {
        _readyButton.onClick.RemoveAllListeners();
        _startButton.onClick.RemoveAllListeners();

        LobbyEvents.OnLobbyReady -= OnLobbyReady;
        //LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
    }


    // Start is called before the first frame update
    void Start()
    {
        _lobbyCodeText.text = $"Lobby code: {GameLobbyManager.Instance.GetLobbyCode()}";

        if (!GameLobbyManager.Instance.IsHost)
        {

        }
    }


    private async void OnReadyPressed() // 레디 버튼을 눌렀을 때 실행
    {
        bool succeed = await GameLobbyManager.Instance.SetPlayerReady();
        if (succeed)
        {
            _readyButton.gameObject.SetActive(false); // 레디 버튼을 누르면 플레이어를 레디 상태로 만들고, 버튼이 사라지게?
            // 레디 해제 버튼으로도 바꿔볼 수 있을 듯
        }
    }


    private void OnLobbyReady()
    {
        _startButton.gameObject.SetActive(true);
    }

    /*
    private void OnLobbyUpdated()
    {

    }
    */

    private async void OnStartButtonClicked()
    {
        await GameLobbyManager.Instance.StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
