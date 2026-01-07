using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(DecalProjector))]
public class FadeOutDecal : MonoBehaviour
{
    public float fadeDuration = 30f;

    private DecalProjector decal;
    private Material decalMaterial;
    private float initialAlpha = 1f;
    private float elapsedTime = 0f;

    void Start()
    {
        decal = GetComponent<DecalProjector>();
        initialAlpha = decal.fadeFactor;
    }

    void Update()
    {
        if (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(initialAlpha, 0f, elapsedTime / fadeDuration);
            decal.fadeFactor = alpha;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
