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

namespace Game // Game ���� ���� Init.cs, LobbyUI.cs ���� ��ũ��Ʈ ���ϵ��� Game ���ӽ����̽� �ȿ� ����. 
    // ���ӽ����̽��� ����� ����: �̸��� ���� �����̳� Ŭ������ ���� ����� ����... ex) LobbyEvents.cs
    // �� ���ӽ����̽��� �ٸ��� �̸��� �ߺ��Ǿ ok. ������ ���ӽ����̽��� �����ָ� �ȴ�.
{
    public class GameLobbyManager : Singleton<GameLobbyManager>
    {

        private List<LobbyPlayerData> _lobbyPlayerDatas = new List<LobbyPlayerData>(); // �κ� ������ ��� �÷��̾��� �����Ͱ� ����Ǵ� ��
        private LobbyPlayerData _localLobbyPlayerData; // ���� �÷��̾� (�����ϴ� ����)�� �����Ͱ� ����Ǵ� ��
        private LobbyData _lobbyData; // ������ ���� �ڵ� ���� �����. �� ���� ��� �߰� �� �� ������ ���⿡ ��
        private int _maxNumberOfPlayers = 2; // ���� �� �ϰ� �����ϱ� private����
        private bool _inGame = false; // �κ񿡼� ������ ������ �̵��ϰ� ������ ���۵Ǹ� �κ�� �����־�� ��. (�κ� ��� lobbyUpdate�� ���� ���ε�, �װ� ���߾��) �װ� ���� ��

        public bool IsHost => _localLobbyPlayerData.Id == LobbyManager.Instance.GetHostId();


        private void OnEnable()
        {
            LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
            // + �Ǵ� -�� �ϳ��� delegate �Լ� LobbyEvents.OnLobbyUpdated�� ���� ���� �Լ��� ����� delegate chain ���·� ����ϴ� ��.
            // LobbyEvents.OnLobbyUpdated�� �� �� ȣ���ϸ� ��ϵ� ���� ���� OnLobbyUpdated�� �� ���� ����ȴ�.
        }


        private void OnDisable()
        {
            LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
            // ���� ��ϵ� �Լ��� �����ϴ� ��
            // �̷��� delegate�� �̿��� Observer �������� �̺�Ʈ�� ó���Ѵ�. (����: https://huiyu.tistory.com/entry/C-%EA%B8%B0%EC%B4%88-%EC%9D%B4%EB%B2%A4%ED%8A%B8%EC%99%80-%EB%8D%B8%EB%A6%AC%EA%B2%8C%EC%9D%B4%ED%8A%B8-Event-Delegate)
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
            */ // LobbyPlayerData.cs �� ����� ���� ����ߴ� �ڵ�

            /*
            LobbyPlayerData playerData = new LobbyPlayerData(); // LobbyPlayerData.cs���� ������ Ŭ����
            playerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "HostPlayer");

            bool succeeded = await LobbyManager.Instance.CreateLobby(maxPlayers: 2, isPrivate: true, playerData.Serialize()); //Serialize�� ������ Dictionary ���·� �Ѱ��־��.
            */

            // ���� �ڵ忡�� playerData -> _localLobbyPlayerData �� ��ü
            _localLobbyPlayerData = new LobbyPlayerData(); // LobbyPlayerData.cs���� ������ Ŭ����
            _localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "HostPlayer");

            _lobbyData = new LobbyData();

            bool succeeded = await LobbyManager.Instance.CreateLobby(maxPlayers: _maxNumberOfPlayers, isPrivate: true, _localLobbyPlayerData.Serialize(), _lobbyData.Serialize()); //Serialize�� ������ Dictionary ���·� �Ѱ��־��.

            return succeeded;
        }


        public async Task<bool> JoinLobby(string code) // �κ� ���� �Լ��� �� async����
        {
            /*
            Dictionary<string, string> playerData = new Dictionary<string, string>()
            {
                { "GamerTag", "JoinPlayer" }
            };
            */ // LobbyPlayerData.cs �� ����� ���� ����ߴ� �ڵ�

            /*
            LobbyPlayerData playerData = new LobbyPlayerData(); // LobbyPlayerData.cs���� ������ Ŭ����
            playerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "JoinPlayer");

            bool succeeded = await LobbyManager.Instance.JoinLobby(code, playerData.Serialize());
            */


            // ���� �ڵ忡�� playerData -> _localLobbyPlayerData �� ��ü
            _localLobbyPlayerData = new LobbyPlayerData(); // LobbyPlayerData.cs���� ������ Ŭ����
            _localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, gamertag: "JoinPlayer");

            bool succeeded = await LobbyManager.Instance.JoinLobby(code, _localLobbyPlayerData.Serialize());

            return succeeded;
        }


        // LobbyManager�� ������Ʈ�� �÷��̾� �����͸� �������ָ�, _lobbyPlayerDatas ����Ʈ�� repopulate�ϴ� �޼ҵ�
        // ���ο� �÷��̾� �����Ͱ� ������Ʈ�� ������ �̸� ������ ������ �� �����͸� �޾ƿ����� �ϴ� �޼ҵ���
        private async void OnLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.Instance.GetPlayersData();
            // LobbyPlayerData.cs�� UpdateState�� ���ڰ� <Dictionary<string, PlayerDataObject> �����̾��� ����
            _lobbyPlayerDatas.Clear();
            // ������Ʈ�� ������ �޾� �����ϹǷ�, ������ �����͸� clear

            // ������ �÷��̾� ���� üũ��, ��� �ο��� ���� �������� Ȯ��
            int numberOfPlayerReady = 0;

            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new LobbyPlayerData();
                lobbyPlayerData.Initialize(data);

                if (lobbyPlayerData.IsReady)
                {
                    numberOfPlayerReady++;
                }


                if (lobbyPlayerData.Id == AuthenticationService.Instance.PlayerId) // Unity Lobby���� �޾ƿ� ������Ʈ�� �÷��̾� �����Ͱ� ���� �÷��̾��� ���̶��
                {
                    _localLobbyPlayerData = lobbyPlayerData; // �޾ƿ� ������Ʈ�� �����͸� _localLobbyPlayerData�� ����
                }

                _lobbyPlayerDatas.Add(lobbyPlayerData); // �޾ƿ� ������Ʈ�� �÷��̾� �����͸� ����Ʈ�� �߰�
            }


            _lobbyData = new LobbyData(); // LobbyData �����ϰ�
            _lobbyData.Initialize(lobby.Data); // LobbyData�� ���� ������ lobby.Data �� �����Ѵ�. �κ� ��ȭ�� ���� ������ ��� ��������.
            // �� ���� OnLobbyUpdated �븮�ڸ� ������ ��ȭ�� �κ� ������ �����ؾ� �ϴ� �̺�Ʈ�� ������


            // ���� �����͸� �޾ƿ� �� structure�� ���������, event�� �����͸� �Ѱ��� �� �ִ�
            Events.LobbyEvents.OnLobbyUpdated?.Invoke(); // Lobby�κ��� ������ -> �κ� �÷��̾� ���� ����
            // Game �ȿ� Events�� �����Ƿ� Events.LobbyEvents�� ����. LobbyEvents�� Game/Events/LobbyEvents.cs�� (GameFramework/Events/LobbyEvents.cs�� �ƴ�)
            // Invoke�� Invoke(Delegate)�� ���δ�. �븮�ڸ� ȣ��. (����: https://cartiertk.tistory.com/67)

            // ���� LobbyManager.cs�� RefreshLobbyCoroutine �޼ҵ忡 OnLobbyUpdated�� �Ἥ �̺�Ʈ�� throw�Ѵ�.

            if (numberOfPlayerReady == lobby.Players.Count) 
            {
                Events.LobbyEvents.OnLobbyReady?.Invoke(); // ��� �÷��̾ ���� ���¸�, OnLobbyReady ����
                // ȣ��Ʈ ȥ�� ���� �� ������� �ʵ��� �ڵ� ���� �ʿ�!!!
            }

            if (_lobbyData.RelayJoinCode != default && !_inGame) // lobbyData���� ������ ���� �ڵ尡 initialize�Ǿ� ������� �ʴٸ�, �׸��� ���� ���� ���� �ƴ϶��
            {
                // !_inGame ������ �־�� ���� ���� �߿� ��� �� �ڵ尡 ����Ǿ� ������ �̵��Ϸ��� ���� ���� �� ����. �� �� �̵��ϸ� �����־��...

                await JoinRelayServer(_lobbyData.RelayJoinCode); // �� ���� �ڵ带 �̿��� ������ ������ ����������
                SceneManager.LoadSceneAsync("MultiPlayScene"); // �� ���� �÷��� ������ �̵�
                // �� if�� ���� �Խ�Ʈ ���� ���� ��. 
                // ȣ��Ʈ�� START ��ư�� ���� GameStart() �޼ҵ带 �����ϸ�, ������ ���� -> ���� �ڵ� ���� -> ���� �ڵ� ������ �κ����� ���� �� ���ִµ�,
                // �Խ�Ʈ�� �� OnLobbyUpdate���� ���� �κ� �����͸� �޾� ������Ʈ�ϰ�, ���� �ڵ尡 ����� �� if������ ���� �÷��� ������ �̵��Ѵ�. ���� ����.
            }

        }

        public List<LobbyPlayerData> GetPlayers()
        {
            return _lobbyPlayerDatas;
        }

        public async Task<bool> SetPlayerReady() // ���� ��ư�� ������ ����Ǵ� �޼ҵ�
        {
            _localLobbyPlayerData.IsReady = true;
            return await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize());
        }

        public async Task StartGame() // ��ŸƮ ��ư�� ������ ������ �����ϴ� �޼ҵ�. �� ȣ��Ʈ ������ �����
        {
            string relayJoinCode = await RelayManager.Instance.CreateRelay(_maxNumberOfPlayers); // ������ ���� �� �ִ� �÷��̾� �� �Ѱ��־�� ��.
            // CreateRelay�� ���� �� ���� �ڵ带 ����. �װ��� joinRelayCode�� ����
            _inGame = true;

            _lobbyData.RelayJoinCode = relayJoinCode; // lobbyData�� �޾ƿ� ������ ���� �ڵ� �������ְ�
            await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize()); // ������ lobbyData �ݿ� -> �κ� ����

            // allocationID�� connectionData�� ������ �Ŵ����� �κ� ��ũ���ֱ� ������
            // ���� ������ �Ŵ����� ������ ���������� ������ �κ�� ���ƿ� �� �ִ�
            // �׷��� �װ͵��� ������ �����ϴ� �ڵ�
            // ����� ������ �Ŵ����� �̹� �˰� �ִ� ���̱� ������ async call�� �� �ʿ� X
            string allocationId = RelayManager.Instance.GetAllocationId();
            string connectionData = RelayManager.Instance.GetConnectionData();
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, connectionData);

            SceneManager.LoadSceneAsync("MultiPlayScene");

        }


        private async Task<bool> JoinRelayServer(string relayJoinCode)
        {
            _inGame = true;

            await RelayManager.Instance.JoinRelay(relayJoinCode);

            // StartGame�� �ڵ�� ����. connection data�� �̿��� �÷��̾� �����͸� ������Ʈ.
            string allocationId = RelayManager.Instance.GetAllocationId();
            string connectionData = RelayManager.Instance.GetConnectionData();
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, connectionData);

            return true;
        }

    }

}
