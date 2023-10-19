using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{

    public class Init : MonoBehaviour
    {
        // Start is called before the first frame update
        async void Start()
        {
            await UnityServices.InitializeAsync();

            if (UnityServices.State == ServicesInitializationState.Initialized) // �� �۵��Ѵٸ�?
            {
                AuthenticationService.Instance.SignedIn += OnSignedIn; // �ܼ� â�� �α� �����

                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // �α��� ��

                if (AuthenticationService.Instance.IsSignedIn)
                {
                    string username = PlayerPrefs.GetString(key: "Username");
                    if (username == "")
                    {
                        username = "Player";
                        PlayerPrefs.SetString("Username", username);
                    }

                    SceneManager.LoadSceneAsync("Start"); // �α��� �Ϸ�Ǹ� Start ������ �̵�
                }
            }
        }

        private void OnSignedIn()
        {
            Debug.Log(message: $"Player Id: {AuthenticationService.Instance.PlayerId}");
            Debug.Log(message: $"Token: {AuthenticationService.Instance.AccessToken}");
        } // �÷��̾� ���̵�� �׼��� ��ū ���� �α׷� ���

        // Update is called once per frame
        void Update()
        {

        }
    }

}
