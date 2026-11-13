using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class FuckassTimer : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTime;

    // Update is called once per frame
    void Update()
    {

        if (remainingTime > 0)
        {

            remainingTime -= Time.deltaTime;

        }
        else if (remainingTime < 0)
        {
            remainingTime = 0;
            timerText.color = Color.red;
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        int miliseconds = Mathf.FloorToInt((remainingTime * 1000) % 1000);
        //timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText.text = GameManager.Instance.PlayMode_TimerSeconds.ToString();


    }
}

