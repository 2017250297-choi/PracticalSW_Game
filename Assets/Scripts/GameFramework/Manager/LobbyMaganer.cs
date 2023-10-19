using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor;
//using Game.Events;
using GameFramework.Events; // LobbyEvents.OnLobbyUpdated가 여기에 있음 (Game.Events에 있는 것과 다름)
//using Mono.Cecil.Cil;

namespace GameFramework.Core.GameFramework.Manager
{
    public class LobbyManager : Singleton<LobbyManager>
    {

        private Lobby _lobby;
        private Coroutine _heartbeatCoroutine;
        private Coroutine _refreshLobbyCoroutine;


        public string GetLobbyCode() // Lobby 씬 UI에 로비 코드를 나타내기 위해 코드를 리턴하는 함수
        {
            return _lobby?.LobbyCode;
            // _lobby 뒤에 붙인 ?는 _lobby가 null인지 체크한다. _lobby가 null이면 null을 리턴
        }


        // 로비가 잘 만들어졌는지 판단하기 위해 bool 사용
        public async Task<bool> CreateLobby(int maxPlayers, bool isPrivate, Dictionary<string, string> data)
        {
            Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);

            Player player = new Player(AuthenticationService.Instance.PlayerId, connectionInfo: null, playerData);
            // Player id, connection info, Dictionary<string.PlayerDataObject> data 등을 넣어주어야

            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = isPrivate,
                Player = player
            };


            // 에러 처리
            try
            {
                _lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", maxPlayers, options); // 만들어진 로비를 저장

            } catch(System.Exception)
            {
                return false;
            }


            Debug.Log(message: $"Lobby created with lobby id {_lobby.Id}");

            _heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(_lobby.Id, 6f)); // 6초마다 로비 유지되도록 heartbeat
            _refreshLobbyCoroutine = StartCoroutine(RefreshLobbyCoroutine(_lobby.Id, 1f)); // 매초마다 로비 업데이트

            return true;
        }

        private IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds) // 로비가 생성되어 유지되는 동안 실행되는 코루틴. 로비가 없어지면 실행되지 않는다
        {
            while (true)
            {
                Debug.Log(message: "Heartbeat");
                LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                yield return new WaitForSecondsRealtime(waitTimeSeconds); // 6초를 기다림
            }
        }

        private IEnumerator RefreshLobbyCoroutine(string lobbyId, float waitTimeSeconds) // 로비를 업데이트해주는 코루틴.
        {
            while (true)
            {
                Task<Lobby> task = LobbyService.Instance.GetLobbyAsync(lobbyId);
                yield return new WaitUntil(() => task.IsCompleted); // task가 완료될 때까지 기다림
                Lobby newLobby = task.Result;
                if (newLobby.LastUpdated > _lobby.LastUpdated) // newLobby가 더 최신이라면
                {
                    _lobby = newLobby; // 로비를 업데이트
                    LobbyEvents.OnLobbyUpdated?.Invoke(_lobby); // OnLobbyUpdated는 대리자니까 Invoke로 실행. 누군가 subscribe해서?
                    // RefreshLobbyCoroutine이 실행되어 유지되는 중, 로비가 refresh될 때마다 GameLobbyManager.cs의 OnLobbyUpdated를 실행해서
                    // 플레이어 정보를 가져오고 업데이트한다.
                }
                yield return new WaitForSecondsRealtime(waitTimeSeconds); // 6초를 기다림
            }
        }

        // CreateLobby 함수에서 입력으로 들어온 data를 <key, PlayerDataObject> 형식으로 재구성해 리턴해주는 함수
        private Dictionary<string, PlayerDataObject> SerializePlayerData(Dictionary<string, string> data)
        {
            Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();
            foreach (var (key, value) in data)
            {
                playerData.Add(key, new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Member, // 로비의 멤버들에게만 보여야 함
                                                                           // VisibilityOptions에는 public, memeber, private 등이 있음
                    value: value));
            }

            return playerData;
        }


        public void OnApplicationQuit() // 로비를 만들었으면 삭제해주는 함수도 있어야...
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
            
            // CreateLobby 함수와 비슷하지만, Heartbeat는 하지 않아도 된다. 그건 호스트만 하면 됨.
            _refreshLobbyCoroutine = StartCoroutine(RefreshLobbyCoroutine(_lobby.Id, 1f)); // 얘는 해줘야 함. 로비의 변화 내용은 호스트와 참가자 모두 받아야 하니까.
            return true;
        }

        // data 리스트에 현재 로비의 모든 플레이어 정보를 저장하고 리턴하는 메소드
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
            // data 안에 playerId도 있긴 함. 근데 없을 수도 있어서 이렇게 plyaerId 파라미터를 따로 받음
            // 뒤의 2개 파라미터는 = default 로 적어서 optional 파라미터로 만듦. 메소드 사용 시 안 넣어줘도 ㄱㅊ.


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
                // 로비 아이디와 플레이어 아이디로 업데이트된 플레이어 정보를 동기화해서 Lobby로 리턴.
            } 
            catch (System.Exception) 
            { 
                return false;
            }

            LobbyEvents.OnLobbyUpdated(_lobby); // 받아온 Lobby로 로비를 업데이트

            return true;
        }

        public string GetHostId()
        {
            return _lobby.HostId;
        }

    }
}