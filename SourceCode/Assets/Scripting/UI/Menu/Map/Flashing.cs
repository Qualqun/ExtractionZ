using UnityEngine;

public class Flashing : MonoBehaviour
{

    [SerializeField]
    GameObject[] lights;

    [SerializeField]
    float minRand = 0.1f;

    [SerializeField]
    float maxRand = 2f;

    float timer;
    bool state = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = Random.Range(minRand, maxRand);
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;

        if(timer < 0)
        {
            SetLights(state);
            state = !state;
            timer = Random.Range(minRand, maxRand);
        }
    }


    void SetLights(bool state)
    {
        foreach (GameObject lightObject in lights)
        {
            lightObject.SetActive(state);
        }
    }
}
