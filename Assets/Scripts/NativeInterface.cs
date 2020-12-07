using System;
using System.Runtime.InteropServices;

public static class NativeInterface
{
    
#if UNITY_IOS
    const var dll = "__Internal";
#else
    const string dll = "skinning_COR";
#endif

#region DLLImports
    [DllImport(dll)]
    public static extern IntPtr CreateMesh(
        IntPtr vertices, int vertexCount,
        IntPtr triangles, int triangleCount,
        IntPtr weights, IntPtr bones, int boneCount
        );

    // to handle exceptions and failures
    [DllImport(dll)]
    public static extern IntPtr HasFailedMeshConstruction(IntPtr mesh);

    [DllImport(dll)]
    public static extern void FreeErrorMessage(IntPtr message);

    [DllImport(dll)]
    public static extern void DestroyMesh(IntPtr mesh);

    [DllImport(dll)]
    public static extern int GetRestVertexCount(IntPtr mesh);

    [DllImport(dll)]
    public static extern int GetRestFaceCount(IntPtr mesh);

    // [DllImport(dll)]
    // public static extern int GetSubdividedVertexCount(IntPtr mesh);

    // [DllImport(dll)]
    // public static extern int GetSubdividedFaceCount(IntPtr mesh);

    // get centers of rotation
    [DllImport(dll)]
    public static extern int GetCenterCount(IntPtr mesh);
    
    // vertices pointer should point to an allocated Vector3[] in C#
    [DllImport(dll)]
    public static extern void GetCentersOfRotation(IntPtr mesh,
        IntPtr vertices, int vertexCount);

    [DllImport(dll)]
    public static extern IntPtr HasFailedGettingCentersOfRotation(IntPtr mesh);

    [DllImport(dll)]
    public static extern void SerializeMesh(IntPtr mesh, string path);

    [DllImport(dll)]
    public static extern void ReadCenters(IntPtr mesh, string path);

    [DllImport(dll)]
    public static extern void SerializeCenters(IntPtr mesh, string path);

    [DllImport(dll)]
    public static extern IntPtr SerializationError(IntPtr mesh);

    // [DllImport(dll)]
    // public static extern void SetMeshVertexBuffer(IntPtr mesh, IntPtr vertexBufferHandle);

    [DllImport(dll)] 
    public static extern void Animate(IntPtr mesh,
        IntPtr rotations, IntPtr translations, IntPtr transformed);

    [DllImport(dll)] 
    public static extern IntPtr AnimationError(IntPtr mesh);
#endregion
}