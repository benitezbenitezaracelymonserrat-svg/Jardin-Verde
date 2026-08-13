using UnityEngine;

public class ComidaSuelo : MonoBehaviour
{
    public float duracionComida = 5f;

    void Start()
    {
        // Si no tiene un MeshRenderer en sí mismo ni en sus hijos, creamos un objeto visual básico
        if (GetComponentInChildren<MeshRenderer>() == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f); // Montículo aplanado de comida

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                // Material simple con color amarillo maíz
                rend.material.color = new Color(0.95f, 0.75f, 0.2f);
            }

            // Destruir el collider del objeto visual para que no cause colisiones físicas indeseadas
            Collider col = visual.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
        }

        Destroy(gameObject, duracionComida);
    }
}
