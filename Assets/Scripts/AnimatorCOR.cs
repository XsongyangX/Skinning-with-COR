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

    // Start is called before the first frame update
    void Start()
    {

    }
}
