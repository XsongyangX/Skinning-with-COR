using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that connects the COR skinning algorithm to the Unity engine
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorCOR : MonoBehaviour
{

    public SkinnedMeshRenderer[] skinnedMeshRenderers;

    private List<SkinnedMesh> meshes = new List<SkinnedMesh>();

    public Transform rootBone;

    private SkeletalRigNode skeletonRoot;
    private int boneCount;

    // Start is called before the first frame update
    void Start()
    {
        // send the skinned mesh for analysis in the plugin
        foreach (var renderer in skinnedMeshRenderers)
        {
            meshes.Add(new SkinnedMesh(renderer.sharedMesh, renderer));

            Debug.Log($"Bone array size for {renderer.sharedMesh.name} is {renderer.bones.Length}");
        }

        // build the skeleton
        (this.skeletonRoot, this.boneCount) = BuildSkeleton(rootBone, 0);
        Debug.Log($"The mesh has {boneCount} bones explored by the skeleton builder");

        Animate(0);
    }

    /// <summary>
    /// Builds a skeleton recursive using DFS and currying
    /// </summary>
    /// <param name="bone">Transform of the bone in the hierarchy</param>
    /// <param name="index">Index of the bone, 0 for the root and +1 down the list</param>
    /// <returns>Node and number of nodes explored</returns>
    private (SkeletalRigNode, int) BuildSkeleton(Transform bone, int index)
    {
        var node = new SkeletalRigNode(index, bone);
        int nodesExplored = 1;
        // exploration
        for (int i = 0; i < bone.childCount; i++)
        {
            var (child, innerChildrenExplored) = 
                BuildSkeleton(bone.GetChild(i), nodesExplored + index);
            
            node.AddChild(child);
            
            nodesExplored += innerChildrenExplored;
        }

        return (node, nodesExplored);
    }

    private Vector3[] EvaluateBoneTranslations()
    {
        return new Vector3[this.boneCount];
    }

    private Vector4[] EvaluateBoneRotations()
    {
        var identity = new Vector4[this.boneCount];
        var identityQuaternion = Quaternion.identity;
        
        // debug
        Debug.Log(identityQuaternion.ToString());

        for (int i = 0; i < identity.Length; i++)
            identity[i] = new Vector4(
                identityQuaternion.x,
                identityQuaternion.y,
                identityQuaternion.z,
                identityQuaternion.w
            );
        return identity;
    }

    public void Animate(float time)
    {
        var rotations = EvaluateBoneRotations();
        var translations = EvaluateBoneTranslations();

        Debug.Assert(rotations.Length == translations.Length);

        foreach (var mesh in this.meshes)
        {
            mesh.Animate(rotations, translations);
        }
    }
}
