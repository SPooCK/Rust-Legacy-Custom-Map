namespace RobinTheilade.Framework
{
    using System;
    using UnityEngine;

    public static class RandomEx
    {
        public static Vector3 Vector3XZ()
        {
            Vector3 vector = new Vector3((UnityEngine.Random.value * 2f) - 1f, 0f, (UnityEngine.Random.value * 2f) - 1f);
            vector.Normalize();
            return vector;
        }

        public static Vector3 Vector3XZ(float distance) => 
            (Vector3XZ() * distance);
    }
}

