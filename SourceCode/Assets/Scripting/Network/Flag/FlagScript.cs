
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;


public class FlagScript : MonoBehaviour
{
#if !UNITY_SERVER
    [SerializeField]
    GameObject flagInteraction;

    [SerializeField]
    float speedCollect = 1f;

    Image loadingImage;

    bool collectStart = false;

    Vector3 defaultPos;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        defaultPos = transform.position;
        loadingImage = GetComponentInChildren<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y <= -8f)
        {
            transform.position = defaultPos;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity flagEntity = ecb.CreateEntity();

            ecb.AddComponent(flagEntity, new FlagRPC { isTaken = false });
            ecb.AddComponent(flagEntity, new SendRpcCommandRequest());

            ecb.Playback(Game.Instance.entityManager);
            ecb.Dispose();
        }

        if (Input.GetKey(KeyCode.E))
        {
            Ray camera = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (Physics.Raycast(camera, out RaycastHit info))
            {
                if (info.collider.CompareTag("Flag"))
                {
                    info.collider.GetComponent<FlagScript>().StartCollect();
                }
            }
        }

        if (collectStart)
        {
            Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));

            loadingImage.fillAmount += Time.deltaTime * speedCollect;

            if (Physics.Raycast(cameraRay, out RaycastHit info))
            {
                if (!info.collider.CompareTag("Flag"))
                {
                    ResetCollect();
                }
            }

            if (loadingImage.fillAmount >= 1f)
            {
                //send rpc info 
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Entity flagEntity = ecb.CreateEntity();

                ecb.AddComponent(flagEntity, new FlagRPC { isTaken = true });
                ecb.AddComponent(flagEntity, new SendRpcCommandRequest());

                ecb.Playback(Game.Instance.entityManager);
                ecb.Dispose();

                ResetCollect();
                flagInteraction.SetActive(false);
            }
        }


    }

    public void DropFlagNetwork()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity flagEntity = ecb.CreateEntity();

        ecb.AddComponent(flagEntity, new FlagRPC { isTaken = false });
        ecb.AddComponent(flagEntity, new SendRpcCommandRequest());

        ecb.Playback(Game.Instance.entityManager);
        ecb.Dispose();
    }

    public void StartCollect()
    {
        collectStart = true;
    }

    public void ResetCollect()
    {
        collectStart = false;
        loadingImage.fillAmount = 0;
    }

    public void DropFlag(Vector3 position, Quaternion rotation)
    {
        flagInteraction.SetActive(true);
        transform.rotation = rotation;
        transform.position = position;

        //if (Physics.Raycast(position + Vector3.up / 2, Vector3.down, out RaycastHit info))
        //{
        //    transform.position = info.point;
        //}
        //else
        //{
        //    transform.position = position;
        //}

    }

    public void TakeFlag()
    {
        flagInteraction.SetActive(false);
    }
#endif
}
