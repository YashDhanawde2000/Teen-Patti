using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILookAtCamera : MonoBehaviour
{
    public GameObject activeCamera;

    public PlayerController playerController;

    private void Start()
    {
        StartCoroutine(Loop_FindAndLookAtCamera());
    }

    IEnumerator Loop_FindAndLookAtCamera()
    {
        if (!playerController.pv.IsMine) yield break;
        while (true)
        {
            yield return FindAndLookAtCamera();
            yield return new WaitForSeconds(2);
        }
        
    }

    IEnumerator FindAndLookAtCamera()
    {
        activeCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (activeCamera != null && playerController.pv.IsMine)
        { 
            transform.LookAt(activeCamera.transform);
            yield return null;
        }
    }

}


