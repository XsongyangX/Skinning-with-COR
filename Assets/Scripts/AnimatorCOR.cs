using System.Collections;
using System.Collections.Generic;
using System.IO;

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

    public GameObject dataPoint;

    [HideInInspector]
    public SkeletalRigNode skeletonRoot
    {
        get
        {
            if (skeleton is null) this.skeleton = new Skeleton(this.rootBone);
            return this.skeleton[0];
        }
    }

    private Skeleton skeleton;

    private int boneCount;

    // Selected animations
    public AnimationClip[] animationClips;

    private Dictionary<string, List<SerializedCurve>> animationCurves = 
        new Dictionary<string, List<SerializedCurve>>();

    private int currentFrame = -1;

    /// <summary>
    /// Index in the animation clips array
    /// </summary>
    public int chosenAnimation = 0;

    public float timeBetweenFrames = 0.05f;

    // Animation cycle
    private void FixedUpdate()
    {
        // check for time out
        if (currentFrame == -1) return;

        // check for animation progress
        AnimationClip animationClip = animationClips[chosenAnimation];
        if (currentFrame * this.timeBetweenFrames < animationClip.length)
        {
            Animate(currentFrame * this.timeBetweenFrames,
                this.animationCurves[animationClip.name]);
            this.currentFrame++;
        }
        else currentFrame = -1;
    }

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
        this.skeleton = new Skeleton(this.rootBone);

        // // visualize centers of rotations
        // foreach (var mesh in this.meshes)
        // {
        //     PlotCenters(mesh);
        // }

        // load serialized animation curves
        // located in streaming assets by default
        foreach (var clip in this.animationClips)
        {
            var curves = ReadSerializedCurves(clip.name);
            this.animationCurves[clip.name] = curves;
        }
    }

    /// <summary>
    /// Reads a serialized animation curve list from disk
    /// </summary>
    /// <param name="clip">Path to the clip, without extensions</param>
    /// <returns></returns>
    private static List<SerializedCurve> ReadSerializedCurves(string clip)
    {
        var clipName = clip + ".curves";
        System.Xml.Serialization.XmlSerializer reader =
            new System.Xml.Serialization.XmlSerializer(typeof(List<SerializedCurve>));
        System.IO.StreamReader file = new System.IO.StreamReader(
            Path.Combine(Application.streamingAssetsPath, clipName));
        List<SerializedCurve> curves = (List<SerializedCurve>)reader.Deserialize(file);
        file.Close();
        return curves;
    }

    /// <summary>
    /// Displays the centers of rotation using cubes
    /// </summary>
    /// <param name="mesh"></param>
    private void PlotCenters(SkinnedMesh mesh)
    {
        var centers = mesh.GetCentersOfRotation();

        foreach (var position in centers)
        {
            Instantiate(dataPoint, position, Quaternion.identity);
        }
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

    public void Animate(float time, List<SerializedCurve> curves)
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
