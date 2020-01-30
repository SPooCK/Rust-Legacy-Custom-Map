namespace RobinTheilade.UnityFramework
{
    using System;
    using UnityEngine;

    public static class GUILayoutEx
    {
        public static void Horizontal(Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            action();
            GUILayout.EndHorizontal();
        }

        public static void Horizontal(Action action, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(style, options);
            action();
            GUILayout.EndHorizontal();
        }

        public static void Horizontal(Action action, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(text, style, options);
            action();
            GUILayout.EndHorizontal();
        }

        public static void Horizontal(Action action, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(content, style, options);
            action();
            GUILayout.EndHorizontal();
        }

        public static void Horizontal(Action action, Texture2D image, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(image, style, options);
            action();
            GUILayout.EndHorizontal();
        }

        public static void Vertical(Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(options);
            action();
            GUILayout.EndVertical();
        }

        public static void Vertical(Action action, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(style, options);
            action();
            GUILayout.EndVertical();
        }

        public static void Vertical(Action action, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(text, style, options);
            action();
            GUILayout.EndVertical();
        }

        public static void Vertical(Action action, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(content, style, options);
            action();
            GUILayout.EndVertical();
        }

        public static void Vertical(Action action, Texture2D image, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(image, style, options);
            action();
            GUILayout.EndVertical();
        }
    }
}

