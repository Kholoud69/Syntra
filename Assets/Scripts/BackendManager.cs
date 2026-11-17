using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class BackendManager : MonoBehaviour
{
    private string baseUrl = "http://192.168.1.5:8000";

    public void CheckBackend()
    {
        StartCoroutine(CheckConnection());
    }

    IEnumerator CheckConnection()
    {
        using (UnityWebRequest req =
               UnityWebRequest.Get(baseUrl + "/health"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Connected!");
                Debug.Log(req.downloadHandler.text);
            }
            else
            {
                Debug.LogError(req.error);
            }
        }
    }

    public void SaveProject()
    {
        StartCoroutine(SendProject());
    }

    IEnumerator SendProject()
    {
        string json =
            "{\"project_name\":\"College Network\"}";

        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req =
               new UnityWebRequest(baseUrl + "/projects", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader(
                "Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Saved: " + req.downloadHandler.text);
            }
            else
            {
                Debug.LogError(
                    req.responseCode + " " +
                    req.downloadHandler.text);
            }
        }
    }
}