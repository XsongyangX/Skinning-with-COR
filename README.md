# Skinning using centers of rotation, Unity C# implementation
Made for a project in IFT 6113, at UdeM

This repo is used in conjunction with the C++ plugin [here](https://github.com/XsongyangX/SkinningCOR-cpp).

# Required components
This is a Unity project starting in version `2019.4.15f1`. No extra packages were installed. So once you add this repo as a project inside Unity Hub, you can open it inside the editor and every C# import will be done for you.

This project requires the C++ DLL plugin described above. You must compile that C++ plugin into a shared library for your operating system and place it inside `Assets/Plugin/Windows/` or in general `Assets/Plugin/your-OS`.

# Scenes
There is only one Unity scene, the `SampleScene`. It contains a trackball camera, a plane and a mesh from mixamo.com. 

The mesh contains a custom script component called `AnimatorCOR` that I created. It mimicks the behavior of Unity's default `Animator`.

Currently, my animator component is not finished. There is still animation curve evaluation and bone transformations to compute, so the latter can be sent to the C++ plugin for mesh deformation.

# Editor script
This repo also contains an editor script meant to serialize mesh and animation data to disk. Because animation curves cannot be accessed at runtime, they need to be serialized into a binary file that is loaded and parsed at runtime.

To run the editor script, navigate to "Windows/Skinning COR" from the top bar. Select the object in the scene with an AnimatorCOR component to serialize and confirm the save location of the serialization.

