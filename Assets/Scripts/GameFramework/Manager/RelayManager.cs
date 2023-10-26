using JetBrains.Annotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEditor;

namespace GameFramework.Core.GameFramework.Manager
{
    public class RelayManager : Singleton<RelayManager>
    {
        private bool _isHost = false;

        private string _joinCode; // �÷��̾���� ������ ���� �����ϴ� �ڵ�
        private string _ip;
        private int _port;
        private byte[] _key;
        private byte[] _connectionData; // raw format���� �����
        private byte[] _hostConnectionData;
        private System.Guid _allocationId;
        private byte[] _allocationIdBytes;

        public bool IsHost 
        {
            get { return _isHost; }
        }

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
            _allocationIdBytes = allocation.AllocationIdBytes;
            _connectionData = allocation.ConnectionData;
            _key = allocation.Key; // �̹� allocation �ȿ� byte[]�� �����ϴ� �͵��� �޾ƿ�
            // GameManger.cs�� SetHostRelayData()�� �μ��� �־��ֱ� ���� �ʿ�


            _isHost = true; // CreateRelay�� �ϴ� ���� ȣ��Ʈ�ϱ�

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
            _allocationIdBytes = allocation.AllocationIdBytes;
            _connectionData = allocation.ConnectionData;
            _hostConnectionData = allocation.HostConnectionData; // ���⸸ JoinRealy�� �߰��� ��. �Խ�Ʈ �÷��̾�� �� �����Ͱ� �ʿ�
            _key = allocation.Key;
            // ������� �ڵ尡 CreateRelay�� �����ؼ� �ߺ�������, 
            // allocation �κ��� ���� �޼ҵ�� ���� allocation�� �Ѱ����� ���ϵ��� �Ǿ� �ִ�
            // ��, �� �̷��� ��� ��

            return true; // ȣ��Ʈ�� ������ ������ �� �����ߴٴ� ���� �˸�

        }


        public (byte[] AllocationId, byte[] Key, byte[] ConnectionData, string _dtlsAddress, int _dtlsPort) GetHostConnectionInfo()
        {
            return (_allocationIdBytes, _key, _connectionData, _ip, _port); // �� ���� ���� ���� ��� ����
        }

        public (byte[] AllocationId, byte[] Key, byte[] ConnectionData, byte[] HostConnectionData, string _dtlsAddress, int _dtlsPort) GetClientConnectionInfo()
        {
            return (_allocationIdBytes, _key, _connectionData, _hostConnectionData, _ip, _port);
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