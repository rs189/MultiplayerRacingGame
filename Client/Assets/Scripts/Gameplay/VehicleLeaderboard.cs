using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VehicleLeaderboard : MonoBehaviour
{
    [SerializeField] public GameObject leaderboardPanel;
    [SerializeField] public TextMeshProUGUI leaderboardText;

    [Header("Objects")]
    [SerializeField] public CustomNetworkObject networkObject;

    private bool isFetchingRecords = false;

    void Start()
    {
        FetchRecords();
    }

    void FetchRecords()
    {
        Debug.Log("Fetching records");

        isFetchingRecords = true;

        if (networkObject && networkObject.networkManager) 
        {
            CustomClient client = networkObject.networkManager.clientObject.GetComponent<CustomClient>();    
            client.SendMessage("GetRecords");

            leaderboardText.text = "";
            for (int i = 0; i < client.leaderboardData.Count; i++)
            {
                leaderboardText.text += client.leaderboardData[i].name + " - " + client.leaderboardData[i].time;
                leaderboardText.text += "\n";
            }
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            if (!isFetchingRecords) FetchRecords();
            leaderboardPanel.SetActive(true);  
        }
        else
        {
            isFetchingRecords = false;
            leaderboardPanel.SetActive(false);
        }
    }
}
