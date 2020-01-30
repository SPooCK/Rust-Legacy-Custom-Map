using Fougerite;
using Fougerite.Events;
using RustBuster2016Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Timers;
using UnityEngine;
using Module = Fougerite.Module;
using Server = Fougerite.Server;

namespace CustomMapServer {
    class ResourceSpawner : MonoBehaviour {

    }

    public class CustomMap : Module {
        public static CustomMap Instance;

        public override string Name => "CustomMap";
        public override string Author => "SPooCK";
        public override Version Version => new Version("1.0.0");

        public int MaxResources;
        public int MaxAnimals;
        public int MaxLoot;
        public float seaLevel;

        public string MapObjects;
        public int[] TerrSize = new int[] { 16000, 16000, 2049 };
        private string CompressMap;

        private List<ulong> LoadedClients = new List<ulong>();
        private Dictionary<ulong, Vector3> TempGod = new Dictionary<ulong, Vector3>();
        private string DefLoc = @Path.Combine(Util.GetRootFolder(), "Heightmaps");
        private string filePath;
        private readonly string cMap = "TestMap.raw";
        private Timer aTimer;
        private int[] TimerWait = new int[] { 1, 10 };
        private int CurrResources = 0;
        private int CurrAnimals = 0;
        private int CurrLooot = 0;

        public override void Initialize() {
            Instance = this;

            Hooks.OnServerInit += OnServerInit;
            Hooks.OnServerLoaded += OnServerLoaded;
            Hooks.OnCommand += OnCommand;

            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnPlayerSpawned += OnPlayerSpawned;
            API.OnRustBusterUserMessage += OnRustBusterUserMessage;

            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnPlayerGathering += OnPlayerGathering;
            Hooks.OnNPCKilled += OnNPCKilled;
            Hooks.OnItemRemoved += OnItemRemoved;

            //Hooks.OnShoot += OnShoot;
        }

        private void OnShoot(ShootEvent shootEvent) {
            Fougerite.Player player = shootEvent.Player;
            bool flag2 = player != null;
            if (flag2) {
                RaycastHit[] array = Physics.RaycastAll(player.PlayerClient.controllable.character.eyesRay);
                bool flag3 = array.Count() > 0;
                if (flag3) {
                    Collider collider = array[0].collider;
                    GameObject obj = collider.gameObject;
                    if (obj != null) {
                        player.Message($"[{obj.tag}] {obj.transform.position.ToString()} {obj.ToString()}");
                    } else {
                        player.Message($"[{collider.tag}] {collider.transform.position.ToString()} {collider.ToString()}");
                    }
                } else {
                    player.Notice("NOTHING");
                }
            }
        }

        public override void DeInitialize() {
            if (aTimer != null) aTimer.Dispose();

            Hooks.OnServerInit -= OnServerInit;
            Hooks.OnServerLoaded -= OnServerLoaded;
            Hooks.OnCommand -= OnCommand;

            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnPlayerSpawned -= OnPlayerSpawned;

            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnPlayerGathering -= OnPlayerGathering;
            Hooks.OnNPCKilled -= OnNPCKilled;
            Hooks.OnItemRemoved -= OnItemRemoved;
        }

        private string Serialize(object obj) {
            MemoryStream memorystream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(memorystream, obj);
            byte[] mStream = API.CompressByte(memorystream.ToArray());
            string slist = Convert.ToBase64String(mStream);
            return slist;
        }

        public void SerilizeString(Dictionary<string, float[]> MapList) {
            MapObjects = Serialize(MapList);
        }

        private void OnRustBusterUserMessage(API.RustBusterUserAPI user, Message msgc) {
            if (msgc.PluginSender == "CustomMapClient") {
                if (msgc.MessageByClient == "Player_Ready") {
                    ConsoleSystem.Print("[✔] " + user.Name + " - SPAWNED");
                    ulong SteamID = user.UID;

                    Fougerite.Player player = Fougerite.Player.FindBySteamID(SteamID.ToString());
                    if (TempGod.ContainsKey(SteamID)) {
                        GlitchTeleportTo(player, TempGod[SteamID]);

                        Timer gTimer = new Timer();
                        gTimer.Elapsed += (sender, args) => RemTempGod(sender, SteamID);
                        gTimer.Interval = 5000;
                        gTimer.AutoReset = false;
                        gTimer.Enabled = true;
                    } else {
                        GlitchTeleportTo(player, player.Location);
                    }
                    if (!LoadedClients.Contains(SteamID)) LoadedClients.Add(SteamID);
                } else if (msgc.MessageByClient == "GET_MapOBJ") {
                    msgc.ReturnMessage = MapObjects;
                } else if (msgc.MessageByClient == "GET_MAP") {
                    msgc.ReturnMessage = CompressMap;
                    ConsoleSystem.Print("[✔] " + user.Name + " - MAP SEND");
                } else if (msgc.MessageByClient == "MAP_LOADED") {
                    ConsoleSystem.Print("[✔] " + user.Name + " - MAP LOADED");
                } else if (msgc.MessageByClient == "MAP_CREATED") {
                    ConsoleSystem.Print("[✔] " + user.Name + " - MAP CREATED");
                } else if (msgc.MessageByClient == "MAP_SPLAT") {
                    ConsoleSystem.Print("[✔] " + user.Name + " - SPLAT LOADED");
                }
            }
        }

        private void RemTempGod(object source, ulong SteamID) {
            if (TempGod.ContainsKey(SteamID)) TempGod.Remove(SteamID);
        }

        private void OnServerInit() {
            AddForDownload();
        }

        private void AddForDownload() {
            try {
                filePath = Path.Combine(DefLoc, cMap);

                CompressMap = Convert.ToBase64String(API.CompressByte(File.ReadAllBytes(filePath)));
                ConsoleSystem.Print("[COMPRESSED] >>>>> |" + cMap + " " + CompressMap.Length + " bytes| ADDED FOR DOWNLOADING...");
            } catch (Exception ex) {
                ConsoleSystem.PrintError("[MAP LOADING ERROR] >>>>> " + ex.ToString());
            }
        }

        private void SendInfo(Fougerite.Player player) {
            string[] Messages = new string[] {
                "[color red]IMPORTANT ![/color] - [color cyan]Early Development[/color] /map",
                "[color orange]--- Know Issues ---[/color]",
                "[color cyan]|✘ Trees can't be gathered| |✘ No Grass| |✘ No Rocks|[/color]",
                "[color cyan]|✘ Possible freeze/crash on loading the map| |✘ No rad towns yet|[/color]",
                "[color cyan]|✘ Possible falling through map death (/fixme - may help)|[/color]",
                "[color cyan]|✘ Trees spawn into water| |✘ Wolf/Bears are not spawning|[/color]",
                "[color cyan]|✘ Animals/Resources can spawn on cliffs| |✘ Animals may not run|[/color]",
                "[color green]We are still working on improvements, please be patient[/color]",
                "[color green]and report any concern in our /discord channel. Enjoy![/color]"
            };
            for (int i = 0; i < Messages.Length; i++) {
                if (player.IsOnline) player.MessageFrom("[CUSTOM MAP]", Messages[i]);
            }

        }

        private void OnPlayerDisconnected(Fougerite.Player player) {
            ulong SteamID = player.UID;
            if (LoadedClients.Contains(SteamID)) LoadedClients.Remove(SteamID);
            if (TempGod.ContainsKey(SteamID)) TempGod.Remove(SteamID);
        }

        private void OnPlayerConnected(Fougerite.Player player) {
            SendInfo(player);
        }

        private float Steepnes(Vector3 target) {
            Vector3 worldPos = target - Terrain.activeTerrain.transform.position;
            Vector3 tnPos = new Vector3(Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.x, worldPos.x), 0,
                                        Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.z, worldPos.z));
            return Terrain.activeTerrain.terrainData.GetSteepness(tnPos.x, tnPos.z);
        }

        public Vector3 RandomPositionOnTerrain() {
            //X:0 Z:0 L:4005 W:4005
            Terrain terrain = Terrain.activeTerrain;
            TerrainData teData = terrain.terrainData;

            int terrainWidth = (int)teData.size.x; // get terrain size x
            int terrainLength = (int)teData.size.z; // get terrain size z
            int terrainPosX = (int)terrain.transform.position.x; // get terrain position x
            int terrainPosZ = (int)terrain.transform.position.z; // get terrain position
            //int posMax = (int)teData.size.y;

            float aboveSea = seaLevel + 1f;
            int posx = UnityEngine.Random.Range(terrainPosX, terrainPosX + terrainWidth); // generate random x position
            int posz = UnityEngine.Random.Range(terrainPosZ, terrainPosZ + terrainLength); // generate random z position
            float posy = Terrain.activeTerrain.SampleHeight(new Vector3(posx, aboveSea, posz)); // get the terrain height at the random position
            Vector3 newPos = new Vector3(posx, posy, posz);

            bool canSpawn = posy > aboveSea && Steepnes(newPos) <= 50f;
            while (!canSpawn) {
                posx = UnityEngine.Random.Range(terrainPosX, terrainPosX + terrainWidth);
                posz = UnityEngine.Random.Range(terrainPosZ, terrainPosZ + terrainLength);
                posy = Terrain.activeTerrain.SampleHeight(new Vector3(posx, aboveSea, posz));
                newPos = new Vector3(posx, posy, posz);
                canSpawn = posy > aboveSea && Steepnes(newPos) <= 50f;
            }

            return newPos;
        }

        public bool GlitchTeleportTo(Fougerite.Player player, Vector3 target, bool dosafechecks = false) {
            if (LoadedClients.Contains(player.UID) && Vector3.Distance(player.Location, Terrain.activeTerrain.transform.position) > TerrSize[0]) {
                player.MessageFrom("[CUSTOM MAP]", "LOL, you're too far away from the map, lets try to fix that...");
                target = RandomPositionOnTerrain();
                player.Location = target;
            } else if (player.Location.y <= seaLevel) {
                player.MessageFrom("[CUSTOM MAP]", "Uuups, your spawn is under the water, lets try to fix that...");
                target = RandomPositionOnTerrain();
                player.Location = target;
            }

            float maxSafeDistance = 360f;
            double ms = 500d;
            string me = "SafeTeleport";

            float bumpConst = 0.75f;
            Vector3 bump = Vector3.up * bumpConst;
            Vector3 terrain = new Vector3(target.x, Terrain.activeTerrain.SampleHeight(target), target.z);
            RaycastHit hit;
            IEnumerable<StructureMaster> structures = from s in StructureMaster.AllStructures where s.containedBounds.Contains(terrain) select s;
            if (terrain.y > target.y)
                target = terrain + bump * 2;

            if (structures.Count() == 1) {
                if (Physics.Raycast(target, Vector3.down, out hit)) {
                    if (hit.collider.name == "HB Hit" && dosafechecks) {
                        // this.Message("There you are.");
                        return false;
                    }
                }

                StructureMaster structure = structures.FirstOrDefault<StructureMaster>();
                if (!structure.containedBounds.Contains(target) || hit.distance > 8f)
                    target = hit.point + bump;

                float distance = Vector3.Distance(player.Location, target);

                if (distance < maxSafeDistance) {
                    return player.TeleportTo(target);
                } else {
                    if (player.TeleportTo(terrain + bump * 2)) {
                        Timer timer = new Timer {
                            Interval = ms,
                            AutoReset = false
                        };
                        timer.Elapsed += delegate (object x, ElapsedEventArgs y) {
                            player.TeleportTo(target);
                        };
                        timer.Start();
                        return true;
                    }

                    return false;
                }
            } else if (structures.Count() == 0) {
                if (Physics.Raycast(terrain + Vector3.up * 300f, Vector3.down, out hit)) {
                    if (hit.collider.name == "HB Hit" && dosafechecks) {
                        player.Message("There you are.");
                        return false;
                    }

                    if (dosafechecks) {
                        float gradient = Steepnes(target);
                        if (gradient > 50f) {
                            player.Message("It's too steep there.");
                            return false;
                        }
                    }

                    target = hit.point + bump * 2;
                }

                float distance = Vector3.Distance(player.Location, target);
                Logger.LogDebug(string.Format("[{0}] player={1}({2}) from={3} to={4} distance={5} terrain={6}", me,
                    player.Name, player.GameID,
                    player.Location.ToString(), target.ToString(), distance.ToString("F2"), terrain.ToString()));

                return player.TeleportTo(target);
            } else {
                Logger.LogDebug(string.Format("[{0}] structures.Count is {1}. Weird.", me,
                    structures.Count().ToString()));
                Logger.LogDebug(string.Format("[{0}] target={1} terrain{2}", me, target.ToString(),
                    terrain.ToString()));
                player.Message("Cannot execute safely with the parameters supplied.");
                return false;
            }
        }

        private void OnPlayerHurt(HurtEvent he) {
            Fougerite.Player player = he.Victim as Fougerite.Player;
            if (TempGod.ContainsKey(player.UID)) he.DamageAmount = 0;
        }

        private void OnPlayerSpawned(Fougerite.Player player, SpawnEvent se) {
            if (LoadedClients.Contains(player.UID)) {
                GlitchTeleportTo(player, player.Location);
            } else {
                if (!TempGod.ContainsKey(player.UID)) TempGod.Add(player.UID, player.Location);
                Vector3 loc = player.Location;
                loc.y += 1000000f; player.TeleportTo(loc);
            }
        }

        private void OnCommand(Fougerite.Player player, string cmd, string[] args) {
            if (cmd == "fixme") {
                player.MessageFrom("[CUSTOM MAP]", "Fixing your default location...");
                Vector3 loc = player.Location; loc.y += 15f;
                GlitchTeleportTo(player, loc);
            } else if (cmd == "map") {
                SendInfo(player);
            }

            if (!player.Admin) return;
            if (cmd == "minfo") {
                foreach (Fougerite.Player plr in Server.GetServer().Players) {
                    SendInfo(plr);
                }
            } else if (cmd == "mres") {
                if (args.Length < 1) {
                    player.MessageFrom("[CUSTOM MAP]", $"[{CurrResources}] Resource are on the map!");
                    return;
                }
                int add = int.Parse(args[0]);
                while (add > 0) { SpawnResource(); add--; }
                player.MessageFrom("[CUSTOM MAP]", $"[{CurrResources}] Resource were spawned!");
            } else if (cmd == "manim") {
                if (args.Length < 1) {
                    player.MessageFrom("[CUSTOM MAP]", $"[{CurrAnimals}] Animals are on the map!");
                    return;
                }
                int add = int.Parse(args[0]);
                while (add > 0) { SpawnAnimal(); add--; }
                player.MessageFrom("[CUSTOM MAP]", $"[{CurrAnimals}] Animals were spawned!");
            } else if (cmd == "mbox") {
                if (args.Length < 1) {
                    player.MessageFrom("[CUSTOM MAP]", $"[{CurrLooot}] Loot Boxes are on the map!");
                    return;
                }
                int add = int.Parse(args[0]);
                while (add > 0) { SpawnLoot(); add--; }
                player.MessageFrom("[CUSTOM MAP]", $"[{CurrLooot}] Loot Boxes were spawned!");
            } else if (cmd == "mtt") {
                Vector3 pos = new Vector3(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]));
                player.TeleportTo(pos);
                player.Message("[TP] " + pos.ToString());
            } else if (cmd == "mss") {
                int radius = int.Parse(args[0]);
                Collider[] hitColliders = Physics.OverlapSphere(player.Location, radius);
                int i = 0; List<string> linesToWrite = new List<string>();
                while (i < hitColliders.Length) {
                    player.Message($"[{hitColliders[i].tag}] {hitColliders[i].transform.position.ToString()} {hitColliders[i].ToString()}");
                    StringBuilder line = new StringBuilder();
                    line.AppendLine($"[{hitColliders[i].transform.position.ToString()} {hitColliders[i].ToString()}");
                    linesToWrite.Add(line.ToString());
                    i++;
                }
                File.WriteAllLines(Path.Combine(DefLoc, "DUMP.txt"), linesToWrite.ToArray());
            }
        }

        private void OnServerLoaded() {
            try {
                GameObject teObject = new GameObject();
                TerrainGenerator teGen = teObject.AddComponent<TerrainGenerator>();
                teGen.teObject = teObject; teObject.SetActive(true);
            } catch (Exception ex) {
                ConsoleSystem.PrintError("[CUSTOM MAP] " + ex.Message.ToString());
            }
        }

        private void OnPlayerGathering(Fougerite.Player player, GatherEvent ge) {
            if (ge.Type == "Tree" || ge.Type == "Animal") return;

            if (!string.IsNullOrEmpty(ge.AmountLeft.ToString()) && !string.IsNullOrEmpty(ge.Quantity.ToString())) {
                if (ge.AmountLeft - ge.Quantity <= 2) {
                    CurrResources--;
                }
            }
        }

        private void OnItemRemoved(InventoryModEvent e) {
            GameObject obj = e.Inventory.gameObject;
            string name = obj.name.ToLower();
            string[] res = new string[] { "AmmoLootBox", "MedicalLootBox", "BoxLoot", "WeaponLootBox" };
            foreach (string inv in res) {
                if (name.Contains(inv.ToLower())) {
                    if (e.Inventory.firstItem == null) {
                        obj.GetComponent<LootableObject>().OnUseExit(null, UseExitReason.Destroy);
                        CurrLooot--;
                        break;
                    }
                }
            }
        }

        private void OnNPCKilled(DeathEvent de) {
            if (de.VictimIsNPC) {
                CurrAnimals--;
            }
        }

        public void WaitResource() {
            int min = UnityEngine.Random.Range(TimerWait[0], TimerWait[1]);
            aTimer = new Timer();
            aTimer.Elapsed += new ElapsedEventHandler(SpawnTimer);
            aTimer.Interval = min * 60000;
            aTimer.Enabled = true;
        }

        private void SpawnTimer(object source, ElapsedEventArgs e) {
            if (aTimer != null) aTimer.Dispose();
            if (CurrResources <= MaxResources) SpawnResource();
            if (CurrAnimals <= MaxAnimals) SpawnAnimal();
            if (CurrLooot <= MaxLoot) SpawnLoot();

            WaitResource();
        }

        public void SpawnAnimal() {
            Vector3 pos = RandomPositionOnTerrain();
            //string[] res = new string[] { ":stag_prefab", ":chicken_prefab", ":rabbit_prefab_a", ":bear_prefab", ":mutant_bear", ":boar_prefab", ":wolf_prefab", ":mutant_wolf" };
            string[] res = new string[] { ":stag_prefab", ":chicken_prefab", ":rabbit_prefab_a", ":boar_prefab" };
            string resSpwn = res[UnityEngine.Random.Range(0, res.Length)];
            Entity anEnt = World.GetWorld().Spawn(resSpwn, pos) as Entity;
            GameObject anGam = ((ResourceTarget)anEnt.Object).gameObject;
            GameObject.Destroy(anGam.GetComponent<NavMeshAgent>());

            /*
            if (anGam.GetComponent<BaseAIMovement>() == null) { anGam.AddComponent("BaseAIMovement"); }
            if (anGam.GetComponent<BasicWildLifeMovement>() == null) { anGam.AddComponent("BasicWildLifeMovement"); }
            if (anGam.GetComponent<BasicWildLifeAI>() == null) { anGam.AddComponent("BasicWildLifeAI"); }
            */
            CurrAnimals++;
        }

        public void SpawnResource() {
            Vector3 atPos = RandomPositionOnTerrain();
            string[] res = new string[] { ";res_woodpile", ";res_ore_1", ";res_woodpile", ";res_ore_2", ";res_woodpile", ";res_ore_3" };
            string resSpwn = res[UnityEngine.Random.Range(0, res.Length)];
            World.GetWorld().Spawn(resSpwn, atPos);
            CurrResources++;
        }

        public void SpawnLoot() {
            Vector3 atPos = RandomPositionOnTerrain();
            string[] res = new string[] { "AmmoLootBox", "MedicalLootBox", "BoxLoot", "WeaponLootBox" };
            string resSpwn = res[UnityEngine.Random.Range(0, res.Length)];
            World.GetWorld().Spawn(resSpwn, atPos);
            CurrLooot++;
        }
    }
}

/*
         private void ExportObjects() {
            GameObject[] objects = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));

            List<string> linesToWrite = new List<string>();
            foreach (GameObject obj in objects) {
                StringBuilder line = new StringBuilder();
                line.AppendLine($"[{obj.isStatic.ToString()}][{obj.GetInstanceID()}] {obj.ToString()}");
                linesToWrite.Add(line.ToString());
            }
            File.WriteAllLines(Path.Combine(DefLoc, "Prefabs.txt"), linesToWrite.ToArray());
        }
         private void FixTrees() {
            Terrain terrain = Terrain.activeTerrain;
            TerrainData teData = terrain.terrainData;

            int treeLen = teData.treeInstances.Length;
            for (int i = 0; i < treeLen; i++) {
                TreeInstance treeInstance = teData.treeInstances[i];
                Vector3 target = treeInstance.position;
                treeInstance.position = new Vector3(target.x, terrain.SampleHeight(target), target.z);
            }

            GameObject treeObj = new GameObject(); treeObj.AddComponent("TreeColliderAdd");
            TreeColliderAdd treeCollider = treeObj.GetComponent<TreeColliderAdd>();
            GameObject treePrefab = new GameObject("Elm Tree 1");
            treePrefab.AddComponent("MeshFilter"); treePrefab.AddComponent("MeshRenderer"); treePrefab.AddComponent("Tree");
            treeCollider.terrain = terrain; treeCollider.treeColliderPrefab = treePrefab;
            treeObj.SetActive(true);
        }
        /*
        private void ExportMap() {
            string filePath = Path.Combine(Util.GetRootFolder(), "Heightmaps\\rust_island_2013.raw");
            if (!File.Exists(filePath)) {
                Terrain terrain = Terrain.activeTerrain;
                TerrainData teData = terrain.terrainData;

                int w = teData.heightmapWidth; int h = teData.heightmapHeight;
                float[,] data = teData.GetHeights(0, 0, w, h);

                byte[] nmbsBytes = new byte[data.GetLength(0) * data.GetLength(1) * 4];
                int k = 0;
                for (int i = 0; i < data.GetLength(0); i++) {
                    for (int j = 0; j < data.GetLength(1); j++) {
                        byte[] array = BitConverter.GetBytes(data[i, j]);
                        for (int m = 0; m < array.Length; m++) {
                            nmbsBytes[k++] = array[m];
                        }
                    }
                }

                File.WriteAllBytes(filePath, nmbsBytes);

                ConsoleSystem.Print("[Original MAP] Map Data Backed Up!");
            } else {
                ConsoleSystem.Print("[Original MAP] Map File Exist, no need to back up.");
            }
        }
        */
