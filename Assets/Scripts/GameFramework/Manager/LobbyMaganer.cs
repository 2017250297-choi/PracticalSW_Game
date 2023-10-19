using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor;
//using Game.Events;
using GameFramework.Events; // LobbyEvents.OnLobbyUpdated�� ���⿡ ���� (Game.Events�� �ִ� �Ͱ� �ٸ�)
//using Mono.Cecil.Cil;

namespace GameFramework.Core.GameFramework.Manager
{
    public class LobbyManager : Singleton<LobbyManager>
    {

        private Lobby _lobby;
        private Coroutine _heartbeatCoroutine;
        private Coroutine _refreshLobbyCoroutine;


        public string GetLobbyCode() // Lobby �� UI�� �κ� �ڵ带 ��Ÿ���� ���� �ڵ带 �����ϴ� �Լ�
        {
            return _lobby?.LobbyCode;
            // _lobby �ڿ� ���� ?�� _lobby�� null���� üũ�Ѵ�. _lobby�� null�̸� null�� ����
        }


        // �κ� �� ����������� �Ǵ��ϱ� ���� bool ���
        public async Task<bool> CreateLobby(int maxPlayers, bool isPrivate, Dictionary<string, string> data)
        {
            Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);

            Player player = new Player(AuthenticationService.Instance.PlayerId, connectionInfo: null, playerData);
            // Player id, connection info, Dictionary<string.PlayerDataObject> data ���� �־��־��

            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = isPrivate,
                Player = player
            };


            // ���� ó��
            try
            {
                _lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", maxPlayers, options); // ������� �κ� ����

            } catch(System.Exception)
            {
                return false;
            }


            Debug.Log(message: $"Lobby created with lobby id {_lobby.Id}");

            _heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(_lobby.Id, 6f)); // 6�ʸ��� �κ� �����ǵ��� heartbeat
            _refreshLobbyCoroutine = StartCoroutine(RefreshLobbyCoroutine(_lobby.Id, 1f)); // ���ʸ��� �κ� ������Ʈ

            return true;
        }

        private IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds) // �κ� �����Ǿ� �����Ǵ� ���� ����Ǵ� �ڷ�ƾ. �κ� �������� ������� �ʴ´�
        {
            while (true)
            {
                Debug.Log(message: "Heartbeat");
                LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                yield return new WaitForSecondsRealtime(waitTimeSeconds); // 6�ʸ� ��ٸ�
            }
        }

        private IEnumerator RefreshLobbyCoroutine(string lobbyId, float waitTimeSeconds) // �κ� ������Ʈ���ִ� �ڷ�ƾ.
        {
            while (true)
            {
                Task<Lobby> task = LobbyService.Instance.GetLobbyAsync(lobbyId);
                yield return new WaitUntil(() => task.IsCompleted); // task�� �Ϸ�� ������ ��ٸ�
                Lobby newLobby = task.Result;
                if (newLobby.LastUpdated > _lobby.LastUpdated) // newLobby�� �� �ֽ��̶��
                {
                    _lobby = newLobby; // �κ� ������Ʈ
                    LobbyEvents.OnLobbyUpdated?.Invoke(_lobby); // OnLobbyUpdated�� �븮�ڴϱ� Invoke�� ����. ������ subscribe�ؼ�?
                    // RefreshLobbyCoroutine�� ����Ǿ� �����Ǵ� ��, �κ� refresh�� ������ GameLobbyManager.cs�� OnLobbyUpdated�� �����ؼ�
                    // �÷��̾� ������ �������� ������Ʈ�Ѵ�.
                }
                yield return new WaitForSecondsRealtime(waitTimeSeconds); // 6�ʸ� ��ٸ�
            }
        }

        // CreateLobby �Լ����� �Է����� ���� data�� <key, PlayerDataObject> �������� �籸���� �������ִ� �Լ�
        private Dictionary<string, PlayerDataObject> SerializePlayerData(Dictionary<string, string> data)
        {
            Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();
            foreach (var (key, value) in data)
            {
                playerData.Add(key, new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Member, // �κ��� ����鿡�Ը� ������ ��
                                                                           // VisibilityOptions���� public, memeber, private ���� ����
                    value: value));
            }

            return playerData;
        }


        public void OnApplicationQuit() // �κ� ��������� �������ִ� �Լ��� �־��...
        {
            if (_lobby != null && _lobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
            }
        }


        public async Task<bool> JoinLobby(string code, Dictionary<string, string> playerData)
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();
            Player player = new Player(AuthenticationService.Instance.PlayerId, connectionInfo: null, SerializePlayerData(playerData));

            options.Player = player;

            try
            {
                _lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
            } catch (System.Exception)
            {
                return false;
            }
            
            // CreateLobby �Լ��� ���������, Heartbeat�� ���� �ʾƵ� �ȴ�. �װ� ȣ��Ʈ�� �ϸ� ��.
            _refreshLobbyCoroutine = StartCoroutine(RefreshLobbyCoroutine(_lobby.Id, 1f)); // ��� ����� ��. �κ��� ��ȭ ������ ȣ��Ʈ�� ������ ��� �޾ƾ� �ϴϱ�.
            return true;
        }

        // data ����Ʈ�� ���� �κ��� ��� �÷��̾� ������ �����ϰ� �����ϴ� �޼ҵ�
        public List<Dictionary<string, PlayerDataObject>> GetPlayersData()
        {
            List<Dictionary<string, PlayerDataObject>> data = new List<Dictionary<string, PlayerDataObject>>();

            foreach (Player player in _lobby.Players)
            {
                data.Add(player.Data);
            }

            return data;
        }

        public async Task<bool> UpdatePlayerData(string playerId, Dictionary<string, string> data, string allocationId = default, string connectionData = default) 
        {
            // data �ȿ� playerId�� �ֱ� ��. �ٵ� ���� ���� �־ �̷��� plyaerId �Ķ���͸� ���� ����
            // ���� 2�� �Ķ���ʹ� = default �� ��� optional �Ķ���ͷ� ����. �޼ҵ� ��� �� �� �־��൵ ����.


            Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);

            UpdatePlayerOptions options = new UpdatePlayerOptions()
            {
                Data = playerData,
                AllocationId = allocationId,
                ConnectionInfo = connectionData
            };
            try
            {
                _lobby = await LobbyService.Instance.UpdatePlayerAsync(_lobby.Id, playerId, options);
                // �κ� ���̵�� �÷��̾� ���̵�� ������Ʈ�� �÷��̾� ������ ����ȭ�ؼ� Lobby�� ����.
            } 
            catch (System.Exception) 
            { 
                return false;
            }

            LobbyEvents.OnLobbyUpdated(_lobby); // �޾ƿ� Lobby�� �κ� ������Ʈ

            return true;
        }

        public string GetHostId()
        {
            return _lobby.HostId;
        }

    }
}