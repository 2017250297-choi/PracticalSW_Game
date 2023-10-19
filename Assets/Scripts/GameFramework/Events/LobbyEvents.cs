using Unity.Services.Lobbies.Models;

namespace GameFramework.Events
{
    public static class LobbyEvents
    {
        public delegate void LobbyUpdated(Lobby lobby); // delegate�� C�� ������ ���� ��. �븮��.
        // Lobby Ÿ���� �����͸� �Ķ���ͷ� �޴� �޼ҵ带 ����Ű�� ��������Ʈ LobbyUpdated Ÿ��?�� ���� ���̴�

        public static LobbyUpdated OnLobbyUpdated; // OnLobbyUpdated�� �̸����� ��������Ʈ LobbyUpdated Ÿ���� ����... �� �� �ƴ϶�
        // GameLobbyManager.cs�� OnLobbyUpdated�� ����Ű�� ��������Ʈ�� �ǰ�?

    }
}