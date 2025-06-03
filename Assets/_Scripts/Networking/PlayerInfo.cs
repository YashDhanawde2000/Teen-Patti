using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public bool isBot = false;

    public int PlayerID;
    public GameObject playerModel;
    public int userBalance = 100;
    public int stakeAmount = 0;
    public PlayerState playerState;

    public bool isBlind = true;

    public bool isRequestingSideshow;
    public bool isRespondingToSideshow;

    public PhotonView photonView;
    public PlayerSeat playerSeat;
    public PlayerHand playerHand;
    public CountdownTimer playerHandTimer;

    public PaymentInfo paymentInfo;
    public PlayerController playerController;

    private bool isBotActionInProgress = false;

    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        
    }

    private void Update()
    {
        if ( GameManager.Instance.gameInProgress && photonView.IsMine && !isBot)
        {
            if (playerController != null && playerHand != null && playerHand.playerCards.Count == 3)
            {
                if (playerController.pv.IsMine)
                {
                    HandlePlayerInput();
                    ViewCards();
                }
            }
            //else if (playerController != null & playerHand.playerCards.Count != 3)
            //{
            //    if (playerController.pv.IsMine)
            //    {
            //        playerController.playerControlsCanvas.SetActive(false);
            //        playerController.playerCardCanvas.SetActive(false);
            //    }
            //}
        }
        //else if ( !GameManager.Instance.gameInProgress && playerController != null && !isBot)
        //{
        //    if (playerController.pv.IsMine)
        //    {
        //        playerController.playerControlsCanvas.SetActive(false);
        //        playerController.playerCardCanvas.SetActive(false);
        //    }
        //}



        if (GameManager.Instance.gameInProgress && isBot )
        {
            if(GameManager.Instance.IsPlayerTurn(this) && !isBotActionInProgress)
            {
                StartCoroutine(HandleBotInput());
            }
        }
    }

    public void HandlePlayerInput()
    {
        //if (GameManager.Instance.IsPlayerTurn(this) )
        //{
        //    playerController.playerControlsCanvas.SetActive(true);
        //    playerController.callStakeAmount.text = stakeAmount.ToString();
        //    playerController.raiseStakeAmount.text = (stakeAmount * 2).ToString();
            
        //    //if (isRespondingToSideshow)
        //    //{
        //    //    playerController.sideshowUI.SetActive(true);

        //    //}
        //    //else
        //    //{
        //    //    playerController.sideshowUI.SetActive(false);
        //    //}
            
        //}
        //else
        //{
        //    playerController.playerControlsCanvas.SetActive(false);
        //    //playerController.sideshowUI.SetActive(false);
        //}
    }


    public IEnumerator HandleBotInput()
    {
        isBotActionInProgress = true;

        PlayerInfo previousPlayer = GameManager.Instance.allPlayers[0];
        if(!previousPlayer.isRequestingSideshow)
        {
            // Wait for a random amount of time between 5 and 14 seconds
            yield return new WaitForSeconds(Random.Range(4f, 12f));

            PlayerAction randomAction = (PlayerAction)Random.Range(0, 3);

            Debug.Log("Random Action: " + randomAction);

            switch (randomAction)
            {
                case PlayerAction.SeenPlay:
                    if (userBalance >= stakeAmount * 2)
                    {
                        Debug.Log("Bot can 'See'. Balance is sufficient.");

                        // Execute the "SeenPlay" action
                        photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.SeenPlay, PlayerID);

                        // Choose randomly again for Call or Raise
                        randomAction = (PlayerAction)Random.Range(1, 3); // Call or Raise

                        Debug.Log("Random Action after 'See': " + randomAction);

                        if (randomAction == PlayerAction.Call && userBalance >= stakeAmount)
                        {
                            Debug.Log("Bot decided to Call.");
                            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Call, PlayerID);
                        }
                        else if (randomAction == PlayerAction.Raise && userBalance >= stakeAmount * 2)
                        {
                            Debug.Log("Bot decided to Raise.");
                            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Raise, PlayerID);
                        }
                    }
                    else
                    {
                        Debug.Log("Not enough balance to 'See', defaulting to Call.");
                        // Not enough balance to "See", default to checking other actions
                        randomAction = PlayerAction.Call; // Fall through to Call
                        goto case PlayerAction.Call;
                    }
                    break;
                case PlayerAction.Call:
                    if (userBalance >= stakeAmount)
                    {
                        Debug.Log("Bot decided to Call.");
                        // Execute the "Call" action
                        photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Call, PlayerID);
                    }
                    else
                    {
                        Debug.Log("Not enough balance to Call, Folding.");
                        photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Fold, PlayerID);
                    }
                    break;
                case PlayerAction.Raise:
                    if (userBalance >= stakeAmount * 2)
                    {
                        Debug.Log("Bot decided to Raise.");
                        // Execute the "Raise" action
                        photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Raise, PlayerID);
                    }
                    else
                    {
                        Debug.Log("Not enough balance to Raise, Folding.");
                        photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Fold, PlayerID);
                    }
                    break;
                case PlayerAction.Fold:
                    Debug.Log("Bot decided to Fold.");
                    // Execute the "Fold" action
                    photonView.RPC(nameof(RPC_PlayerAction),RpcTarget.MasterClient, PlayerAction.Fold, PlayerID);
                    break;
                default:
                    Debug.Log("Default case, Folding.");
                    // If the action is not handled, fold by default
                    photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Fold, PlayerID);
                    break;
            }
        }
        else
        {
            //yield return new WaitForSeconds(3f);
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Accept, PlayerID);
        }

        

        // Reset the check
        isBotActionInProgress = false;
    }



    public void ViewCards()
    {
        if (playerHand.playerCards[0] != null && playerHand.playerCards[1] != null && playerHand.playerCards[2] != null)
        {
            playerController.playerCardCanvas.SetActive(true);
            if (!this.isBlind)
            {
                
                playerController.card1.GetComponent<Image>().sprite = playerHand.playerCards[0].GetComponent<Card>().cardSprite;
                playerController.card2.GetComponent<Image>().sprite = playerHand.playerCards[1].GetComponent<Card>().cardSprite;
                playerController.card3.GetComponent<Image>().sprite = playerHand.playerCards[2].GetComponent<Card>().cardSprite;
                playerController.callButtonText.text = "Chaal";
            }
            else
            {
                playerController.card1.GetComponent<Image>().sprite = playerController.ogCardSprite;
                playerController.card2.GetComponent<Image>().sprite = playerController.ogCardSprite;
                playerController.card3.GetComponent<Image>().sprite = playerController.ogCardSprite;
                playerController.callButtonText.text = "Blind";
            }
        }
        
    }

    private void OnEnable()
    {
        playerInputActions.Player.See.performed += OnSeePerformed;
        playerInputActions.Player.Call.performed += OnCallPerformed;
        playerInputActions.Player.Raise.performed += OnRaisePerformed;
        playerInputActions.Player.Fold.performed += OnFoldPerformed;
        playerInputActions.Player.Sideshow.performed += OnSideshowRequested;
        playerInputActions.Player.Accept.performed += OnSideshowAccepted;
        playerInputActions.Player.Decline.performed += OnSideshowDeclined;
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Player.See.performed -= OnSeePerformed;
        playerInputActions.Player.Call.performed -= OnCallPerformed;
        playerInputActions.Player.Raise.performed -= OnRaisePerformed;
        playerInputActions.Player.Fold.performed -= OnFoldPerformed;
        playerInputActions.Player.Sideshow.performed += OnSideshowRequested;
        playerInputActions.Player.Accept.performed -= OnSideshowAccepted;
        playerInputActions.Player.Decline.performed -= OnSideshowDeclined;
        playerInputActions.Disable();
    }


    private void OnSeePerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.IsPlayerTurn(this) && isBlind && photonView.IsMine && !isBot)
        {
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.SeenPlay, PlayerID);
        }
    }

    private void OnCallPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.IsPlayerTurn(this) && photonView.IsMine && !isBot)
        {
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Call, PlayerID);
        }
    }

    private void OnRaisePerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.IsPlayerTurn(this) && photonView.IsMine && !isBot)
        {
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Raise, PlayerID);
        }
    }

    private void OnFoldPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.IsPlayerTurn(this) && photonView.IsMine && !isBot)
        {
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Fold, PlayerID);
        }
    }

    private void OnSideshowRequested(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.IsPlayerTurn(this) && photonView.IsMine && !isBot)
        {
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Sideshow, PlayerID);
        }
    }

    private void OnSideshowAccepted(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.IsPlayerTurn(this) && photonView.IsMine && isRespondingToSideshow && !isBot)
        {
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Accept, PlayerID);
        }
    }
    private void OnSideshowDeclined(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.IsPlayerTurn(this) && photonView.IsMine && isRespondingToSideshow && !isBot)
        {
            photonView.RPC(nameof(RPC_PlayerAction), RpcTarget.MasterClient, PlayerAction.Decline, PlayerID);
        }
    }



    [PunRPC]
    public void RPC_PlayerAction(PlayerAction action, int playerId)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.ProcessPlayerAction(action, playerId);
        }
    }
}

public enum PlayerAction
{
    SeenPlay,
    Call,
    Raise,
    Fold,
    Sideshow,
    Accept,
    Decline
}
