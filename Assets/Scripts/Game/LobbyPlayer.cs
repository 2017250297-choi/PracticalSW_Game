// 로비에 입장하면 캐릭터를 스폰해주는 스크립트

using GameFramework.Core.Data;
using TMPro;
using UnityEngine;

namespace Game
{
    public class LobbyPlayer : MonoBehaviour // 플레이어 프리펩을 넣을 것
    {
        [SerializeField] private TextMeshPro _playerName; // 캐릭터 아래에 띄울 플레이어 이름 텍스트
        [SerializeField] private GameObject _isReadyText; // 캐릭터 위에 띄울 Ready 텍스트

        private LobbyPlayerData _data;

        
        public void SetData(LobbyPlayerData data) // 로비에 참여한 플레이어의 정보를 set
        {
            _data = data;
            _playerName.text = _data.Gamertag;

            if (_data.IsReady)
            {
                _isReadyText.SetActive(true);
            }

            gameObject.SetActive(true); // 로비의 플레이어들이 스스로 activate하도록
        }
    }
}