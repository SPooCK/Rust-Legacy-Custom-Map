using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomMapClient {
    class TerrainGenerator : MonoBehaviour {
        public GameObject teObject;

        public Dictionary<string, float[]> MapObjects;
        public float seaLevel;
        public string CompressMap;

        private int[] TerrSize = new int[] { 16000, 16000, 2049 };

        private float[,] neMapData;
        private bool SettingSplat = false;
        private int ySPL = 0;
        private float[,,] splatmapData;

        private void CleanObjects() {
            LoadingScreen.Update("[CUSTOM MAP] Cleaning all Rocks & Water...");
            GameObject[] objects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objects) {
                try {
                    if (obj.tag != "Tree Collider" && (obj.name.Length <= 1 || int.TryParse(obj.name, out int n) || obj.name.ToLower().Contains("rock") || obj.name == "road")) {
                        Destroy(obj);
                    } else if (obj.name == "RustWater" && seaLevel > 0) {
                        Vector3 watLoc = obj.transform.position; watLoc.y = seaLevel;
                        obj.transform.position = watLoc;
                    }
                } catch (Exception ex) {
                    LoadingScreen.Update("[CUSTOM MAP] >>>>> " + ex.ToString());
                }
            }
        }

        private void MoveObjects() {
            GameObject[] objects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objects) {
                string sPos = $"{obj.transform.position.x}_{obj.transform.position.y}_{obj.transform.position.z}";
                string name = sPos + "_" + obj.name;
                bool hasIt = MapObjects.ContainsKey(name);
                if (hasIt) {
                    float[] att = MapObjects[name];
                    Vector3 pos = new Vector3(att[0], att[1], att[2]);
                    obj.transform.position = pos;
                    Quaternion rot = new Quaternion(att[3], att[4], att[5], att[6]);
                    obj.transform.rotation = rot;
                }
            }
        }

        private float[,] LoadRawMap() {
            //byte[] fBytes = File.ReadAllBytes(filePath);
            byte[] fBytes = RustBuster2016.API.Hooks.DeCompressByte(Convert.FromBase64String(CompressMap));

            int size = (int)Mathf.Sqrt(fBytes.Length / 2);
            float[,] rawHeights = new float[size, size];

            int i = 0;
            for (int z = 0; z < size; z++) {
                for (int x = size - 1; x >= 0; x--) {
                    rawHeights[x, z] = (fBytes[i + 1] * 256f + fBytes[i]) / (65535f / 2f);
                    i += 2;
                }
            }

            int tSize = (int)(size * 7.808687164470473f);
            TerrSize = new int[] { tSize, tSize, size };
            return rawHeights;
        }

        private void SetSplat() {
            TerrainData terrainData = Terrain.activeTerrain.terrainData;

            // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
            //float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

            //for (int y = 0; y < terrainData.alphamapHeight; y++) {
            for (int x = 0; x < terrainData.alphamapWidth; x++) {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = ySPL / (float)terrainData.alphamapHeight;
                float x_01 = x / (float)terrainData.alphamapWidth;

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01, x_01);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                // CHANGE THE RULES BELOW TO SET THE WEIGHTS OF EACH TEXTURE ON WHATEVER RULES YOU WANT

                // Texture[0] has constant influence
                splatWeights[0] = 0.5f;

                // Texture[1] is stronger at lower altitudes
                splatWeights[1] = Mathf.Clamp01((terrainData.heightmapHeight - height));

                // Texture[2] stronger on flatter terrain
                // Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
                // Subtract result from 1.0 to give greater weighting to flat surfaces
                splatWeights[2] = 1.0f - Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / 5.0f));

                // Texture[3] increases with height but only on surfaces facing positive Z axis 
                splatWeights[3] = height * Mathf.Clamp01(normal.z);

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++) {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, ySPL, i] = splatWeights[i];
                }
            }
            //}

            ySPL++;
            // Finally assign the new splatmap to the terrainData:
            //return splatmapData;
        }

        protected void Start() {
            //DefLoc = @Path.Combine(Environment.CurrentDirectory, @"RB_Data\Heightmaps");
            //filePath = @Path.Combine(DefLoc, "TestMap.raw");

            //CleanObjects(); ClientMap.Instance.ClientReady(); return;
            neMapData = LoadRawMap();
            ClientMap.Instance.SendMessageToServer("MAP_LOADED");
            InitMap();
        }

        protected void Update() {
            if (SettingSplat) {
                if (ySPL < Terrain.activeTerrain.terrainData.alphamapHeight) {
                    SetSplat();
                } else {
                    SettingSplat = false;
                    Terrain.activeTerrain.terrainData.SetAlphamaps(0, 0, splatmapData);
                    ClientMap.Instance.SendMessageToServer("MAP_SPLAT");
                    splatmapData = null; Destroy(teObject); Destroy(this);
                }
            }
        }

        private void FixTrees(TerrainData teData) {
            /*
            TreePrototype[] orProt = terrain.terrainData.treePrototypes;
            TreePrototype[] neProt = new TreePrototype[terrain.terrainData.treePrototypes.Length];
            for (int i = 0; i < orProt.Length; i++) {
                neProt[i] = new TreePrototype { bendFactor = orProt[i].bendFactor };

                GameObject treColl = new GameObject("Tree Collider", typeof(CapsuleCollider)) { tag = "Tree Collider" };
                CapsuleCollider capsule = treColl.GetComponent<CapsuleCollider>();
                capsule.radius = 0.8f; capsule.height = 30f;
                treColl.SetActive(true);

                neProt[i].prefab = treColl;
            }
            terrain.terrainData.treePrototypes = orProt;
            */

            foreach (TreePrototype tree in teData.treePrototypes) {
                GameObject treObj = tree.prefab;

                CapsuleCollider capsule = treObj.AddComponent<CapsuleCollider>();
                capsule.radius = 0.8f; capsule.center = new Vector3(0, 11f, 0);

                //Instantiate(capsule);
            }
            teData.RefreshPrototypes();
        }

        private void InitMap() {
            Terrain terrain = Terrain.activeTerrain;
            TerrainData orData = terrain.terrainData;

            orData.heightmapResolution = TerrSize[2];
            orData.size = new Vector3(TerrSize[0], 1500, TerrSize[1]);
            orData.SetHeights(0, 0, neMapData); neMapData = null;
            terrain.transform.position = Vector3.zero; FixTrees(orData);

            terrain.treeDistance = 225;
            terrain.Flush();

            ClientMap.Instance.SendMessageToServer("MAP_CREATED");
            CleanObjects(); //MoveObjects();

            splatmapData = new float[orData.alphamapWidth, orData.alphamapHeight, orData.alphamapLayers];
            SettingSplat = true;

            ClientMap.Instance.ClientReady();
        }
    }
}


/*
     protected void FixedUpdate() {
            if (SpawningTrees) { 
                if (newTrees.Count < MaxTrees) {
                    Vector3 ranPos = ClientMap.Instance.RandomPositionOnTerrain(false);
                    float nX = ranPos.x / TerrSize[0]; float nY = ranPos.z / TerrSize[1];
                    Vector3 treePos = new Vector3(nX, ranPos.y / TerrSize[0], nY);
                    TreeInstance treeInstance = new TreeInstance {
                        prototypeIndex = UnityEngine.Random.Range(0, terrain.terrainData.treePrototypes.Length),
                        color = new Color(1f, 1f, 1f),
                        lightmapColor = new Color(1f, 1f, 1f),
                        heightScale = UnityEngine.Random.Range(0.8700000f, 1.200000f),
                        widthScale = UnityEngine.Random.Range(0.8700000f, 1.200000f),
                        position = treePos
                    };
                    newTrees.Add(treeInstance); terrain.AddTreeInstance(treeInstance);
                    int indx = treeInstance.prototypeIndex;
                    Instantiate(terrain.terrainData.treePrototypes[indx].prefab, ranPos, Quaternion.identity);
                } else {
                    SpawningTrees = false;
                    terrain.terrainData.RefreshPrototypes();
                    Finished();
                }
            }
        }
      protected void Update() {
          if (!FileLoading) { WaitForFile(); }
      }

      private string CalculateMD5() {
          using (var md5 = MD5.Create()) {
              using (var stream = File.OpenRead(filePath)) {
                  var hash = md5.ComputeHash(stream);
                  return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
              }
          }
      }

      private void WaitForFile() {
          try {
              if (!File.Exists(filePath)) return;
              if (CalculateMD5() != fMD5) return;
              FileLoading = true;

              neMapData = LoadRawMap(filePath);
              File.WriteAllText(filePath, "What are you looking in here? Ghost, is that you? :P");
              FileLoaded = true;
              InitMap();
          } catch (Exception ex) {
              LoadingScreen.Update(ex.Message.ToString());
              Rust.Notice.Popup("", $"[MAP POPULATE ERROR] {ex.Message.ToString()}", 60f);
              return;
          }
      }
      */
