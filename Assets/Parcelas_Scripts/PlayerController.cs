using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float gravedad = -9.81f;
    public float velocidadRotacion = 10f;
    [Header("Cámara")]
    public Transform camara;
    [Header("Sonido de pasos")]
    public AudioClip sonidoPasos;
    public float intervaloPasos = 0.4f;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private AudioSource audioSource;
    private float temporizadorPasos = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (camara == null)
            camara = Camera.main.transform;
    }
    void Update()
    {
        Mover();
    }
    void Mover()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 direccion = camara.forward * v + camara.right * h;
        direccion.y = 0f;
        direccion.Normalize();
        if (direccion.magnitude > 0.1f)
        {
            controller.Move(direccion * velocidad * Time.deltaTime);
            Quaternion rotObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, rotObjetivo,
                velocidadRotacion * Time.deltaTime
            );

            if (controller.isGrounded)
            {
                temporizadorPasos -= Time.deltaTime;
                if (temporizadorPasos <= 0f)
                {
                    ReproducirPaso();
                    temporizadorPasos = intervaloPasos;
                }
            }
        }
        else
        {
            temporizadorPasos = 0f;
        }

        if (controller.isGrounded)
            velocidadVertical.y = -2f;
        else
            velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    void ReproducirPaso()
    {
        if (audioSource != null && sonidoPasos != null)
        {
            audioSource.PlayOneShot(sonidoPasos);
        }
    }
}