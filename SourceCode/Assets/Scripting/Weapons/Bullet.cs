#if !UNITY_SERVER
using UnityEngine;


public class Bullet : MonoBehaviour
{
    public float lifetime = 5f; // Temps avant destruction (modifiable dans l'inspecteur)
    [SerializeField] private Rigidbody rb;
    public GameObject trail;

    public GameObject impactPrefab;

    void Start()
    {
        
        Destroy(gameObject, lifetime); // Détruit l'objet après 'lifetime' secondes
    }

    public void Initialize(Vector3 direction, float force)
    {
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Vector3 offsetPosition = transform.position + transform.forward * -0.5f;
        //GameObject.Instantiate(impactPrefab, offsetPosition, transform.rotation);

        Destroy(gameObject);
    }
}
#endif
