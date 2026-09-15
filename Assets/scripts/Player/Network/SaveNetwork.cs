using System.Collections;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
public class SaveNetwork
{
    private ApiSettings apiSettings;
    public SaveNetwork(ApiSettings apiSettings)
    {  this.apiSettings = apiSettings;}
    public IEnumerator SaveGame(PlayerSaveData data)
    {
        string fullurl =apiSettings.baseUrl+ "/save?slot="+ SaveSlotManager.CurrentSlot;
        string json=JsonUtility.ToJson(data);
        Debug.Log("发送保存数据："+json);
        UnityWebRequest request=new UnityWebRequest(fullurl,"POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler=new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type","application/json");
        yield return request.SendWebRequest();
        if(request.result==UnityWebRequest.Result.Success)
        {
            Debug.Log("游戏保存成功：" + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("游戏保存失败：" + request.error);
        }
    }
    public IEnumerator LoadGame(System.Action<PlayerSaveData> onSuccess)
    {
        string fullurl =apiSettings.baseUrl+ "/save?slot="+ SaveSlotManager.CurrentSlot;
        UnityWebRequest request = UnityWebRequest.Get(fullurl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("服务器存档数据：" + json);
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
            if (data != null)
            {
                Debug.Log("服务器存档解析成功");
                onSuccess?.Invoke(data);
            }
            else
            {
                Debug.LogError("服务器存档解析失败");
            }
        }
        else
        {
            Debug.Log("加载游戏失败");
        }
    }
    public IEnumerator ClearSave(System.Action<bool> callback = null)
    {
        string fullurl =apiSettings.baseUrl +"/save/clear?slot=" +SaveSlotManager.CurrentSlot;
        Debug.Log("准备清空存档：" + fullurl);
        UnityWebRequest request = new UnityWebRequest(fullurl, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("后端存档清空成功：" +request.downloadHandler.text);
            callback?.Invoke(true);
        }
        else
        {
            Debug.LogError("后端存档清空失败：" +request.error +"返回：" + request.downloadHandler.text);
            callback?.Invoke(false);
        }
        request.Dispose();
    }
    public IEnumerator ClearResources(System.Action<bool> callback = null)
    {
        string fullurl =apiSettings.baseUrl + "/resource/clear?slot=" + SaveSlotManager.CurrentSlot;
        Debug.Log("准备清空资源：" + fullurl);
        UnityWebRequest request = new UnityWebRequest(fullurl, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log( "后端资源清空成功：" + request.downloadHandler.text);
            callback?.Invoke(true);
        }
        else
        {
            Debug.LogError("后端资源清空失败：" + request.error + "返回：" +request.downloadHandler.text
            );
            callback?.Invoke(false);
        }
    }
}

