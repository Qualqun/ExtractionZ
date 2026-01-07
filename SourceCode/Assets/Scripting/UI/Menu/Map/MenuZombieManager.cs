using UnityEngine;

public class MenuZombieManager : MonoBehaviour
{
    [SerializeField] Transform menuZombie;

    [SerializeField] Vector3 start;
    [SerializeField] Vector3 end;
    [SerializeField] float speed;

    [SerializeField] float minRand;
    [SerializeField] float maxRand;

    float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = Random.Range(minRand, maxRand);
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;

        if(Vector3.Distance(start, end) > Vector3.Distance(start, menuZombie.position))
        {
            Vector3 zombiesPos = menuZombie.position;
            zombiesPos.x -= speed * Time.deltaTime;

            menuZombie.position = zombiesPos;
        }

        if (timer < 0)
        {
            menuZombie.position = start;
            timer = Random.Range(minRand, maxRand);
        }
    }
}
