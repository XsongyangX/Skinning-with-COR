using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

/// <summary>
/// Struct for marshaling data
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Bone
{
    public int vertexIndex;
    public float weight;

    public Bone(int index, float weight)
    {
        this.vertexIndex = index;
        this.weight = weight;
    }
}

public class SkinnedMesh
{
    private Mesh mesh;

    private SkinnedMeshRenderer renderer;

    private IntPtr _cppMesh;

    /// <summary>
    /// Creates a skinned mesh in conjunction with cpp by copying
    /// the current mesh data to the plugin using pointers only
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="renderer"></param>
    public SkinnedMesh(Mesh mesh, SkinnedMeshRenderer renderer)
    {
        this.mesh = mesh;
        this.renderer = renderer;

        SendSkinnedMesh();

        // get debug data from C++ plugin
        Debug.Log("Vertex count from cpp: " + GetRestVertexCount());
        Debug.Log("Face count from cpp: " + GetRestFaceCount());

        // serialize for work outside of unity
        string path = "C:/Users/Song/Documents/UDEM/ift6113/project/skinning_cor/logs/"
                    + mesh.name;
        Serialize(path);
    }

    private void SendSkinnedMesh()
    {
        //if (mesh.subMeshCount > 0) Debug.Log("Submesh present: " + mesh.subMeshCount);

        var vertices = mesh.vertices; // Vector3[]
        var faces = mesh.triangles.Clone() as int[]; // int[]
        var nativeWeights = mesh.GetAllBoneWeights(); // NativeArray<BoneWeight1>
        var boneCounts = mesh.GetBonesPerVertex().ToArray(); // byte[]

        var uniqueBones = renderer.bones.Length;

        Debug.Assert(boneCounts.Length == vertices.Length, "Bone per vertex array has different length from vertex array");

        // copy because original struct is not interop-friendly
        Bone[] weights = new Bone[nativeWeights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            var native = nativeWeights[i];
            weights[i] = new Bone(native.boneIndex, native.weight);
        }

        GCHandle gcVertices = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        GCHandle gcFaces = GCHandle.Alloc(faces, GCHandleType.Pinned);
        GCHandle gcWeights = GCHandle.Alloc(weights, GCHandleType.Pinned);
        GCHandle gcBoneCounts = GCHandle.Alloc(boneCounts, GCHandleType.Pinned);

        var cppMesh = CreateMesh(gcVertices.AddrOfPinnedObject(), mesh.vertexCount,
            gcFaces.AddrOfPinnedObject(), faces.Length / 3,
            gcWeights.AddrOfPinnedObject(), gcBoneCounts.AddrOfPinnedObject(),
            uniqueBones);

        gcVertices.Free();
        gcFaces.Free();
        gcWeights.Free();
        gcBoneCounts.Free();

        // checking for correct construction
        for (int i = 0; i < 5; i += 3)
        {
            Debug.Log("Triangle " + i + ": " + faces[i] + faces[i + 1] + faces[i + 2]);
        }
        string errorMessage = ExtractFailureMessage( HasFailedMeshConstruction(cppMesh) );
        if (errorMessage.Equals(""))
            this._cppMesh = cppMesh;
        else{
            DestroyMesh(cppMesh);
            throw new Exception(errorMessage);
        }
    }

    private string ExtractFailureMessage(IntPtr message)
    {
        var errorMessage = Marshal.PtrToStringAnsi(message);
        FreeErrorMessage(message);
        return errorMessage;
    }

    ~SkinnedMesh()
    {
        DestroyMesh(this._cppMesh);
    }

    // Wrappers for DLL functions

    // Simple functions
    public int GetRestVertexCount() => GetRestVertexCount(this._cppMesh);
    public int GetRestFaceCount() => GetRestFaceCount(this._cppMesh);
    public int GetSubdividedFaceCount() => GetSubdividedFaceCount(this._cppMesh);
    public int GetSubdividedVertexCount() => GetSubdividedVertexCount(this._cppMesh);
    public int GetCenterCount() {
        var count = GetCenterCount(this._cppMesh);
        var error = ExtractFailureMessage( HasFailedGettingCentersOfRotation(_cppMesh) );
        if (error.Equals("")) return count;
        else throw new Exception(error);
    }

    // Reading centers of rotations from plugin
    public Vector3[] GetCentersOfRotation()
    {
        int count = GetCenterCount();
        var centers = new Vector3[count];
        var handle = GCHandle.Alloc(centers, GCHandleType.Pinned);
        GetCentersOfRotation(this._cppMesh, handle.AddrOfPinnedObject(), count);
        handle.Free();
        
        return centers;
    }
    
    public void Serialize(string path)
    {
        SerializeMesh(this._cppMesh, path);
    }

#if UNITY_IOS
    const var dll = "__Internal";
#else
    const string dll = "skinning_COR";
#endif

#region DLLImports
    [DllImport(dll)]
    private static extern IntPtr CreateMesh(
        IntPtr vertices, int vertexCount,
        IntPtr triangles, int triangleCount,
        IntPtr weights, IntPtr bones, int boneCount
        );

    // to handle exceptions and failures
    [DllImport(dll)]
    private static extern IntPtr HasFailedMeshConstruction(IntPtr mesh);

    [DllImport(dll)]
    private static extern void FreeErrorMessage(IntPtr message);

    [DllImport(dll)]
    private static extern void DestroyMesh(IntPtr mesh);

    [DllImport(dll)]
    private static extern int GetRestVertexCount(IntPtr mesh);

    [DllImport(dll)]
    private static extern int GetRestFaceCount(IntPtr mesh);

    [DllImport(dll)]
    private static extern int GetSubdividedVertexCount(IntPtr mesh);

    [DllImport(dll)]
    private static extern int GetSubdividedFaceCount(IntPtr mesh);

    // get centers of rotation
    [DllImport(dll)]
    private static extern int GetCenterCount(IntPtr mesh);
    
    // vertices pointer should point to an allocated Vector3[] in C#
    [DllImport(dll)]
    private static extern void GetCentersOfRotation(IntPtr mesh,
        IntPtr vertices, int vertexCount);

    [DllImport(dll)]
    private static extern IntPtr HasFailedGettingCentersOfRotation(IntPtr mesh);

    [DllImport(dll)]
    private static extern void SerializeMesh(IntPtr mesh, string path);
#endregion
}