using System;
using System.Collections.Generic;
using UnityEngine;

namespace RobinTheilade {
    /// <summary>
    /// A region quadtree implementation used for fast lookup in a two dimensional world.
    /// </summary>
    /// <typeparam name="T">
    /// The type to store inside the tree.
    /// </typeparam>
    /// <remarks>
    /// This implementation is not thread-safe.
    /// </remarks>
    // Token: 0x02000004 RID: 4
    public class Quadtree<T> {
        /// <summary>
        /// Gets the number of values inside this tree.
        /// </summary>
        // Token: 0x17000001 RID: 1
        // (get) Token: 0x0600000D RID: 13 RVA: 0x0000218B File Offset: 0x0000038B
        // (set) Token: 0x0600000E RID: 14 RVA: 0x00002193 File Offset: 0x00000393
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Quadtree`1" /> class.
        /// </summary>
        /// <param name="boundaries">
        /// The boundaries of the region.
        /// </param>
        /// <param name="nodeCapacity">
        /// The maximum number of nodes per tree.
        /// If the amount of nodes exceeds the tree will be subdivided into 4 sub trees.
        /// A value of 32 seems fine in terms of insert and remove speed.
        /// A value greater than 32 improves insert speed but slows down remove speed.
        /// </param>
        // Token: 0x0600000F RID: 15 RVA: 0x0000219C File Offset: 0x0000039C
        public Quadtree(Rect boundaries, int nodeCapacity = 32) {
            this.boundaries = boundaries;
            this.nodeCapacity = nodeCapacity;
            this.nodes = new List<Quadtree<T>.QuadtreeNode>(nodeCapacity);
        }

        /// <summary>
        /// Inserts a value into the region.
        /// </summary>
        /// <param name="x">
        /// The X component of the value's position.
        /// </param>
        /// <param name="y">
        /// The y component of the value's position.
        /// </param>
        /// <param name="value">
        /// The value to insert.
        /// </param>
        /// <returns>
        /// true if the value was inserted into the region;
        /// false if the value's position was outside the region.
        /// </returns>
        // Token: 0x06000010 RID: 16 RVA: 0x000021C8 File Offset: 0x000003C8
        public bool Insert(float x, float y, T value) {
            Vector2 position = new Vector2(x, y);
            Quadtree<T>.QuadtreeNode node = new Quadtree<T>.QuadtreeNode(position, value);
            return this.Insert(node);
        }

        /// <summary>
        /// Inserts a value into the region.
        /// </summary>
        /// <param name="position">
        /// The position of the value.
        /// </param>
        /// <param name="value">
        /// The value to insert.
        /// </param>
        /// <returns>
        /// true if the value was inserted into the region;
        /// false if the value's position was outside the region.
        /// </returns>
        // Token: 0x06000011 RID: 17 RVA: 0x000021F0 File Offset: 0x000003F0
        public bool Insert(Vector2 position, T value) {
            Quadtree<T>.QuadtreeNode node = new Quadtree<T>.QuadtreeNode(position, value);
            return this.Insert(node);
        }

        /// <summary>
        /// Inserts a node into the region.
        /// </summary>
        /// <param name="node">
        /// The node to insert.
        /// </param>
        /// <returns>
        /// true if the node was inserted into the region;
        /// false if the position of the node was outside the region.
        /// </returns>
        // Token: 0x06000012 RID: 18 RVA: 0x0000220C File Offset: 0x0000040C
        private bool Insert(Quadtree<T>.QuadtreeNode node) {
            if (!this.boundaries.Contains(node.Position)) {
                return false;
            }
            if (this.children != null) {
                Quadtree<T> quadtree;
                if (node.Position.y < this.children[2].boundaries.yMin) {
                    if (node.Position.x < this.children[1].boundaries.xMin) {
                        quadtree = this.children[0];
                    } else {
                        quadtree = this.children[1];
                    }
                } else if (node.Position.x < this.children[1].boundaries.xMin) {
                    quadtree = this.children[2];
                } else {
                    quadtree = this.children[3];
                }
                if (quadtree.Insert(node)) {
                    this.Count++;
                    return true;
                }
            }
            if (this.nodes.Count < this.nodeCapacity) {
                this.nodes.Add(node);
                this.Count++;
                return true;
            }
            this.Subdivide();
            return this.Insert(node);
        }

        /// <summary>
        /// Returns the values that are within the specified <paramref name="range" />.
        /// </summary>
        /// <param name="range">
        /// A rectangle representing the region to query.
        /// </param>
        /// <returns>
        /// Any value found inside the specified <paramref name="range" />.
        /// </returns>
        // Token: 0x06000013 RID: 19 RVA: 0x000025F4 File Offset: 0x000007F4
        public IEnumerable<T> Find(Rect range) {
            if (this.Count != 0) {
                bool allowInverse = false;
                if (this.boundaries.Overlaps(range, allowInverse)) {
                    if (this.children == null) {
                        for (int index = 0; index < this.nodes.Count; index++) {
                            Quadtree<T>.QuadtreeNode node = this.nodes[index];
                            if (range.Contains(node.Position)) {
                                yield return node.Value;
                            }
                        }
                    } else {
                        for (int index2 = 0; index2 < this.children.Length; index2++) {
                            Quadtree<T> child = this.children[index2];
                            foreach (T value in child.Find(range)) {
                                yield return value;
                            }
                        }
                    }
                }
            }
            yield break;
        }

        /// <summary>
        /// Removes a value from the region.
        /// </summary>
        /// <param name="x">
        /// The X component of the value's position.
        /// </param>
        /// <param name="z">
        /// The Z component of the value's position.
        /// </param>
        /// <param name="value">
        /// The value to remove.
        /// </param>
        /// <returns>
        /// true if the value was removed from the region;
        /// false if the value's position was outside the region.
        /// </returns>
        // Token: 0x06000014 RID: 20 RVA: 0x00002618 File Offset: 0x00000818
        public bool Remove(float x, float z, T value) {
            return this.Remove(new Vector2(x, z), value);
        }

        /// <summary>
        /// Removes a value from the region.
        /// </summary>
        /// <param name="position">
        /// The position of the value.
        /// </param>
        /// <param name="value">
        /// The value to remove.
        /// </param>
        /// <returns>
        /// true if the value was removed from the region;
        /// false if the value's position was outside the region.
        /// </returns>
        // Token: 0x06000015 RID: 21 RVA: 0x00002628 File Offset: 0x00000828
        public bool Remove(Vector2 position, T value) {
            if (this.Count == 0) {
                return false;
            }
            if (!this.boundaries.Contains(position)) {
                return false;
            }
            if (this.children != null) {
                bool result = false;
                Quadtree<T> quadtree;
                if (position.y < this.children[2].boundaries.yMin) {
                    if (position.x < this.children[1].boundaries.xMin) {
                        quadtree = this.children[0];
                    } else {
                        quadtree = this.children[1];
                    }
                } else if (position.x < this.children[1].boundaries.xMin) {
                    quadtree = this.children[2];
                } else {
                    quadtree = this.children[3];
                }
                if (quadtree.Remove(position, value)) {
                    result = true;
                    this.Count--;
                }
                if (this.Count <= this.nodeCapacity) {
                    this.Combine();
                }
                return result;
            }
            for (int i = 0; i < this.nodes.Count; i++) {
                Quadtree<T>.QuadtreeNode quadtreeNode = this.nodes[i];
                if (quadtreeNode.Position.Equals(position)) {
                    this.nodes.RemoveAt(i);
                    this.Count--;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Splits the region into 4 new subregions and moves the existing values into the new subregions.
        /// </summary>
        // Token: 0x06000016 RID: 22 RVA: 0x00002764 File Offset: 0x00000964
        private void Subdivide() {
            this.children = new Quadtree<T>[4];
            float num = this.boundaries.width * 0.5f;
            float num2 = this.boundaries.height * 0.5f;
            for (int i = 0; i < this.children.Length; i++) {
                Rect rect = new Rect(this.boundaries.xMin + num * (float)(i % 2), this.boundaries.yMin + num2 * (float)(i / 2), num, num2);
                this.children[i] = new Quadtree<T>(rect, 32);
            }
            this.Count = 0;
            for (int j = 0; j < this.nodes.Count; j++) {
                Quadtree<T>.QuadtreeNode node = this.nodes[j];
                this.Insert(node);
            }
            this.nodes.Clear();
        }

        /// <summary>
        /// Joins the contents of the children into this region and remove the child regions.
        /// </summary>
        // Token: 0x06000017 RID: 23 RVA: 0x00002838 File Offset: 0x00000A38
        private void Combine() {
            for (int i = 0; i < this.children.Length; i++) {
                Quadtree<T> quadtree = this.children[i];
                this.nodes.AddRange(quadtree.nodes);
            }
            this.children = null;
        }

        /// <summary>
        /// The maximum number of nodes per tree.
        /// </summary>
        // Token: 0x04000001 RID: 1
        private readonly int nodeCapacity = 32;

        /// <summary>
        /// The nodes inside this region.
        /// </summary>
        // Token: 0x04000002 RID: 2
        private readonly List<Quadtree<T>.QuadtreeNode> nodes;

        /// <summary>
        /// The child trees inside this region.
        /// </summary>
        // Token: 0x04000003 RID: 3
        private Quadtree<T>[] children;

        /// <summary>
        /// The boundaries of this region.
        /// </summary>
        // Token: 0x04000004 RID: 4
        private Rect boundaries;

        /// <summary>
        /// A single node inside a quadtree used for keeping values and their position.
        /// </summary>
        // Token: 0x02000005 RID: 5
        private class QuadtreeNode {
            /// <summary>
            /// Gets the position of the value.
            /// </summary>
            // Token: 0x17000002 RID: 2
            // (get) Token: 0x06000018 RID: 24 RVA: 0x00002879 File Offset: 0x00000A79
            // (set) Token: 0x06000019 RID: 25 RVA: 0x00002881 File Offset: 0x00000A81
            public Vector2 Position { get; private set; }

            /// <summary>
            /// Gets the value.
            /// </summary>
            // Token: 0x17000003 RID: 3
            // (get) Token: 0x0600001A RID: 26 RVA: 0x0000288A File Offset: 0x00000A8A
            // (set) Token: 0x0600001B RID: 27 RVA: 0x00002892 File Offset: 0x00000A92
            public T Value { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="T:QuadtreeNode" /> class.
            /// </summary>
            /// <param name="position">
            /// The position of the value.
            /// </param>
            /// <param name="value">
            /// The value.
            /// </param>
            // Token: 0x0600001C RID: 28 RVA: 0x0000289B File Offset: 0x00000A9B
            public QuadtreeNode(Vector2 position, T value) {
                this.Position = position;
                this.Value = value;
            }
        }
    }
}
