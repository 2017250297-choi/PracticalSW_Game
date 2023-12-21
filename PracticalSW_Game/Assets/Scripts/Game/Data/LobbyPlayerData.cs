using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace GameFramework.Core.Data
{
    // �κ� ������ �÷��̾���� �����͸� Unity Lobby�� ���� send �� get �ϴ� Ŭ����. (���� �����ǵ���)
    // �κ� ������ ����ȭ
    // GameLobbyManager.cs���� ����Ѵ�
    public class LobbyPlayerData
    {
        private string _id;
        private string _gamertag;
        private bool _isReady; // �÷��̾��� ���� ����

        public string Id => _id;
        public string Gamertag => _gamertag;
        // => �� get �޼ҵ带 ������ �Ӽ��� �����ϰ� ��Ÿ�� ��. �� Id �Ӽ��� _id�� �����Ѵ�. LobbyPlayerData.Id ��� ���� _id�� ������.
        // private �Ӽ��� _id�� �ٷ� ������ ���� �ʰ�, �̷� public �Ӽ��� �����ؼ� _id ���� �򵵷� �Ѵ�

        public bool IsReady
        {
            get => _isReady;
            set => _isReady = value;
        }

        public void Initialize(string id, string gamertag) // �÷��̾ �κ� ó�� �������� �� �ʱ�ȭ
        {
            _id = id;
            _gamertag = gamertag;
        }


        // �� Initialize�� Unity Lobby���� �����Ͱ� ���� �� �ʱ�ȭ�� ���ִ� ����.
        // Unity Lobby���� �����͸� �޾ƿ� �Ŷ� �Ķ���Ͱ� Dictionary<string, PlayerDataObject> Ÿ����.
        // ���� �� dependency�� ���ִ� �� ����?
        public void Initialize(Dictionary<string, PlayerDataObject> playerData)
        {
            UpdateState(playerData);
            // �ʱ�ȭ �� �� ���ְ� ���� ������ ���⿡ �߰��ϸ� �ȴ�
        }


        // Initialize�Ǵ� �� �ƴ�����, Unity Lobby�κ��� ���ο� �����͸� �޾��� �� ������Ʈ���ִ� �޼ҵ�
        public void UpdateState(Dictionary<string, PlayerDataObject> playerData)
        {
            if (playerData.ContainsKey("Id")) // Serialize()���� Dictioncary Ÿ������ �����Ǿ� Unity Lobby�� ���޵� �����͸� �޾����Ƿ�, ������ ����
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


        public Dictionary<string, string> Serialize() // ����Ƽ ���� ���񽺿� �Ѱ��ִ� ������ �������� ��ȯ. UpdateState�� ���ȴ�?
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