
using TMPro;
using UnityEngine;

public class FPSShow : MonoBehaviour
{
#if !UNITY_SERVER
    public TextMeshProUGUI fpsText; // Référence au TextMeshProUGUI
    private float deltaTime;

    void Update()
    {
        // Calculer le temps écoulé entre les images
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // Calculer les FPS
        float fps = 1.0f / deltaTime;

        // Afficher les FPS dans le texte
        fpsText.text = $"FPS: {Mathf.Ceil(fps)}";
    }
#endif
}

