using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using LitJson;

public class TrackedImageInfoRuntimeCaptureManager : MonoBehaviour
{
    [SerializeField]
    private Text debugLog;

    public RawImage image;

    [SerializeField]
    private Text jobLog;

    [SerializeField]
    private Text currentImageText;

    [SerializeField]
    private Button captureImageButton;

    [SerializeField]
    private GameObject placedObject;

    [SerializeField]
    private Vector3 scaleFactor = new Vector3(0.1f,0.1f,0.1f);

    [SerializeField]
    private XRReferenceImageLibrary runtimeImageLibrary;
    
    private ARTrackedImageManager trackImageManager;

    //custom objects
   

    List<string> ImagesToRead = new List<string>();//{ "GameFrame_1", "GameFrame_2" };
    public  GameObject[] prefabsToInstantiate;
    Dictionary<string, GameObject> mapingImageToPrefab = new Dictionary<string, GameObject>();
    Texture2D[] mainTextures;

    
    IEnumerator Start()
    {
        yield return StartCoroutine(DownloadJson());
        LoadDictionary();

        mainTextures = new Texture2D[ImagesToRead.Count];
        debugLog.text += "Creating Runtime Mutable Image Library\n";
        LoadResource();
        trackImageManager = gameObject.AddComponent<ARTrackedImageManager>();
        trackImageManager.referenceLibrary = trackImageManager.CreateRuntimeLibrary(runtimeImageLibrary);
        trackImageManager.maxNumberOfMovingImages = mainTextures.Length+1;
        trackImageManager.trackedImagePrefab = placedObject;

        trackImageManager.enabled = true;

        trackImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        
        ShowTrackerInfo();

        captureImageButton.onClick.AddListener(() => StartCoroutine(CaptureImage()));

    }

   IEnumerator DownloadJson()
    {
        string path = Path.Combine("jar:file://" + Application.dataPath + "!/assets", "Test.json");
        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();

        string res = www.downloadHandler.text;

        JsonData jsonv = JsonMapper.ToObject(res);

        for (int i=0;i<jsonv["info"].Count;i++)
        {
            ImagesToRead.Add(jsonv["info"][i]["imageName"].ToString());
        }

        
    }
   public void LoadDictionary()
    {
        for(int i=0;i<ImagesToRead.Count;i++)
        {
            string[] s = ImagesToRead[i].Split('_');
            string prefabString = s[0];
            if(prefabString.ToLower().Contains("aud"))
            {
                mapingImageToPrefab.Add(ImagesToRead[i], prefabsToInstantiate[0]);   
            }
            else if(prefabString.ToLower().Contains("img"))
            {
                mapingImageToPrefab.Add(ImagesToRead[i], prefabsToInstantiate[1]);   
            }
            else if(prefabString.ToLower().Contains("vid"))
            {
                mapingImageToPrefab.Add(ImagesToRead[i], prefabsToInstantiate[2]);   
            }
        }    

        mapingImageToPrefab.Add("UnityLogo", prefabsToInstantiate[0]);
        mapingImageToPrefab.Add("GameFrame_1", prefabsToInstantiate[1]);
        mapingImageToPrefab.Add("GameFrame_2", prefabsToInstantiate[2]);
        foreach (GameObject g in mapingImageToPrefab.Values)
        {
            g.SetActive(false);
        }
    }

    public void Reset()
    {
        foreach (GameObject g in mapingImageToPrefab.Values)
        {
            g.SetActive(false);
        }
    }

    void LoadResource()
    {
        for (int i = 0; i < ImagesToRead.Count; i++)
        {
            string url = Path.Combine("jar:file://" + Application.dataPath + "!/assets", "TargetImages/" + ImagesToRead[i] +".jpg");

            try
            {
                StartCoroutine(LoadFromStreamingAssets(url, i));

            }
            catch (Exception e)
            {
                jobLog.text = "Error message : " + e;

            }
        }
        // mainTextures = Resources.LoadAll<Texture2D>("Images");
        // jobLog.text = "Image Loaded";
    }

    IEnumerator LoadFromStreamingAssets(string path, int index)
    {
        //byte[] imgData;
        //Texture2D tex = new Texture2D(2, 2);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
       yield return www.SendWebRequest();
        if(www.error==null)
        {
        var tex = DownloadHandlerTexture.GetContent(www); 
        mainTextures[index] = tex;
        image.texture = tex;
        debugLog.text+="LOADEDDD " + mainTextures[0].name;
        }
        else
        {
            debugLog.text += $"We are facing an error:" + www.error.ToString()+ "\n";
        }
     }

    private IEnumerator CaptureImage()
    {
        yield return new WaitForEndOfFrame();

        jobLog.text = "Capturing Image...";

        //var texture = ScreenCapture.CaptureScreenshotAsTexture();
        
        try
        {
            for (int i = 0; i < mainTextures.Length; i++)
            {
                jobLog.text = "Calling the coroutine";
                StartCoroutine(AddImageJob(mainTextures[i],ImagesToRead[i]));
            }
        }
        catch(Exception e)
        {
            jobLog.text = "Error : " + e;
        }
        
       
    }


    public void ShowTrackerInfo()
    {
        var runtimeReferenceImageLibrary = trackImageManager.referenceLibrary as MutableRuntimeReferenceImageLibrary;
       
        debugLog.text += $"TextureFormat.RGBA32 supported: {runtimeReferenceImageLibrary.IsTextureFormatSupported(TextureFormat.RGBA32)}\n";
        debugLog.text += $"Supported Texture Count ({runtimeReferenceImageLibrary.supportedTextureFormatCount})\n";
        debugLog.text += $"trackImageManager.trackables.count ({trackImageManager.trackables.count})\n";
        debugLog.text += $"trackImageManager.trackedImagePrefab.name ({trackImageManager.trackedImagePrefab.name})\n";
        debugLog.text += $"trackImageManager.maxNumberOfMovingImages ({trackImageManager.maxNumberOfMovingImages})\n";
        debugLog.text += $"trackImageManager.supportsMutableLibrary ({trackImageManager.subsystem.SubsystemDescriptor.supportsMutableLibrary})\n";
    }  
    void OnDisable()
    {
        trackImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

   public IEnumerator AddImageJob(Texture2D texture2D, string imageName)
    {

        jobLog.text = "Job started";

        yield return null;
        
        debugLog.text = string.Empty;
    
        debugLog.text += "Adding image\n";

        jobLog.text = "Job Starting...";

        var firstGuid = new SerializableGuid(0,0);
        var secondGuid = new SerializableGuid(0,0);
        
        XRReferenceImage newImage = new XRReferenceImage(firstGuid, secondGuid, new Vector2(0.1f,0.1f), imageName, texture2D);
        
        try
        {

            MutableRuntimeReferenceImageLibrary mutableRuntimeReferenceImageLibrary = trackImageManager.referenceLibrary as MutableRuntimeReferenceImageLibrary;
            
            debugLog.text += $"TextureFormat.RGBA32 supported: {mutableRuntimeReferenceImageLibrary.IsTextureFormatSupported(TextureFormat.RGBA32)}\n";

            debugLog.text += $"TextureFormat size: {texture2D.width}px width {texture2D.height}px height\n";

            var jobHandle = mutableRuntimeReferenceImageLibrary.ScheduleAddImageJob(texture2D, imageName, 0.1f);

            while(!jobHandle.IsCompleted)
            {
                jobLog.text = "Job Running...";
            }

            jobLog.text = "Job Completed..." + mainTextures.Length;
            debugLog.text += $"Job Completed ({mutableRuntimeReferenceImageLibrary.count})\n";
            debugLog.text += $"Supported Texture Count ({mutableRuntimeReferenceImageLibrary.supportedTextureFormatCount})\n";
            debugLog.text += $"trackImageManager.trackables.count ({trackImageManager.trackables.count})\n";
            debugLog.text += $"trackImageManager.trackedImagePrefab.name ({trackImageManager.trackedImagePrefab.name})\n";
            debugLog.text += $"trackImageManager.maxNumberOfMovingImages ({trackImageManager.maxNumberOfMovingImages})\n";
            debugLog.text += $"trackImageManager.supportsMutableLibrary ({trackImageManager.subsystem.SubsystemDescriptor.supportsMutableLibrary})\n";
            debugLog.text += $"trackImageManager.requiresPhysicalImageDimensions ({trackImageManager.subsystem.SubsystemDescriptor.requiresPhysicalImageDimensions})\n";
        }
        catch(Exception e)
        {
            jobLog.text = "Error message : " + e;
            if(texture2D == null)
            {
                debugLog.text += "texture2D is null";    
            }
            debugLog.text += $"Error: {e.ToString()}";
        }
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            if(trackedImage.referenceImage.name.ToLower().Contains("aud"))
            {
                string[] s = trackedImage.referenceImage.name.Split('_');
                
                StartCoroutine(AudioManager(s[s.Length-1]));
               
            }
            else if(trackedImage.referenceImage.name.ToLower().Contains("img"))
            {
                string[] s = trackedImage.referenceImage.name.Split('_');
                StartCoroutine(LoadSpecificTexture(s[s.Length-1]));
            }
            mapingImageToPrefab[trackedImage.referenceImage.name].transform.localPosition = Vector3.zero;
            mapingImageToPrefab[trackedImage.referenceImage.name].SetActive(true);
            currentImageText.text = trackedImage.referenceImage.name;
            mapingImageToPrefab[trackedImage.referenceImage.name].transform.position = trackedImage.transform.position;
            mapingImageToPrefab[trackedImage.referenceImage.name].transform.rotation = trackedImage.transform.rotation;
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            mapingImageToPrefab[trackedImage.referenceImage.name].transform.position = trackedImage.transform.position;
            mapingImageToPrefab[trackedImage.referenceImage.name].transform.rotation = trackedImage.transform.rotation;
            // Display the name of the tracked image in the canvas
            currentImageText.text = trackedImage.referenceImage.name;
            
        }
    }

    IEnumerator AudioManager(string name)
    {
       
        string url = Path.Combine("jar:file://" + Application.dataPath + "!/assets", "Asset/" + name +".mp3");
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip( url, AudioType.MPEG );
        yield return request.SendWebRequest();
        if( request.isNetworkError )
        {
            jobLog.text =  request.error + "\n" + url ;
        } 
        else
         {
            AudioClip clip = DownloadHandlerAudioClip.GetContent( request );
            prefabsToInstantiate[0].GetComponent<AudioSource>().clip = clip;
            prefabsToInstantiate[0].GetComponent<AudioSource>().Play();
         }
    }

    IEnumerator LoadSpecificTexture(string name)
    {
       GameObject g = prefabsToInstantiate[1].transform.GetChild(0).gameObject;
        
       string path = Path.Combine("jar:file://" + Application.dataPath + "!/assets", "Asset/" + name +".jpg");
       UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
       yield return www.SendWebRequest();
        if(www.error==null)
        {
        var tex = DownloadHandlerTexture.GetContent(www); 
        g.GetComponent<Renderer>().material.mainTexture = tex; 
        }
       
    }
}
