using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using UnityEngine.UI;
public class SaveSelectUI : MonoBehaviour
{
    public ApiSettings apiSettings;
    [Header("三个存档槽的通关星星")]
    public Image slot1Star;
    public Image slot2Star;
    public Image slot3Star;
    private int pendingDeleteSlot = 0;
    public GameObject deleteConfirmPanel;
    private void Start()
    {
        StartCoroutine(LoadAllSlots());
    }
    // 选择存档
    public void SelectSlot(int slot)
    {
        SaveSlotManager.CurrentSlot = slot;
        Debug.Log("当前选择存档：" + slot);
        StartCoroutine(EnterGame(slot));
    }
    // 删除指定存档
    public void DeleteSlot(int slot)
    {
        Debug.Log("准备删除存档：" + slot);
        StartCoroutine(DeleteSlotCoroutine(slot));
    }

    private IEnumerator DeleteSlotCoroutine(int slot)
    {
        string url = apiSettings.baseUrl + "/save/clear?slot=" + slot;
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("存档" + slot + "删除成功");
            // 删除后重新刷新星星
            yield return LoadAllSlots();
        }
        else
        {
            Debug.LogError("删除存档" + slot + "失败：" + request.error +"，服务器返回：" + request.downloadHandler.text
            );
        }
    }
    private IEnumerator LoadAllSlots()
    {
        yield return CheckSlot(1, slot1Star);
        yield return CheckSlot(2, slot2Star);
        yield return CheckSlot(3, slot3Star);
    }
    private IEnumerator CheckSlot(int slot, Image starImage)
    {
        string url = apiSettings.baseUrl + "/save?slot=" + slot;
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            PlayerSaveData data =JsonUtility.FromJson<PlayerSaveData>( request.downloadHandler.text );
            if (data != null && data.completed)
            {
                starImage.gameObject.SetActive(true);
                Debug.Log("存档" + slot + "已经通关");
            }
            else
            {
                starImage.gameObject.SetActive(false);
            }
        }
        else
        {
            starImage.gameObject.SetActive(false);
        }
    }
    private IEnumerator EnterGame(int slot)
    {
        string url = apiSettings.baseUrl + "/save?slot=" + slot;
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("存档" + slot + "已存在，直接进入游戏");
            SceneManager.LoadScene("GameScene");
            yield break;
        }
        Debug.Log("存档" + slot + "不存在，创建新存档");
        PlayerSaveData newData = new PlayerSaveData();
        newData.resources = new System.Collections.Generic.List<ResourceData>();
        newData.playerPosition = new PlayerPosition();
        newData.playerPosition.x = 0;
        newData.playerPosition.y = 0;
        newData.openedChestIds = new System.Collections.Generic.List<string>();
        newData.defeatedEnemyIds = new System.Collections.Generic.List<string>();
        newData.completed = false;
        string json = JsonUtility.ToJson(newData);
        UnityWebRequest createRequest =new UnityWebRequest(url, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        createRequest.uploadHandler =new UploadHandlerRaw(body);
        createRequest.downloadHandler = new DownloadHandlerBuffer();
        createRequest.SetRequestHeader(
            "Content-Type",
            "application/json"
        );
        yield return createRequest.SendWebRequest();
        if (createRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("新存档创建成功：" + slot);
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            Debug.LogError("创建存档失败：" + createRequest.error + "服务器返回：" +createRequest.downloadHandler.text
            );
        }
    }
    public void AskDeleteSlot(int slot)
    {
        pendingDeleteSlot = slot;
        deleteConfirmPanel.SetActive(true);
        Debug.Log("准备删除存档：" + slot);
    }
    public void CancelDelete()
    {
        pendingDeleteSlot = 0;
        deleteConfirmPanel.SetActive(false);
        Debug.Log("取消删除存档");
    }
    public void ConfirmDelete()
    {
        if (pendingDeleteSlot <= 0)
        {
            return;
        }
        int slot = pendingDeleteSlot;
        pendingDeleteSlot = 0;
        deleteConfirmPanel.SetActive(false);
        DeleteSlot(slot);
    }
}