

using System;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// A utility class for serializing and saving data
/// </summary>
public static class DataSerializer
{
    /// <summary>
    /// Ensures the provided path begins with a '/' character, corrects the path if this is not the case
    /// </summary>
    private static string CorrectRelativePath(string relativePath)
    {
        if(string.IsNullOrEmpty(relativePath))
        {
            return "/";
        }

        if(relativePath[0] != '/')
        {
            relativePath = "/" + relativePath;
        }
        return relativePath;
    }

    /// <summary>
    /// Saves an object in the Application.persistentDataPath directory, in a sub location specified by relativeSavePath
    /// </summary>
    public static void SaveObjectToFile(object dataObject, string relativeSavePath)
    {
        relativeSavePath = CorrectRelativePath(relativeSavePath);

        string json = JsonUtility.ToJson(dataObject, true);
        Debug.Log(relativeSavePath);
        File.WriteAllText(Application.persistentDataPath + relativeSavePath, json);
    }
    
    /// <summary>
    /// Loads an object structure from a file
    /// </summary>
    /// <typeparam name="T">The structure to map the save file to</typeparam>
    public static T LoadObjectFromFile<T>(string relativeSavePath)
    {
        relativeSavePath = CorrectRelativePath(relativeSavePath);

        string fullPath = Application.persistentDataPath + relativeSavePath;
        
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"File not found at: {fullPath}");
            return default(T);
        }
        
        string json = File.ReadAllText(fullPath);
        T loadedObject = JsonUtility.FromJson<T>(json);
        return loadedObject;
    }
}