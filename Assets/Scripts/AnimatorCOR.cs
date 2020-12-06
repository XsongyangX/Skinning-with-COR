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

    // Start is called before the first frame update
    void Start()
    {
        // send the skinned mesh for analysis in the plugin
        foreach (var renderer in skinnedMeshRenderers)
        {
            meshes.Add(new SkinnedMesh(renderer.sharedMesh, renderer));

            Debug.Log($"Bone count for {renderer.sharedMesh.name} is {renderer.bones.Length}");
        }

        // build the skeleton
        int boneCount;
        (this.skeletonRoot, boneCount) = BuildSkeleton(rootBone, 0);
        Debug.Log($"The mesh has {boneCount} bones");

        
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
}
