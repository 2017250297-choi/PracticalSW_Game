using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace GameFramework.Core.Data
{
    public class LobbyData
    {
        private string _relayJoinCode; // 릴레이 서버에 참여하기 위한 코드

        public string RelayJoinCode
        {
            get => _relayJoinCode;
            set => _relayJoinCode = value;
        }

        public void Initialize(Dictionary<string, DataObject> lobbyData)
        {
            UpdateState(lobbyData);
        }

        public void UpdateState(Dictionary<string, DataObject> lobbyData)
        {
            if (lobbyData.ContainsKey("RelayJoinCode"))
            {
                _relayJoinCode = lobbyData["RelayJoinCode"].Value; // 받아온 lobbyData에서 릴레이 조인 코드를 찾아 저장
            }
        }

        public Dictionary<string, string> Serialize()
        {
            return new Dictionary<string, string>()
            {
                { "RelayJoinCode", _relayJoinCode }
            };
        }
    }
}