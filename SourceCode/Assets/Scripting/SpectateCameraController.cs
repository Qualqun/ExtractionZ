using UnityEngine;

//Debug camera spectator for presentation purposes
public class SpectateCameraController : MonoBehaviour
{

    [SerializeField] float speed = 10f;
    [SerializeField] float speedLook = 10f;

    void Update()
    {
        Vector3 movementCameraX = Vector3.zero;
        Vector3 movementCameraY = Vector3.zero;

        movementCameraY.y = Input.mousePositionDelta.x;
        movementCameraX.x = -Input.mousePositionDelta.y;

        transform.GetChild(0).Rotate(movementCameraX * speedLook * Time.deltaTime);
        transform.Rotate(movementCameraY * speedLook * Time.deltaTime);

        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl))
        {
            transform.position -= transform.up * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.GetChild(0).forward * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.GetChild(0).forward * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }
        
        if (Input.GetKey(KeyCode.KeypadPlus))
        {
            speed += 1;
        }

        if (Input.GetKey(KeyCode.KeypadMinus))
        {
            speed -= 1;
        }
    }
}
