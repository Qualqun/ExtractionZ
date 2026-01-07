using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BloodScreenEffect : MonoBehaviour
{
    public static BloodScreenEffect Instance { get; private set; }
    [Header("Assign in Inspector")]
    public Image bloodBackground;
    public List<Image> bloodStains;
    public float fadeDuration = 1.5f;

    private class StainInfo
    {
        public Image image;
        public float alpha;
        public Coroutine fadeRoutine;

        public StainInfo(Image img)
        {
            image = img;
            alpha = 0f;
            SetAlpha(0f);
        }

        public void SetAlpha(float a)
        {
            alpha = a;
            if (image != null)
            {
                Color color = image.color;
                color.a = a;
                image.color = color;
            }
        }
    }

    private List<StainInfo> activeStains = new List<StainInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        // Initialize all stains to invisible
        foreach (var stain in bloodStains)
        {
            stain.gameObject.SetActive(false);
        }

        // Hide background
        if (bloodBackground != null)
        {
            var bgColor = bloodBackground.color;
            bgColor.a = 0f;
            bloodBackground.color = bgColor;
            bloodBackground.gameObject.SetActive(false);
        }
    }

    public void ShowBloodStain()
    {
        Debug.Log("[ShowBloodStain] - ");
        if (bloodStains.Count == 0) return;

        // Enable background if not already
        if (bloodBackground != null && !bloodBackground.gameObject.activeSelf)
        {
            bloodBackground.gameObject.SetActive(true);
            SetImageAlpha(bloodBackground, 0.3f);
        }

        // Pick random stain
        var randomStain = bloodStains[Random.Range(0, bloodStains.Count)];
        if (!randomStain.gameObject.activeSelf)
            randomStain.gameObject.SetActive(true);

        var stainInfo = new StainInfo(randomStain);
        activeStains.Add(stainInfo);

        if (stainInfo.fadeRoutine != null)
            StopCoroutine(stainInfo.fadeRoutine);

        stainInfo.fadeRoutine = StartCoroutine(FadeStain(stainInfo));
    }

    private IEnumerator FadeStain(StainInfo stainInfo)
    {
        stainInfo.SetAlpha(1f);
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(1f - (time / fadeDuration));
            stainInfo.SetAlpha(t);
            yield return null;
        }

        stainInfo.SetAlpha(0f);
        stainInfo.image.gameObject.SetActive(false);
        activeStains.Remove(stainInfo);

        // If no more stains are active, fade background
        if (activeStains.Count == 0 && bloodBackground != null)
        {
            StartCoroutine(FadeBackground());
        }
    }

    private IEnumerator FadeBackground()
    {
        float startAlpha = bloodBackground.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(1f - (time / fadeDuration));
            SetImageAlpha(bloodBackground, t * startAlpha);
            yield return null;
        }

        SetImageAlpha(bloodBackground, 0f);
        bloodBackground.gameObject.SetActive(false);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        var c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
