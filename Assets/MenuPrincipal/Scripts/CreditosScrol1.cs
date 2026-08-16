
using UnityEngine;

public class CreditosScroll : MonoBehaviour
{
    [Header("Movimiento de los créditos")]
    public float velocidad = 32f;

    [Header("Dónde debe detenerse")]
    public float posicionFinalY = 120f;

    private RectTransform rect;
    private Vector2 posicionInicial;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        posicionInicial = rect.anchoredPosition;
    }

    void OnEnable()
    {
        // Cada vez que abras Créditos,
        // vuelve a comenzar desde abajo.
        if (rect == null)
            rect = GetComponent<RectTransform>();

        rect.anchoredPosition = posicionInicial;
    }

    void Update()
    {
        if (rect == null)
            return;

        // Si todavía no llegó a su posición final, sigue subiendo.
        if (rect.anchoredPosition.y < posicionFinalY)
        {
            float nuevaY = Mathf.MoveTowards(
                rect.anchoredPosition.y,
                posicionFinalY,
                velocidad * Time.deltaTime
            );

            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                nuevaY
            );
        }
    }
}
