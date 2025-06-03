using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomButton : MonoBehaviour
{
    public TMP_Text roomName;

    public void JoinSelectedRoom()
    {
        if(LobbyManager.Instance != null)
        {
            LobbyManager.Instance.GetComponent<LobbyManager>().JoinRoomInList(roomName.text);
        }
    }
}
