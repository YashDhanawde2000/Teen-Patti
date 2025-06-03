using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameObject playerControlsCanvas;
    public GameObject controlUI;
    public GameObject sideshowUI;
    public GameObject playerCardCanvas;
    public GameObject paymentInfoCanvas;

    public TMP_Text callStakeAmount;
    public TMP_Text raiseStakeAmount;
    public TMP_Text callButtonText;

    public TMP_Text sideshowText;

    public Sprite ogCardSprite;
    public GameObject card1, card2, card3;

    public PhotonView pv;
    public PlayerInfo playerInfo;

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        if (!pv.IsMine && playerControlsCanvas != null && playerCardCanvas != null && paymentInfoCanvas != null)
        {
            playerControlsCanvas.SetActive(false);
            playerCardCanvas.SetActive(false);
            paymentInfoCanvas.SetActive(false);
        }
    }


}
