using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviourPunCallbacks
{
    public string LobbySceneName = "Lobby";
    public string pve_SceneName = "Teenpatti_AI";
    public string demoSceneName = "Teenpatti_Demo";

    public bool playWithBots = false;


    public void PlayWithFriends()
    {
        playWithBots = false;
        //PhotonNetwork.OfflineMode = false;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void PlayWithBot()
    {
        playWithBots = true;
        //PhotonNetwork.OfflineMode = false;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void PlayDemo()
    {
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 1 });
    }




    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        if(!playWithBots)
        {

            PhotonNetwork.LoadLevel(LobbySceneName);
        }
        else
        {
            PhotonNetwork.CreateRoom("aiTest", new RoomOptions { MaxPlayers = 1 }, null);
        }
        
    }

    public override void OnJoinedRoom()
    {
        if(playWithBots)
        {
            PhotonNetwork.LoadLevel(pve_SceneName);
        }

        if (PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.LoadLevel(demoSceneName);
        }

    }

}
