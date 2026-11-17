
using UnityEngine;

public class CableManager : MonoBehaviour
{
    public static bool IsCableMode { get; private set; }

    public Camera mainCamera;
    public GameObject cablePrefab;

    private NetworkDevice firstDevice;

    public void EnableCableMode()
    {
        IsCableMode = true;
        firstDevice = null;

        Debug.Log("Select the first device");
    }

    public void CancelCableMode()
    {
        IsCableMode = false;
        firstDevice = null;
    }

    void Update()
    {
        if (!IsCableMode)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(
                Input.mousePosition
            );

            if (Physics.Raycast(
                ray, out RaycastHit hit))
            {
                NetworkDevice device =
                    hit.collider.GetComponentInParent<
                        NetworkDevice>();

                if (device == null)
                    return;

                if (firstDevice == null)
                {
                    firstDevice = device;

                    Debug.Log(
                        "First device: " +
                        device.data.deviceName
                    );
                }
                else
                {
                    if (firstDevice == device)
                    {
                        Debug.Log(
                            "Select another device"
                        );

                        return;
                    }

                    CreateCable(firstDevice, device);

                    CancelCableMode();
                }
            }
        }
    }

    void CreateCable(
        NetworkDevice first,
        NetworkDevice second)
    {
        if (cablePrefab == null)
        {
            Debug.LogError("Cable Prefab is missing!");
            return;
        }

        if (first.connectionPoint == null ||
            second.connectionPoint == null)
        {
            Debug.LogError(
                "Connection Point is missing!"
            );

            return;
        }

        GameObject cable = Instantiate(cablePrefab);

        CableConnection connection =
            cable.GetComponent<CableConnection>();

        if (connection == null)
        {
            Debug.LogError(
                "CableConnection component is missing!"
            );

            Destroy(cable);
            return;
        }

        connection.SetConnection(
            first.connectionPoint,
            second.connectionPoint
        );

        Debug.Log(
            "Connected " +
            first.data.deviceName +
            " to " +
            second.data.deviceName
        );
    }
}
