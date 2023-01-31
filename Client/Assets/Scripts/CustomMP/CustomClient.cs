using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading; 
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;

public class CustomNetworkObjectData
{
    public string uid;
    public Vector3 position;
    public Vector3 rotation;
    public GameObject gameObject;
    public int order;

    public CustomNetworkObjectData(string uid, Vector3 position, Vector3 rotation, GameObject gameObject, int order)
    {
        this.uid = uid;
        this.position = position;
        this.rotation = rotation;
        this.gameObject = gameObject;
        this.order = order;
    }
}

public class CustomNetworkLeaderboard
{
    public string name;
    public string time;

    public CustomNetworkLeaderboard(string name, string time)
    {
        this.name = name;
        this.time = time;
    }
}

public class CustomClient : MonoBehaviour
{
    private UdpClient udpClient;

    [Header("Network")]
    [SerializeField] public string uid;  
    [SerializeField] public List<string> players = new List<string>();
    [SerializeField] public List<CustomNetworkObjectData> playerData = new List<CustomNetworkObjectData>();
    [SerializeField] public List<CustomNetworkLeaderboard> leaderboardData = new List<CustomNetworkLeaderboard>();

    [Header("Objects")]
    [SerializeField] public CustomNetworkObject networkObject;
    [SerializeField] public CustomNetworkManager networkManager;

    private float timeSinceLastCall = 0f;
    private float callFrequency = 0f;
    
    void Awake()
    {
        udpClient = new UdpClient(networkManager.clientPort);
    }

    void Start()
    {
        callFrequency = 1f / (1);
    }

    void OnEnable()
    {
        Debug.Log("Connecting to the server...");

        try
        { 
            udpClient.Connect(networkManager.clientIP, 12000);

            Thread receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.Start();
        }
        catch(Exception e)
        {
            print("Exception thrown " + e.Message);
        }
    }

    void OnDisable()
    {
        udpClient.Close();
    }

    void Update()
    {
        timeSinceLastCall += Time.deltaTime;
        if (timeSinceLastCall >= callFrequency)
        {
            timeSinceLastCall = 0f;
        }
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 12000);
        while (true)
        {
            byte[] receiveBytes = udpClient.Receive(ref remoteEndPoint);
            string receivedString = Encoding.ASCII.GetString(receiveBytes);
            ReceiveCallback(receivedString);
        }
    }

    private IEnumerator SpawnNetworkObjectMain(string uid, Vector3 position) 
    {
        players.Add(uid);
        networkManager.SpawnNetworkObject(uid, position);
		yield return null;
	}

    private IEnumerator UpdateNetworkObjectMain(string uid, Vector3 position, Vector3 rotation, int order) 
    {
        networkManager.UpdateNetworkObject(uid, position, rotation, order);
        yield return null;
    }

    private void ReceiveCallback(string receivedString)
    {
        // Spawned:{uid}
        if (receivedString.StartsWith("Spawned:"))
        {
            var receivedUid = receivedString.Split(':')[1];
            var receivedPosition = receivedString.Split(':')[2];
            var receivedPositionVector = new Vector3(float.Parse(receivedPosition.Split(',')[0].Split('(')[1]), float.Parse(receivedPosition.Split(',')[1]), float.Parse(receivedPosition.Split(',')[2].Split(')')[0]));
            Debug.Log("Spawned: " + receivedUid + " at " + receivedPositionVector);
            if (uid == receivedUid)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(SpawnNetworkObjectMain(uid, receivedPositionVector)); 
            }
        }

        // PhysicsUpdate:{uid}:{x}:{y}:{z}:{rx}:{ry}:{rz}:{order}
        if (receivedString.StartsWith("PhysicsUpdate:"))
        {
            var receivedUid = receivedString.Split(':')[1];
            if (receivedUid != uid) {
                var receivedPosition = new Vector3(float.Parse(receivedString.Split(':')[2]), float.Parse(receivedString.Split(':')[3]), float.Parse(receivedString.Split(':')[4]));
                var receivedRotation = new Vector3(float.Parse(receivedString.Split(':')[5]), float.Parse(receivedString.Split(':')[6]), float.Parse(receivedString.Split(':')[7]));
                var order = receivedString.Split(':')[8];

                if (networkManager.IsClient()) {
                    UnityMainThreadDispatcher.Instance().Enqueue(UpdateNetworkObjectMain(receivedUid, receivedPosition, receivedRotation, int.Parse(order)));
                }
            }
        }

        // GetRecords
        if (receivedString.StartsWith("Records"))
        {
            Debug.Log("GetRecords Records: " + receivedString);

            leaderboardData.Clear();

            var records = receivedString.Substring(receivedString.IndexOf(':') + 1);
            string[] parts = records.Split(';');

            foreach (string part in parts)
            {      
                string[] nameTime = part.Split(':');
                if (nameTime.Length == 2)
                {
                    leaderboardData.Add(new CustomNetworkLeaderboard(nameTime[0], nameTime[1]));
                }
                else
                {
                    Debug.Log("Invalid data format: " + part);
                }
            }
        }
    }

    public void SendMessage(string message) 
    {
        byte[] sendBytes = Encoding.ASCII.GetBytes(message);
        udpClient.Send(sendBytes, sendBytes.Length);
    }

    public void SendPhysicsUpdate(string senderUid, Vector3 senderPosition, Vector3 senderRotation)
    {
        var message = "PhysicsUpdate:" + senderUid + ":" + senderPosition.x + ":" + senderPosition.y + ":" + senderPosition.z + ":" + senderRotation.x + ":" + senderRotation.y + ":" + senderRotation.z;
        SendMessage(message);
    }
}
