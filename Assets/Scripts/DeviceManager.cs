
using UnityEngine;
using System;
using System.Collections.Generic;

public class DeviceManager : MonoBehaviour
{
    [Header("Device Prefabs")]
    public GameObject pcPrefab;
    public GameObject switchPrefab;
    public GameObject routerPrefab;
    public GameObject serverPrefab;

    [Header("Spawn Position")]
    public Transform spawnPoint;

    public List<NetworkDevice> devices =
        new List<NetworkDevice>();

    public void AddPC()
    {
        CreateDevice(pcPrefab, "PC");
    }

    public void AddSwitch()
    {
        CreateDevice(switchPrefab, "Switch");
    }

    public void AddRouter()
    {
        CreateDevice(routerPrefab, "Router");
    }

    public void AddServer()
    {
        CreateDevice(serverPrefab, "Server");
    }
    void CreateDevice(GameObject prefab, string type)
    {
        if (prefab == null)
        {
            Debug.LogError(type + " Prefab is missing!");
            return;
        }

        Vector3 startPosition = spawnPoint != null
            ? spawnPoint.position
            : Vector3.zero;

        Vector3 position = startPosition +
            Vector3.right * devices.Count * 2f;

        GameObject newObject = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        NetworkDevice device =
            newObject.GetComponent<NetworkDevice>();

        if (device == null)
        {
            Debug.LogError(
                "NetworkDevice component is missing!"
            );

            Destroy(newObject);
            return;
        }

        int count = 0;

        foreach (NetworkDevice d in devices)
        {
            if (d != null && d.data != null &&
                d.data.deviceType == type)
            {
                count++;
            }
        }

        string deviceName = type + (count + 1);

        string id = Guid.NewGuid().ToString();

        device.Initialize(id, deviceName, type);

        devices.Add(device);

        Debug.Log("Created: " + deviceName);
    }
}
