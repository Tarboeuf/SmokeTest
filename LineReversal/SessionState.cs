using System.Net.Sockets;
using System.Net;
using System.Text;

namespace LineReversal;

public class SessionState
{

    private readonly IPEndPoint _clientEndPoint;
    private readonly object _lockObject = new();

    private int _expectedPacketNumber = 0;
    private byte[]? _receivedData;

    public SessionState(IPEndPoint clientEndPoint)
    {
        this._clientEndPoint = clientEndPoint;
    }

    public void HandleReceivedPacket(int packetNumber, int totalPackets, byte[] payload, IPEndPoint senderEndPoint)
    {
        lock (_lockObject)
        {
            if (packetNumber == _expectedPacketNumber)
            {
                AppendPayload(payload);
                _expectedPacketNumber++;

                if (_expectedPacketNumber == totalPackets)
                {
                    // All packets received; reverse the text and send it back
                    byte[] reversedData = Encoding.ASCII.GetBytes((string) Encoding.ASCII.GetString(_receivedData!, 0, _receivedData!.Length)
                        .Reverse());

                    SendAcknowledgment(_expectedPacketNumber, senderEndPoint);
                    SendData(reversedData);

                    ResetSession();
                }
            }
            else if (packetNumber < _expectedPacketNumber)
            {
                // Received a duplicate packet; resend acknowledgment
                SendAcknowledgment(packetNumber, senderEndPoint);
            }
            else
            {
                // Out-of-order packet; ignore and request retransmission
                SendAcknowledgment(_expectedPacketNumber, senderEndPoint);
            }
        }
    }

    private void AppendPayload(byte[] payload)
    {
        _receivedData = _receivedData == null ? payload : _receivedData.Concat(payload).ToArray();
    }

    private void SendAcknowledgment(int packetNumber, IPEndPoint senderEndPoint)
    {
        byte[] acknowledgmentData = BitConverter.GetBytes(packetNumber);
        using var udpClient = new UdpClient();
        udpClient.Send(acknowledgmentData, acknowledgmentData.Length, senderEndPoint);
    }

    private void SendData(byte[] data)
    {
        using var udpClient = new UdpClient();
        udpClient.Send(data, data.Length, _clientEndPoint);
    }

    private void ResetSession()
    {
        _expectedPacketNumber = 0;
        _receivedData = null;
    }
}