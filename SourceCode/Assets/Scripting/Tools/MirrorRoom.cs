#if !UNITY_SERVER
using Unity.Transforms;
using UnityEngine;

[ExecuteInEditMode]
public class MirrorRoom : MonoBehaviour
{
    public bool generateMirror = false;

    void Generate(Transform current, Transform mirrorParent)
    {
        Vector3 mirrorAxis = (Vector3.right - Vector3.forward).normalized;

        if (current.childCount != 0)
        {
            GameObject go = new GameObject(current.name);
            go.transform.parent = mirrorParent;

            go.transform.position = Vector3.Reflect(current.transform.position, mirrorAxis);
            go.transform.rotation = ReflectRotation(current.transform.rotation, mirrorAxis);

            foreach (Transform t in current)
            {
                Generate(t, go.transform);
            }
        }
        else
        {
            GameObject go = Instantiate(current.gameObject);
            go.name = current.name;
            go.transform.parent = mirrorParent;

            go.transform.position = Vector3.Reflect(current.transform.position, mirrorAxis);
            go.transform.rotation = ReflectRotation(current.transform.rotation, mirrorAxis);

        }
    }
    void Update()
    {
        if(generateMirror)
        {
            generateMirror = false;
            Generate(transform, null);
        }
    }

    private Quaternion ReflectRotation(Quaternion source, Vector3 normal)
    {
        return Quaternion.LookRotation(Vector3.Reflect(source * Vector3.forward, normal), Vector3.Reflect(source * Vector3.up, normal));
    }
}
#endif