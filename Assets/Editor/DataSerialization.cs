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
