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


    private async void OnReadyPressed() // ���� ��ư�� ������ �� ����
    {
        bool succeed = await GameLobbyManager.Instance.SetPlayerReady();
        if (succeed)
        {
            _readyButton.gameObject.SetActive(false); // ���� ��ư�� ������ �÷��̾ ���� ���·� �����, ��ư�� �������?
            // ���� ���� ��ư���ε� �ٲ㺼 �� ���� ��
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
