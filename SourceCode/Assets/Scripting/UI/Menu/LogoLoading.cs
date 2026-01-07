#if !UNITY_SERVER
using UnityEngine;

public class LogoLoading : MonoBehaviour
{
    [SerializeField]
    float speed = 80;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, -speed * Time.deltaTime);
    }
}
#endif
