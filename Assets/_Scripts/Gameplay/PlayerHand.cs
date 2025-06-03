using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public List<GameObject> playerCards = new List<GameObject>();
    public HandRank Rank { get; private set; }
    

    public void UpdatePlayerCardsList()
    {
        playerCards.Clear();

        foreach (Transform card in transform)
        {
            if (card.gameObject.GetComponent<Card>() != null)
            {
                playerCards.Add(card.gameObject);
            }
        }
        if (playerCards.Count > 0)
        {
            EvaluateHandRank();
        }

    }


    public HandRank EvaluateHandRank()
    {
        // Sort cards by value (highest to lowest)
        playerCards.Sort((cardA, cardB) => cardB.GetComponent<Card>().cardValue.CompareTo(cardA.GetComponent<Card>().cardValue));

        // Check for Set (Three of a Kind)
        if (IsSet())
        {
            Rank = HandRank.Set;
            return Rank;
        }

        // Check for Straight Flush (highest priority after Set)
        if (IsStraightFlush())
        {
            Rank = HandRank.StraightFlush;
            return Rank;
        }

        // Check for Flush (after checking for StraightFlush)
        if (IsFlush())
        {
            Rank = HandRank.Flush;
            return Rank;
        }

        // Check for Straight (after checking for Flush)
        if (IsStraight())
        {
            Rank = HandRank.Straight;
            return Rank;
        }

        // Check for Pair
        if (IsPair())
        {
            Rank = HandRank.Pair;
            return Rank;
        }

        // Default Rank - High Card
        Rank = HandRank.HighCard;
        return Rank;
    }

    private bool IsSet()
    {
        int firstCardValue = playerCards[0].GetComponent<Card>().cardValue;
        return playerCards[1].GetComponent<Card>().cardValue == firstCardValue &&
               playerCards[2].GetComponent<Card>().cardValue == firstCardValue;
    }
    private bool IsStraightFlush()
    {
        return IsStraight() && IsFlush();
    }
    private bool IsFlush()
    {
        CardSuit firstCardSuit = playerCards[0].GetComponent<Card>().cardSuit;
        for (int i = 1; i < playerCards.Count; i++)
        {
            if (playerCards[i].GetComponent<Card>().cardSuit != firstCardSuit)
            {
                return false;
            }
        }
        return true;
    }
    private bool IsStraight()
    {
        // Extract card values and sort them in descending order
        List<int> cardValues = playerCards.Select(card => card.GetComponent<Card>().cardValue).OrderByDescending(value => value).ToList();

        // Custom straight sequences
        List<List<int>> straightSequences = new List<List<int>>
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

        // Check if cardValues matches any of the custom straight sequences
        foreach (var sequence in straightSequences)
        {
            if (sequence.All(cardValues.Contains))
            {
                return true;
            }
        }

        return false;
    }
    private bool IsPair()
    {
        for (int i = 0; i < playerCards.Count - 1; i++)
        {
            int firstCardValue = playerCards[i].GetComponent<Card>().cardValue;
            for (int j = i + 1; j < playerCards.Count; j++)
            {
                if (firstCardValue == playerCards[j].GetComponent<Card>().cardValue)
                {
                    return true;
                }
            }
        }
        return false;
    }

}


public enum HandRank
{
    HighCard,
    Pair,
    Flush,
    Straight,
    StraightFlush,
    Set
}