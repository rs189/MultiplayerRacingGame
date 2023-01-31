using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomConnectionManager : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] public CustomNetworkManager networkManager;

    void OnGUI()
    {
        if (networkManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!networkManager.IsClient())
        {
            StartButtons();
        }

        GUILayout.EndArea();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void StartButtons()
    {
        if (GUILayout.Button("Connect")) StartClient();

        GUILayout.Label("IP:");
        networkManager.clientIP = GUILayout.TextField(networkManager.clientIP);

        GUILayout.Label("Port:");
        networkManager.clientPort = int.Parse(GUILayout.TextField(networkManager.clientPort.ToString()));

        GUILayout.Label("Username:");
        networkManager.username = GUILayout.TextField(networkManager.username);
    }

    void StartClient()
    {
        networkManager.StartClient();
    }
}
