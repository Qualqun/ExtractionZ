using System;
using UnityEngine;
using UnityEngine.UI;

public class ExtractionInfoUi : MonoBehaviour
{

    [SerializeField] Vector3 allyExtractionPos;
    [SerializeField] Vector3 enemyExtractionPos;

    [SerializeField] GameObject allyExtractionHUD;
    [SerializeField] GameObject enemyExtractionHUD;


    [SerializeField]
    float screenOffset = 25f;

    [SerializeField]
    float minSizeDistance = 5f;

    [SerializeField]
    float maxSizeDistance = 25f;

    [SerializeField]
    Vector2 minimumSize = new Vector2(37.5f, 50f);

    [SerializeField]
    Sprite arrowSprite;

    Image indicator;

    Sprite indicatorSprite;

    RectTransform rectTransform;


    private void Start()
    {
        //Swap team for blue 
        if(Game.Instance.playerTeam == (int) PlayerTeam.BLUE)
        {
            Vector3 temp = allyExtractionPos;
            allyExtractionPos = enemyExtractionPos;
            enemyExtractionPos = temp;  
        }

        StopHud();
    }
    // Update is called once per frame
    void Update()
    {
        if ((allyExtractionHUD.activeSelf || enemyExtractionHUD.activeSelf) && Camera.main != null)
        {
            Vector3 hudPos = allyExtractionHUD.activeSelf ? allyExtractionPos : enemyExtractionPos;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(hudPos);

            if (screenPos.z > 0)
            {
                float pourcentage = (Mathf.Clamp(Vector3.Distance(Camera.main.transform.position, hudPos), minSizeDistance, maxSizeDistance) - minSizeDistance) * 1 / (maxSizeDistance - minSizeDistance); // produit en croix pour du pourcentage

                rectTransform.position = new Vector3(Mathf.Clamp(screenPos.x, screenOffset, Screen.width - screenOffset), Mathf.Clamp(screenPos.y, screenOffset, Screen.height - screenOffset), 0);
                rectTransform.sizeDelta = minimumSize + (minimumSize * (1 - pourcentage)) * 4;

                DetectScreenEdge();

                indicator.enabled = true;
            }
            else
            {
                indicator.enabled = false;
            }
        }

    }


    public void StopHud()
    {
        if (indicator != null && indicatorSprite != null)
        {
            indicator.sprite = indicatorSprite;
        }

        allyExtractionHUD.SetActive(false);
        enemyExtractionHUD.SetActive(false);
    }

    public void ActiveExtractionHUD(bool isAlly = true)
    {
        
        if (isAlly)
        {
            allyExtractionHUD.SetActive(true);

            indicator = allyExtractionHUD.GetComponent<Image>();
            indicatorSprite = indicator.sprite;
            rectTransform = allyExtractionHUD.GetComponent<RectTransform>();
        }
        else
        {
            enemyExtractionHUD.SetActive(true);

            indicator = enemyExtractionHUD.GetComponent<Image>();
            indicatorSprite = indicator.sprite;
            rectTransform = enemyExtractionHUD.GetComponent<RectTransform>();
        }

        DetectScreenEdge();
    }


    void DetectScreenEdge()
    {
        float offSetEdge = 50f;
        float edgeScreenWidth = Screen.width - screenOffset - offSetEdge;
        float edgeScreenHeight = Screen.height - screenOffset - offSetEdge;

        Vector2 edgeLogo = new Vector2(0, 0);
        //Debug.Log("width edge " + edgeScreenWidth + "  height " + edgeScreenHeight + " posx " + );

        if (rectTransform.position.x < screenOffset + offSetEdge)
        {
            edgeLogo.x = -1;
        }
        else if (rectTransform.position.x > edgeScreenWidth)
        {
            edgeLogo.x = 1;
        }

        if (rectTransform.position.y < screenOffset + offSetEdge)
        {
            edgeLogo.y = -1;
        }
        else if (rectTransform.position.y > edgeScreenHeight)
        {
            edgeLogo.y = 1;
        }

        //Swap Sprite
        if ((edgeLogo.x != 0 || edgeLogo.y != 0) && indicator.sprite != arrowSprite)
        {
            indicator.sprite = arrowSprite;
        }
        else if ((edgeLogo.x == 0 && edgeLogo.y == 0) && indicator.sprite != indicatorSprite)
        {
            indicator.sprite = indicatorSprite;
            rectTransform.rotation = Quaternion.Euler(Vector3.zero);
        }

        //Check if that a diagonal
        if (edgeLogo.x != 0 && edgeLogo.y != 0)
        {
            if (edgeLogo.x + edgeLogo.y > 0)
            {
                Debug.Log("Logo diagonale haut droite");

                rectTransform.rotation = Quaternion.Euler(0, 0, -45);
            }
            else if (edgeLogo.x + edgeLogo.y < 0)
            {
                Debug.Log("Logo diagonale bas gauche");
                rectTransform.rotation = Quaternion.Euler(0, 0, 135);
            }
            else
            {
                if (edgeLogo.x == -1)
                {
                    Debug.Log("Logo diagonale haut gauche");
                    rectTransform.rotation = Quaternion.Euler(0, 0, 45);
                }
                else if (edgeLogo.x == 1)
                {
                    Debug.Log("Logo diagonale bas droite");
                    rectTransform.rotation = Quaternion.Euler(0, 0, -135);
                }
            }
        }
        else
        {
            //Width check
            if (edgeLogo.x != 0)
            {
                if (edgeLogo.x == -1)
                {
                    Debug.Log("Logo gauche");
                    rectTransform.rotation = Quaternion.Euler(0, 0, 90);
                }
                else if (edgeLogo.x == 1)
                {
                    Debug.Log("Logo droite");
                    rectTransform.rotation = Quaternion.Euler(0, 0, -90);
                }
            }
            else // height check
            {
                if (edgeLogo.y == -1)
                {
                    Debug.Log("Logo bas");
                    rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                }
                else if (edgeLogo.y == 1)
                {
                    Debug.Log("Logo haut");
                    rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                }
            }
        }

    }


}
