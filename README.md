# UnityNet
UnityNet aims for out-of-the-box TCP connectivity without garbage generation during messaging. UnityNet uses the built-in Socket class, making it compatible with various compilation targets.

**Please read the USAGE section carefuly before using UnityNet.**

Scroll below to find examples.

## Usage
UnityNet uses two different types of packets. Both NetPacket and RawPacket are based on value-types (structs) and **must be passed by ref** at all times.
**Disposed should be called on both types when they are no longer needed**. It is adviced to create and dispose packets in the same location, as to not cause memory leaks.

UnityNet offers the following:
- TcpListener and TcpSocket to establish client-server communication.
- Packet reassembly.
- NetPacket with build-in serialization and bound checking.
- NetPacket with various compression algorithms and string encodings.
- Garbage-free Tcp communication.
- Exception-free socket communication. Error-codes are used instead to optimize memory usage.
- Ability to limit the maximum packet size for servers.
- Interface compatible with classes and structs for self-defined serialization.
- Does not use a specific protocol, making it compatible with other Tcp solutions.
- NetPacket traffic is appended with a 4-byte value indicating the length of the NetPacket's payload in bytes.

## Tutorial

### Connecting a Tcp Socket

The following shows how to connect a client socket to a server. It connects to localhost on port 1234 with a timeout of 20 seconds.

```C#
TcpSocket client = new TcpSocket();
SocketStatus status = client.Connect("localhost", 1234, 20);

if (status != SocketStatus.Done)
{
    // socket was unable to connect...
}
```

All function calls of the TcpSocket are non-blocking by default, with the exception of Connect with a timeout of greater than 0.

The server side first requires an active TcpListener to accept the incoming client connection.
This connection can then be accepted and returned as a TcpSocket.

```C#
TcpListener listener = new TcpListener();
if (listener.Listen(1234) != SocketStatus.Done)
{
    // an error occured...
}

if (listener.Accept(out TcpSocket clientConnection) == SocketStatus.Done)
{
    // a new connection has been established.
}
```

After the connection has been accepted, messages can be sent to, and read from the connected client.

### Sending and receiving data

The TcpSocket offers many different Send and Receive methods. They support pointers to memory as well as byte arrays.
Lastly, they also support sending and receiving of NetPackets.

Below is an example of sending a NetPacket over the socket.
```C#
NetPacket packet = new NetPacket();
// Write some data to the packet.
packet.WriteDouble(Math.PI);

if (socket.Send(ref packet)!= SocketStatus.Done)
{
    // packet was not sent (entirely).
}

// **Dispose the NetPacket** if the send has completed and it is no longer needed.
```

Below is an example of receiving a NetPacket over the socket.

```C#
NetPacket packet = new NetPacket();
if (socket.Receive(ref packet) == SocketStatus.Done)
{
    // entire packet has been received.
    // **Dispose the NetPacket** if it is no longer needed.
}
```

Data will only be copied to the NetPacket if an entire packet has been received and reassembled.

## NetPacket serialization

The NetPacket object is based on the [BitSerializer](https://github.com/DennisCorvers/BitSerializer) library.

When sending a NetPacket where a status of Partial returns, the send offset is stored in the NetPacket. Simply send the same packet again on the same socket to finish sending the rest of the packet.

A NetPacket can be re-used multiple times, as receiving into an existing NetPacket will simply clear its internal buffer and overwrite it with the new packet.

Just make sure to call Dispose on the NetPacket before it runs out of scope. If you don't, a memory leak will occur.

Please refer to [this example](https://github.com/DennisCorvers/UnityNet/blob/master/UnityNetTest/Packet/ObjectTests.cs) on how to serialize user-defined classes and structs.

## Allocation-free example

The code snippet below shows a use-case where no garbage is allocated during network communication:

```C#
public void Run()
{
    packet.Clear();

    packet.WriteLong(DateTime.Now.Ticks);
    client.Send(ref packet);

    packet.Clear();

    server.Receive(ref packet);

    var currentTicks = packet.ReadLong();
}
```
