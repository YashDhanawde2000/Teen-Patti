using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDeck : MonoBehaviourPunCallbacks
{
    private List<Card> cardDataList = new List<Card>(); // The list to store card instances
    public List<GameObject> cardObjList = new List<GameObject>(); // The list of card models
    private List<GameObject> ogCardObjList = new List<GameObject>();// The List of Card Models in their original order 
    
    public List<PlayerHand> playerHands = new List<PlayerHand>();   // The List of Hands of Cards held by available Players

    public GameObject dealerBot;

    Vector3 cardSpawnPos;
    public Transform[] targetPositions = new Transform[6]; // Array to store target positions for animation
    public List<GameObject> winningCards = new List<GameObject>();


    public TMP_Text winnerPopup;

    void Start()
    {
        foreach (var cardObj in cardObjList)
        {
            ogCardObjList.Add(cardObj);
        }
        
    }

    //private void Update()
    //{
    //    if ( Input.GetKeyDown(KeyCode.Space) )
    //    {
    //        CallInitializeDeck();
    //    }
    //}

 

    #region RPC Calls
    [PunRPC]
    private void InitializeDeck()
    {
        cardDataList.Clear();
        cardObjList.Clear();
        winnerPopup.transform.parent.gameObject.SetActive(false);
        foreach (Transform cardContainerPos in targetPositions)
        {
            if (cardContainerPos.parent.GetComponentInChildren<PlayerHand>() != null)
                cardContainerPos.parent.GetComponentInChildren<PlayerHand>().playerCards.Clear();
        }
        cardSpawnPos = transform.position;

        CardSuit[] suits = { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spades };
        CardColor[] colors = { CardColor.Black, CardColor.Red, CardColor.Red, CardColor.Black };

        int cardIndex = 0;

        for (int suitIndex = 0; suitIndex < suits.Length; suitIndex++)
        {
            for (int value = 1; value <= 13; value++)
            {
                if (cardIndex < ogCardObjList.Count)
                {
                    // Instantiate the card model and store it in the spawnedCardList
                    cardSpawnPos.y += 0.0008f;
                    ogCardObjList[cardIndex].transform.position = cardSpawnPos;
                    ogCardObjList[cardIndex].transform.rotation = transform.rotation;

                    ogCardObjList[cardIndex].transform.parent = null;
                    ogCardObjList[cardIndex].transform.SetParent(transform, true);

                    Card card = ogCardObjList[cardIndex].GetComponent<Card>();
                    cardDataList.Add(card);
                    cardObjList.Add(ogCardObjList[cardIndex]);

                    // Set card value, ensuring aces are set to 14
                    card.cardValue = (value == 1) ? 14 : value;
                    card.cardColor = colors[suitIndex];
                    card.cardSuit = suits[suitIndex];
                    card.cardModel = ogCardObjList[cardIndex];

                    cardIndex++;
                }
            }
        }

        CallShuffleDeck();

    }

    [PunRPC]
    private void ShuffleDeck(int[] shuffledIndices, Vector3[] shuffledPositions)
    {
        // Apply the shuffled order to cardObjLists
        List<GameObject> shuffledCardObjList = new List<GameObject>(cardObjList.Count);
        for (int i = 0; i < shuffledIndices.Length; i++)
        {
            shuffledCardObjList.Add(cardObjList[shuffledIndices[i]]);
            shuffledCardObjList[i].transform.position = shuffledPositions[i];
        }

        cardObjList = shuffledCardObjList;

        CallGiveCards();
    }

    [PunRPC]
    private void GiveCards()
    {
        StartCoroutine(DistributeCards());
    }
    private IEnumerator DistributeCards()
    {
        dealerBot.GetComponent<Animator>().SetTrigger("GiveCards");
        

        yield return new WaitForSeconds(3f);

        float cardThrowAnimDuration = 0.4f; // Duration of the animation
        float cardOffset = 0f;
        int cardIndex = 0; // To track which card we are distributing

        // Deal 3 cards to each occupied position
        for (int round = 0; round < 3; round++)
        {
            foreach (Transform target in targetPositions)
            {
                if (target.parent.gameObject.GetComponent<PlayerSeat>().isOccupied)
                {
                    Vector3 startPosition = cardObjList[cardIndex].transform.position;
                    Vector3 targetPosition = target.position + new Vector3(0, cardOffset / 10, cardOffset);

                    float elapsedTime = 0;

                    while (elapsedTime < cardThrowAnimDuration)
                    {
                        cardObjList[cardIndex].transform.rotation = target.rotation;
                        cardObjList[cardIndex].transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / cardThrowAnimDuration));
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }

                    cardObjList[cardIndex].transform.position = targetPosition;

                    photonView.RPC(nameof(SetParent), RpcTarget.All, cardIndex, System.Array.IndexOf(targetPositions, target)); // Set the parent of the card to the target position on all clients



                    cardIndex++;
                }

                cardOffset += 0.012f;
            }
        }

        playerHands.Clear();

        yield return new WaitForSeconds(2);

        foreach (Transform cardContainer in targetPositions)
        {
            if (cardContainer.parent.gameObject.GetComponent<PlayerSeat>().isOccupied)
            {
                PlayerHand playerHand = cardContainer.GetComponentInChildren<PlayerHand>();
                playerHand.UpdatePlayerCardsList(); // Ensure the player's cards are up to date
                playerHands.Add(playerHand);
            }
        }
        //// Evaluate hands and determine the winner
        //DetermineWinner();


        dealerBot.GetComponent<Animator>().SetTrigger("Default");

        yield return new WaitForSeconds(5f);
        

    }
    [PunRPC]
    private void SetParent(int cardIndex, int targetIndex)
    {
        cardObjList[cardIndex].transform.SetParent(targetPositions[targetIndex], true);
    }

    // The RPC method that will be called on all clients
    // The RPC method that will be called on all clients
    [PunRPC]
    public void RPC_ShowWinner(string winnerPopupText, int wc1, int wc2, int wc3 )          //, Sprite[] winningCardSprites
    {
        winnerPopup.transform.parent.gameObject.SetActive(true);
        winnerPopup.text = winnerPopupText;

        winningCards[0].GetComponent<Image>().sprite = PhotonView.Find(wc1).gameObject.GetComponent<Card>().cardSprite;
        winningCards[1].GetComponent<Image>().sprite = PhotonView.Find(wc2).gameObject.GetComponent<Card>().cardSprite;
        winningCards[2].GetComponent<Image>().sprite = PhotonView.Find(wc3).gameObject.GetComponent<Card>().cardSprite;

    }

    #endregion RPC Calls

    // Call these methods when you want to initialize, shuffle or give cards
    public void CallInitializeDeck()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(InitializeDeck), RpcTarget.AllBuffered);
        }
    }

    public void CallShuffleDeck()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int[] shuffledIndices = new int[cardObjList.Count];
            Vector3[] shuffledPositions = new Vector3[cardObjList.Count];

            // Populate the indices array with sequential values
            for (int i = 0; i < shuffledIndices.Length; i++)
            {
                shuffledIndices[i] = i;
            }

            // Shuffle the indices
            for (int i = 0; i < shuffledIndices.Length; i++)
            {
                int randomIndex = Random.Range(i, shuffledIndices.Length);

                // Swap the indices
                int temp = shuffledIndices[i];
                shuffledIndices[i] = shuffledIndices[randomIndex];
                shuffledIndices[randomIndex] = temp;
            }

            // Get the positions of the shuffled cards
            for (int i = 0; i < shuffledIndices.Length; i++)
            {
                shuffledPositions[i] = cardObjList[shuffledIndices[i]].transform.position;
            }

            // Call the RPC to synchronize the shuffled deck with all clients
            photonView.RPC(nameof(ShuffleDeck), RpcTarget.AllBuffered, shuffledIndices, shuffledPositions);
        }
    }

    public void CallGiveCards()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(GiveCards), RpcTarget.AllBuffered);
        }
    }


    #region Winner Determination Logic
    // Determine The Winner
    public void DetermineWinner()
    {
        //playerHands.Clear();

        //foreach (Transform cardContainer in targetPositions)
        //{
        //    if (cardContainer.parent.gameObject.GetComponent<PlayerSeat>().isOccupied)
        //    {
        //        PlayerHand playerHand = cardContainer.GetComponentInChildren<PlayerHand>();
        //        playerHand.UpdatePlayerCardsList(); // Ensure the player's cards are up to date
        //        playerHands.Add(playerHand);
        //    }
        //}

        // Sort player hands based on rank and card values
        PlayerHand winningHand = FindWinningHand(playerHands);
                
        string winnerPopupText = $"The winning hand is a : {winningHand.Rank} by {winningHand.gameObject.GetComponent<PlayerInfo>().paymentInfo.userNameHud.text}";
        
        //// Collect winning card information
        

        int wc1 = winningHand.playerCards[0].GetComponent<PhotonView>().ViewID;
        int wc2 = winningHand.playerCards[1].GetComponent<PhotonView>().ViewID;
        int wc3 = winningHand.playerCards[2].GetComponent<PhotonView>().ViewID;

        // Call RPC to show winner and sync card images
        photonView.RPC(nameof(RPC_ShowWinner), RpcTarget.All, winnerPopupText, wc1, wc2, wc3);             //, winningCards
        PlayerInfo winner = winningHand.gameObject.GetComponent<PlayerInfo>();
        winner.paymentInfo.pv.RPC(nameof(winner.paymentInfo.WinGameRequest), RpcTarget.All, GameManager.Instance.currentPot);
        //photonView.RPC(nameof(RPC_ShowWinner), RpcTarget.All, winnerPopupText);


        //winningCards[0].GetComponent<Image>().sprite = winningHand.playerCards[0].GetComponent<Card>().cardSprite;
        //winningCards[1].GetComponent<Image>().sprite = winningHand.playerCards[1].GetComponent<Card>().cardSprite;
        //winningCards[2].GetComponent<Image>().sprite = winningHand.playerCards[2].GetComponent<Card>().cardSprite;


    }
    

    // Find the Winning Hand
    public PlayerHand FindWinningHand(List<PlayerHand> playerHands)
    {
        if (playerHands.Count == 0)
        {
            return null; // No players
        }

        PlayerHand currentWinner = playerHands[0];

        for (int i = 1; i < playerHands.Count; i++)
        {
            PlayerHand hand = playerHands[i];

            // Compare hand ranks (higher rank wins)
            if (hand.Rank > currentWinner.Rank)
            {
                currentWinner = hand;
            }
            else if (hand.Rank == currentWinner.Rank)
            {
                // Handle ties based on hand value comparison rules (implement your logic here)
                currentWinner = CompareHandsWithSameRank(currentWinner, hand);
            }
        }

        return currentWinner;
    }

    #endregion Winner Determination Logic

    #region Tie-Breaking Logic
    // Function to handle tie-breaking logic for hands with the same rank
    private PlayerHand CompareHandsWithSameRank(PlayerHand hand1, PlayerHand hand2)
    {
        switch (hand1.Rank)
        {
            case HandRank.Set:
                return CompareSets(hand1, hand2);
            case HandRank.Straight:
            case HandRank.StraightFlush:
                return CompareStraights(hand1, hand2);
            case HandRank.Flush:
                return CompareFlushes(hand1, hand2);
            case HandRank.Pair:
                return ComparePairs(hand1, hand2);
            default:
                return CompareHighCard(hand1, hand2); // Default to comparing highest card
        }
    }


    private PlayerHand CompareSets(PlayerHand hand1, PlayerHand hand2)
    {
        int hand1Value = hand1.playerCards[0].GetComponent<Card>().cardValue;
        int hand2Value = hand2.playerCards[0].GetComponent<Card>().cardValue;

        if (hand1Value > hand2Value) { return hand1; }
        else if (hand1Value < hand2Value) { return hand2; } 
        else { return CompareHighCard(hand1, hand2); }

        
    }

    private PlayerHand CompareStraights(PlayerHand hand1, PlayerHand hand2)
    {
        // Custom straight order
        List<List<int>> straightOrder = new List<List<int>>
        {
            new List<int> { 14, 13, 12 }, // A-K-Q
            new List<int> { 14, 2, 3 },   // A-2-3
            new List<int> { 13, 12, 11 }, // K-Q-J
            new List<int> { 12, 11, 10 }, // Q-J-10
            new List<int> { 11, 10, 9 },  // J-10-9
            new List<int> { 10, 9, 8 },   // 10-9-8
            new List<int> { 9, 8, 7 },    // 9-8-7
            new List<int> { 8, 7, 6 },    // 8-7-6
            new List<int> { 7, 6, 5 },    // 7-6-5
            new List<int> { 6, 5, 4 },    // 6-5-4
            new List<int> { 5, 4, 3 },    // 5-4-3
            new List<int> { 4, 3, 2 }     // 4-3-2
        };

        // Sort card values in both hands (highest to lowest)
        List<int> hand1Values = hand1.playerCards.Select(card => card.GetComponent<Card>().cardValue).OrderByDescending(value => value).ToList();
        List<int> hand2Values = hand2.playerCards.Select(card => card.GetComponent<Card>().cardValue).OrderByDescending(value => value).ToList();

        // Find the matching straight order for each hand
        int matchingIndex1 = straightOrder.FindIndex(order => order.All(value => hand1Values.Contains(value)));
        int matchingIndex2 = straightOrder.FindIndex(order => order.All(value => hand2Values.Contains(value)));

        // Compare matching order indexes (higher index wins)
        if(matchingIndex1 > matchingIndex2) { return hand1; }
        else if (matchingIndex1 < matchingIndex2) { return hand2; }
        else { return CompareHighCard(hand1, hand2); }

    }

    private PlayerHand CompareFlushes(PlayerHand hand1, PlayerHand hand2)
    {
        // Custom flush order (highest to lowest)
        List<int> flushOrder = new List<int> { 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 }; // A-K-J, A-K-10, ...

        // Sort card values in both hands (highest to lowest)
        List<int> hand1Values = hand1.playerCards.Select(card => card.GetComponent<Card>().cardValue).OrderByDescending(value => value).ToList();
        List<int> hand2Values = hand2.playerCards.Select(card => card.GetComponent<Card>().cardValue).OrderByDescending(value => value).ToList();

        // Find the matching highest card value in the flush order for each hand
        int matchingValue1 = flushOrder.Find(value => hand1Values.Contains(value));
        int matchingValue2 = flushOrder.Find(value => hand2Values.Contains(value));

        // Compare matching order indexes (higher index wins)
        if (matchingValue1 > matchingValue2) { return hand1; }
        else if (matchingValue1 < matchingValue2) { return hand2; }
        else { return CompareHighCard(hand1, hand2); }
    }

    private PlayerHand ComparePairs(PlayerHand hand1, PlayerHand hand2)
    {
        // Custom pair order (highest to lowest pair value)
        List<int> pairOrder = new List<int> { 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 }; // A-A-K, A-A-Q, ...

        // Find the pair value in each hand (assuming both hands have valid pairs)
        int pairValue1 = GetPairValue(hand1.playerCards);
        int pairValue2 = GetPairValue(hand2.playerCards);

        // Helper function to find the pair value in a hand (assuming a valid pair exists)
        int GetPairValue(List<GameObject> cards)
        {
            int firstCardValue = cards[0].GetComponent<Card>().cardValue;
            for (int i = 1; i < cards.Count; i++)
            {
                if (firstCardValue == cards[i].GetComponent<Card>().cardValue)
                {
                    return firstCardValue;
                }
            }
            return -1; // Should never reach here if a valid pair exists
        }

        // Compare pair values (higher value wins)
        
        if (pairValue1 > pairValue2) { return hand1; }
        else if (pairValue1 < pairValue2) { return hand2; }
        else { return CompareHighCard(hand1, hand2); }
    }

    private PlayerHand CompareHighCard(PlayerHand hand1, PlayerHand hand2)
    {
        // Get sorted card values in descending order
        List<int> sortedValues1 = hand1.playerCards.Select(card => card.GetComponent<Card>().cardValue).OrderByDescending(value => value).ToList();
        List<int> sortedValues2 = hand2.playerCards.Select(card => card.GetComponent<Card>().cardValue).OrderByDescending(value => value).ToList();

        for (int i = 0; i < sortedValues1.Count; i++)
        {
            if (sortedValues1[i] > sortedValues2[i])
            {
                return hand1;
            }
            else if (sortedValues1[i] < sortedValues2[i])
            {
                return hand2;
            }
            // If equal, continue to the next highest card
        }

        // If all cards are equal, return either hand as they are equivalent in rank
        return hand1;
    }
    
    #endregion Tie-Breaking Logic

}
