namespace Game.Events
{
    public static class LobbyEvents
    {
        public delegate void LobbyUpdated(); // 인풋이 없는 이유: GameLobbyManager에서 데이터를 fetch해온 일부만 원하기 떄문...?
        public static LobbyUpdated OnLobbyUpdated;

        public delegate void LobbyReady(); // 그냥 LobbyUI에 스타트 버튼을 새로 생성하라는 것만 알리는 용도이기 때문에, 인풋이 필요 X
        public static LobbyReady OnLobbyReady;
    }
}