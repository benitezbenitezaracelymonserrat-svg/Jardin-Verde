using UnityEngine;
using UnityEngine.EventSystems;

public class CamaraFollow : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public float distancia = 5f;
    
    public float sensibilidadX = 3f;
    public float sensibilidadY = 3f;
    
    public float limiteMinY = -10f;
    public float limiteMaxY = 60f;

    private float rotacionX = 0f;
    private float rotacionY = 0f;

    void Start()
    {
        if (objetivo != null)
        {
            rotacionY = objetivo.eulerAngles.y;
        }
        // Dejar el cursor libre por defecto al iniciar
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Al presionar click derecho, ocultar y bloquear cursor para rotar
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // Al soltar click derecho, liberar y mostrar cursor
        if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Solo rotar cuando se mantiene presionado el click derecho
        if (Input.GetMouseButton(1))
        {
            rotacionY += Input.GetAxis("Mouse X") * sensibilidadX;
            rotacionX -= Input.GetAxis("Mouse Y") * sensibilidadY;
            rotacionX = Mathf.Clamp(rotacionX, limiteMinY, limiteMaxY);
        }

        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0);
        Vector3 direccion = new Vector3(0, 0, -distancia);
        
        transform.position = objetivo.position + offset + (rotacion * direccion);
        transform.rotation = rotacion;
    }
}