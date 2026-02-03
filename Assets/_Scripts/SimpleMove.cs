using UnityEngine;

public class SimpleMove : MonoBehaviour
{
    public float speed = 5f;
    public float turnSpeed = 180f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("Движение готово! WASD для движения, мышь для вращения камеры.");
    }

    void Update()
    {
        // Поворот камеры мышью (только если зажата правая кнопка)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * turnSpeed * Time.deltaTime;
            transform.Rotate(0, mouseX, 0);
        }

        // Движение WASD
        float moveHorizontal = Input.GetAxis("Horizontal"); // A/D
        float moveVertical = Input.GetAxis("Vertical");     // W/S

        // Поворот от клавиш A/D
        transform.Rotate(0, moveHorizontal * turnSpeed * Time.deltaTime, 0);

        // Движение вперед/назад
        Vector3 movement = transform.forward * moveVertical * speed;
        rb.MovePosition(rb.position + movement * Time.deltaTime);
    }
}