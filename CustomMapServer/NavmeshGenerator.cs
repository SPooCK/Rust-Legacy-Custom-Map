/*
using UnityEngine;
using System.Collections.Generic;


namespace CustomMap {
    class NavmeshGenerator {
        public static void RayGenerateNavMesh() {
            int SamplesPerLine = 10;
            int LineSamples = 10;
            int RayCasterHeight = 1000;

            float X_PerSample = (Terrain.activeTerrain.terrainData.size.x) / (SamplesPerLine - 1);
            float Z_PerSample = (Terrain.activeTerrain.terrainData.size.z) / (LineSamples - 1);
            float terrainX = Terrain.activeTerrain.transform.position.x;
            float terrainZ = Terrain.activeTerrain.transform.position.z;
            Vector3 currPos = new Vector3(terrainX, RayCasterHeight, terrainZ);

            //Iteration Vars
            List<Vector3> vertexes = new List<Vector3>();

            for (int x = 0; x < LineSamples; x++) {
                for (int z = 0; z < SamplesPerLine; z++) {
                    Physics.Raycast(currPos, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity);
                    //If Normal vs raycast angle is too great (which is what?) then do not add point
                    vertexes.Add(hitInfo.point);
                    currPos.z += Z_PerSample;
                }
                currPos.x += X_PerSample;
                currPos.z = 0;
            }

            //Generate the mesh and set it's verticies to raycasted points
            Mesh navMesh = new Mesh {
                name = "NavagationMesh",
                vertices = vertexes.ToArray()
            };

            //Link Triangles from vertexes
            List<int> tris = new List<int>();
            int vert;
            for (int line = 0; line < LineSamples - 1; line++) {
                for (int i = 0; i < SamplesPerLine - 1; i++) {
                    vert = (SamplesPerLine * line) + i; //current vertex in a linear list
                                                        //generate quad
                                                        //tri 1
                    tris.Add(vert);
                    tris.Add(vert + 1);
                    tris.Add(vert + SamplesPerLine);
                    //tri 2
                    tris.Add(vert + 1);
                    tris.Add(vert + SamplesPerLine);
                    tris.Add(vert + SamplesPerLine + 1);
                }
            }

            navMesh.triangles = tris.ToArray();

            GameObject NavGObject = new GameObject { name = "NavMesh" };
            NavGObject.AddComponent("MeshRenderer");
            NavGObject.AddComponent("MeshFilter");
            MeshFilter Filter = (MeshFilter)NavGObject.GetComponent("MeshFilter");
            Filter.mesh = navMesh;
            Object.DontDestroyOnLoad(NavGObject);
            //Object.Instantiate(NavGObject);
            
            NavMesh nav = new NavMesh();
        }
    }
}
*/