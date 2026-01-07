#if !UNITY_SERVER
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public static class Navigation
{
    public static bool CalculatePath(float3 start, float3 end, out NativeArray<float3> pathCorners)
    {
        var path = new NavMeshPath();
        if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
        {
            pathCorners = new NativeArray<float3>(path.corners.Length, Allocator.Temp);
            for (int i = 0; i < path.corners.Length; i++)
            {
                pathCorners[i] = path.corners[i];
            }
            return true;
        }

        pathCorners = new NativeArray<float3>(0, Allocator.Temp);
        return false;
    }
}
#endif
