using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance;


    public TMP_InputField createRoomInputField;
    public TMP_InputField joinRoomInputField;

    public string teenpattiMP_SceneName;
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(createRoomInputField.text, new RoomOptions { MaxPlayers = 6 , CleanupCacheOnLeave = false, IsVisible = false },TypedLobby.Default, null);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(joinRoomInputField.text);
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomOrCreateRoom( null, 6);
    }

    public void JoinRoomInList(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }    

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel(teenpattiMP_SceneName);
        //SceneManager.LoadScene(teenpattiMP_SceneName);
    }

}
