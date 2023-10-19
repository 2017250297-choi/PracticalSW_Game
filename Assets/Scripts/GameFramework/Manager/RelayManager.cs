using JetBrains.Annotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEditor;

namespace GameFramework.Core.GameFramework.Manager
{
    public class RelayManager : Singleton<RelayManager>
    {
        private string _joinCode; // 플레이어들이 연결을 위해 공유하는 코드
        private string _ip;
        private int _port;
        private byte[] _connectionData; // raw format으로 저장됨
        private System.Guid _allocationId;

        public async Task<string> CreateRelay(int maxConnection) // 호스트 쪽에서 릴레이 서버를 만드는 메소드
        {

            // maxConnection을 알려주는 이유:
            // 한 서버에 최대 인원이 모두 들어오면 더 이상의 연결은 차단해야 해서
            
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnection); // Relay에 할당 생성 요청
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // allocation.serverEndpoints: 넷코드를 이용한 연결을 위해 필요함. 넷코드는 기본적으로 서로의 IP를 받아 포트를 통해 연결하고 데이터를 주고받음
            // allocation.connectionData: 로비와 릴레이매니저를 연결해 게임에서 나가거나 동일 로비로 이동하도록 해주는 데이터

            RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
            // DTLS 말고 UDP로 구현해보자

            // 넷코드로 들어오는 트래픽이 릴레이를 통과하도록 하기 위해
            // 파라미터들을 전달해야 함. 여기에 Unity Transport 프로토콜이 사용됨
            _ip = dtlsEndpoint.Host;
            _port = dtlsEndpoint.Port;

            _allocationId = allocation.AllocationId;
            _connectionData = allocation.ConnectionData;

            return _joinCode; // 연결된 다른 플레이어에게 같은 릴레이에 접속하도록 주는 코드

        }


        public async Task<bool> JoinRelay(string joinCode) // 게스트 쪽에서 호스트의 릴레이 서버에 참여하는 메소드
        {

            // 호스트가 리턴한 joinCode를 받아서 릴레이에 조인하고,
            // 잘 연결되었는지 bool을 리턴함

            _joinCode = joinCode;
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
            // DTLS 말고 UDP로 구현해보자

            _ip = dtlsEndpoint.Host;
            _port = dtlsEndpoint.Port;

            _allocationId = allocation.AllocationId;
            _connectionData = allocation.ConnectionData;
            // 여기까지 코드가 CreateRelay와 동일해서 중복되지만, 
            // allocation 부분은 따로 메소드로 빼서 allocation을 넘겨주지 못하도록 되어 있다
            // 즉, 걍 이렇게 써야 함

            return true; // 호스트의 릴레이 서버에 잘 접속했다는 것을 알림

        }


        public string GetAllocationId()
        {
            return _allocationId.ToString();
        }

        public string GetConnectionData()
        {
            return _connectionData.ToString();
        }

    }
}