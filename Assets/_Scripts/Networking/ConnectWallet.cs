using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectWallet : MonoBehaviour
{
    public static ConnectWallet Instance;

    public string address;
    //public GameObject connectBtn;
    //public RoomManager roomManager;
    //public PaymentInfo paymentInfo;
    public bool isConnected;


    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        //connectBtn = gameObject;
        if (!isConnected)
        {
            ConnectWalletFromUnity();
        }
        
    }

    #region Wallet Connection
    public void OnWalletConnected(string walletAddress)
    {
        if (!isConnected)
        {
            Debug.Log("Wallet connected: " + walletAddress);
            address = walletAddress;

            isConnected = true;
        }
        
    }

    public string GetWalletAddress()
    {
        return address;
    }

    public void ConnectWalletFromUnity()
    {
        // Call the connectWallet function from JavaScript
        Application.ExternalCall("ConnectWallet");

    }

    #endregion Wallet Connection
}

