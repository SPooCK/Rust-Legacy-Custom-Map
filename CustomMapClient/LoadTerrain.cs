/*
using Facepunch.Progress;
using Rust;
using RustBuster2016;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace CustomMapClient {
    public class LoadTerrain : MonoBehaviour {
        public bool FileDownloading = true;
        private bool FileLoaded = false;
        private bool FileLoading = false;
        private bool MapLoaded = false;
        private bool TerrLoading = false;
        private bool TerrLoaded = false;
        private int[] TerrSize = new int[] { 2049, 2049, 16000 };

        private float seaLevel = 1f;
        public string filePath;
        public string fiMD5;
        private float[,] neMapData;
        private float totWait = 0;

        protected void Start() {
            //GameObject rustLoader = GameObject.Find("Loading Level:rust_island_2013");
            //rustMono = rustLoader.GetComponent<MonoBehaviour>();
        }

        protected void Update() {
            if (FileDownloading) {
                if (!MapLoaded) {
                    WaitForTerrain();
                }
                return;
            }

            totWait += 0.01f;
            if (!MapLoaded) {
                //LoadingScreen.Update("[CUSTOM MAP] Waiting Active Terrain... (" + totWait.ToString("0.0") + " sec.)");
                WaitForTerrain();
            } else if (!FileLoading) {
                //LoadingScreen.Update("[CUSTOM MAP] Verifying Map File... (" + totWait.ToString("0.0") + " sec.)");
                WaitForFile();
            } else if (FileLoading && !FileLoaded) {
                //LoadingScreen.Update("[CUSTOM MAP] Verifying Map File... (" + totWait.ToString("0.0") + " sec.)");
            } else if (!TerrLoading) {
                //LoadingScreen.Update("[CUSTOM MAP] Loading New Terrain... (" + totWait.ToString("0.0") + " sec.)");
                Load();
            } else if (TerrLoading && !TerrLoaded) {
                //LoadingScreen.Update("[CUSTOM MAP] Loading New Terrain... (" + totWait.ToString("0.0") + " sec.)");
            }
        }

        private string CalculateMD5(string filename) {
            using (var md5 = MD5.Create()) {
                using (var stream = File.OpenRead(filename)) {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private float[,] LoadRawMap(string path) {
            byte[] bytes = File.ReadAllBytes(path);

            int size = (int)Mathf.Sqrt(bytes.Length / 2);
            float[,] rawHeights = new float[size, size];

            int i = 0;
            for (int z = 0; z < size; z++) {
                for (int x = size - 1; x >= 0; x--) {
                    rawHeights[x, z] = (bytes[i + 1] * 256f + bytes[i]) / (65535f / 2f);
                    i += 2;
                }
            }

            int tSize = (int)(size * 7.808687164470473f);
            TerrSize = new int[] { tSize, tSize, size };
            return rawHeights;
        }

        private void WaitForFile() {
            try {
                if (!File.Exists(filePath)) return;
                if (CalculateMD5(filePath) != fiMD5) return;
                FileLoading = true;

                neMapData = LoadRawMap(filePath);
                File.WriteAllText(filePath, "What are you looking in here? Ghost, is that you? :P");
                FileLoaded = true;

                Terrain terrain = Terrain.activeTerrain;
                if (terrain == null) { return; }

                MapLoaded = true;
            } catch (Exception ex) {
                
                LoadingScreen.Update(ex.Message.ToString());
                Notice.Popup("", $"[MAP POPULATE ERROR] {ex.Message.ToString()}", 60f);
                return;
            }
        }

        private void WaitForTerrain() {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null) { return; }

            MapLoaded = true;
            
        }

        private void CleanObjects() {
            LoadingScreen.Update("[CUSTOM MAP] Cleaning all Rocks & Water...");
            GameObject[] objects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objects) {
                try {
                    if (obj.name.Length <= 1 || obj.name == "RadiationZone" || int.TryParse(obj.name, out int n) || obj.name.ToLower().Contains("rock") || obj.name == "road") {
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

        private void Load() {
            try {
                TerrLoading = true;

                Terrain terrain = Terrain.activeTerrain;
                TerrainData teData = terrain.terrainData;

                teData.heightmapResolution = TerrSize[2];
                teData.size = new Vector3(TerrSize[0], teData.size.y, TerrSize[1]);
                teData.SetHeights(0, 0, neMapData); neMapData = new float[0, 0];
                terrain.Flush(); CleanObjects();

                TerrLoaded = true;
                ClientMap.ClientReady();
                Destroy(this);
            } catch (Exception ex) {
                
                LoadingScreen.Update($"[MAP LOAD ERROR] {ex.Message.ToString()}");
                Notice.Popup("", $"[MAP LOAD ERROR] {ex.Message.ToString()}", 60f);
            }
        }
    }
}
*/