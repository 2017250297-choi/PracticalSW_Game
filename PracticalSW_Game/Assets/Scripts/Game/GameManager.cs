using GameFramework.Core.GameFramework.Manager;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true; // ������ �����ϴ� ��� ��û�� ȣ��Ʈ�� ����

        // ���ϴ� �� �÷��̾ ���� ���� �����ǵ��� �ϴ� �ڵ�
        if(RelayManager.Instance.IsHost)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval; // �ݹ� �Լ�

            (byte[] allocationId, byte[] key, byte[] connectionData, string ip, int port) = RelayManager.Instance.GetHostConnectionInfo();

            // ���� �����̿��� ������ ��û�ؾ�. SetHostRelayData�� ipAddress, port, ... ���� ���ڸ� ����. ���� GetHostConnectionInfo()�� �ʿ��� �Ķ���͸� �޾ƿ´�
            // �� ������ RelayManager.cs���� CreateRelay�� �� �� Relay�� �˾Ƽ� ���� �ش�. ������ ������ �� �� ������ �ʿ���
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ip, (ushort)port, allocationId, key, connectionData, isSecure: true);
            NetworkManager.Singleton.StartHost();

        } else
        {
            // ���� ȣ��Ʈ�� �ƴ� �Խ�Ʈ �ʿ��� ����Ǵ� �ڵ�. GetClientConnectionInfo(), SetClientRelayData(), StartClient()�� ���
            (byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData, string ip, int port) = RelayManager.Instance.GetClientConnectionInfo();
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ip, (ushort)port, allocationId, key, connectionData, hostConnectionData, isSecure: true);
            NetworkManager.Singleton.StartClient();
        }
    }


    private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true; // ������ �� �Ǹ� �÷��̾� ������Ʈ�� ������ �� �ֵ��� ��
        response.Pending = false; // true�� ��ٸ��� �ִٴ� ��, false�� ������ �� �����ߴٴ� ��
    }
}
