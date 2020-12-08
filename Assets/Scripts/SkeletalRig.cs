using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Wrapper class on the rig
/// </summary>
public class Skeleton
{
    private SkeletalRigNode rootNode { get => this[0]; }

    private List<SkeletalRigNode> _indexed;

    public SkeletalRigNode this[int index]
    {
        get => this._indexed[index];
    }

    public Skeleton(Transform rootBone)
    {
        BuildSkeleton(rootBone, 0);
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
        this._indexed.Add(node);

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

    /// <summary>
    /// Change the rotations from local to world space
    /// </summary>
    /// <param name="rotations">Ordered by bone index</param>
    /// <returns>Ordered by bone index</returns>
    public Quaternion[] FromLocalToWorld(Quaternion[] rotations)
    {
        return null;
    }

    /// <summary>
    /// Change the translations from local to world space
    /// </summary>
    /// <param name="translations">Ordered by bone index</param>
    /// <returns>Ordered by bone index</returns>
    public Vector3[] FromLocalToWorld(Vector3[] translations)
    {
        throw new System.NotImplementedException();
    }
}

/// <summary>
/// A recursive structure holding the skeleton rig of
/// the model
/// </summary>
public class SkeletalRigNode
{
    /// <summary>
    /// Index of the bone at this node
    /// </summary>
    /// <value>Positive value or 0</value>
    public int boneIndex {get;}

    /// <summary>
    /// Reference to the unity Object's transform
    /// </summary>
    public Transform transform {get;}

    /// <summary>
    /// Rest rotation, world space
    /// </summary>
    private Quaternion restRotation;

    /// <summary>
    /// Rest position, world space
    /// </summary>
    private Vector3 restPosition;

    /// <summary>
    /// List of child bones
    /// </summary>
    /// <typeparam name="SkeletalRigNode"></typeparam>
    /// <returns></returns>
    private List<SkeletalRigNode> childBones = new List<SkeletalRigNode>();

    /// <summary>
    /// Initialize a skeletal rig node with a bone index and associated
    /// transform
    /// </summary>
    /// <param name="boneIndex">0 or more</param>
    /// <param name="transform">Unity GameObject</param>
    public SkeletalRigNode(int boneIndex, Transform transform)
    {
        this.boneIndex = boneIndex;
        this.transform = transform;

        var rotation = transform.rotation;
        this.restRotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);

        var position = transform.position;
        this.restPosition = new Vector3(position.x, position.y, position.z);
    }

    /// <summary>
    /// Add children to this node
    /// </summary>
    /// <param name="node">Skeletal rig node</param>
    public void AddChild(SkeletalRigNode node)
    {
        this.childBones.Add(node);
    }

    public IEnumerator<SkeletalRigNode> GetEnumerator() =>
        this.childBones.GetEnumerator();

    /// <summary>
    /// Searches the rig node and children for the name
    /// of a matching game object
    /// </summary>
    /// <param name="name">Name of the object as seen in the scene hierarchy</param>
    /// <returns>Bone index</returns>
    public int BoneIndexRecursive(string name)
    {
        var split = name.Split('/');
        var baseName = split[split.Length - 1];

        if (this.transform.name.Equals(baseName))
            return this.boneIndex;
        else
        {
            if (split.Length != 1)
            {
                var childToExplore = split[1];
                foreach (var child in this)
                {
                    if (child.transform.name.Equals(childToExplore))
                        return child.BoneIndexRecursive(split, 1);
                }
            }
            
            throw new System.Exception($"No bone found with name {name}");
        }
    }

    /// <summary>
    /// Reads the path to search the bone in O(log n)
    /// </summary>
    /// <param name="split">Split hierarchy names</param>
    /// <param name="atDepth">Looking at this index in the split names array</param>
    /// <returns></returns>
    private int BoneIndexRecursive(string[] split, int atDepth)
    {

        // reached a leaf, name was checked before calling
        if (split.Length - 1 == atDepth)
            return this.boneIndex;

        // keep searching
        foreach (var child in this)
        {
            if (child.transform.name.Equals(split[atDepth+1]))
                return child.BoneIndexRecursive(split, atDepth + 1);
        }

        string fullPath = string.Join("/", split);
        throw new System.Exception($"No bone found with name {fullPath}");
    }
}