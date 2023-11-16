using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace GameFramework.Core.Data
{
    // 로비에 참여한 플레이어들의 데이터를 Unity Lobby를 끼고 send 및 get 하는 클래스. (서로 공유되도록)
    // 로비 내에서 동기화
    // GameLobbyManager.cs에서 사용한다
    public class LobbyPlayerData
    {
        private string _id;
        private string _gamertag;
        private bool _isReady; // 플레이어의 레디 여부

        public string Id => _id;
        public string Gamertag => _gamertag;
        // => 는 get 메소드를 가지는 속성을 간단하게 나타낸 것. 즉 Id 속성은 _id를 리턴한다. LobbyPlayerData.Id 라고 쓰면 _id를 리턴함.
        // private 속성인 _id를 바로 가져다 쓰지 않고, 이런 public 속성을 정의해서 _id 값을 얻도록 한다

        public bool IsReady
        {
            get => _isReady;
            set => _isReady = value;
        }

        public void Initialize(string id, string gamertag) // 플레이어가 로비에 처음 참여했을 때 초기화
        {
            _id = id;
            _gamertag = gamertag;
        }


        // 이 Initialize는 Unity Lobby에서 데이터가 왔을 때 초기화를 해주는 역할.
        // Unity Lobby에서 데이터를 받아온 거라 파라미터가 Dictionary<string, PlayerDataObject> 타입임.
        // 원래 이 dependency를 없애는 게 좋다?
        public void Initialize(Dictionary<string, PlayerDataObject> playerData)
        {
            UpdateState(playerData);
            // 초기화 시 더 해주고 싶은 연산을 여기에 추가하면 된다
        }


        // Initialize되는 건 아니지만, Unity Lobby로부터 새로운 데이터를 받았을 때 업데이트해주는 메소드
        public void UpdateState(Dictionary<string, PlayerDataObject> playerData)
        {
            if (playerData.ContainsKey("Id")) // Serialize()에서 Dictioncary 타입으로 생성되어 Unity Lobby에 전달된 데이터를 받았으므로, 동일한 형식
            {
                _id = playerData["Id"].Value;
            }

            if (playerData.ContainsKey("GamerTag"))
            {
                _gamertag = playerData["GamerTag"].Value;
            }

            if (playerData.ContainsKey("IsReady"))
            {
                _isReady = playerData["IsReady"].Value == "True";
            }
        }


        public Dictionary<string, string> Serialize() // 유니티 게임 서비스에 넘겨주는 데이터 형식으로 변환. UpdateState에 사용된다?
        {
            return new Dictionary<string, string>()
            {
                {"Id", _id},
                {"GamerTag", _gamertag},
                {"IsReady", _isReady.ToString()} // True or False
            };
        }

    }
}