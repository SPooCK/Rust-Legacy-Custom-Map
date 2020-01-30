using RustBuster2016.API;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace CustomMapClient {
    public class ClientMap : RustBusterPlugin {
        public static ClientMap Instance;

        public override string Name => "CustomMapClient";
        public override string Author => "SPooCK";
        public override Version Version => new Version("1.0.0");

        GameObject teObject;
        private readonly float seaLevel = 1f;

        public override void Initialize() {
            Instance = this;
            Hooks.OnRustBusterClientReady += Start;
        }

        public override void DeInitialize() {
            Hooks.OnRustBusterClientReady -= Start;
            UnityEngine.Object.Destroy(teObject);
        }

        public Transform GetLocation() {
            return Controllable.localPlayerControllableExists ? Controllable.localPlayerControllable.transform : null;
        }

        private object Unserialize(string str) {
            byte[] mData = Hooks.DeCompressByte(Convert.FromBase64String(str));
            MemoryStream memorystream = new MemoryStream(mData);
            BinaryFormatter bf = new BinaryFormatter();
            object obj = bf.Deserialize(memorystream);
            return obj;
        }

        private void Start() {
            LoadingScreen.Operations.Clear();
            LoadingScreen.Show();
            LoadingScreen.Update("LOADING Custom Map...");

            //string ObjLoc = SendMessageToServer("GET_MapOBJ");
            //Dictionary<string, float[]> MapObjects = (Dictionary<string, float[]>)Unserialize(ObjLoc);
            string CompressMap = SendMessageToServer("GET_MAP");

            teObject = new GameObject();
            TerrainGenerator terGen = teObject.AddComponent<TerrainGenerator>();
            terGen.CompressMap = CompressMap; //terGen.MapObjects = MapObjects;
            terGen.teObject = teObject; terGen.seaLevel = seaLevel;

            teObject.SetActive(true);
        }

        public void ClientReady() {
            Instance.SendMessageToServer("Player_Ready");
            LoadingScreen.Operations.Clear();
            LoadingScreen.Hide();
            HudEnabled.Enable();
        }
    }
}


/*
       private void UnloadTerrain() {
            try {
                Terrain terrain = Terrain.activeTerrain;
                TerrainData teData = terrain.terrainData;

                TerrainData orData = (TerrainData)Resources.FindObjectsOfTypeAll(typeof(TerrainData))[0];
                int w = orData.heightmapWidth; int h = orData.heightmapHeight;
                orData.heightmapResolution = orData.heightmapResolution; teData.size = orData.size;
                teData.SetHeights(0, 0, orData.GetHeights(0, 0, w, h));
                terrain.Flush();

                Notice.Popup("", "Custom MAP UNLOADED!", 10f);
            } catch (Exception ex) {
                Notice.Popup("", $"[MAP UNLOAD ERROR] {ex.Message.ToString()}", 60f);
            }
        }

        public void PluginsReady() {
            //UnityEngine.Terrain.activeTerrain.treeDistance = 0f;
            DefLoc = Path.Combine(Directory.GetCurrentDirectory(), @"RB_Data\Heightmaps");
            filePath = Path.Combine(DefLoc, "TestMap.raw");

            teObject = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(teObject);
            teObject.AddComponent(typeof(LoadTerrain));

            LoadTerrain teLoader = teObject.GetComponent<LoadTerrain>();
            teLoader.filePath = filePath; teObject.SetActive(true);

            RequestMD5();
            
            
            
            using (WebClient wc = new WebClient()) {
                wc.DownloadProgressChanged += DownloadProgressChanged;
                wc.DownloadFileCompleted += DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri("http://76.247.131.162/TestMap.raw"), filePath);
            }
            
        }

        
        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            int perc = e.ProgressPercentage;
            LoadingScreen.Update("[CUSTOM MAP] Downloading Map... (" + perc.ToString() + "%)");
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e) {
            LoadingScreen.Update("[CUSTOM MAP] Finalizing Map Data... (!!)");

            aTimer = new Timer(500);
            aTimer.Elapsed += AddMD5;
            aTimer.Enabled = true;
        }

        private void AddMD5(object source, ElapsedEventArgs e) {
            aTimer.Dispose();
            teObject.GetComponent<LoadTerrain>().MD5 = CalculateMD5(filePath);

            aTimer = new Timer(500);
            aTimer.Elapsed += StartClient;
            aTimer.Enabled = true;
        }

        private void StartClient(object source, ElapsedEventArgs e) {
            aTimer.Dispose();
            teObject.GetComponent<LoadTerrain>().FileDownloading = false;
        }
        */
