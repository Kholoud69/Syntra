
using UnityEngine;

public class MoveRotateDevice : MonoBehaviour
{
    private static MoveRotateDevice selectedDevice;

    private Camera cam;
    private bool dragging;

    private Plane dragPlane;
    private Vector3 offset;

    public float rotationSpeed = 5f;

    private float rotationX;
    private float rotationY;

    private bool rotateMode = false;

    void Start()
    {
        cam = Camera.main;

        rotationX = transform.eulerAngles.x;
        rotationY = transform.eulerAngles.y;
    }

    void Update()
    {
        if (CableManager.IsCableMode)
        {
            dragging = false;
            return;
        }

        // Switch between Move and Rotate
        if (selectedDevice == this && Input.GetKeyDown(KeyCode.R))
        {
            rotateMode = !rotateMode;
        }

        // Select device
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                MoveRotateDevice device =
                    hit.collider.GetComponentInParent<MoveRotateDevice>();

                if (device != null)
                {
                    selectedDevice = device;

                    if (device == this)
                    {
                        dragging = true;

                        dragPlane = new Plane(
                            Vector3.up,
                            new Vector3(0, transform.position.y, 0)
                        );

                        if (dragPlane.Raycast(ray, out float distance))
                        {
                            offset = transform.position -
                                     ray.GetPoint(distance);
                        }
                    }
                }
            }
        }


        if (dragging && Input.GetMouseButton(0))
        {
            if (rotateMode)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                // Rotate horizontally
                transform.Rotate(
                    cam.transform.up,
                    mouseX * rotationSpeed,
                    Space.World
                );

                // Rotate vertically
                transform.Rotate(
                    cam.transform.right,
                    -mouseY * rotationSpeed,
                    Space.World
                );
            }
            else
            {
                // Move device
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);

                if (dragPlane.Raycast(ray, out float distance))
                {
                    transform.position =
                        ray.GetPoint(distance) + offset;
                }
            }
        }


        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
    }
}
