using Game.Events;
using GameFramework.Core.Data;
using System.Collections.Generic;
using UnityEngine;


namespace Game
{
    public class LobbySpawner : MonoBehaviour
    {
        [SerializeField] private List<LobbyPlayer> _players; // 로비에 조인하거나 나간 플레이어들의 캐릭터를 보이거나 없어지게 하는


        private void OnEnable()
        {
            LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
        }


        private void OnDisable()
        {
            LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
        }


        private void OnLobbyUpdated()
        {
            List<LobbyPlayerData> playerDatas = GameLobbyManager.Instance.GetPlayers(); // 로비에 있는 플에이어들의 데이터를 가져오고

            for (int i = 0; i < playerDatas.Count; i++) // 로비에서 모든 플레이어의 시작에서 플레이어들이 동일한 위치에 존재해야 함. 호스트가 가장 앞.
            {
                LobbyPlayerData data = playerDatas[i];
                _players[i].SetData(data); // 로비 내 i번째 _player 오브젝트에 로비에서 가져온 i번째 플레이어 데이터를 저장. LobbyPlayer.cs에서 정의
            }
        }

    }

}
