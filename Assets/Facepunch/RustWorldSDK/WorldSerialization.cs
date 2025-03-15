using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using LZ4;
using Newtonsoft.Json; 

using RustMapEditor.Variables;

public class BreakerSerialization
{
	public BreakerPreset breaker = new BreakerPreset();
	

	public void Save(string fileName)
    {
        try
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    
                    using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
                        Serializer.Serialize(compressionStream, breaker);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
		
    public BreakerPreset Load(string fileName)
    {
        try
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    
                    using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
					{
						breaker = Serializer.Deserialize<BreakerPreset>(compressionStream);
						return breaker;
					}
				
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
			return breaker;
        }
    }
}

public class WorldSerialization
{
    public const uint CurrentVersion = 9;
	public const uint REPrefabVersion = 1;

    public static uint Version
    {
        get; private set;
    }

    public WorldData world = new WorldData();
	public REPrefabData rePrefab = new REPrefabData();

    public WorldSerialization()
    {
        Version = CurrentVersion;
    }

    [ProtoContract]
    public class WorldData
    {
        [ProtoMember(1)] public uint size = 4000;
        [ProtoMember(2)] public List<MapData> maps = new List<MapData>();
        [ProtoMember(3)] public List<PrefabData> prefabs = new List<PrefabData>();
        [ProtoMember(4)] public List<PathData> paths = new List<PathData>();
    }
	
	
	[ProtoContract]
	public class REPrefabData
    {
		[ProtoMember(1)] public ModifierData modifiers = new ModifierData();
        [ProtoMember(3)] public List<PrefabData> prefabs = new List<PrefabData>();
        [ProtoMember(5)] public CircuitDataHolder electric = new CircuitDataHolder();	
		[ProtoMember(6)] public string emptychunk1 = "";		
		[ProtoMember(7)] public NPCDataHolder npcs = new NPCDataHolder();		
		[ProtoMember(8)] public string emptychunk3 = "";		
		[ProtoMember(9)] public string emptychunk4 = "";		
		[ProtoMember(10)] public string buildingchunk = "";		
		[ProtoMember(11)] public string checksum;
    }

	[Serializable]
    [ProtoContract]
	public class ModifierData
	{
		[ProtoMember(1)] public int size;
		[ProtoMember(2)] public int fade;
		[ProtoMember(3)] public int fill;
		[ProtoMember(4)] public int counter;
		[ProtoMember(5)] public uint id;
	}
	
	[Serializable]
    [ProtoContract]
	public class NPCDataHolder
	{
		[ProtoMember(1)] public List<NPCData> bots = new List<NPCData>();
		
		public NPCDataHolder() { }
        public NPCDataHolder(List<NPCData> bots)
        {
            this.bots = bots;
		}
	}

    [ProtoContract]
    public class MapData
    {
        [ProtoMember(1)] public string name;
        [ProtoMember(2)] public byte[] data;
    }

	[Serializable]
    [ProtoContract]
	public class CircuitDataHolder
	{
		[ProtoMember(1)] public List<CircuitData> circuitData = new List<CircuitData>();
		
		public CircuitDataHolder() { }
        public CircuitDataHolder(List<CircuitData> circuitData)
        {
            this.circuitData = circuitData;
		}
	}

	[Serializable]
    [ProtoContract]
    public class CircuitData
	{
		[ProtoMember(1)] public string path;
        [ProtoMember(2)] public VectorData wiring;
		[ProtoMember(3)] public List<Circuit> branchIn = new List<Circuit>();
		[ProtoMember(4)] public List<Circuit> branchOut = new List<Circuit>();
		[ProtoMember(5)] public int cardType;
		[ProtoMember(6)] public int flow1;
		[ProtoMember(7)] public float setting;
		//[ProtoMember(11)] public int manipulator;
		//[ProtoMember(14)] public int flow3;
		
		[ProtoMember(14)] public string cctv;
		[ProtoMember(16)] public int flow2;
		[ProtoMember(17)] public string phone;
		//[ProtoMember(17)] public int flow5;
		
		public Circuit[] connectionsIn;
		public Circuit[] connectionsOut;
		
		public CircuitData() { }
        public CircuitData(string path, Vector3 wiring, List<Circuit> branchIn, List<Circuit> branchOut, int flow1, float setting,int flow2, string cctv, string phone)
        {
            this.path = path;
            this.wiring = wiring;
            this.branchIn = branchIn;
			this.branchOut = branchOut;
			this.flow1 = flow1;
			this.setting = setting;
			this.flow2 = flow2;
			this.cctv = cctv;
			this.phone = phone;
        }
		public CircuitData(string path, Vector3 wiring, Circuit[] connectionsIn, Circuit[] connectionsOut, int flow1, float setting, int flow2, string cctv, string phone)
        {
            this.path = path;
            this.wiring = wiring;
            this.connectionsIn = connectionsIn;
			this.connectionsOut = connectionsOut;
			this.flow1 = flow1;
			this.setting = setting;
			this.flow2 = flow2;
			this.cctv = cctv;
			this.phone = phone;
		}
		
	}
	
	[Serializable]
    [ProtoContract]
    public class NPCData
	{
		[ProtoMember(1)] public int type;
		[ProtoMember(2)] public int respawnMin;
		[ProtoMember(3)] public int respawnMax;
		[ProtoMember(4)] public VectorData scientist;
		[ProtoMember(5)] public string category;
		
		public NPCData() { }
		public NPCData(int type, int respawnMin, int respawnMax, VectorData scientist, string category)
		{
			this.type = type;
			this.respawnMin = respawnMin;
			this.respawnMax = respawnMax;
			this.scientist = scientist;
			this.category = category;
		}
	}
	
	[Serializable]
    [ProtoContract]
    public class Circuit
	{
		[ProtoMember(1)] public string path;
        [ProtoMember(2)] public VectorData wiring;
        [ProtoMember(3)] public int flow1;
        [ProtoMember(4)] public int flow2;
        [ProtoMember(5)] public int fluid1;
		
		public Circuit() { }
        public Circuit(string path, Vector3 wiring, int flow1, int flow2, int fluid1)
        {
            this.path = path;
            this.wiring = wiring;
			this.flow1 = flow1;
			this.flow2 = flow2;
			this.fluid1 = fluid1;

        }
	}
	

    [Serializable]
    [ProtoContract]
    public class PrefabData
    {
        [ProtoMember(1)] public string category;
        [ProtoMember(2)] public uint id;
        [ProtoMember(3)] public VectorData position;
        [ProtoMember(4)] public VectorData rotation;
        [ProtoMember(5)] public VectorData scale;


        public PrefabData() { }
        public PrefabData(string category, uint id, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.category = category;
            this.id = id;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    [Serializable]
    [ProtoContract]
    public class PathData
    {
        [ProtoMember(1)] public string name;
        [ProtoMember(2)] public bool spline;
        [ProtoMember(3)] public bool start;
        [ProtoMember(4)] public bool end;
        [ProtoMember(5)] public float width;
        [ProtoMember(6)] public float innerPadding;
        [ProtoMember(7)] public float outerPadding;
        [ProtoMember(8)] public float innerFade;
        [ProtoMember(9)] public float outerFade;
        [ProtoMember(10)] public float randomScale;
        [ProtoMember(11)] public float meshOffset;
        [ProtoMember(12)] public float terrainOffset;
        [ProtoMember(13)] public int splat;
        [ProtoMember(14)] public int topology;
        [ProtoMember(15)] public VectorData[] nodes;
        [ProtoMember(16)] public int hierarchy;
    }

	

    [Serializable]
    [ProtoContract]
    public class VectorData
    {
        [ProtoMember(1)] public float x;
        [ProtoMember(2)] public float y;
        [ProtoMember(3)] public float z;

        public VectorData()
        {
        }

        public VectorData(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator VectorData(Vector3 v)
        {
            return new VectorData(v.x, v.y, v.z);
        }

        public static implicit operator VectorData(Quaternion q)
        {
            return q.eulerAngles;
        }

        public static implicit operator Vector3(VectorData v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static implicit operator Quaternion(VectorData v)
        {
            return Quaternion.Euler(v);
        }		
    }

    public MapData GetMap(string name)
    {
        for (int i = 0; i < world.maps.Count; i++){
            if (world.maps[i].name == name) return world.maps[i];
		}
        return null;
    }
	

    public void AddMap(string name, byte[] data)
    {
        var map = new MapData();

        map.name = name;
        map.data = data;

        world.maps.Add(map);
    }

    public void Save(string fileName)
    {
        try
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(Version);

                    using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
                        Serializer.Serialize(compressionStream, world);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
	
	public void SaveREPrefab(string fileName)
    {
		string checksum;

        try
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(REPrefabVersion);

                    using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
					{
						
                        Serializer.Serialize(compressionStream, rePrefab);
					}
                }
            }
			
				using (var md5 = System.Security.Cryptography.MD5.Create())
						{
							using(var stream = System.IO.File.OpenRead(fileName))
							{
								var hash = md5.ComputeHash(stream);
								checksum = BitConverter.ToString(hash).Replace("-", "").ToLower();
								
								Debug.LogError(checksum);
								rePrefab.checksum = checksum;
							}
						}
						
		using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(REPrefabVersion);

                    using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
					{
                        Serializer.Serialize(compressionStream, rePrefab);
					}
                }
            }
			
			
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
	
	
	//outputs decompressed lz4 binary file protobuftest
	public void Decompress(string fileName)
	{
		try
		{
			using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
				using (var binaryReader = new BinaryReader(fileStream))
                {
						Version = binaryReader.ReadUInt32();
						
						using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
							{
								MemoryStream destination = new MemoryStream();
								compressionStream.CopyTo(destination);
								var b = new byte[destination.Length];
								b = destination.ToArray();
								File.WriteAllBytes("protobuftest", b);
								Debug.LogError("Actually decompressed");
							}
				}
			}
		}	
		catch (Exception e)
		{
			Debug.LogError(e.Message);
		}
	}

    public void Load(string fileName)
    {
		
		try
        {
		    using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
				
				using (var binaryReader = new BinaryReader(fileStream))
                {
                    Version = binaryReader.ReadUInt32();

                    if (Version != CurrentVersion)
						Debug.LogError("wrong version");
					
					using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
					{
						world = Serializer.Deserialize<WorldData>(compressionStream);
					}
					
					
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
	
	public void LoadREPrefab(string fileName)
    {
        try
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
				using (var binaryReader = new BinaryReader(fileStream))
                {
					uint ver = binaryReader.ReadUInt32();
					//Debug.LogError(ver);
					
                    using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
					{
						
						rePrefab = Serializer.Deserialize<REPrefabData>(compressionStream);
						
					}
				}
                
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
	
    public void SavePrefabJSON(string fileName)
    {
        try
        {
            // Serialize REPrefabData to JSON with Newtonsoft.Json
            string json = JsonConvert.SerializeObject(rePrefab, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Handle circular references
                NullValueHandling = NullValueHandling.Include // Include null values
            });

            // Write JSON to file
            File.WriteAllText(fileName, json);
            Debug.Log($"Saved JSON to {fileName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving JSON: {e.Message}");
        }
    }
	
}