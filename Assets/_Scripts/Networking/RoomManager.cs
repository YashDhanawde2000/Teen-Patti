using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    public GameObject playerModelPrefab, playerPrefab;
    public List<Transform> playerSeats;

    public List<GameObject> instantiatedPlayers = new List<GameObject>();

    public GameManager gameManager;
    //public ConnectWallet connectWallet;

    private PhotonView pv;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        //PhotonNetwork.ConnectUsingSettings();
        gameManager = GameManager.Instance;
        //connectWallet = ConnectWallet.Instance;
        StartCoroutine(SpawnPlayerIfSeatAvailable());

    }


    //public override void OnConnectedToMaster()
    //{
    //    base.OnConnectedToMaster();
    //    Debug.Log("Connected to Server...");

    //    PhotonNetwork.JoinLobby();
    //}

    //public override void OnJoinedLobby()
    //{
    //    base.OnJoinedLobby();

    //    Debug.Log("Lobby Joined...");

    //    PhotonNetwork.JoinOrCreateRoom("test1", new RoomOptions { MaxPlayers = 6}, null);
    //}

    //public override void OnPlayerEnteredRoom(Player newPlayer)
    //{
    //    base.OnPlayerEnteredRoom(newPlayer);

        
    //    //Invoke(nameof(CallGMCheck), 30f);
    //    //gameManager.CheckAndStartGame();                          // Use for Multiplayer!!!!!!!!

    //}
    public void CallGMCheck()
    {
        Debug.Log("gm check called");
        gameManager.CheckAndStartGame();
    }


    //public override void OnJoinedRoom()
    //{
    //    base.OnJoinedRoom();
    //    Debug.Log("Room Joined...");

    //    StartCoroutine(SpawnPlayerIfSeatAvailable());

    //    //Debug.Log("gm called");                                   //use only for 1player testing in unity
    //    //Invoke(nameof(CallGMCheck), 5f);

    //}

    private IEnumerator SpawnPlayerIfSeatAvailable()
    {
        for (int i = 0; i < playerSeats.Count; i++)
        {
            GameObject playerSeatObj = playerSeats[i].gameObject;
            PlayerSeat playerSeat = playerSeatObj.GetComponent<PlayerSeat>();

            // Call an RPC to get the isSeated status from the master client
            playerSeat.pv.RPC("GetIsSeatedOverNetwork", RpcTarget.MasterClient);

            yield return new WaitForSecondsRealtime(1f); // Wait for RPC response VERY IMPORTANT!!

            if (!playerSeat.isOccupied)
            {
                GameObject playerModel = PhotonNetwork.Instantiate(playerModelPrefab.name, playerSeat.transform.position, playerSeat.transform.rotation);
                playerModel.SetActive(true);
                // Call an RPC to set isSeated on all clients
                playerSeat.pv.RPC("SetIsSeatedOverNetwork", RpcTarget.AllBuffered, true);
                int playerSeatViewID = playerSeat.pv.ViewID;
                int playerModelViewID = playerModel.GetComponent<PhotonView>().ViewID;
                pv.RPC(nameof(SetParentOverNetwork), RpcTarget.AllBuffered, playerModelViewID, playerSeatViewID);
                //playerModel.transform.parent = playerSeat.transform;
                
                int playerInfoViewID = playerModel.transform.parent.GetComponentInChildren<PlayerInfo>().gameObject.GetComponent<PhotonView>().ViewID;
                pv.RPC(nameof(InstantiatePlayerInfoOverNetwork), RpcTarget.AllBuffered, playerModelViewID, playerInfoViewID);
                yield return new WaitForSeconds(1f); // Wait for RPC response VERY IMPORTANT!!


                //// Ensure unique player name using PhotonNetwork.LocalPlayer.UserId
                playerModel.name = "Player_" + PhotonNetwork.LocalPlayer.UserId;
                PhotonNetwork.LocalPlayer.NickName = playerModel.name;
                playerSeat.pv.RPC("SetPlayerNameOverNetwork", RpcTarget.AllBuffered, playerModel.name);
                Debug.Log(PhotonNetwork.LocalPlayer.NickName);

                PhotonNetwork.CurrentRoom.IsVisible = true;

                break;
            }
            else
            {
                continue;
            }
        }
    }

    // RPC method to instantiate and set up the PlayerInfo object
    [PunRPC]
    private void InstantiatePlayerInfoOverNetwork(int playerModelViewID, int playerInfoViewID)
    {
        GameObject _playerModel = PhotonView.Find(playerModelViewID).gameObject;
        PlayerInfo _playerInfo = PhotonView.Find(playerInfoViewID).gameObject.GetComponent<PlayerInfo>();
        _playerInfo.gameObject.GetComponent<PhotonView>().TransferOwnership(_playerModel.GetComponent<PhotonView>().Owner);
        //Debug.Log(playerInfo.gameObject.GetComponent<PhotonView>().Owner);
        _playerInfo.playerModel = _playerModel;
        _playerInfo.playerController = _playerModel.GetComponent<PlayerController>();
        _playerInfo.paymentInfo = _playerModel.GetComponent<PaymentInfo>();
        _playerInfo.paymentInfo.roomManager = this;
        //walletManager.paymentInfo = _playerInfo.paymentInfo;
        _playerModel.GetComponent<PaymentInfo>().playerInfo = _playerInfo;
        _playerModel.GetComponent<PlayerController>().playerInfo = _playerInfo;

    }

    // RPC method to set the parent transform on all clients
    [PunRPC]
    private void SetParentOverNetwork(int playerViewID, int playerSeatViewID)
    {
        GameObject player = PhotonView.Find(playerViewID).gameObject;
        GameObject playerSeat = PhotonView.Find(playerSeatViewID).gameObject;
        player.transform.parent = playerSeat.transform;
        //Debug.Log(player.transform.parent.name);
    }



    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    base.OnPlayerLeftRoom(otherPlayer);

    //    StartCoroutine(MakeSeatAvailable(otherPlayer));
    //}

    //private IEnumerator MakeSeatAvailable(Player otherPlayer)
    //{
    //    for (int i = 1; i < playerSeats.Count; i++)
    //    {
    //        GameObject playerSeatObj = playerSeats[i].gameObject;
    //        PlayerSeat _playerSeat = playerSeatObj.GetComponent<PlayerSeat>();

            
    //        _playerSeat.pv.RPC("GetPlayerNameOverNetwork", RpcTarget.MasterClient);

    //        yield return new WaitForSecondsRealtime(1f);                                    // Wait for RPC response VERY IMPORTANT!!
            

    //        if (_playerSeat.playerName == otherPlayer.NickName)
    //        {
    //            Debug.Log(_playerSeat.isOccupied);
    //            _playerSeat.pv.RPC("SetIsSeatedOverNetwork", RpcTarget.AllBuffered, false);
    //            yield return new WaitForSecondsRealtime(1f);
    //            _playerSeat.pv.RPC("GetIsSeatedOverNetwork", RpcTarget.MasterClient);

    //            //GameObject _playerModel = _playerSeat.gameObject.GetComponentInChildren<PlayerController>().gameObject;
    //            //PhotonNetwork.Destroy(_playerModel);

    //            PlayerInfo _playerInfo = _playerSeat.gameObject.GetComponentInChildren<PlayerInfo>();
    //            if (_playerInfo != null)
    //            {
    //                if(gameManager.gameInProgress && PhotonNetwork.IsMasterClient)
    //                {
    //                    gameManager.photonView.RPC("FoldPlayer", RpcTarget.All, _playerInfo.PlayerID);
    //                    if (_playerInfo.PlayerID == gameManager.currentPlayerIndex)
    //                    {
    //                        gameManager.photonView.RPC("EndPlayerTurn", RpcTarget.All, 0, _playerInfo.PlayerID);
    //                    }

    //                    gameManager.allPlayers.Remove(_playerInfo);
    //                    gameManager.activePlayers.Remove(_playerInfo);
    //                    gameManager.activePlayerCount = gameManager.activePlayers.Count;
    //                }


    //                _playerInfo.playerModel = null;
    //                _playerInfo.playerController = null;
    //                _playerInfo.paymentInfo = null;

    //            }

    //            break;
    //        }
    //        else
    //        {
    //            continue;
    //        }
    //    }
    //}

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        StartCoroutine(HandlePlayerLeft(otherPlayer));
    }

    private IEnumerator HandlePlayerLeft(Player otherPlayer)
    {
        for (int i = 0; i < playerSeats.Count; i++)
        {
            GameObject playerSeatObj = playerSeats[i].gameObject;
            PlayerSeat _playerSeat = playerSeatObj.GetComponent<PlayerSeat>();

            _playerSeat.pv.RPC("GetPlayerNameOverNetwork", RpcTarget.MasterClient);

            yield return new WaitForSecondsRealtime(0.5f); // Wait for RPC response

            Debug.Log(_playerSeat.playerName);
            Debug.Log(otherPlayer.NickName);

            if (_playerSeat.playerName == otherPlayer.NickName)
            {
                Debug.Log("Player left: " + otherPlayer.NickName);
                //Debug.Log(_playerSeat.isOccupied);

                PlayerInfo _playerInfo = _playerSeat.gameObject.GetComponentInChildren<PlayerInfo>();
                if (_playerInfo != null)
                {
                    // Transfer ownership of this specific PlayerInfo object to the new MasterClient

                    _playerInfo.photonView.TransferOwnership(PhotonNetwork.MasterClient);
                    RoomManager.Instance.photonView.TransferOwnership(PhotonNetwork.MasterClient);
                    gameManager.photonView.TransferOwnership(PhotonNetwork.MasterClient);
                    gameManager.cardDeck.photonView.TransferOwnership(PhotonNetwork.MasterClient);
                    yield return new WaitForSecondsRealtime(1f); // Wait for RPC response

                    Debug.Log(_playerInfo.PlayerID + "  " + gameManager.IsPlayerTurn(_playerInfo));
                    // Handle any game-specific logic if the MasterClient was the current player
                    if (gameManager.IsPlayerTurn(_playerInfo))
                    {
                        // Remove the player from lists but do not fold or end turn 
                        gameManager.photonView.RPC(nameof(gameManager.FoldPlayer), RpcTarget.All, _playerInfo.PlayerID);
                        gameManager.photonView.RPC(nameof(gameManager.EndPlayerTurn), RpcTarget.All, 0, _playerInfo.PlayerID);
                        yield return new WaitForSecondsRealtime(1f); // Wait for RPC response
                    }
                    else
                    {
                        gameManager.photonView.RPC(nameof(gameManager.FoldPlayer), RpcTarget.All, _playerInfo.PlayerID);
                        //gameManager.photonView.RPC(nameof(gameManager.SetPlayerState), RpcTarget.All, _playerInfo.PlayerID, PlayerState.Waiting, _playerInfo.isBlind);
                        yield return new WaitForSecondsRealtime(1f); // Wait for RPC response
                    }

                    //gameManager.allPlayers.Remove(_playerInfo);
                    yield return new WaitForSecondsRealtime(15f); // Wait for RPC response
                    // Clean up references
                    PhotonNetwork.Destroy(_playerInfo.playerModel.gameObject);
                    _playerInfo.playerModel = null;
                    _playerInfo.playerController = null;
                    _playerInfo.paymentInfo = null;
                    _playerSeat.pv.RPC("SetIsSeatedOverNetwork", RpcTarget.AllBuffered, false);

                    // Do not destroy the PlayerInfo gameObject

                }

                break;
            }
            else 
            { 
                Debug.Log("PlayerSeat Not Found "); 
                continue;
            }
        }
    }

    //public override void OnMasterClientSwitched(Player newMasterClient)
    //{
    //    base.OnMasterClientSwitched(newMasterClient);

    //    // Handle any game-specific logic if the MasterClient was the current player
    //    if (gameManager.gameInProgress)
    //    {
    //        foreach (PlayerInfo playerInfo in gameManager.activePlayers)
    //        {
    //            if (playerInfo.PlayerID == gameManager.currentPlayerIndex)
    //            {
    //                gameManager.photonView.RPC("EndPlayerTurn", RpcTarget.All, 0, playerInfo.PlayerID);
    //            }
    //        }
    //    }
    //}

}
