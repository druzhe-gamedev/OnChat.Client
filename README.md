# OnChat.Client
OnChat.Client is a CLI application for communicating with [OnChat.Server](https://github.com/druzhe-gamedev/OnChat.Server).

Application uses Microsoft Hosting and DI for project orchestration.

Client uses TcpClient and NetworkStream for serializing and reading network data. Request API is implementend for sending and receiving packets from server

```csharp
IResponse response = await ChatConnection.Request(packet);
```

For encryption, AES.GCM with ECDH with keys derivation is used (asymmetric encryption). To communicate with other clients, you must load their public keys first. Only two-participant chat is implemented now.

[OnChat.Shared](https://github.com/druzhe-gamedev/OnChat.Shared) is a common repo for both [OnChat.Server](https://github.com/druzhe-gamedev/OnChat.Server) and client where packets and encryption are present. For now, there's ReceiveMessageHandler, that implements PacketHandler<TPacket> abstract class, that reacts when receiveing a message

System.CommandLine package is used for parsing command line arguments.

There are 7 commands available for now:
- help. Shows information about all commands
- reg. Register user
  
  Arguments
  - -u Username  -m  E-mail -p Password -a Age
- log. Authenticate in OnChat
  
  Arguments
  - -l Login (username or email) -p Password
- msg. Send message to user by his id
  
  Arguments
  - -r Receiver guid -m Message
- users. Load users with their public key's info
- key-gen. Create key-pair
- load-msgs. Load messages from chat with some user by his id
  
  Arguments
  - -i Chat participant id -p Page (starting from 1) -q Messages per page
