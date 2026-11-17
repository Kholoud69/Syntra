
using UnityEngine;

public class NetworkDevice : MonoBehaviour
{
    public DeviceData data;

    public Transform connectionPoint;

    public void Initialize(string id, string deviceName, string type)
    {
        data = new DeviceData();

        data.deviceID = id;
        data.deviceName = deviceName;
        data.deviceType = type;

        data.position = transform.position;
    }

    void LateUpdate()
    {
        if (data != null)
        {
            data.position = transform.position;
        }
    }
}
