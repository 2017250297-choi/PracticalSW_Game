using System.Collections;
using System.Net;
using System;
using UnityEngine;

public class NetworkController
{
    const int USE_PORT = 50765;
    TransportTCP m_network; // ���� ����ϹǷ� ������


    // ���� Ŭ���̾�Ʈ ������
    public enum HostType
    {
        Server,
        Client,
    };
    HostType m_hostType;


    // �������� ����� ��
    public NetworkController()
    {
        m_hostType = HostType.Server;

        GameObject nObj = GameObject.Find("Network");
        m_network = nObj.GetComponent<TransportTCP>();
        m_network.StartServer(USE_PORT, 1);
    }

    // Ŭ���̾�Ʈ���� ����� ��
    public NetworkController(string serverAddress)
    {
        m_hostType = HostType.Client;

        GameObject nObj = GameObject.Find("Network");
        m_network = nObj.GetComponent<TransportTCP>();
        m_network.Connect(serverAddress, USE_PORT);
    }


    // ��Ʈ��ũ ���� ȹ��
    public bool IsConnected()
    {
        return m_network.IsConnected();
    }

    // ȣ��Ʈ Ÿ�� ȹ��
    public HostType GetHostType()
    {
        return m_hostType;
    }


    // �׼� �۽�
    public void SendActionData(ActionKind actionKind, float actionTime)
    {
        // ����ü�� byte �迭�� ��ȯ
        byte[] data = new byte[3];
        data[0] = (byte)actionKind;

        // ����ȭ
        short actTime = (short)(actionTime * 1000.0f);
        // ��Ʈ��ũ ����Ʈ������ ��ȯ
        short netOrder = IPAddress.HostToNetworkOrder(actTime);
        // byte[] ������ ��ȯ
        byte[] conv = BitConverter.GetBytes(netOrder);
        data[1] = conv[0];
        data[2] = conv[1];

        // ������ �۽�
        m_network.Send(data, data.Length);
    }

    // �׼� ����
    public bool ReceiveActionData(ref ActionKind actionKind, ref float actionTime)
    {
        byte[] data = new byte[1024];

        // ������ ����
        int recvSize = m_network.Receive(ref data, data.Length);
        if (recvSize < 0)
        {
            // �Է� ������ �������� ���� ���
            return false;
        }

        // byte �迭�� ����ü�� ��ȯ
        actionKind = (ActionKind)data[0];
        // byte[] ������ short ������ ��ȯ
        short netOrder = (short)BitConverter.ToUInt16(data, 1);
        // ȣ��Ʈ ����Ʈ������ ��ȯ
        short hostOrder = IPAddress.NetworkToHostOrder(netOrder);
        // float ���� �ð����� �ǵ���
        actionTime = hostOrder / 1000.0f;

        return true;
    }

}
