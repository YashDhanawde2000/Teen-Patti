using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomsList : MonoBehaviourPunCallbacks
{
    public GameObject roomButtonPrefab;
    public GameObject contentObj;

    public GameObject[] allRooms;



    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        for (int i = 0; i < allRooms.Length; i++)
        {
            if (allRooms[i] != null)
            {
                Destroy(allRooms[i]);
            }
        }

        allRooms = new GameObject[roomList.Count];


        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].IsOpen && roomList[i].IsVisible && roomList[i].PlayerCount >= 1)
            {
                GameObject roomButton = Instantiate(roomButtonPrefab, Vector3.zero, Quaternion.identity, contentObj.transform);
                roomButton.GetComponent<RoomButton>().roomName.text = roomList[i].Name;

                allRooms[i] = roomButton;
            }
            
        }
    }

}
