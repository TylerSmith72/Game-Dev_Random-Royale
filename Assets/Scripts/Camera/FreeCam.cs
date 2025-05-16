using UnityEngine;

public class FreeCam : MonoBehaviour
{
    public float moveSpeed = 30f;
    public float lookSpeed = 100f;

    private float yaw;
    private float pitch;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        // Mouse look
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.eulerAngles = new Vector3(pitch, yaw, 0);

        Vector3 move = new Vector3(
            Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")
        );

        transform.position += transform.TransformDirection(move) * moveSpeed * Time.deltaTime;
        
        float upDown = 0f;
        if (Input.GetKey(KeyCode.Space))
        {
            upDown += 1f;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            upDown -= 1f;
        }
        if (upDown != 0f)
        {
            transform.position += Vector3.up * upDown * moveSpeed * Time.deltaTime;
        }
    }
}