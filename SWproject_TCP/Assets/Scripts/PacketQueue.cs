using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class PacketQueue
{
    // 패킷 저장 정보
    struct PacketInfo
    {
        public int offset;
        public int size;
    };

    private MemoryStream m_streamBuffer; // 메모리에 Byte 데이터를 순서대로 읽고 씀
    private List<PacketInfo> m_offsetList;
    private int m_offset = 0;

    //
    public PacketQueue()
    {
        m_streamBuffer = new MemoryStream();
        m_offsetList = new List<PacketInfo>();
    }

    //
    public int Enqueue(byte[] data, int size)
    {
        PacketInfo info = new PacketInfo();

        info.offset = m_offset;
        info.size = size;

        // 패킷 저장 정보 보존
        m_offsetList.Add(info);

        // 패킷 데이터를 보존
        m_streamBuffer.Position = m_offset;
        m_streamBuffer.Write(data, 0, size);
        m_streamBuffer.Flush();
        m_offset += size;

        return size;
    }

    public int Dequeue(ref byte[] buffer, int size)
    {
        if (m_offsetList.Count <= 0)
        {
            return -1;
        }

        PacketInfo info = m_offsetList[0];

        // 버퍼에서 해당하는 패킷 가져오기
        int dataSize = Math.Min(size, info.size);
        m_streamBuffer.Position = info.offset;
        int recvSize = m_streamBuffer.Read(buffer, 0, dataSize);

        // 큐 데이터 추출했으므로 가장 앞의 요소 삭제
        if (recvSize > 0)
        {
            m_offsetList.RemoveAt(0);
        }

        // 모든 큐 데이터를 꺼냈을 때는, 스트림을 클리어해서 메모리 절약
        if (m_offsetList.Count == 0)
        {
            Clear();
            m_offset = 0;
        }

        return recvSize;
    }

    public void Clear()
    {
        byte[] buffer = m_streamBuffer.GetBuffer();
        Array.Clear(buffer, 0, buffer.Length);

        m_streamBuffer.Position = 0;
        m_streamBuffer.SetLength(0);
    }
}
