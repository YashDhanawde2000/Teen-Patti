using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class PaymentInfo : MonoBehaviour
{
    #region Variables and References
    [Header("Network Variables")]
    public string getApiUrl = "https://backend.decentrawood.com/play/getUserBalance3D?accountId=";
    public string placeBetApiUrl = "https://backend.decentrawood.com/teenpatti/place-bet";
    public string postPlayGameApiUrl = "https://backend.decentrawood.com/teenpatti/play-game";


    public string walletAddress = "0xf913ce781dc10cbbe2f431b4f8fa65f1dbf27576"; // Example wallet address
    public bool isWalletAddressSet = false;

    private WaitForSeconds apiFetchDelay = new WaitForSeconds(1f);

    //[Header("Gameplay Variables")]


    [Header("Script References")]
    public PhotonView pv;
    public PlayerInfo playerInfo;
    public RoomManager roomManager;
    //public ConnectWallet connectWallet;

    //[Header("Object References")]

    //public GameObject userBalanceDisplay;
    //public GameObject userNameDisplay;
    public TMP_Text userBalanceText;
    public TMP_Text userNameHud;
    private bool userNameSet = false;


    #endregion Variables and References

    private void Start()
    {
        pv = GetComponent<PhotonView>();

        //walletManager.ConnectWalletFromUnity();

        //WinGameRequest(100);
        
    }

    private void Update()
    {

        if( ConnectWallet.Instance.isConnected && !isWalletAddressSet && string.IsNullOrEmpty(walletAddress) && !playerInfo.isBot)
        {
            walletAddress = ConnectWallet.Instance.GetWalletAddress();
            pv.RPC(nameof(RPC_SetApiURL), RpcTarget.All);
            //RPC_SetApiURL();
            StartCoroutine(LoopGetUserDataFromApi());
            roomManager.Invoke(nameof(roomManager.CallGMCheck), 5f);
            
        }
    }



    #region GET API 
    [PunRPC]
    public void RPC_SetApiURL()
    {
        //if (!pv.IsMine) return;
        if (!string.IsNullOrEmpty(walletAddress) && !isWalletAddressSet)
        {
            getApiUrl = getApiUrl + walletAddress;
            isWalletAddressSet = true;
        }
    }

    // GET User Balance and Name
    public IEnumerator LoopGetUserDataFromApi()
    {
        if (!pv.IsMine) yield break;
        while (true)
        {
            yield return GetUserDataFromApi();
            yield return apiFetchDelay; // Wait for 1 second before fetching data again
        }
    }

    public IEnumerator GetUserDataFromApi()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(getApiUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to fetch data from API: " + www.error);
            }
            else
            {
                string jsonResult = www.downloadHandler.text;
                //Debug.Log("API Response: " + jsonResult);

                // Deserialize JSON response
                GetApiResponse response = JsonUtility.FromJson<GetApiResponse>(jsonResult);

                if (response != null && response.status)
                {
                    if(!userNameSet)
                    {
                        //userNameText.text = "Username: \n" + response.data.userName;
                        //pv.Owner.NickName = response.data.userName;
                        //userNameHud.text = response.data.userName;
                        pv.RPC(nameof(RPC_UsernameText), RpcTarget.All, response.data.userName);
                    }
                    
                    if (userBalanceText !=null)
                    { userBalanceText.text = "Balance: \n" + response.data.amount; }

                    if (playerInfo != null)
                    {
                        playerInfo.userBalance = response.data.amount;                                            // Setting the User Balance HERE
                        GameManager.Instance.photonView.RPC(nameof(GameManager.RPC_UpdateBalance) ,RpcTarget.All, playerInfo.PlayerID, playerInfo.userBalance);
                    }
                }
                else
                {
                    Debug.LogError("API Error: " + response.message);
                }
            }
        }
    }



    #endregion GET API

    #region POST API

    [PunRPC]
    public void PlaceBetRequest(int amt)
    {
        if (!pv.IsMine) return;
        StartCoroutine(PlaceBetOnApi(amt));
    }
    public IEnumerator PlaceBetOnApi(int amount)
    {
        
        BetPayload betPayload = new BetPayload
        {
            accountId = walletAddress,
            amount = amount
        };

        string jsonPayload = JsonUtility.ToJson(betPayload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(placeBetApiUrl, UnityWebRequest.kHttpVerbPOST))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to place bet: " + www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                PostApiResponse response = JsonUtility.FromJson<PostApiResponse>(jsonResponse);

                if (response != null && response.status)
                {
                    Debug.Log("Response: " + response.message);
                }
                else
                {
                    Debug.LogError("Response: " + response.message);
                }
            }
        }
    }


    [PunRPC]
    public void WinGameRequest(int amt)
    {
        //if (!PhotonNetwork.IsMasterClient) return;
        if (!pv.IsMine) return;
        StartCoroutine(WinGameOnApi("TeenPatti", "Win", amt));
    }
    public IEnumerator WinGameOnApi(string gameType, string gameStatus, int amount)
    {
        

        WinGamePayload winGamePayload = new WinGamePayload
        {
            accountId = walletAddress,
            gameType = gameType,
            gameStatus = gameStatus,
            amount = amount,
            
        };

        string jsonPayload = JsonUtility.ToJson(winGamePayload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(postPlayGameApiUrl, UnityWebRequest.kHttpVerbPOST))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to Win Game: " + www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                PostApiResponse response = JsonUtility.FromJson<PostApiResponse>(jsonResponse);

                if (response != null && response.status)
                {
                    Debug.Log("Response: " + response.message);
                }
                else
                {
                    Debug.LogError("Response: " + response.message);
                }
            }
        }
    }

    #endregion POST API


    #region Defined Data
    //GET 
    [System.Serializable]
    public class GetApiResponse
    {
        public bool status;
        public string message;
        public UserData data;
    }

    [System.Serializable]
    public class UserData
    {
        public string accountId;
        public string userName;
        public int amount;
        public string asset;
    }


    //POST
    [System.Serializable]
    public class BetPayload
    {
        public string accountId;
        public int amount;
    }

    [System.Serializable]
    public class WinGamePayload
    {
        public string accountId;
        public string gameType;
        public string gameStatus;
        public int amount;
    }

    [System.Serializable]
    public class PostApiResponse
    {
        public bool status;
        public string message;
    }

    #endregion Defined Data


    [PunRPC]
    public void RPC_UsernameText(string usernameText)
    {
        userNameHud.text = usernameText;
    }
    
}
