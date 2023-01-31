using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapCounter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // On Trigger Enter
    private void OnTriggerEnter(Collider other)
    {
        // If other.gameObject is a vehicle
        if (other.gameObject.CompareTag("TriggerVehicle"))
        {
            Debug.Log("Vehicle entered lap counter");

            // Increment lapCount
            //other.gameObject.GetComponent<Vehicle>().lapCount++;
            VehicleLapCounter vehicle = other.gameObject.GetComponent<VehicleLapCounter>();
            
            // If vehicle is not on the last lap
            if (!vehicle.isTimerRunning) {
                vehicle.StartLap();
            } else {
                vehicle.EndLap();
            }
        }
    }
}
