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
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true; // 서버에 연결하는 모든 요청을 호스트가 승인

        // 이하는 두 플레이어가 같은 씬에 스폰되도록 하는 코드
        if(RelayManager.Instance.IsHost)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval; // 콜백 함수

            (byte[] allocationId, byte[] key, byte[] connectionData, string ip, int port) = RelayManager.Instance.GetHostConnectionInfo();

            // 이제 릴레이에게 연결을 요청해야. SetHostRelayData는 ipAddress, port, ... 등의 인자를 받음. 위의 GetHostConnectionInfo()로 필요한 파라미터를 받아온다
            // 이 값들은 RelayManager.cs에서 CreateRelay를 할 때 Relay가 알아서 만들어서 준다. 게임을 시작할 때 이 값들이 필요함
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ip, (ushort)port, allocationId, key, connectionData, isSecure: true);
            NetworkManager.Singleton.StartHost();

        } else
        {
            // 여긴 호스트가 아닌 게스트 쪽에서 실행되는 코드. GetClientConnectionInfo(), SetClientRelayData(), StartClient()를 사용
            (byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData, string ip, int port) = RelayManager.Instance.GetClientConnectionInfo();
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ip, (ushort)port, allocationId, key, connectionData, hostConnectionData, isSecure: true);
            NetworkManager.Singleton.StartClient();
        }
    }


    private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true; // 연결이 잘 되면 플레이어 오브젝트를 스폰할 수 있도록 함
        response.Pending = false; // true면 기다리고 있다는 뜻, false면 응답이 잘 도착했다는 뜻
    }
}
