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

    // Start is called before the first frame update
    void Start()
    {
        foreach (var renderer in skinnedMeshRenderers)
        {
            meshes.Add(new SkinnedMesh(renderer.sharedMesh, renderer));
        }
    }
}
