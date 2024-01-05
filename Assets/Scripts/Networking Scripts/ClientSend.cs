using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    
    private static void SendTCPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.tcp.SendData(_packet);

    }

    private static void SendUDPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.udp.SendData(_packet);

    }

    #region Packets

    public static void WelcomeReceived()
    {
        Debug.Log("Sending Welcome received");
        using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived)) {

            _packet.Write(Client.instance.myID);
            _packet.Write("Connected to the server");


            //_packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(_packet);
        }
        Debug.Log("Sent");
    }


    public static void PlayerMovement(bool[] _inputs)
    {
        

        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Write(_inputs.Length);
            foreach (bool _input in _inputs)
            {
                _packet.Write(_input);
            }


            _packet.Write(GameManager.players[Client.instance.myID].transform.rotation);

            SendUDPData(_packet);

        }
    }

    public static void PlayerShoot(Vector3 _viewDir)
    {
        using (Packet _packet = new Packet((int) ClientPackets.playerShoot))
        {
            _packet.Write(_viewDir);

            SendTCPData (_packet);
        }
    }
    #endregion

}
