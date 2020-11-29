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

    public SkinnedMesh(Mesh mesh, SkinnedMeshRenderer renderer)
    {
        this.mesh = mesh;
        this.renderer = renderer;

        SendSkinnedMesh();

        Debug.Log("First five faces");
        for (int i = 0; i < 3 * 5; i += 3)
        {
            Debug.Log(mesh.triangles[i] + "" + mesh.triangles[i + 1] + "" + mesh.triangles[i + 2]);
        }
    }

    private void SendSkinnedMesh()
    {
        if (mesh.subMeshCount > 0) Debug.Log("Submesh present: " + mesh.subMeshCount);

        var vertices = mesh.vertices; // Vector3[]
        var faces = mesh.triangles.Clone() as int[]; // int[]
        var nativeWeights = mesh.GetAllBoneWeights(); // BoneWeight1[]
        var boneCounts = mesh.GetBonesPerVertex().ToArray(); // byte[]

        var uniqueBones = renderer.bones.Length;

        Debug.Assert(boneCounts.Length == vertices.Length, "Bone per vertex array has different length from vertex array");

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
            gcFaces.AddrOfPinnedObject(), faces.Length,
            gcWeights.AddrOfPinnedObject(), gcBoneCounts.AddrOfPinnedObject(),
            uniqueBones
        );
        this._cppMesh = cppMesh;

        gcVertices.Free();
        gcFaces.Free();
        gcWeights.Free();
        gcBoneCounts.Free();
    }

#if UNITY_IOS
    const var dll = "__Internal";
#else
    const string dll = "skinning_COR";
#endif

    [DllImport(dll)]
    private static extern IntPtr CreateMesh(
        IntPtr vertices, int vertexCount,
        IntPtr triangles, int triangleCount,
        IntPtr weights, IntPtr bones, int boneCount
        );
}