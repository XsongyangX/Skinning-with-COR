using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

public class DataSerialization : EditorWindow
{
    AnimatorCOR animated;

    string location = "Assets/StreamingAssets";

    bool serializeMesh = false;
    bool serializeAnimations = true;

    [MenuItem("Window/Skinning COR")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(DataSerialization));
    }

    private void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        animated = EditorGUILayout.ObjectField("Animated Object", animated, typeof(AnimatorCOR),
            allowSceneObjects: true
        ) as AnimatorCOR;

        EditorGUILayout.LabelField("Relative paths start in Assets/");
        location = EditorGUILayout.TextField("Save folder", location);

        serializeMesh = EditorGUILayout.Toggle("Write mesh", serializeMesh);
        serializeAnimations = EditorGUILayout.Toggle("Write animations", serializeAnimations);

        if (GUILayout.Button("Serialize"))
        {
            Serialize();
        }
    }

    private void Serialize()
    {
        if (serializeMesh)
        {
            foreach (var renderer in this.animated.skinnedMeshRenderers)
            {
                var skinnedMesh = new SkinnedMesh(renderer.sharedMesh, renderer);
                skinnedMesh.Serialize(location);
            }
        }

        if (serializeAnimations)
        {
            foreach (var clip in this.animated.animationClips)
            {
                // editor binding
                var bindings = AnimationUtility.GetCurveBindings(clip);

                // animation: bone index + propertyName + curves
                var curves = new List<SerializedCurve>();
                foreach (var binding in bindings)
                {
                    // dont serialize root gameobject component animation curves
                    if (binding.path.Equals("")) continue;

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                    SerializedCurve serialized =
                        new SerializedCurve(
                            this.animated.skeletonRoot.BoneIndexRecursive(binding.path),
                        binding.propertyName, curve
                    );
                    
                    curves.Add(serialized);
                }

                System.Xml.Serialization.XmlSerializer writer =
                    new System.Xml.Serialization.XmlSerializer(typeof(List<SerializedCurve>));

                var path = Path.Combine(location, clip.name + ".curves");
                var file = File.Create(path);

                writer.Serialize(file, curves);
                file.Close();
            }
        }
    }
}

public struct SerializedCurve
{
    public int boneIndex { get; }
    public string propertyName { get; }
    public AnimationCurve curve;

    public SerializedCurve(int boneIndex, string propertyName, AnimationCurve curve)
    {
        this.boneIndex = boneIndex;
        this.propertyName = propertyName;
        this.curve = curve;
    }

    public override bool Equals(object obj)
    {
        return obj is SerializedCurve other &&
               boneIndex == other.boneIndex &&
               propertyName == other.propertyName &&
               EqualityComparer<AnimationCurve>.Default.Equals(curve, other.curve);
    }

    public override int GetHashCode()
    {
        int hashCode = 341329424;
        hashCode = hashCode * -1521134295 + boneIndex.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(propertyName);
        hashCode = hashCode * -1521134295 + EqualityComparer<AnimationCurve>.Default.GetHashCode(curve);
        return hashCode;
    }

    public void Deconstruct(out int item1, out string item2, out AnimationCurve item3)
    {
        item1 = boneIndex;
        item2 = propertyName;
        item3 = curve;
    }

    public static implicit operator (int, string, AnimationCurve)(SerializedCurve value)
    {
        return (value.boneIndex, value.propertyName, value.curve);
    }

    public static implicit operator SerializedCurve((int, string, AnimationCurve) value)
    {
        return new SerializedCurve(value.Item1, value.Item2, value.Item3);
    }
}