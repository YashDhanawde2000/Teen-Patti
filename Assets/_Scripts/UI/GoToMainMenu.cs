using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToMainMenu : MonoBehaviourPunCallbacks
{
    public string MainMenuSceneName = "MainMenu";

    public void ExitToMainMenu()
    {
        if(GameManager.Instance != null && RoomManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
            Destroy(RoomManager.Instance.gameObject);
        }

        PhotonNetwork.Disconnect();
        LoadMainMenu();
        
    }

    //public override void OnLeftRoom()
    //{
    //    if (PhotonNetwork.InLobby)
    //    {
    //        PhotonNetwork.LeaveLobby();
    //    }
    //    else
    //    {
    //        PhotonNetwork.Disconnect();
    //        LoadMainMenu();
    //    }
    //}

    //public override void OnLeftLobby()
    //{
    //    PhotonNetwork.Disconnect();
    //    LoadMainMenu();
    //}

    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //    LoadMainMenu();
    //}

    private void LoadMainMenu()
    {
        PhotonNetwork.LoadLevel(MainMenuSceneName);

    }
}
