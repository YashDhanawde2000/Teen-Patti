using UnityEngine;

public class Card : MonoBehaviour
{
    public int cardValue;
    public CardColor cardColor;
    public CardSuit cardSuit;
    public GameObject cardModel;

    
    public Sprite cardSprite;

}

public enum CardColor
{
    Red,
    Black
}

public enum CardSuit
{
    Club,
    Diamond,
    Heart,
    Spades
}

