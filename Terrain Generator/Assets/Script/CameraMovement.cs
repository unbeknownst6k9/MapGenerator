using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float sensetivity = 100f;
    public float speed = 50f;
    public float xRotation = 0f;
    public float yRotation = 0f;
    //Transform camera;
    // Start is called before the first frame update
    void Start()
    {
        //camera = gameObject.GetComponent(typeof(Transform));
        Debug.Log(gameObject.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        movement();
    }
    //control camera: wasd to mvoe camera, and shift to rotate

    private void movement()
    {
        //rotate camera

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            float LR = Input.GetAxis("Horizontal") * sensetivity * Time.deltaTime;
            float UD = Input.GetAxis("Vertical") * sensetivity * Time.deltaTime;
            xRotation -= UD;
            yRotation += LR;
            gameObject.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            //gameObject.transform.localRotation = Quaternion.Euler(0f , yRotation, 0f);
        }
        else
        {
            float x = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Height");
            float z = Input.GetAxis("Vertical");

            Vector3 move = (gameObject.transform.right * x + gameObject.transform.forward * z) * Time.deltaTime * speed;
            move.y = moveY * Time.deltaTime * speed;

            gameObject.transform.position += move;
        }
    }
}
