using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Photon.Pun.Demo.PunBasics;
using System.Linq;


public enum GameState
{
    WaitingForPlayers,
    Playing
}

public enum PlayerState
{
    Waiting,
    BlindPlay,
    SeenPlay,
    Showing
}

public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameManager Instance;
    public bool isPVE = false;
    public bool hasOwnerLeft;

    [Header("Money Variables")]
    [SerializeField] private int initialPot = 10;
    public int currentPot;
    //public int stakeAmount;
    [SerializeField] private int stakeMultiplier = 1;
    public int currentRound = 1;

    [Header("Gameplay Variables")]
    [SerializeField] private GameState gameState = GameState.WaitingForPlayers;
    [SerializeField] public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    [SerializeField] public List<PlayerInfo> activePlayers = new List<PlayerInfo>();
    [SerializeField] public int currentPlayerIndex = 0;
    [SerializeField] private float turnTime = 10f;
    private Coroutine playerTurnCoroutine;
    public bool gameInProgress = false;
    [SerializeField] public int activePlayerCount;

    [Header("Script References")]
    [SerializeField] public CardDeck cardDeck;

    [Header("Object References")]
    public GameObject commonCanvas;
    public TMP_Text currentPotDisplay;
    public TMP_Text currentRoundDisplay;
    public TMP_Text playerActionDisplay;

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
    

    private void FixedUpdate()
    {

        if (commonCanvas.activeInHierarchy)
        {
            currentPotDisplay.text = $" Current Pot: \n {Instance.currentPot}";
            currentRoundDisplay.text = $" Round: {Instance.currentRound}";
        }
        
    }

    [PunRPC]
    public void CheckAndStartGame()
    {
        if (!isPVE)
        {
            Debug.Log(gameInProgress + PhotonNetwork.CurrentRoom.PlayerCount.ToString() + PhotonNetwork.IsMasterClient);
            if (!gameInProgress && PhotonNetwork.CurrentRoom.PlayerCount >= 2 && PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Player count check passed");
                gameInProgress = true;
                photonView.RPC(nameof(StartNewGame), RpcTarget.MasterClient);
            }
            else { Debug.Log("Player count check failed"); }
            //if (!gameInProgress && PhotonNetwork.IsMasterClient)
            //{
            //    Debug.Log("Player count check passed");
            //    gameInProgress = true;
            //    photonView.RPC(nameof(StartNewGame), RpcTarget.MasterClient);
            //}
        }
        else
        {
            if (!gameInProgress && PhotonNetwork.CurrentRoom.PlayerCount >= 1 && PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Player count check passed");
                
                photonView.RPC(nameof(StartNewGame), RpcTarget.MasterClient);
            }
        }

    }

    [PunRPC]
    IEnumerator StartNewGame()
    {

        yield return new WaitForSeconds(6f);
        InitializeGame();

        yield return new WaitForSeconds(8f); // Small delay before starting
        StartNewRound();
    }

    void InitializeGame()
    {
        //commonCanvas.SetActive(true);
        photonView.RPC(nameof(RPC_ToggleCanvas), RpcTarget.All, true);

        activePlayers.Clear();
        foreach (PlayerInfo player in allPlayers)
        {
            if (player.playerSeat.isOccupied)
            {
                activePlayers.Add(player);
            }
        }
        activePlayerCount = activePlayers.Count;
        currentPlayerIndex = 0;
        currentPot = 0;
        currentRound = 1;
        stakeMultiplier = 1;
        // Sync active players across all clients
        int[] activePlayerIds = activePlayers.Select(player => player.PlayerID).ToArray();
        photonView.RPC(nameof(SyncActivePlayers), RpcTarget.All, activePlayerIds);

        foreach (PlayerInfo player in activePlayers)
        {

            player.isBlind = true;
            photonView.RPC(nameof(SetPlayerState), RpcTarget.All, player.PlayerID, PlayerState.BlindPlay, player.isBlind);
            //player.userBalance -= initialPot;

            player.stakeAmount = initialPot;
            photonView.RPC(nameof(RPC_UpdateStakeAmount), RpcTarget.All, player.PlayerID, player.stakeAmount);

            if (player.userBalance >= initialPot)
            {
                player.paymentInfo.pv.RPC("PlaceBetRequest", RpcTarget.All, initialPot);
                currentPot += initialPot;
                photonView.RPC(nameof(RPC_UpdatePot), RpcTarget.All, currentPot);
                //photonView.RPC(nameof(RPC_UpdateBalance), RpcTarget.All, player.PlayerID, player.userBalance);
            }
            else
            {
                photonView.RPC(nameof(FoldPlayer), RpcTarget.All, player.PlayerID);
                string playerActionText = player.paymentInfo.userNameHud.text + "has Insufficient Balance to Play!";
                photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
            }


        }
        cardDeck.CallInitializeDeck();
    }
    [PunRPC]
    void SyncActivePlayers(int[] playerIds)
    {
        activePlayers.Clear();
        foreach (int id in playerIds)
        {
            PlayerInfo player = allPlayers.Find(p => p.PlayerID == id);
            if (player != null)
            {
                activePlayers.Add(player);
            }
        }
    }

    void StartNewRound()
    {
        StartCoroutine(InitializeRound());
    }

    IEnumerator InitializeRound()
    {
        //if (currentRound != 1)
        //{
        //    foreach (PlayerInfo player in activePlayers)
        //    {
        //        player.stakeAmount *= 2;
        //        photonView.RPC(nameof(RPC_UpdateStakeAmount), RpcTarget.All, player.PlayerID, player.stakeAmount);
        //    }
        //}

        yield return new WaitForSeconds(2);
        gameInProgress = true;
        photonView.RPC(nameof(StartPlayerTurn), RpcTarget.All, currentPlayerIndex);
        yield return null;
    }

    [PunRPC]
    void StartPlayerTurn(int playerIndex)
    {
        // Only Master Client should handle turn transitions
        if (!PhotonNetwork.IsMasterClient) return; // Only Master Client should handle turn transitions)
        Debug.Log("StartPlayerTurn called for playerIndex: " + playerIndex);
        if (playerTurnCoroutine != null)
        {
            StopCoroutine(playerTurnCoroutine);
        }

        if (currentPlayerIndex < activePlayers.Count)
        {
            if (activePlayers.Count > 0)
            {
                PlayerInfo currentPlayer = activePlayers[playerIndex];

                PlayerInfo previousPlayer = activePlayers[((playerIndex - 1) + activePlayers.Count) % activePlayers.Count];

                if (previousPlayer.isRequestingSideshow)
                {
                    StartCoroutine(SideshowCoroutine(previousPlayer, currentPlayer));
                }
                else
                {
                    playerTurnCoroutine = StartCoroutine(PlayerTurnCoroutine(currentPlayer));
                }
            }
            
        }
        else 
        {
            NextRound();
        }
    }

    IEnumerator PlayerTurnCoroutine(PlayerInfo currentPlayer)
    {
        photonView.RPC(nameof(RPC_TogglePlayerCanvas), RpcTarget.All, currentPlayer.PlayerID, true);
        photonView.RPC(nameof(RPC_TogglePlayerTimer), RpcTarget.All, currentPlayer.PlayerID, true);
        yield return new WaitForSeconds(0.5f);

        float turnStartTime = Time.time;
        bool actionTaken = false;
        //int stakeAmount = initialPot * stakeMultiplier;

        while (!actionTaken && (Time.time - turnStartTime <= turnTime))
        {
            //if (currentPlayer.gameObject.GetComponent<PhotonView>().IsMine)
            //{
            //    //if (Input.GetKeyDown(KeyCode.Space) && currentPlayer.isBlind)
            //    //{
            //    //    photonView.RPC("SetPlayerState", RpcTarget.All, currentPlayer.PlayerID, PlayerState.SeenPlay);
            //    //    currentPlayer.isBlind = false;
            //    //    stakeAmount *= 2;
            //    //    actionTaken = true;
            //    //    Debug.Log("Player " + currentPlayer.PlayerID + " has Seen");
            //    //}
            //    //else if (Input.GetKeyDown(KeyCode.C) && currentPlayer.userBalance >= initialPot * stakeMultiplier)
            //    //{
            //    //    actionTaken = true;
            //    //    Debug.Log("Player " + currentPlayer.PlayerID + " has Called");
            //    //    photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, stakeAmount, currentPlayer.PlayerID);
            //    //}
            //    //else if (Input.GetKeyDown(KeyCode.R) && currentPlayer.userBalance >= initialPot * stakeMultiplier * 2)
            //    //{
            //    //    stakeAmount *= 2;
            //    //    actionTaken = true;
            //    //    Debug.Log("Player " + currentPlayer.PlayerID + " has Raised");
            //    //    photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, stakeAmount, currentPlayer.PlayerID);
            //    //}
            //}

            yield return null;
        }

        if (!actionTaken)
        {
            Debug.Log("Turn time exceeded without action. Folding player.");
            photonView.RPC(nameof(FoldPlayer), RpcTarget.All, currentPlayer.PlayerID);
            photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, 0, currentPlayer.PlayerID);
        }
    }

    IEnumerator SideshowCoroutine(PlayerInfo requestingPlayer, PlayerInfo currentPlayer)
    {
        
        photonView.RPC(nameof(RPC_DisplaySideshowRequest), RpcTarget.All, requestingPlayer.PlayerID, currentPlayer.PlayerID);
        
        //yield return new WaitForSeconds(8f); // Wait for player to respond
        yield return new WaitForSeconds(0.5f);
        float turnStartTime = Time.time;
        bool actionTaken = false;


        while (!actionTaken && (Time.time - turnStartTime <= turnTime ))
        {
            yield return null;
        }

        if (!actionTaken && currentPlayer.isRespondingToSideshow)
        {
            photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, currentPlayer.paymentInfo.userNameHud.text + " did not respond to the Sideshow request");
            photonView.RPC(nameof(RPC_DeclineSideshow), RpcTarget.All, requestingPlayer.PlayerID, currentPlayer.PlayerID);
        }

        

    }
    public void ProcessPlayerAction(PlayerAction action, int playerId)
    {
        PlayerInfo currentPlayer = allPlayers.Find(p => p.PlayerID == playerId);
        PlayerInfo previousPlayer = null;
        if (activePlayers.Count > 0)
        {
            previousPlayer = activePlayers[(currentPlayerIndex - 1 + activePlayers.Count) % activePlayers.Count];
        }
        
        if (currentPlayer == null) return;
        string playerUsername = currentPlayer.paymentInfo.userNameHud.text; 

        string playerActionText;

        //stakeAmount = initialPot * stakeMultiplier;
        //if (!currentPlayer.isBlind) { stakeAmount *= 2; }                                       // Double the Stake if Player has Seen.

        switch (action)
        {
            case PlayerAction.SeenPlay:
                if (currentPlayer.isBlind && currentPlayer.userBalance >= currentPlayer.stakeAmount * 2)
                {
                    currentPlayer.isBlind = false;
                    currentPlayer.stakeAmount *= 2;
                    photonView.RPC(nameof(RPC_UpdateStakeAmount), RpcTarget.All, currentPlayer.PlayerID, currentPlayer.stakeAmount);
                    photonView.RPC(nameof(SetPlayerState), RpcTarget.All, currentPlayer.PlayerID, PlayerState.SeenPlay, currentPlayer.isBlind);
                    //Debug.Log("Player " + playerId + " has Seen");
                    playerActionText = playerUsername + " has Seen";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                else if (!currentPlayer.isBlind)
                {
                    //Debug.Log(" Insufficient Balance or Already Seen");
                    playerActionText = playerUsername + " has Already Seen";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                else if (currentPlayer.isBlind && currentPlayer.userBalance < currentPlayer.stakeAmount * 2)
                {
                    playerActionText = playerUsername+ " has Insufficient Balance to See";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                break;
            case PlayerAction.Call:
                if (currentPlayer.userBalance >= currentPlayer.stakeAmount)
                {
                    //Debug.Log("Player " + playerId + " has Called");
                    if (currentPlayer.isBlind)
                    {
                        playerActionText = playerUsername + " has played Blind";
                        photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                    }
                    else
                    {
                        playerActionText = playerUsername + " has played Chaal";
                        photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                    }
                    
                    photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, currentPlayer.stakeAmount, currentPlayer.PlayerID);
                    //currentPlayer.paymentInfo.PlaceBetRequest(stakeAmount);
                }
                else
                {
                    playerActionText = playerUsername + " has Insufficient Balance to Call";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                break;
            case PlayerAction.Raise:
                //Debug.Log($"Balance For {currentPlayer.PlayerID} is : {currentPlayer.userBalance} before Check");
                if (currentPlayer.userBalance >= currentPlayer.stakeAmount * 2)
                {
                    foreach (PlayerInfo player in activePlayers)
                    {
                        //Debug.Log($"Balance For {currentPlayer.PlayerID} is : {currentPlayer.userBalance} after Check");
                        //Debug.Log($"StakeAmt {stakeAmount} before update for Player {player.PlayerID} ");
                        player.stakeAmount *= 2;
                        photonView.RPC(nameof(RPC_UpdateStakeAmount), RpcTarget.All, player.PlayerID, player.stakeAmount);
                    }
                    stakeMultiplier *= 2;
                    //Debug.Log("Player " + playerId + " has Raised");
                    playerActionText = playerUsername + " has Raised";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                    photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, currentPlayer.stakeAmount, currentPlayer.PlayerID);
                    //currentPlayer.paymentInfo.PlaceBetRequest(stakeAmount);
                }
                else
                {
                    playerActionText = playerUsername + " has Insufficient Balance to Raise";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                break;
            case PlayerAction.Fold:
                {
                    //Debug.Log("Player " + playerId + " has Folded");
                    playerActionText = playerUsername + " has Packed";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                    photonView.RPC(nameof(FoldPlayer), RpcTarget.All, currentPlayer.PlayerID);
                    photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, 0, currentPlayer.PlayerID);
                }
                break;
            case PlayerAction.Sideshow:
                if (currentPlayer.userBalance >= currentPlayer.stakeAmount && !currentPlayer.isBlind)
                {
                    currentPlayer.isRequestingSideshow = true;
                    photonView.RPC(nameof(RPC_RequestSideshow), RpcTarget.All, currentPlayer.PlayerID);
                    playerActionText = playerUsername + " has requested a Sideshow";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                    photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, currentPlayer.stakeAmount, currentPlayer.PlayerID);
                }
                else if (currentPlayer.isBlind)
                {
                    playerActionText = "Cannot request Sideshow if User is Blind";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                else if (currentPlayer.userBalance < currentPlayer.stakeAmount && !currentPlayer.isBlind)
                {
                    playerActionText = playerUsername + " has Insufficient Balance to request Sideshow";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                break;
            case PlayerAction.Accept:
                if (currentPlayer.isRespondingToSideshow && previousPlayer != null)
                {
                    photonView.RPC(nameof(RPC_AcceptSideshow), RpcTarget.All, previousPlayer.PlayerID, playerId);
                    playerActionText = playerUsername + " has Accepted the Sideshow";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                break;
            case PlayerAction.Decline:
                if (currentPlayer.isRespondingToSideshow && previousPlayer != null)
                {
                    photonView.RPC(nameof(RPC_DeclineSideshow), RpcTarget.All, previousPlayer.PlayerID, playerId);
                    playerActionText = playerUsername + " has Declined the Sideshow";
                    photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);
                }
                break;
        }
 
    }

    [PunRPC]
    public void EndPlayerTurn(int stakeAmt, int playerId)
    {
        if (!PhotonNetwork.IsMasterClient) return; // Only Master Client should handle turn transitions

        Debug.Log("EndPlayerTurn called for playerId: " + playerId + " with stakeAmount: " + stakeAmt);
        if (playerTurnCoroutine != null)
        {
            StopCoroutine(playerTurnCoroutine);
        }

        PlayerInfo currentPlayer = allPlayers.Find(p => p.PlayerID == playerId);
        if (currentPlayer != null)
        {
            currentPot += stakeAmt;
            photonView.RPC(nameof(RPC_UpdatePot), RpcTarget.All, currentPot);
            //currentPlayer.userBalance -= stakeAmount;
            if ( stakeAmt != 0 )
            {
                currentPlayer.paymentInfo.pv.RPC("PlaceBetRequest", RpcTarget.All, stakeAmt);
            }
            

            photonView.RPC(nameof(RPC_TogglePlayerTimer), RpcTarget.All, currentPlayer.PlayerID, false);
            photonView.RPC(nameof(RPC_TogglePlayerCanvas), RpcTarget.All, currentPlayer.PlayerID, false);
            photonView.RPC(nameof(RPC_ToggleSideShowUI), RpcTarget.All, currentPlayer.PlayerID, false);
            //photonView.RPC(nameof(RPC_UpdateBalance), RpcTarget.All, currentPlayer.PlayerID, currentPlayer.userBalance);

            currentPlayerIndex++;

            if (currentPlayerIndex < activePlayers.Count)
            {
                photonView.RPC(nameof(StartPlayerTurn), RpcTarget.All, currentPlayerIndex);
                //Debug.Log("Starting New Turn! " + currentPlayerIndex);
            }

            else
            {
                //photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, "Starting New Round!");
                currentPlayerIndex = 0;
                //Debug.Log("Starting New Round!" + currentPlayerIndex);
                Invoke(nameof(NextRound),2f);
            }
        }
    }

    void NextRound()
    {
        if (!PhotonNetwork.IsMasterClient) return; // Only Master Client should handle round transitions

        activePlayers.RemoveAll(players => GetPlayerState(players) == PlayerState.Waiting);
        activePlayerCount = activePlayers.Count;
        cardDeck.playerHands.RemoveAll(playerHand => GetPlayerState(playerHand.gameObject.GetComponent<PlayerInfo>()) == PlayerState.Waiting);


        DetermineWinnerAndStartNewGame();

 
    }

    [PunRPC]
    void RPC_RequestSideshow (int playerId)
    {
        PlayerInfo currentPlayer = allPlayers.Find(p => p.PlayerID == playerId);
        if ( currentPlayer != null)
        {
            currentPlayer.isRequestingSideshow = true;
        }
    }

    [PunRPC]
    void RPC_DisplaySideshowRequest(int requestingPlayerId, int currentPlayerId)
    {
        // Show UI to currentPlayer to accept or decline sideshow
        PlayerInfo currentPlayer = allPlayers.Find(p => p.PlayerID == currentPlayerId);
        PlayerInfo prevPlayer = allPlayers.Find(p => p.PlayerID == requestingPlayerId);
        if (currentPlayer != null && prevPlayer != null)
        {
            currentPlayer.isRespondingToSideshow = true;
            //currentPlayer.playerController.sideshowUI.SetActive(true);
            photonView.RPC(nameof(RPC_ToggleSideShowUI), RpcTarget.All, currentPlayer.PlayerID, true);
            currentPlayer.playerController.sideshowText.text = $"{prevPlayer.paymentInfo.userNameHud.text} has requested a Sideshow with You!"; 
        } 
    }

    [PunRPC]
    void RPC_AcceptSideshow(int requestingPlayerId, int currentPlayerId)
    {

        PlayerInfo currentPlayer = allPlayers.Find(p => p.PlayerID == currentPlayerId);
        PlayerInfo prevPlayer = allPlayers.Find(p => p.PlayerID == requestingPlayerId);
        if (currentPlayer != null && prevPlayer != null)
        {
            string playerActionText;
            

            currentPlayer.isRespondingToSideshow = false;
            prevPlayer.isRequestingSideshow = false;
            //currentPlayer.playerController.sideshowUI.SetActive(false);
            photonView.RPC(nameof(RPC_ToggleSideShowUI), RpcTarget.All, currentPlayer.PlayerID, false);


            if (!PhotonNetwork.IsMasterClient) return;

            List<PlayerHand> playerHands = new List<PlayerHand>();
            playerHands.Add(prevPlayer.playerHand);
            playerHands.Add(currentPlayer.playerHand);
            PlayerHand winningHand = cardDeck.FindWinningHand(playerHands);
            

            if (winningHand == prevPlayer.playerHand)
            {
                playerActionText = prevPlayer.paymentInfo.userNameHud.text + " has Won the Sideshow";
                photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);

                photonView.RPC(nameof(FoldPlayer), RpcTarget.All, currentPlayer.PlayerID);
                //cardDeck.playerHands.Remove(currentPlayer.playerHand);
                //activePlayers.Remove(currentPlayer);
                //activePlayerCount = activePlayers.Count;
                photonView.RPC(nameof(EndPlayerTurn), RpcTarget.All, 0, currentPlayer.PlayerID);

            }
            else if (winningHand == currentPlayer.playerHand) 
            {
                playerActionText = currentPlayer.paymentInfo.userNameHud.text + " has Won the Sideshow";
                photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, playerActionText);

                photonView.RPC(nameof(FoldPlayer), RpcTarget.All, prevPlayer.PlayerID);
                //cardDeck.playerHands.Remove(prevPlayer.playerHand);
                //activePlayers.Remove(prevPlayer);
                //activePlayerCount = activePlayers.Count;
                //if (currentPlayerIndex < activePlayers.Count)
                //{
                //    playerTurnCoroutine = StartCoroutine(PlayerTurnCoroutine(currentPlayer));
                //}
                //else
                //{
                //    NextRound();
                //}
                

                photonView.RPC(nameof(StartPlayerTurn), RpcTarget.All, currentPlayerIndex);
            }
        }
    }

    [PunRPC]
    void RPC_DeclineSideshow(int requestingPlayerId, int currentPlayerId)
    {
        PlayerInfo currentPlayer = allPlayers.Find(p => p.PlayerID == currentPlayerId);
        PlayerInfo prevPlayer = allPlayers.Find(p => p.PlayerID == requestingPlayerId);
        if (currentPlayer != null && prevPlayer != null)
        {
            currentPlayer.isRespondingToSideshow = false;
            prevPlayer.isRequestingSideshow = false;
            //currentPlayer.playerController.sideshowUI.SetActive(false);
            photonView.RPC(nameof(RPC_ToggleSideShowUI), RpcTarget.All, currentPlayer.PlayerID, false);

            if (!PhotonNetwork.IsMasterClient) return;
            //if (currentPlayerIndex < activePlayers.Count)
            //{
            //    playerTurnCoroutine = StartCoroutine(PlayerTurnCoroutine(currentPlayer));
            //}
            //else
            //{
            //    NextRound();
            //}
            //playerTurnCoroutine = StartCoroutine(PlayerTurnCoroutine(currentPlayer));
            photonView.RPC(nameof(StartPlayerTurn), RpcTarget.All, currentPlayerIndex);
        }
    }


    [PunRPC]
    void DetermineWinnerAndStartNewGame()
    {
        if (!PhotonNetwork.IsMasterClient) return; // Only Master Client should handle game end
        

        if (activePlayerCount < 2)
        {
            PlayerInfo winner = cardDeck.playerHands[0].gameObject.GetComponent<PlayerInfo>();

            // Give Winning Card PhotonView ID of Winning Cards to the RPC
            int wc1 = cardDeck.playerHands[0].playerCards[0].GetComponent<PhotonView>().ViewID;
            int wc2 = cardDeck.playerHands[0].playerCards[1].GetComponent<PhotonView>().ViewID;
            int wc3 = cardDeck.playerHands[0]   .playerCards[2].GetComponent<PhotonView>().ViewID;
            string winnerPopupText = $"The winner is : {winner.paymentInfo.userNameHud.text}";
            cardDeck.photonView.RPC(nameof(cardDeck.RPC_ShowWinner), RpcTarget.All, winnerPopupText, wc1, wc2, wc3);                      //, cardDeck.winningCards
            winner.paymentInfo.pv.RPC(nameof(winner.paymentInfo.WinGameRequest), RpcTarget.All, GameManager.Instance.currentPot);

            Instance.gameInProgress = false;
            //photonView.RPC(nameof(CheckAndStartGame), RpcTarget.MasterClient);
            if (!hasOwnerLeft)
            {
                photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, "Starting New Game!");
                Instance.Invoke(nameof(Instance.CheckAndStartGame), 5f);
                //Instance.CheckAndStartGame();
                //photonView.RPC(nameof(StartNewGame), RpcTarget.MasterClient);
            }
            else
            {
                photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, "Room Owner Left! Create new room");
                // Close the room so no new players can join
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                // Call RPC to notify all players to leave the room
                photonView.RPC(nameof(DisconnectEverybody), RpcTarget.All);
            }


        }
        else if (currentPot >= (initialPot * 1024))
        {
            cardDeck.DetermineWinner();
            PlayerInfo winner = cardDeck.playerHands[0].gameObject.GetComponent<PlayerInfo>();
            winner.paymentInfo.pv.RPC( nameof (winner.paymentInfo.WinGameRequest), RpcTarget.All, currentPot);
            //activePlayers[0].paymentInfo.pv.RPC("WinGameRequest", RpcTarget.All, currentPot);


            Instance.gameInProgress = false;
            if (!hasOwnerLeft)
            {
                //photonView.RPC(nameof(CheckAndStartGame), RpcTarget.MasterClient);
                photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, "Starting New Game!");
                Instance.Invoke(nameof(Instance.CheckAndStartGame), 5f);
                //Instance.CheckAndStartGame();
                //photonView.RPC(nameof(StartNewGame), RpcTarget.MasterClient);
            }
            else
            {
                photonView.RPC(nameof(RPC_PlayerActionPopup), RpcTarget.All, "Room Owner Left! Create new room");
                // Close the room so no new players can join
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                // Call RPC to notify all players to leave the room
                photonView.RPC(nameof(DisconnectEverybody), RpcTarget.All);
            }

        }
        else
        {
            stakeMultiplier *= 2;
            currentRound++;
            currentPlayerIndex = 0;
            StartNewRound();
        }
    }

    [PunRPC]
    public void FoldPlayer(int playerId)
    {
        PlayerInfo player = allPlayers.Find(p => p.PlayerID == playerId);
        if (player != null)
        {
            //cardDeck.playerHands.Remove(player.playerHand);
            activePlayers.Remove(player);
            Debug.Log(activePlayers.Count);
            activePlayerCount = activePlayers.Count;
            // Sync active players across all clients
            int[] activePlayerIds = activePlayers.Select(p => p.PlayerID).ToArray();
            photonView.RPC(nameof(SyncActivePlayers), RpcTarget.All, activePlayerIds);
            photonView.RPC(nameof(SetPlayerState), RpcTarget.All, player.PlayerID, PlayerState.Waiting, player.isBlind);
        }
    }

    [PunRPC]
    public void SetPlayerState(int playerId, PlayerState state, bool isBlind)
    {
        PlayerInfo player = allPlayers.Find(p => p.PlayerID == playerId);
        if (player != null)
        {
            player.playerState = state;
            player.isBlind = isBlind;
        }
    }
    PlayerState GetPlayerState(PlayerInfo player)
    {
        return player.playerState;
    }

    [PunRPC]
    void RPC_UpdatePot(int pot)
    {
        currentPot = pot;
    }

    [PunRPC]
    public void RPC_PlayerActionPopup(string popupText)
    {
        StartCoroutine(PlayerActionPopup(popupText));
    }
    IEnumerator PlayerActionPopup(string popupText)
    {
        playerActionDisplay.transform.parent.gameObject.SetActive(true);
        playerActionDisplay.text = popupText;

        yield return new WaitForSeconds(3);
        playerActionDisplay.transform.parent.gameObject.SetActive(false);

        yield return null;
    }

    [PunRPC]
    public void RPC_UpdateBalance(int playerId, int balance)
    {
        PlayerInfo player = allPlayers.Find(p => p.PlayerID == playerId);
        if (player != null)                 //&& !player.photonView.IsMine
        {
            player.userBalance = balance;
        }
    }

    [PunRPC]
    void RPC_TogglePlayerTimer(int playerId, bool isActive)
    {
        PlayerInfo player = allPlayers.Find(p => p.PlayerID == playerId);
        if (player != null)
        {
            player.playerHandTimer.gameObject.SetActive(isActive);

            if (isActive)
            {
                player.playerHandTimer.BeginTimer(player.playerHandTimer.turnDuration);
            }
   
        }
    }

    [PunRPC]
    void RPC_TogglePlayerCanvas(int playerId, bool isActive)
    {
        PlayerInfo player = allPlayers.Find(p => p.PlayerID == playerId);
        if (player != null)
        {
            if (player.photonView.IsMine && !player.isBot)
            {
                player.playerController.controlUI.SetActive(isActive);
                if (isActive)
                {
                    player.playerController.callStakeAmount.text = player.stakeAmount.ToString();
                    player.playerController.raiseStakeAmount.text = (player.stakeAmount * 2).ToString();
                }
            }
        }
    }
    [PunRPC]
    void RPC_ToggleSideShowUI(int playerId, bool isActive)
    {
        PlayerInfo player = allPlayers.Find(p => p.PlayerID == playerId);
        if (player != null)
        {
            if (player.photonView.IsMine && !player.isBot)
            {
                player.playerController.sideshowUI.SetActive(isActive);
            }
        }
    }

    [PunRPC]
    void RPC_ToggleCanvas(bool isActive)
    {
        commonCanvas.SetActive(isActive);
    }

    public bool IsPlayerTurn(PlayerInfo player)
    {
        return allPlayers[currentPlayerIndex] == player;
    }

    [PunRPC]
    public void RPC_UpdateStakeAmount(int playerId, int stakeAmt)
    {
        PlayerInfo player = allPlayers.Find(p => p.PlayerID == playerId);
        if (player != null)
        {
            Debug.Log($"StakeAmt {player.stakeAmount} updated for Player {player.PlayerID} ");
            player.stakeAmount = stakeAmt;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(initialPot);
            stream.SendNext(currentPot);
            stream.SendNext(stakeMultiplier);
            stream.SendNext(gameState);
            stream.SendNext(currentPlayerIndex);
            stream.SendNext(turnTime);
            stream.SendNext(hasOwnerLeft);
            stream.SendNext(gameInProgress);
            stream.SendNext(activePlayerCount);
        }
        else
        {
            initialPot = (int)stream.ReceiveNext();
            currentPot = (int)stream.ReceiveNext();
            stakeMultiplier = (int)stream.ReceiveNext();
            gameState = (GameState)stream.ReceiveNext();
            currentPlayerIndex = (int)stream.ReceiveNext();
            turnTime = (float)stream.ReceiveNext();
            hasOwnerLeft = (bool)stream.ReceiveNext();
            gameInProgress = (bool)stream.ReceiveNext();
            activePlayerCount = (int)stream.ReceiveNext();
        }
    }

    // Add event handlers for player leaving and master client switching
    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    base.OnPlayerLeftRoom(otherPlayer);
    //    Debug.Log("Player left: " + otherPlayer.NickName);


    //}

    [PunRPC]
    public void DisconnectEverybody()
    {
        Invoke(nameof(PhotonNetwork.LeaveRoom), 5f);
        
    }

    public override void OnLeftRoom()
    {
        // Load the lobby scene
        PhotonNetwork.LoadLevel("Lobby");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        Debug.Log("New master client: " + newMasterClient.NickName);

        photonView.TransferOwnership(PhotonNetwork.MasterClient);
        hasOwnerLeft = true;

    }
}
