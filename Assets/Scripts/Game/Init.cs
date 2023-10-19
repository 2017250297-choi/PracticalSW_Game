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

            if (UnityServices.State == ServicesInitializationState.Initialized) // 잘 작동한다면?
            {
                AuthenticationService.Instance.SignedIn += OnSignedIn; // 콘솔 창에 로그 띄워줌

                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // 로그인 등

                if (AuthenticationService.Instance.IsSignedIn)
                {
                    string username = PlayerPrefs.GetString(key: "Username");
                    if (username == "")
                    {
                        username = "Player";
                        PlayerPrefs.SetString("Username", username);
                    }

                    SceneManager.LoadSceneAsync("Start"); // 로그인 완료되면 Start 씬으로 이동
                }
            }
        }

        private void OnSignedIn()
        {
            Debug.Log(message: $"Player Id: {AuthenticationService.Instance.PlayerId}");
            Debug.Log(message: $"Token: {AuthenticationService.Instance.AccessToken}");
        } // 플레이어 아이디와 액세스 토큰 등을 로그로 띄움

        // Update is called once per frame
        void Update()
        {

        }
    }

}
