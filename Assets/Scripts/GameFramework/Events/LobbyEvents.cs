using Unity.Services.Lobbies.Models;

namespace GameFramework.Events
{
    public static class LobbyEvents
    {
        public delegate void LobbyUpdated(Lobby lobby); // delegate는 C의 포인터 같은 거. 대리자.
        // Lobby 타입의 데이터를 파라미터로 받는 메소드를 가리키는 델리게이트 LobbyUpdated 타입?을 만든 것이다

        public static LobbyUpdated OnLobbyUpdated; // OnLobbyUpdated란 이름으로 델리게이트 LobbyUpdated 타입을 선언... 한 게 아니라
        // GameLobbyManager.cs의 OnLobbyUpdated를 가리키는 델리게이트인 건가?

    }
}