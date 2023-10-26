using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.Core.Data;
using GameFramework.Core.GameFramework.Manager;
using GameFramework.Events;
//using Mono.Cecil.Cil;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;

namespace Game // Game 폴더 내의 Init.cs, LobbyUI.cs 등의 스크립트 파일들을 Game 네임스페이스 안에 모음. 
    // 네임스페이스를 만드는 이유: 이름이 같은 파일이나 클래스명 등을 만들기 위해... ex) LobbyEvents.cs
    // 즉 네임스페이스가 다르면 이름이 중복되어도 ok. 참조할 네임스페이스를 적어주면 된다.
{
    public class GameLobbyManager : Singleton<GameLobbyManager>
    {

        private List<LobbyPlayerData> _lobbyPlayerDatas = new List<LobbyPlayerData>(); // 로비에 참여한 모든 플레이어의 데이터가 저장되는 곳
        private LobbyPlayerData _localLobbyPlayerData; // 로컬 플레이어 (게임하는 본인)의 데이터가 저장되는 곳
        private LobbyData _lobbyData; // 릴레이 조인 코드 등이 저장됨. 맵 선택 기능 추가 시 맵 정보도 여기에 들어감
        private int _maxNumberOfPlayers = 2; // 수정 안 하고 싶으니까 private으로
        private bool _inGame = false; // 로비에서 릴레이 서버로 이동하고 게임이 시작되면 로비는 멈춰주어야 함. (로비가 계속 lobbyUpdate를 리슨 중인데, 그걸 멈추어야) 그걸 위한 값

        public bool IsHost => _localLobbyPlayerData.Id == LobbyManager.Instance.GetHostId();


        private void OnEnable()
        {
            LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
            // + 또는 -로 하나의 delegate 함수 LobbyEvents.OnLobbyUpdated에 여러 개의 함수를 등록해 delegate chain 형태로 사용하는 것.
            // LobbyEvents.OnLobbyUpdated를 한 번 호출하면 등록된 여러 개의 OnLobbyUpdated가 한 번에 실행된다.
        }


        private void OnDisable()
        {
            LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
            // 여긴 등록된 함수를 제거하는 것
            // 이렇게 delegate를 이용해 Observer 패턴으로 이벤트를 처리한다. (참고: https://huiyu.tistory.com/entry/C-%EA%B8%B0%EC%B4%88-%EC%9D%B4%EB%B2%A4%ED%8A%B8%EC%99%80-%EB%8D%B8%EB%A6%AC%EA%B2%8C%EC%9D%B4%ED%8A%B8-Event-Delegate)
        }


        public string GetLobbyCode()
        {
            return LobbyManager.Instance.GetLobbyCode();
        }

        public async Task<bool> CreateLobby()
        {
            /*
            Dictionary<string, string> playerData = new Dictionary<string, string>()
            {
                { "GamerTag", "HostPlayer" }
            };
            */ // LobbyPlayerData.cs 를 만들기 전에 사용했던 코드

            /*
            LobbyPlayerData playerData = new LobbyPlayerData(); // LobbyPlayerData.cs에서 정의한 클래스
            playerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "HostPlayer");

            bool succeeded = await LobbyManager.Instance.CreateLobby(maxPlayers: 2, isPrivate: true, playerData.Serialize()); //Serialize를 적용해 Dictionary 형태로 넘겨주어야.
            */

            // 위의 코드에서 playerData -> _localLobbyPlayerData 로 대체
            _localLobbyPlayerData = new LobbyPlayerData(); // LobbyPlayerData.cs에서 정의한 클래스
            _localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "HostPlayer");

            _lobbyData = new LobbyData();

            bool succeeded = await LobbyManager.Instance.CreateLobby(maxPlayers: _maxNumberOfPlayers, isPrivate: true, _localLobbyPlayerData.Serialize(), _lobbyData.Serialize()); //Serialize를 적용해 Dictionary 형태로 넘겨주어야.

            return succeeded;
        }


        public async Task<bool> JoinLobby(string code) // 로비 관련 함수는 다 async여야
        {
            /*
            Dictionary<string, string> playerData = new Dictionary<string, string>()
            {
                { "GamerTag", "JoinPlayer" }
            };
            */ // LobbyPlayerData.cs 를 만들기 전에 사용했던 코드

            /*
            LobbyPlayerData playerData = new LobbyPlayerData(); // LobbyPlayerData.cs에서 정의한 클래스
            playerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "JoinPlayer");

            bool succeeded = await LobbyManager.Instance.JoinLobby(code, playerData.Serialize());
            */


            // 위의 코드에서 playerData -> _localLobbyPlayerData 로 대체
            _localLobbyPlayerData = new LobbyPlayerData(); // LobbyPlayerData.cs에서 정의한 클래스
            _localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "JoinPlayer");

            bool succeeded = await LobbyManager.Instance.JoinLobby(code, _localLobbyPlayerData.Serialize());

            return succeeded;
        }


        // LobbyManager가 업데이트된 플레이어 데이터를 전달해주면, _lobbyPlayerDatas 리스트를 repopulate하는 메소드
        // 새로운 플레이어 데이터가 업데이트될 때마다 이를 구독한 게임이 그 데이터를 받아오도록 하는 메소드임
        private async void OnLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.Instance.GetPlayersData();
            // LobbyPlayerData.cs의 UpdateState의 인자가 <Dictionary<string, PlayerDataObject> 형식이었기 때문
            _lobbyPlayerDatas.Clear();
            // 업데이트된 정보를 받아 저장하므로, 기존의 데이터를 clear

            // 레디한 플레이어 수를 체크해, 모든 인원이 레디 상태인지 확인
            int numberOfPlayerReady = 0;

            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new LobbyPlayerData();
                lobbyPlayerData.Initialize(data);

                if (lobbyPlayerData.IsReady)
                {
                    numberOfPlayerReady++;
                }


                if (lobbyPlayerData.Id == AuthenticationService.Instance.PlayerId) // Unity Lobby에게 받아온 업데이트된 플레이어 데이터가 로컬 플레이어의 것이라면
                {
                    _localLobbyPlayerData = lobbyPlayerData; // 받아온 업데이트된 데이터를 _localLobbyPlayerData에 저장
                }

                _lobbyPlayerDatas.Add(lobbyPlayerData); // 받아온 업데이트된 플레이어 데이터를 리스트에 추가
            }


            _lobbyData = new LobbyData(); // LobbyData 생성하고
            _lobbyData.Initialize(lobby.Data); // LobbyData를 새로 가져온 lobby.Data 로 리셋한다. 로비에 변화가 있을 때마다 계속 리셋해줌.
            // 그 다음 OnLobbyUpdated 대리자를 실행해 변화된 로비 정보가 실행해야 하는 이벤트를 실행함


            // 이제 데이터를 받아와 내 structure로 만들었으니, event에 데이터를 넘겨줄 수 있다
            Events.LobbyEvents.OnLobbyUpdated?.Invoke(); // Lobby로부터 리슨함 -> 로비에 플레이어 스폰 가능
            // Game 안에 Events가 있으므로 Events.LobbyEvents라 적음. LobbyEvents는 Game/Events/LobbyEvents.cs임 (GameFramework/Events/LobbyEvents.cs가 아님)
            // Invoke는 Invoke(Delegate)로 쓰인다. 대리자를 호출. (참고: https://cartiertk.tistory.com/67)

            // 이제 LobbyManager.cs의 RefreshLobbyCoroutine 메소드에 OnLobbyUpdated를 써서 이벤트를 throw한다.

            if (numberOfPlayerReady == lobby.Players.Count) 
            {
                Events.LobbyEvents.OnLobbyReady?.Invoke(); // 모든 플레이어가 레디 상태면, OnLobbyReady 실행
                // 호스트 혼자 있을 때 실행되지 않도록 코드 수정 필요!!!
            }

            if (_lobbyData.RelayJoinCode != default && !_inGame) // lobbyData에서 릴레이 조인 코드가 initialize되어 비어있지 않다면, 그리고 게임 실행 중이 아니라면
            {
                // !_inGame 조건을 주어야 게임 실행 중에 계속 이 코드가 실행되어 씬으로 이동하려는 것을 막을 수 있음. 한 번 이동하면 끝내주어야...

                await JoinRelayServer(_lobbyData.RelayJoinCode); // 이 조인 코드를 이용해 릴레이 서버에 참여시켜줌
                SceneManager.LoadSceneAsync("MultiPlayScene"); // 그 다음 플레이 씬으로 이동
                // 이 if문 안은 게스트 측을 위한 것. 
                // 호스트가 START 버튼을 눌러 GameStart() 메소드를 실행하면, 릴레이 생성 -> 조인 코드 생성 -> 조인 코드 포함해 로비데이터 업뎃 을 해주는데,
                // 게스트는 이 OnLobbyUpdate에서 새로 로비 데이터를 받아 업데이트하고, 조인 코드가 생기면 요 if문으로 인해 플레이 씬으로 이동한다. 게임 시작.
            }

        }

        public List<LobbyPlayerData> GetPlayers()
        {
            return _lobbyPlayerDatas;
        }

        public async Task<bool> SetPlayerReady() // 레디 버튼을 누르면 실행되는 메소드
        {
            _localLobbyPlayerData.IsReady = true;
            return await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize());
        }

        public async Task StartGame() // 스타트 버튼을 누르면 게임을 시작하는 메소드. 즉 호스트 측에서 실행됨
        {
            string relayJoinCode = await RelayManager.Instance.CreateRelay(_maxNumberOfPlayers); // 릴레이 생성 시 최대 플레이어 수 넘겨주어야 함.
            // CreateRelay는 실행 후 조인 코드를 리턴. 그것을 joinRelayCode에 저장
            _inGame = true;

            _lobbyData.RelayJoinCode = relayJoinCode; // lobbyData에 받아온 릴레이 조인 코드 저장해주고
            await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize()); // 업뎃된 lobbyData 반영 -> 로비 업뎃

            // allocationID와 connectionData가 릴레이 매니저와 로비를 링크해주기 때문에
            // 내가 릴레이 매니저와 연결이 끊어졌더라도 동일한 로비로 돌아올 수 있다
            // 그래서 그것들을 가져와 저장하는 코드
            // 참고로 릴레이 매니저가 이미 알고 있는 값이기 때문에 async call을 할 필요 X
            string allocationId = RelayManager.Instance.GetAllocationId();
            string connectionData = RelayManager.Instance.GetConnectionData();
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, connectionData);

            SceneManager.LoadSceneAsync("MultiPlayScene");

        }


        private async Task<bool> JoinRelayServer(string relayJoinCode)
        {
            _inGame = true;

            await RelayManager.Instance.JoinRelay(relayJoinCode);

            // StartGame의 코드와 동일. connection data를 이용해 플레이어 데이터를 업데이트.
            string allocationId = RelayManager.Instance.GetAllocationId();
            string connectionData = RelayManager.Instance.GetConnectionData();
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, connectionData);

            return true;
        }

    }

}
