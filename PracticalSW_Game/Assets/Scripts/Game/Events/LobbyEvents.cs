namespace Game.Events
{
    public static class LobbyEvents
    {
        public delegate void LobbyUpdated(); // ��ǲ�� ���� ����: GameLobbyManager���� �����͸� fetch�ؿ� �Ϻθ� ���ϱ� ����...?
        public static LobbyUpdated OnLobbyUpdated;

        public delegate void LobbyReady(); // �׳� LobbyUI�� ��ŸƮ ��ư�� ���� �����϶�� �͸� �˸��� �뵵�̱� ������, ��ǲ�� �ʿ� X
        public static LobbyReady OnLobbyReady;
    }
}