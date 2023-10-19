using Game.Events;
using GameFramework.Core.Data;
using System.Collections.Generic;
using UnityEngine;


namespace Game
{
    public class LobbySpawner : MonoBehaviour
    {
        [SerializeField] private List<LobbyPlayer> _players; // �κ� �����ϰų� ���� �÷��̾���� ĳ���͸� ���̰ų� �������� �ϴ�


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
            List<LobbyPlayerData> playerDatas = GameLobbyManager.Instance.GetPlayers(); // �κ� �ִ� �ÿ��̾���� �����͸� ��������

            for (int i = 0; i < playerDatas.Count; i++) // �κ񿡼� ��� �÷��̾��� ���ۿ��� �÷��̾���� ������ ��ġ�� �����ؾ� ��. ȣ��Ʈ�� ���� ��.
            {
                LobbyPlayerData data = playerDatas[i];
                _players[i].SetData(data); // �κ� �� i��° _player ������Ʈ�� �κ񿡼� ������ i��° �÷��̾� �����͸� ����. LobbyPlayer.cs���� ����
            }
        }

    }

}
