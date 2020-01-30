using Fougerite;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomMapServer {
    class TerrainGenerator : MonoBehaviour {
        public GameObject teObject;
        private readonly string cMap = "TestMap.raw";
        private string DefLoc = @Path.Combine(Util.GetRootFolder(), "Heightmaps");
        private float seaLevel = 1f;
        private int[] TerrSize = new int[] { 16000, 16000, 2049 };

        private int MaxResources;
        private int MaxAnimals;
        private int MaxLoot;

        private void CleanObjects() {
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> CLEANING ALL ANIMALS & RESOURCES SPAWNS...");
            GenericSpawner[] objects = FindObjectsOfType<GenericSpawner>();
            foreach (GenericSpawner obj in objects) {
                try {
                    Destroy(obj);
                } catch (Exception ex) {
                    ConsoleSystem.PrintError("[CUSTOM MAP] >>>>> " + ex.ToString());
                }
            }

            ConsoleSystem.Print("[CUSTOM MAP] >>>>> CLEANING ALL ROCKS & WATER...");
            GameObject[] objects2 = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objects2) {
                try {
                    if (obj.tag != "Tree Collider" && (obj.name.Length <= 1 || int.TryParse(obj.name, out int n) || obj.name.ToLower().Contains("rock") || obj.name == "road")) {
                        Destroy(obj);
                    } else if (obj.name == "RustWater" && seaLevel > 0) {
                        //int terrainLevelY = (int)(seaLevel / 1.5f);
                        Vector3 watLoc = obj.transform.position; watLoc.y = seaLevel;
                        obj.transform.position = watLoc;
                    }
                } catch (Exception ex) {
                    ConsoleSystem.PrintError("[CUSTOM MAP] >>>>> " + ex.ToString());
                }
            }
        }

        private void MoveObjects() {
            Dictionary<string, float[]> MapObjects = new Dictionary<string, float[]>();

            GameObject[] objects = FindObjectsOfType<GameObject>();
            string[] valid = new string[] {
                "Building", "Tank", "Wall", "Pallet", "Barrel", "Shack", "Apartment", "Container", "fence", "Radiation",
                "gate", "NoPlacementZone", "Garage", "Tire", "Ramp", "Bunker", "pipe", "buid", "Shed", "crane",
                "spool", "house", "Hangar", "MedicalLootBox", "ContextHint", "BoxLoot", "AmmoLootBox", "crate"
            };
            int tot = 0;
            foreach (GameObject obj in objects) {
                bool hasIt = false;
                foreach (string name in valid) { if (obj.name.ToLower().Contains(name.ToLower())) { hasIt = true; break; } }
                if (hasIt && !obj.name.ToLower().Contains("door")) {
                    string sPos = $"{obj.transform.position.x}_{obj.transform.position.y}_{obj.transform.position.z}";
                    string name = sPos + "_" + obj.name;
                    Vector3 pos = CustomMap.Instance.RandomPositionOnTerrain();
                    obj.transform.position = pos;

                    Quaternion smoothTilt = obj.transform.rotation;
                    Quaternion newRot = new Quaternion();
                    Vector3 theRay = obj.transform.TransformDirection(Vector3.down);

                    Vector3 origin = new Vector3(pos.x, pos.y + 2f, pos.z);
                    if (Physics.Raycast(origin, theRay, out RaycastHit rcHit, LayerMask.GetMask("Terrain"))) {
                        Quaternion targetRotation = Quaternion.FromToRotation(obj.transform.up, rcHit.normal) * obj.transform.rotation;
                        newRot = Quaternion.Slerp(obj.transform.rotation, targetRotation, Time.deltaTime * 0.1f);
                        obj.transform.rotation = newRot;
                    }

                    if (obj.name.ToLower().Contains("garage"))
                        ConsoleSystem.Print(pos.ToString() + " " + obj.name);
                    MapObjects.Add(name, new float[] { pos.x, pos.y, pos.z, newRot.x, newRot.y, newRot.z, newRot.w });
                    tot++;
                }
            }
            CustomMap.Instance.SerilizeString(MapObjects);
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> MOVED |" + tot.ToString() + "| GAME OBJECTS");
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
            CustomMap.Instance.TerrSize = TerrSize;
            return rawHeights;
        }

        private void AddObjects() {
            int ss = (int)(TerrSize[2] * 0.25f);
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> CREATING |" + ss + "| RANDOM SPAWN POINTS...");
            SpawnManager.SpawnData[] spawnData = new SpawnManager.SpawnData[ss];
            for (int i = 0; i < ss; i++) {
                spawnData[i].pos = CustomMap.Instance.RandomPositionOnTerrain();
                spawnData[i].rot = Quaternion.identity;
            }
            SpawnManager._spawnPoints = spawnData;

            MaxResources = (int)(TerrSize[2] * 2f);
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> SPAWNING |" + MaxResources + "| RANDOM RESOURCES...");
            for (int i = MaxResources; i > 0; i--) CustomMap.Instance.SpawnResource();

            MaxAnimals = (int)(TerrSize[2] * 2f);
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> SPAWNING |" + MaxAnimals + "| RANDOM ANIMALS...");
            for (int i = MaxAnimals; i > 0; i--) CustomMap.Instance.SpawnAnimal();

            MaxLoot = (int)(TerrSize[2] * 0.5f);
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> SPAWNING |" + MaxLoot + "| RANDOM LOOT BOXES...");
            for (int i = MaxLoot; i > 0; i--) CustomMap.Instance.SpawnLoot();

            CustomMap.Instance.MaxResources = MaxResources;
            CustomMap.Instance.MaxAnimals = MaxAnimals;
            CustomMap.Instance.MaxLoot = MaxLoot;
            CustomMap.Instance.seaLevel = seaLevel;
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

        protected void Start() {
            //CleanObjects(); return;
            string filePath = Path.Combine(DefLoc, cMap);
            float[,] neMapData = LoadRawMap(filePath);
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> CREATING |" + TerrSize[2] + "x" + TerrSize[2] + "| SIZED TERRAIN...");

            Terrain terrain = Terrain.activeTerrain;
            TerrainData orData = terrain.terrainData;

            orData.heightmapResolution = TerrSize[2];
            orData.size = new Vector3(TerrSize[0], 1500, TerrSize[1]);
            orData.SetHeights(0, 0, neMapData); neMapData = null;
            terrain.transform.position = Vector3.zero; FixTrees(orData);

            terrain.treeDistance = 225;
            terrain.Flush();

            CleanObjects(); AddObjects();
            //MoveObjects();

            CustomMap.Instance.WaitResource();
            ConsoleSystem.Print("[CUSTOM MAP] >>>>> |" + cMap + "| MAP GENERATED !");

            Destroy(teObject); Destroy(this);
        }
    }
}
