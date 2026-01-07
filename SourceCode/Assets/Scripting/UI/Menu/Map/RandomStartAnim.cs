using UnityEngine;

public class RandomStartAnim : MonoBehaviour
{

    Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float randomTime = Random.Range(0f, 1f);

        animator = GetComponent<Animator>();
        animator.Play("Run", 0, randomTime);
    }

}
