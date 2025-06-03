using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSeat : MonoBehaviour, IPunObservable
{
    
    public bool isOccupied;
    
    public int seatIndex;
    
    public string playerName;

    
    public PhotonView pv;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();

    }
    

    // Method to get isSeated over the network
    [PunRPC]
    public bool GetIsSeatedOverNetwork()
    {
        // Request the value of isSeated from the master client
        return isOccupied;
    }

    // Method to set isSeated over the network using RPC
    [PunRPC]
    public void SetIsSeatedOverNetwork(bool value)
    {
        isOccupied = value;
        pv.RPC("SyncIsOccupied", RpcTarget.All, value);
    }

    [PunRPC]
    private void SyncIsOccupied(bool seated)
    {
        isOccupied = seated;
    }

    // Method to get playerName over the network
    [PunRPC]
    public string GetPlayerNameOverNetwork()
    {
        // Request the playerName from the master client
        return playerName;
    }


    // Method to set playerName over the network using RPC
    [PunRPC]
    public void SetPlayerNameOverNetwork(string name)
    {
        playerName = name;
        pv.RPC("SyncPlayerName", RpcTarget.All, name);
    }

    [PunRPC]
    private void SyncPlayerName(string name)
    {
        playerName = name;
    }



    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isOccupied);
            stream.SendNext(playerName);
        }
        else if (stream.IsReading)
        {
            isOccupied = (bool)stream.ReceiveNext();
            playerName = (string)stream.ReceiveNext();
        }
       
    }
}


