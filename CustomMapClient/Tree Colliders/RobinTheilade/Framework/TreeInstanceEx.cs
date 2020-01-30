namespace RobinTheilade.Framework
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public static class TreeInstanceEx
    {
        public static bool Same(this TreeInstance instance1, TreeInstance instance2) => 
            ((instance1.position == instance2.position) && ((instance1.prototypeIndex == instance2.prototypeIndex) && ((instance1.heightScale == instance2.heightScale) && ((instance1.widthScale == instance2.widthScale) && ((instance1.color == instance2.color) && (instance1.lightmapColor == instance2.lightmapColor))))));
    }
}

