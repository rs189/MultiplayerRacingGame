using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Text;
using VehiclePhysics;

public class CustomNetworkObject : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] public string uid;

    [Header("Objects")]
    [SerializeField] public CustomNetworkManager networkManager;

    private float timeSinceLastCall = 0f;
    private float callFrequency = 0f;

    public VPVehicleController vehicleController; 
    public Rigidbody rigidbody;
    public GameObject camera;
    public GameObject dash;
    public GameObject ui;

    public Queue<Vector3> recentPositions;
    public int maxQueueSize;

    public Vector3 targetPosition;
    public Quaternion targetRotation;

    void Awake()
    {
        recentPositions = new Queue<Vector3>();
        maxQueueSize = 15;

        vehicleController = GetComponent<VPVehicleController>();
        rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        recentPositions = new Queue<Vector3>();
        maxQueueSize = 15;

        vehicleController = GetComponent<VPVehicleController>();
        rigidbody = GetComponent<Rigidbody>();

        callFrequency = 1f / networkManager.serverTargetFps;
    }

    void UpdateRemote()
    {
        CustomClient CustomClient = networkManager.clientObject.GetComponent<CustomClient>();
        if (uid == CustomClient.uid)
        {
            return;
        }

        gameObject.transform.rotation = targetRotation;

        //Vector3 interpolatedPosition = Vector3.Lerp(gameObject.transform.position, targetPosition, 0.5f);
        //Vector3 newPosition = Vector3.MoveTowards(gameObject.transform.position, interpolatedPosition, 0.5f);
        //gameObject.transform.position = newPosition;

        // Add new position to queue
        recentPositions.Enqueue(targetPosition);

        // Remove oldest position if queue is too big
        if (recentPositions.Count > maxQueueSize)
        {
            recentPositions.Dequeue();
        }

        // Calculate average position
        Vector3 averagePosition = Vector3.zero;
        foreach (Vector3 position in recentPositions)
        {
            averagePosition += position;
        }

        averagePosition /= recentPositions.Count;

        // Move towards average position
        //Vector3 newPosition = Vector3.MoveTowards(gameObject.transform.position, averagePosition, 0.5f);
        //gameObject.transform.position = newPosition;
        //
        //gameObject.transform.rotation = targetRotation;
        //gameObject.transform.position = targetPosition;

        float lerpSpeed = 0.5f;
        Vector3 interpolatedPosition = Vector3.Lerp(averagePosition, gameObject.transform.position, lerpSpeed);
        gameObject.transform.position = interpolatedPosition;
    }

    void Update()
    {
        UpdateRemote();

        timeSinceLastCall += Time.deltaTime;
        if (timeSinceLastCall >= callFrequency)
        {
            timeSinceLastCall = 0f;
            SyncPhysics();
        }
    }

    void SyncPhysics()
    {
        if (networkManager.IsClient())
        {
            CustomClient CustomClient = networkManager.clientObject.GetComponent<CustomClient>();
            Vector3 position = transform.position;
            Vector3 rotation = transform.rotation.eulerAngles;
            CustomClient.SendPhysicsUpdate(uid, position, rotation);
        }
    }
}
