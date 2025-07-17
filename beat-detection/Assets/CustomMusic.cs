using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;

public class SimpleMusicLoader : MonoBehaviour
{
    [Header("Components")]
    public AudioSource audioSource;
    public BeatDetection beatDetection;
    
    [Header("Settings")]
    public string musicFolderName = "CustomMusic";
    public bool loadOnStart = true;
    
    private string musicFolderPath;
    
    void Start()
    {
        // Create music folder path next to the .exe
        musicFolderPath = Path.Combine(Application.dataPath, "..", musicFolderName);
        
        // Create the folder if it doesn't exist
        if (!Directory.Exists(musicFolderPath))
        {
            Directory.CreateDirectory(musicFolderPath);
            Debug.Log("Created CustomMusic folder at: " + musicFolderPath);
        }
        
        if (loadOnStart)
        {
            StartCoroutine(LoadFirstMusicFileCoroutine());
        }
    }
    
    public void LoadFirstMusicFile()
    {
        StartCoroutine(LoadFirstMusicFileCoroutine());
    }
    
    IEnumerator LoadFirstMusicFileCoroutine()
    {
        if (!Directory.Exists(musicFolderPath))
        {
            Debug.LogWarning("CustomMusic folder not found!");
            yield break;
        }
        
        // Get all files in the music folder
        string[] allFiles = Directory.GetFiles(musicFolderPath);
        
        // Find the first supported audio file
        string firstMusicFile = null;
        foreach (string filePath in allFiles)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            
            if (extension == ".mp3" || extension == ".wav" || extension == ".ogg")
            {
                firstMusicFile = filePath;
                break; // Take the first one found
            }
        }
        
        if (firstMusicFile == null)
        {
            Debug.LogWarning("No music files found in CustomMusic folder! Supported formats: .mp3, .wav, .ogg");
            yield break;
        }
        
        Debug.Log("Loading music file: " + Path.GetFileName(firstMusicFile));
        
        // Load the music file
        yield return StartCoroutine(LoadAudioFile(firstMusicFile));
    }
    
    IEnumerator LoadAudioFile(string filePath)
    {
        // Determine audio type based on file extension
        AudioType audioType = GetAudioType(filePath);
        
        string url = "file://" + filePath;
        
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                
                if (clip != null)
                {
                    // Replace the current audio clip
                    audioSource.clip = clip;
                    clip.name = Path.GetFileNameWithoutExtension(filePath);
                    
                    Debug.Log("Successfully loaded: " + clip.name);
                    
                    // Auto-play if the audio source was already playing
                    if (audioSource.isPlaying)
                    {
                        audioSource.Stop();
                        audioSource.Play();
                    }
                }
                else
                {
                    Debug.LogError("Failed to create AudioClip from: " + Path.GetFileName(filePath));
                }
            }
            else
            {
                Debug.LogError("Failed to load music file: " + www.error);
            }
        }
    }
    
    AudioType GetAudioType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        
        switch (extension)
        {
            case ".mp3": return AudioType.MPEG;
            case ".wav": return AudioType.WAV;
            case ".ogg": return AudioType.OGGVORBIS;
            default: return AudioType.MPEG;
        }
    }
    
    void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            LoadFirstMusicFile();
        }
    }
}
