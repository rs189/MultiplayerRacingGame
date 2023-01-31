using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VehicleLapCounter : MonoBehaviour
{
    [Header("Settings")]
    public bool isTimerRunning = false;
    public float secondsCount;
    public int minuteCount;
    public int hourCount;
    string timeString;

    [Header("Objects")]
    [SerializeField] public CustomNetworkObject networkObject;

    [Header("UI")]
    [SerializeField]public TextMeshProUGUI timerText;

    void Start()
    {
        
    }

    void Update()
    {
        if (isTimerRunning)
        {
            secondsCount += Time.deltaTime;
            if(secondsCount >= 60) {
                minuteCount++;
                secondsCount = 0;
            } else if(minuteCount >= 60) {
                hourCount++;
                minuteCount = 0;
            }  

            timeString = hourCount.ToString("00") + ":" + minuteCount.ToString("00") + ":" + secondsCount.ToString("00"); 
            timerText.text = timeString;
        }
    }

    public void StartLap()
    {
        isTimerRunning = true;
    }

    public void EndLap()
    {
        isTimerRunning = false;

        if (networkObject && networkObject.networkManager) 
        {
            CustomClient client = networkObject.networkManager.clientObject.GetComponent<CustomClient>();
            if (client.uid == networkObject.uid) {
                timeString = hourCount.ToString("00") + "." + minuteCount.ToString("00") + "." + secondsCount.ToString("00");
                string record = networkObject.networkManager.username + ":" + timeString;
                Debug.Log("Sending record: " + record);
                client.SendMessage("SaveRecord:" + record);
            }
        }
    }
}
