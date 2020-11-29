using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

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
    }

    private void SendSkinnedMesh()
    {
        if (mesh.subMeshCount > 0) Debug.Log("Submesh present: " + mesh.subMeshCount);

        var vertices = mesh.vertices;
        var faces = mesh.triangles;
        var weights = mesh.GetAllBoneWeights();
        var boneCounts = mesh.GetBonesPerVertex();
		
        var uniqueBones = renderer.bones.Length;

        GCHandle gcVertices = GCHandle.Alloc (vertices, GCHandleType.Pinned);
        GCHandle gcFaces = GCHandle.Alloc(faces, GCHandleType.Pinned);
        GCHandle gcWeights = GCHandle.Alloc(weights, GCHandleType.Pinned);
        GCHandle gcBoneCounts = GCHandle.Alloc(boneCounts, GCHandleType.Pinned);

		var cppMesh = CreateMesh(gcVertices.AddrOfPinnedObject(), mesh.vertexCount,
            gcFaces.AddrOfPinnedObject(), faces.Length,
            gcWeights.AddrOfPinnedObject(), gcBoneCounts.AddrOfPinnedObject(),
            uniqueBones
        );
        this._cppMesh = cppMesh;

		gcVertices.Free ();
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