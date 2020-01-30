namespace RobinTheilade
{
    using System;
    using UnityEngine;

    public static class Measure
    {
        public static void DebugLogTime(string caption, Action action)
        {
            Debug.Log(string.Concat(new object[] { "Time Measure: ", caption, " (", Time(action), "ms)" }));
        }

        public static float Time(Action action)
        {
            action();
            return (UnityEngine.Time.realtimeSinceStartup - UnityEngine.Time.realtimeSinceStartup);
        }
    }
}

