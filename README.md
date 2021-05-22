# UnityNet
A small UDP and TCP library designed for Unity.

## Example

Below shows an example of the UdpSocket. The API is loosely based on .Net's UdpClient. The Tcp variants are based on TcpListener and TcpClient.

```C#
    UdpSocket server = new UdpSocket();
    server.Bind(666);
    
    UdpSocket client = new UdpSocket();
    client.Connect("localhost", 666);
    
    NetPacket packet = new NetPacket();
    packet.WriteString("Some text");
    
    // ## Sending
    client.Send(ref packet);
    
    byte[] buffer = new byte[128];
    // ## Receiving
    server.Receive(buffer, out _, ref ip);
    
    packet.Dispose(); // NetPackets should be disposed before they go out of scope. They should be re-used when possible.
```

Connection for Udp is optional but makes it so that Send operations always send to the same endpoint.


