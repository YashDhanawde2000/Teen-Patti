using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private Image circleFill;
    [SerializeField] private TMP_Text timeText;

    public int turnDuration, remainingDuration;

    

    public void BeginTimer(int _turnDuration)
    {
        remainingDuration = _turnDuration;
        StartCoroutine(UpdateTimer());
    }

    public IEnumerator UpdateTimer()
    {
        while (remainingDuration > 0)
        {
            timeText.text = $"{remainingDuration / 60:00} : {remainingDuration % 60:00}";
            circleFill.fillAmount = Mathf.InverseLerp(0, turnDuration, remainingDuration);
            remainingDuration--;
            yield return new WaitForSeconds(1);
            

        }
        yield return null;
    }

}
