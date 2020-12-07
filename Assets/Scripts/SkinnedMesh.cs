using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.IO;

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
    /// <summary>
    /// Unity mesh
    /// </summary>
    private Mesh mesh;

    /// <summary>
    /// Reference to the renderer, used for bone counts
    /// </summary>
    private SkinnedMeshRenderer renderer;

    /// <summary>
    /// Pointer to the cpp object
    /// </summary>
    private IntPtr _cppMesh;
    
    /// <summary>
    /// File where to store the centers of rotation
    /// </summary>
    private string centersFile;

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

        // compute centers of rotation, or load it if already available
        this.centersFile = Path.Combine(Application.streamingAssetsPath, mesh.name);
        int centerCount;
        if (File.Exists(this.centersFile + ".centers"))
        {
            ReadCenters(this.centersFile);
            centerCount = GetCenterCount();
        }
        else
        {
            centerCount = GetCenterCount();
            if (centerCount != 0)
            {
                SerializeCenters(this.centersFile);
            }
        }

    }

    /// <summary>
    /// Instantiates a mesh object inside cpp plugin and stores
    /// a reference to it in this class
    /// </summary>
    private void SendSkinnedMesh()
    {
        // The plugin will want to modify the vertex buffer -- on many platforms
		// for that to work we have to mark mesh as "dynamic" (which makes the buffers CPU writable --
		// by default they are immutable and only GPU-readable).
		mesh.MarkDynamic ();

        var vertices = mesh.vertices; // Vector3[]
        var faces = mesh.triangles.Clone() as int[]; // int[]
        var nativeWeights = mesh.GetAllBoneWeights(); // NativeArray<BoneWeight1>
        var boneCounts = mesh.GetBonesPerVertex().ToArray(); // byte[]

        Debug.Assert(boneCounts.Length == vertices.Length, "Bone per vertex array has different length from vertex array");

        // copy because original struct is not interop-friendly
        int largestBoneIndex = 0;
        Bone[] weights = new Bone[nativeWeights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            var native = nativeWeights[i];
            weights[i] = new Bone(native.boneIndex, native.weight);

            largestBoneIndex = native.boneIndex > largestBoneIndex 
                ? native.boneIndex : largestBoneIndex;
        }
        Debug.Log($"Largest bone index in mesh {mesh.name}: {largestBoneIndex}");

        GCHandle gcVertices = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        GCHandle gcFaces = GCHandle.Alloc(faces, GCHandleType.Pinned);
        GCHandle gcWeights = GCHandle.Alloc(weights, GCHandleType.Pinned);
        GCHandle gcBoneCounts = GCHandle.Alloc(boneCounts, GCHandleType.Pinned);

        var cppMesh = NativeInterface.CreateMesh(gcVertices.AddrOfPinnedObject(), mesh.vertexCount,
            gcFaces.AddrOfPinnedObject(), faces.Length / 3,
            gcWeights.AddrOfPinnedObject(), gcBoneCounts.AddrOfPinnedObject(),
            largestBoneIndex + 1);

        gcVertices.Free();
        gcFaces.Free();
        gcWeights.Free();
        gcBoneCounts.Free();

        string errorMessage = ExtractFailureMessage( NativeInterface.HasFailedMeshConstruction(cppMesh) );
        if (errorMessage.Equals(""))
            this._cppMesh = cppMesh;
        else{
            NativeInterface.DestroyMesh(cppMesh);
            throw new Exception(errorMessage);
        }

        // send pointer to the mesh buffer
        // if (mesh.vertexBufferCount > 1)
        //     Debug.LogWarning("There are more than one vertex buffer: " + mesh.vertexBufferCount);
        // NativeInterface.SetMeshVertexBuffer(this._cppMesh, mesh.GetNativeVertexBufferPtr(0));
    }

    /// <summary>
    /// Helper method that extracts a string from the pointer returned
    /// by the cpp plugin and frees the associated memory of the C++ string
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private string ExtractFailureMessage(IntPtr message)
    {
        var errorMessage = Marshal.PtrToStringAnsi(message);
        NativeInterface.FreeErrorMessage(message);
        return errorMessage;
    }

    // Destroys the cpp object
    ~SkinnedMesh()
    {
        NativeInterface.DestroyMesh(this._cppMesh);
    }

    // Wrappers for DLL functions

    // Simple functions
    /// <summary>
    /// Get the number of vertices in the rest pose, no subdivision
    /// </summary>
    /// <returns>Number of vertices</returns>
    public int GetRestVertexCount() => NativeInterface.GetRestVertexCount(this._cppMesh);

    /// <summary>
    /// Get the number of triangles in the rest pose, no subdivision
    /// </summary>
    /// <returns>Number of triangles</returns>
    public int GetRestFaceCount() => NativeInterface.GetRestFaceCount(this._cppMesh);
    // public int GetSubdividedFaceCount() => NativeInterface.GetSubdividedFaceCount(this._cppMesh);
    // public int GetSubdividedVertexCount() => NativeInterface.GetSubdividedVertexCount(this._cppMesh);

    /// <summary>
    /// Get the number of center of rotations in the mesh.
    /// It will computed it if the number is not defined.
    /// </summary>
    /// <returns></returns>
    public int GetCenterCount() {
        var count = NativeInterface.GetCenterCount(this._cppMesh);
        var error = ExtractFailureMessage(NativeInterface.HasFailedGettingCentersOfRotation(_cppMesh) );
        if (error.Equals("")) return count;
        else 
        {
            NativeInterface.DestroyMesh(this._cppMesh);
            throw new Exception(error);
        }
    }

    /// <summary>
    /// Get a list of centers of rotations from the cpp mesh, copied
    /// </summary>
    /// <returns></returns>
    public Vector3[] GetCentersOfRotation()
    {
        int count = GetCenterCount();
        var centers = new Vector3[count];
        var handle = GCHandle.Alloc(centers, GCHandleType.Pinned);
        NativeInterface.GetCentersOfRotation(this._cppMesh, handle.AddrOfPinnedObject(), count);
        handle.Free();
        
        return centers;
    }

    /// <summary>
    /// Serialize the entire mesh on disk, including vertices,
    /// faces, weights and centers of rotation (if available)
    /// </summary>
    /// <param name="path">A folder for where to store the files</param>
    public void Serialize(string path)
    {
        NativeInterface.SerializeMesh(this._cppMesh, Path.Combine(path, this.mesh.name));

        var error = ExtractFailureMessage(NativeInterface.SerializationError(this._cppMesh));
        if (!error.Equals(""))
        {
            NativeInterface.DestroyMesh(this._cppMesh);
            throw new Exception(error);
        }
    }

    /// <summary>
    /// Serialize the centers of rotation, if already computed.
    /// Otherwise, it will throw an exception.
    /// </summary>
    /// <param name="path">A base name for where to store the file</param>
    public void SerializeCenters(string path)
    {
        NativeInterface.SerializeCenters(this._cppMesh, path);

        var error = ExtractFailureMessage(NativeInterface.SerializationError(this._cppMesh));
        if (!error.Equals(""))
        {
            NativeInterface.DestroyMesh(this._cppMesh);
            throw new Exception(error);
        }
    }

    /// <summary>
    /// Read a previously serialized file with centers of rotation
    /// </summary>
    /// <param name="path"></param>
    public void ReadCenters(string path)
    {
        NativeInterface.ReadCenters(this._cppMesh, path);

        var error = ExtractFailureMessage(NativeInterface.SerializationError(this._cppMesh));
        if (!error.Equals(""))
        {
            NativeInterface.DestroyMesh(this._cppMesh);
            throw new Exception(error);
        }
    }

    /// <summary>
    /// To be called on FixedUpdate every frame
    /// </summary>
    public void Animate(Vector4[] rotations, Vector3[] translations)
    {
        GCHandle gcRotations = GCHandle.Alloc(rotations, GCHandleType.Pinned);
        GCHandle gcTranslations = GCHandle.Alloc(translations, GCHandleType.Pinned);

        // results
        var deformed = new Vector3[this.GetRestVertexCount()];
        deformed.Initialize();
        GCHandle gcDeformed = GCHandle.Alloc(deformed, GCHandleType.Pinned);

        NativeInterface.Animate(this._cppMesh, gcRotations.AddrOfPinnedObject(),
            gcTranslations.AddrOfPinnedObject(), gcDeformed.AddrOfPinnedObject());

        gcRotations.Free();
        gcTranslations.Free();
        gcDeformed.Free();

        string errorMessage = ExtractFailureMessage( NativeInterface.AnimationError(this._cppMesh));
        if (!errorMessage.Equals(""))
        {
            NativeInterface.DestroyMesh(this._cppMesh);
            throw new Exception(errorMessage);
        }

        this.mesh.SetVertices(deformed);
    }
}