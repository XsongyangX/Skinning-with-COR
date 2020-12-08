using System.Collections.Generic;

using UnityEngine;

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