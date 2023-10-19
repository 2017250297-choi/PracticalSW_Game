using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private GameObject _mainScreen;
    [SerializeField] private GameObject _joinScreen;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;

    [SerializeField] private Button _submitCodeButton;
    [SerializeField] private TextMeshProUGUI _codeText;


    void OnEnable()
    {
        _hostButton.onClick.AddListener(OnHostClicked);
        _joinButton.onClick.AddListener(OnJoinClicked);
        _submitCodeButton.onClick.AddListener(OnSubmitCodeButton);

        //_joinScreen.SetActive(false);
    }

    void OnDisable()
    {
        _hostButton.onClick.RemoveListener(OnHostClicked);
        _joinButton.onClick.RemoveListener(OnJoinClicked);
        _submitCodeButton.onClick.RemoveListener(OnSubmitCodeButton);

        //_joinScreen.SetActive(false);
    }

    private async void OnHostClicked()
    {
        //Debug.Log(message: "Host");

        bool succeeded = await GameLobbyManager.Instance.CreateLobby();
        if (succeeded)
        {
            SceneManager.LoadSceneAsync("Lobby");
        }
    }

    private void OnJoinClicked()
    {
        //Debug.Log(message: "Join");

        _mainScreen.SetActive(false);
        _joinScreen.SetActive(true);
    }

    private async void OnSubmitCodeButton()
    {
        string code = _codeText.text;
        code = code.Substring(0, code.Length - 1); // 입력한 코드의 마지막 글자(공백?) 제거

        bool succeeded = await GameLobbyManager.Instance.JoinLobby(code);
        if (succeeded)
        {
            SceneManager.LoadSceneAsync("Lobby");
        }
        //Debug.Log(code);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
