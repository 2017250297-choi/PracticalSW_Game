using System.Collections;
using System.Net;
using System;
using UnityEngine;


public class NetworkController
{
    const int USE_PORT = 50765;
    TransportTCP m_network; // 자주 사용하므로 만들어둠


    // 서버 클라이언트 판정용
    public enum HostType
    {
        Server,
        Client,
    };
    HostType m_hostType;


    // 서버에서 사용할 때
    public NetworkController()
    {
        m_hostType = HostType.Server;

        GameObject nObj = GameObject.Find("Network");
        m_network = nObj.GetComponent<TransportTCP>();
        m_network.StartServer(USE_PORT, 1);
    }

    // 클라이언트에서 사용할 때
    public NetworkController(string serverAddress)
    {
        m_hostType = HostType.Client;

        GameObject nObj = GameObject.Find("Network");
        m_network = nObj.GetComponent<TransportTCP>();
        m_network.Connect(serverAddress, USE_PORT);
    }


    // 네트워크 상태 획득
    public bool IsConnected()
    {
        return m_network.IsConnected();
    }

    // 호스트 타입 획득
    public HostType GetHostType()
    {
        return m_hostType;
    }


    // 액션 송신
    /*
        매개변수에서 float actionTime 없애고, (state와 actionKind를 비교해서 판정)
        position.x값을 받도록 해서 현재 나의 위치 x값도 같이 보내도록? 하면 어떨까
        그러면 상대방은 그 위치값과 액션 종류를 받아서 상대방이 플레이어 캐릭터를
        움직이도록 모션과 위치를 계산/조정하게...
        그러면 타이밍이 많이 어긋나려나?
    */

    // actionKind, state, damageValue 3개를 주고받음
    public void SendActionData(ActionKind actionKind, State state, short damageValue, short validDamage)
    {
        // 구조체를 byte 배열로 변환
        byte[] data = new byte[6];
        data[0] = (byte)actionKind;
        data[1] = (byte)state;

        // 정수화
        //short actTime = (short)(actionTime * 1000.0f);
        // 네트워크 바이트오더로 변환
        short netOrder1 = IPAddress.HostToNetworkOrder(damageValue);
        short netOrder2 = IPAddress.HostToNetworkOrder(validDamage);
        // byte[] 형으로 변환
        byte[] conv = BitConverter.GetBytes(netOrder1); // short형은 2byte라서 conv[2]가 된다
        data[2] = conv[0];
        data[3] = conv[1];

        conv = BitConverter.GetBytes(netOrder2);
        data[4] = conv[0];
        data[5] = conv[1];

        // 데이터 송신
        m_network.Send(data, data.Length);
    }

    // 액션 수신
    public bool ReceiveActionData(ref ActionKind actionKind, ref State state, ref short damageValue, ref short validDamage)
    {
        byte[] data = new byte[1024];

        // 데이터 수신
        int recvSize = m_network.Receive(ref data, data.Length);
        if (recvSize < 0)
        {
            // 입력 정보를 수신하지 않은 경우
            return false;
        }

        // byte 배열을 구조체로 변환
        actionKind = (ActionKind)data[0];
        state = (State)data[1];
        // byte[] 형에서 short 형으로 변환
        short netOrder1 = (short)BitConverter.ToUInt16(data, 2);
        short netOrder2 = (short)BitConverter.ToUInt16(data, 4);
        // 호스트 바이트오더로 변환
        short hostOrder1 = IPAddress.NetworkToHostOrder(netOrder1);
        short hostOrder2 = IPAddress.NetworkToHostOrder(netOrder2);
        // float 단위 시간으로 되돌림
        //actionTime = hostOrder / 1000.0f;
        damageValue = hostOrder1;
        validDamage = hostOrder2;

        return true;
    }

    // 서버 닫기
    public void CloseServer()
    {
        m_network.StopServer();
    }

}