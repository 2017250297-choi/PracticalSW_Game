// �κ� �����ϸ� ĳ���͸� �������ִ� ��ũ��Ʈ

using GameFramework.Core.Data;
using TMPro;
using UnityEngine;

namespace Game
{
    public class LobbyPlayer : MonoBehaviour // �÷��̾� �������� ���� ��
    {
        [SerializeField] private TextMeshPro _playerName; // ĳ���� �Ʒ��� ��� �÷��̾� �̸� �ؽ�Ʈ
        [SerializeField] private GameObject _isReadyText; // ĳ���� ���� ��� Ready �ؽ�Ʈ

        private LobbyPlayerData _data;

        
        public void SetData(LobbyPlayerData data) // �κ� ������ �÷��̾��� ������ set
        {
            _data = data;
            _playerName.text = _data.Gamertag;

            if (_data.IsReady)
            {
                _isReadyText.SetActive(true);
            }

            gameObject.SetActive(true); // �κ��� �÷��̾���� ������ activate�ϵ���
        }
    }
}