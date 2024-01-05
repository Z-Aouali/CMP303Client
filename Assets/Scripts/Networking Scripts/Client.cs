using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using UnityEngine.SceneManagement;
using System.Data;

public class Client : MonoBehaviour
{
    public static Client instance;
    
    public static int dataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 26950;
    public int myID = 0;
    public TCP tcp;
    public UDP udp;


    public int _serverTick;

    public int ServerTick
    {
        get => _serverTick;
        set { _serverTick = value;
            InterpolationTick = value - TicksBetweenPosUpdates;
            }
    }

    public int InterpolationTick;
    private int _ticksBetweenPosUpdates = 5;
    public int TicksBetweenPosUpdates
    {
        get => _ticksBetweenPosUpdates;
        set { _ticksBetweenPosUpdates = value; InterpolationTick = (ServerTick - value); }
    }

    [SerializeField] private int tickDivergence = 1;


    private bool isConnected = false;

    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void Awake()
    {

        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object");
            Destroy(this);
        }
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();

        ServerTick = 4;
    
    }

    private void FixedUpdate()
    {
        ServerTick++;
    }


    private void OnApplicationQuit()
    {
        Disconnect();
    }

    public void ConnectToServer()
    {
        Debug.Log("Connecting to server");
        InitializeClientData();

        isConnected = true;

        //Debug.Log("Is connected set to true");

        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;



        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];


            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);

            //Debug.Log("TCP connected");
        }

        private void ConnectCallback(IAsyncResult _result)
        {
            //Debug.Log("TCP Connect callback reached");
            socket.EndConnect(_result);
            //Debug.Log("TCP Socket end connection completed");
            if (!socket.Connected)
            {
                //Debug.Log("returning from TCP connect callback, no socket connection");
                return;
            }

            stream = socket.GetStream();
            //Debug.Log("Socket get stream completed");
            receivedData = new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            //Debug.Log("TCP Connect callback completed okay");
        }

        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    //Debug.Log("About to send TCP data");
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    //Debug.Log("Sent TCP Data");
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                //Debug.Log("TCP Receive callback reached");
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    instance.Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
            }
        }

        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;

            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet);
                    }
                });

                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true;
            }

            return false;
        }

        private void Disconnect()
        {

            instance.Disconnect();

            stream = null;
            receiveBuffer = null;
            receivedData = null;
            socket = null;



        }

    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;


        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int _localPort)
        {
            //Debug.Log("UDP connecting");
            socket = new UdpClient(_localPort);
            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet _packet = new Packet())
            {
                //Debug.Log("UDP Connection good, sending packet");
                SendData(_packet);

            }

        }


        public void SendData(Packet _packet)
        {

            try
            {
                //Debug.Log("Creating UDP packet with ID");
                _packet.InsertInt(instance.myID);

                if(socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error handling UDP data: {ex}");
                Disconnect();
            }
        }

        public void ReceiveCallback(IAsyncResult _result)
        {

            try
            {
                //Debug.Log("UDP receive Callback reached");
                byte[] data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if(data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                HandleData(data);
            }

            catch
            {
                Disconnect();
            }
        }

        private void HandleData(byte[] data)
        {

            using (Packet _packet = new Packet(data))
            {
                int _packetLength = _packet.ReadInt();
                data = _packet.ReadBytes(_packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using(Packet _packet = new Packet(data))
                {
                    int _packetID = _packet.ReadInt();
                    packetHandlers[_packetID](_packet);
                }
            }


            );
        }
        
        private void Disconnect()
        {
            instance.Disconnect();

            

            endPoint = null;
            socket = null;

            
        }



    }

    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Disconnected from server");

            UIManager.instance.startMenu.SetActive(true);
            UIManager.instance.usernameField.interactable = true;

            SceneManager.LoadScene(0);
            //Debug.Log("Changed scene ?");

        }
    }

    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandler.Welcome },
            {(int)ServerPackets.spawnPlayer, ClientHandler.SpawnPlayer },
            { (int)ServerPackets.playerPosition, ClientHandler.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandler.PlayerRotation },
            { (int)ServerPackets.playerDisconnected, ClientHandler.PlayerDisconnected },
            { (int)ServerPackets.playerHealth, ClientHandler.PlayerHealth },
            { (int)ServerPackets.playerRespawn, ClientHandler.PlayerRespawn },
            {(int)ServerPackets.playerSync, ClientHandler.Sync }
        };
        Debug.Log("Initialized packets.");
    }

   public void SetTick(int _tick)
   {
       if(Mathf.Abs(ServerTick - _tick) > tickDivergence)
       {
           Debug.Log($"Tick divergence too high, setting tick to {_tick} from {ServerTick}");
           ServerTick = _tick;
       }
   }
}