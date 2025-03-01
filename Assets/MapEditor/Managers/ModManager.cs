using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using UnityEngine;

public static class ModManager
{
    public static List<WorldSerialization.MapData> moddingData = new List<WorldSerialization.MapData>();
    public static readonly string[] KnownDataNames = new string[] 
    { 
        "ioentitydata", "vehiclespawnpoints", "lootcontainerdata", "vendingdata", 
        "npcspawnpoints", "bradleypathpoints", "anchorpaths", "mappassword" 
    };
	
	//compatibility for high-security data fields (prefab count + salt)
	public static string MapDataName(int PreFabCount, string DataName)
    {      
       try
       {
           using (var aes = Aes.Create())
           {
               var rfc2898DeriveBytes = new Rfc2898DeriveBytes(PreFabCount.ToString(), new byte[] { 73, 118, 97, 110, 32, 77, 101, 100, 118, 101, 100, 101, 118 });
               aes.Key = rfc2898DeriveBytes.GetBytes(32);
               aes.IV = rfc2898DeriveBytes.GetBytes(16);
               using (var memoryStream = new MemoryStream())
               {
                   using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                   {
                       var D = Encoding.Unicode.GetBytes(DataName);
                       cryptoStream.Write(D, 0, D.Length);
                       cryptoStream.Close();
                   }

                   return Convert.ToBase64String(memoryStream.ToArray());
               }
           }
       }
       catch { }
       return DataName;
    }

    public static void SetModdingData(List<WorldSerialization.MapData> data)
    {
        moddingData.Clear();
        if (data != null)
        {
            moddingData.AddRange(data);
        }
        else
        {
        }
    }

    public static List<WorldSerialization.MapData> GetModdingData()
    {
        return new List<WorldSerialization.MapData>(moddingData);
    }


    public static void AddOrUpdateModdingData(string name, byte[] data)
    {
        var existing = moddingData.Find(md => md.name == name);
        if (existing != null)
        {
            existing.data = data;
        }
        else
        {
            moddingData.Add(new WorldSerialization.MapData { name = name, data = data });
        }
    }


    public static void ClearModdingData()
    {
        moddingData.Clear();
    }


    public static string[] GetKnownDataNames()
    {
        return KnownDataNames;
    }
}