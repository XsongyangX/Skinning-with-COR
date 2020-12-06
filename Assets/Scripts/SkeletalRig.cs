using System.Collections;
using System.Collections.Generic;

using UnityEngine;

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
}