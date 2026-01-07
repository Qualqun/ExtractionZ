using Unity.Entities;
using UnityEngine;


public class ExtractionAuthoring : MonoBehaviour
{
    [SerializeField]
    float sizeZone = 5f;

    [SerializeField]
    PlayerTeam teamSpawn;

    [SerializeField]
    Color colorExtraction = Color.white;

    public class ExtractionBaker : Baker<ExtractionAuthoring>
    {
        public override void Bake(ExtractionAuthoring authoring)
        {
            Entity extractionEntity = GetEntity(TransformUsageFlags.None);
            ExtractionZone extractionZone = new ExtractionZone();

            //ExtractionInfoUi extractionInfo = FindAnyObjectByType<ExtractionInfoUi>();


            //if (Game.Instance.playerTeam == (int)authoring.teamSpawn)
            //{
            //    extractionInfo.allyExtractionPos = authoring.transform.position;
            //}
            //else
            //{
            //    extractionInfo.enemyExtractionPos = authoring.transform.position;
            //}

            extractionZone.positionPoint = authoring.transform.position;
            extractionZone.sizeZones = authoring.sizeZone;
            extractionZone.teamId = (int)authoring.teamSpawn;

            AddComponent(extractionEntity, extractionZone);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = colorExtraction;
        Gizmos.DrawWireSphere(transform.position, sizeZone);
    }
}
