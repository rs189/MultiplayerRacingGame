using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using static UnityEngine.Random;

public class CustomNetworkManager : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] public string clientIP = "127.0.0.1";
    [SerializeField] public int clientPort = 5669;  
    [SerializeField] public string username = "Player";  

    [Header("Prefabs")]
    [SerializeField] public GameObject playerPrefab;

    [Header("Objects")]
    [SerializeField] public GameObject clientObject;
    [SerializeField] public GameObject tempCamera;

    [Header("Performance")]
    [SerializeField] public int serverTargetFps = 256;

    private bool isClient = false;
    private float timeSinceLastCall = 0f;
    private float callFrequency = 0f;

    void Start()
    {
        Application.runInBackground = true;

        callFrequency = 1f / serverTargetFps;
    }

    void Update()
    {
        timeSinceLastCall += Time.deltaTime;
        if (timeSinceLastCall >= callFrequency)
        {
            timeSinceLastCall = 0f;
            if (isClient) UpdateClient();
        }
    }

    void UpdateClient()
    {
    }

    public void SpawnNetworkObject(string uid, Vector3 position, bool foreign = false)
    {
        GameObject player = Instantiate(this.playerPrefab, position, Quaternion.identity);
        player.transform.position = position;
        player.transform.Rotate(0, 180, 0);
        player.GetComponent<CustomNetworkObject>().uid = uid;
        player.GetComponent<CustomNetworkObject>().networkManager = this;

        CustomClient client = clientObject.GetComponent<CustomClient>();
        if (!foreign) 
        {
            client.networkObject = player.GetComponent<CustomNetworkObject>();
        }
        else
        {
            CustomNetworkObject customNetworkObject = player.GetComponent<CustomNetworkObject>();
            customNetworkObject.vehicleController.enabled = false;
            customNetworkObject.rigidbody.isKinematic = true;
            customNetworkObject.rigidbody.detectCollisions = false;
            customNetworkObject.camera.SetActive(false);
            customNetworkObject.dash.SetActive(false);
            customNetworkObject.ui.SetActive(false);
            player.SetActive(true);

            CustomNetworkObjectData data = new CustomNetworkObjectData(uid, new Vector3(0, 0, 0), new Vector3(0, 0, 0), player, 0);
            client.playerData.Add(data);
        }
    }

    public void UpdateNetworkObject(string uid, Vector3 position, Vector3 rotation, int order)
    {
        CustomClient client = clientObject.GetComponent<CustomClient>();

        bool found = false;
        for (int i = 0; i < client.playerData.Count; i++)
        {
            if (client.playerData[i].uid == uid)
            {
                found = true;

                int currentOrder = client.playerData[i].order;
                int receivedOrder = int.Parse(order.ToString());

                if (receivedOrder < currentOrder && currentOrder != 0) {
                    //Debug.Log("Received order is older than current order, ignoring");
                    break;
                }
                else {
                    //Debug.Log("Received order is newer than current order, updating");
                }

                client.playerData[i].order = order;

                //client.playerData[i].recentPositions.Enqueue(position);
//
                //if (client.playerData[i].recentPositions.Count > client.playerData[i].maxQueueSize)
                //{
                //    client.playerData[i].recentPositions.Dequeue();
                //}
//
                //// Interpolate over the set of recent positions
                //Vector3 extrapolatedPosition = Vector3.zero;
                //foreach (Vector3 pos in client.playerData[i].recentPositions)
                //{
                //    extrapolatedPosition += pos;
                //}
                //extrapolatedPosition /= client.playerData[i].recentPositions.Count;

                //Vector3 interpolatedPosition = Vector3.Lerp(client.playerData[i].position, position, 0.5f);
                //Vector3 newPosition = Vector3.MoveTowards(client.playerData[i].gameObject.transform.position, interpolatedPosition, 0.5f);
                //client.playerData[i].position = newPosition;

                CustomNetworkObject networkObject = client.playerData[i].gameObject.GetComponent<CustomNetworkObject>();
                networkObject.targetPosition = position;
                networkObject.targetRotation = Quaternion.Euler(rotation);
                //networkObject.gameObject.transform.rotation = Quaternion.Euler(rotation);
                //networkObject.gameObject.transform.position = newPosition;

                //client.playerData[i].gameObject.transform.position = newPosition;
//
                
//
               // Debug.Log("Updated " + uid + " to " + position );

                break;
            }
        }

        if (!found)
        {
            SpawnNetworkObject(uid, position, true);
        }
    }

    public bool IsClient()
    {
        return isClient;
    }

    IEnumerator StartClientAsync()
    {
        yield return new WaitForSeconds(2);

        CustomClient client = clientObject.GetComponent<CustomClient>();
        client.uid = System.Guid.NewGuid().ToString();

        client.SendMessage("Spawn:" + client.uid);
        tempCamera.SetActive(false);
    }
    
    public void StartClient()
    {
        isClient = true;

        clientObject.SetActive(true);
        CustomClient client = clientObject.GetComponent<CustomClient>();

        StartCoroutine(StartClientAsync());
    }
}
