using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using UnityEngine.Networking;
using System.IO;

public class DownloadImages : MonoBehaviour
{

    string targetImagePath;
    string assetPath;
    public Text Feedback;

    // Start is called before the first frame update
    void Start()
    {
         targetImagePath = Application.streamingAssetsPath + "/TargetImages";
         assetPath = Application.streamingAssetsPath + "/Asset";

        if (!Directory.Exists(Application.streamingAssetsPath + "/TargetImages"))
        {
            //if it doesn't, create it
            Directory.CreateDirectory(Application.streamingAssetsPath + "/TargetImages");

        }

        if (!Directory.Exists(Application.streamingAssetsPath + "/Asset"))
        {
            //if it doesn't, create it
            Directory.CreateDirectory(Application.streamingAssetsPath + "/Asset");

        }

        StartCoroutine(DownloadJson());
    }
   

    IEnumerator DownloadJson()
    {
        UnityWebRequest www = UnityWebRequest.Get(Application.streamingAssetsPath + "/Test.json");
        yield return www.SendWebRequest();

        string res = www.downloadHandler.text;

        JsonData jsonv = JsonMapper.ToObject(res);

        for (int i=0;i<jsonv["info"].Count;i++)
        {
            string imageUrl = jsonv["info"][i]["imageUrl"].ToString();
            string assets = jsonv["info"][i]["asset"].ToString();
            string imageName = jsonv["info"][i]["imageName"].ToString();
            yield return StartCoroutine(DownloadImage(imageUrl,assets,imageName+".jpg"));
        }

        Feedback.text = "DONE";
    }

    IEnumerator DownloadImage(string imageUrl, string assetUrl,string name)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            if (!System.IO.File.Exists(targetImagePath + "/" + name))
            {
                byte[] b = www.downloadHandler.data;
                File.WriteAllBytes(targetImagePath + "/" + name, b);

                UnityWebRequest w = UnityWebRequestTexture.GetTexture(assetUrl);
                yield return w.SendWebRequest();

                if (w.isNetworkError || w.isHttpError)
                {
                    Debug.Log(w.error);
                }
                else
                {
                    string[] splited = name.Split('_');

                    byte[] bb = w.downloadHandler.data;
                    File.WriteAllBytes(assetPath + "/" + splited[1], bb);
                }
            }
            else
            {
                Debug.Log("Dont do anything");
            }
        }
    }

}