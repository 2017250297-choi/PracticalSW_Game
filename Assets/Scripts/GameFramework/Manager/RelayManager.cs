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
        private string _joinCode; // �÷��̾���� ������ ���� �����ϴ� �ڵ�
        private string _ip;
        private int _port;
        private byte[] _connectionData; // raw format���� �����
        private System.Guid _allocationId;

        public async Task<string> CreateRelay(int maxConnection) // ȣ��Ʈ �ʿ��� ������ ������ ����� �޼ҵ�
        {

            // maxConnection�� �˷��ִ� ����:
            // �� ������ �ִ� �ο��� ��� ������ �� �̻��� ������ �����ؾ� �ؼ�
            
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnection); // Relay�� �Ҵ� ���� ��û
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // allocation.serverEndpoints: ���ڵ带 �̿��� ������ ���� �ʿ���. ���ڵ�� �⺻������ ������ IP�� �޾� ��Ʈ�� ���� �����ϰ� �����͸� �ְ����
            // allocation.connectionData: �κ�� �����̸Ŵ����� ������ ���ӿ��� �����ų� ���� �κ�� �̵��ϵ��� ���ִ� ������

            RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
            // DTLS ���� UDP�� �����غ���

            // ���ڵ�� ������ Ʈ������ �����̸� ����ϵ��� �ϱ� ����
            // �Ķ���͵��� �����ؾ� ��. ���⿡ Unity Transport ���������� ����
            _ip = dtlsEndpoint.Host;
            _port = dtlsEndpoint.Port;

            _allocationId = allocation.AllocationId;
            _connectionData = allocation.ConnectionData;

            return _joinCode; // ����� �ٸ� �÷��̾�� ���� �����̿� �����ϵ��� �ִ� �ڵ�

        }


        public async Task<bool> JoinRelay(string joinCode) // �Խ�Ʈ �ʿ��� ȣ��Ʈ�� ������ ������ �����ϴ� �޼ҵ�
        {

            // ȣ��Ʈ�� ������ joinCode�� �޾Ƽ� �����̿� �����ϰ�,
            // �� ����Ǿ����� bool�� ������

            _joinCode = joinCode;
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
            // DTLS ���� UDP�� �����غ���

            _ip = dtlsEndpoint.Host;
            _port = dtlsEndpoint.Port;

            _allocationId = allocation.AllocationId;
            _connectionData = allocation.ConnectionData;
            // ������� �ڵ尡 CreateRelay�� �����ؼ� �ߺ�������, 
            // allocation �κ��� ���� �޼ҵ�� ���� allocation�� �Ѱ����� ���ϵ��� �Ǿ� �ִ�
            // ��, �� �̷��� ��� ��

            return true; // ȣ��Ʈ�� ������ ������ �� �����ߴٴ� ���� �˸�

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