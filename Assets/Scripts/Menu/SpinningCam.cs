using UnityEngine;

public class SpinningCamera : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 10f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}