using UnityEngine;
using TMPro;
using System;
using System.IO;
using UnityEngine.InputSystem;

public class TutorialUi : MonoBehaviour
{
    [Serializable]
    public struct TutorialPath
    {
        public string title;
        public string description;
    }

    [SerializeField] TutorialPath[] tutorialPath;
#if !UNITY_SERVER
    [SerializeField] AK.Wwise.Event[] soundTutorial;
#endif
    public int tutorialStep = 0;

    TMP_Text[] textsTutorial;
    PlayerInputs playerInput;

    [SerializeField] int indexTutoriel = 0;
    int fading = 1;
    bool isFading = false;
    bool initialized = false;

    bool isRun = false;
    ExtractionInfoUi extractionInfo;

    float timerExplanation;
    float timeExplanation = 5f;
    bool lastSpeech = true;
    bool combatSpeech = true;

    private void Start()
    {
        extractionInfo = FindAnyObjectByType<ExtractionInfoUi>();

        timerExplanation = 9f;
    }

    void Update()
    {
#if !UNITY_SERVER
        if (Game.Instance.playerList.Count > 0 && !initialized)
        {
            textsTutorial = GetComponentsInChildren<TMP_Text>(); // table of 2 first title, second description
            playerInput = Game.Instance.playerList[0].GetPlayerInputs;

            ApplyTutorialText(tutorialPath[0]);

            playerInput.FindAction("Move").performed += OnMovePerformed;
            playerInput.FindAction("Run").performed += OnRunPerformed;
            playerInput.FindAction("Crouch").performed += OnCrouchPerformed;
            playerInput.FindAction("Jump").performed += OnJumpPerformed;
            playerInput.FindAction("Shoot").performed += OnShootPerformed;
            playerInput.FindAction("Aim").performed += OnAimPerformed;

            PostSoundTuto(0);
            initialized = true;
        }
#endif
        if (Game.Instance.teamWin != -1 && lastSpeech)
        {
            PostSoundTuto(6);
            lastSpeech = false;
        }

        if (initialized)
        {
            float fadeStatus = ApplyFade(Time.deltaTime * fading);

            isFading = fadeStatus == 1f;

            if (indexTutoriel == 5 && tutorialStep == 2)
            {
                fading = -1;
                if (combatSpeech)
                {
                    PostSoundTuto(3);
                    combatSpeech = false;
                }
            }

            Debug.Log("Fade status " + isFading);

            //Start Explanation for objective

            if (isFading && indexTutoriel == 8)
            {
                if (timerExplanation == 9f)
                {
                    extractionInfo.ActiveExtractionHUD(false);
                    PostSoundTuto(4);
                }

                timerExplanation -= Time.deltaTime;

                Debug.Log("Fade timer " + timerExplanation);

                if (timerExplanation < 0)
                {
                    extractionInfo.StopHud();
                    fading = -1;
                    timerExplanation = timeExplanation;
                }
            }

            if (isFading && indexTutoriel == 9)
            {
                if (timerExplanation == timeExplanation)
                {
                    extractionInfo.ActiveExtractionHUD(true);
                    PostSoundTuto(5);
                }

                timerExplanation -= Time.deltaTime;

                if (timerExplanation < 0)
                {
                    extractionInfo.StopHud();
                    fading = -1;
                    timerExplanation = timeExplanation;
                }
            }
        }

    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (isFading && indexTutoriel == 0)
        {
            fading = -1;
        }
    }

    private void OnRunPerformed(InputAction.CallbackContext context)
    {
        if (isFading && indexTutoriel == 1)
        {
            fading = -1;
        }
        else if (isFading && indexTutoriel == 3)
        {
            isRun = true;
        }
    }

    private void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        if (isFading && indexTutoriel == 3 && isRun)
        {
            fading = -1;
        }


        if (isFading && indexTutoriel == 2)
        {
            fading = -1;
            PostSoundTuto(1);

        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isFading && indexTutoriel == 4)
        {
            fading = -1;
            PostSoundTuto(2);
        }
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (isFading && indexTutoriel == 6)
        {
            fading = -1;

        }
    }

    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        if (isFading && indexTutoriel == 7)
        {
            fading = -1;
        }
    }

    void ApplyTutorialText(TutorialPath path)
    {
        textsTutorial[0].text = path.title;
        textsTutorial[1].text = path.description;
    }

    float ApplyFade(float fadingRation)
    {
        Color alphaColor = textsTutorial[0].color;
        bool canApplyText =
            (indexTutoriel < 3 && tutorialStep == 0) ||
            (indexTutoriel < 5 && tutorialStep == 1) ||
            tutorialStep == 2;



        alphaColor.a += fadingRation;
        alphaColor.a = alphaColor.a >= 1f ? 1f : alphaColor.a;



        if (alphaColor.a < 0f)
        {
            alphaColor.a = 0f;

            if (canApplyText)
            {
                fading = 1;
                indexTutoriel++;

                if (tutorialPath.Length >= indexTutoriel)
                {
                    ApplyTutorialText(tutorialPath[indexTutoriel]);
                }
                else
                {
                    Debug.LogError(this.ToString() + " - Hors range tutorial path");
                }
            }

        }

        textsTutorial[0].color = alphaColor;
        textsTutorial[1].color = alphaColor;

        return alphaColor.a;
    }

    void PostSoundTuto(int index)
    {
#if !UNITY_SERVER
        soundTutorial[index].Post(Game.Instance.playerList[0].GetComponentInChildren<PedMonobehaviour>().gameObject);
#endif
    }
}
