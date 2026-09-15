using UnityEngine;
using System.Collections;
public class SaveManager : MonoBehaviour
{
    public ResourceSystemHost resourceSystem;
    public Transform player;
    public WorldStateManager worldStateManager;
    private bool _isSaving = false;

    private void Start()
    {
        LoadGame();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X) && !_isSaving)
        {
            SaveGame();
        }
    }
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void SaveGame()
    {
        _isSaving = true;
        PlayerSaveData data = new PlayerSaveData();
        data.resources = resourceSystem.Manager.GetAllResource();
        data.playerPosition = new PlayerPosition();
        data.playerPosition.x = player.position.x;
        data.playerPosition.y = player.position.y;
        //世界状态
        data.openedChestIds=worldStateManager.GetOpenedChestIds();
        data.defeatedEnemyIds=worldStateManager.GetDefeatedEnemyIds();
        Debug.Log(
            "准备保存游戏，资源数量：" + data.resources.Count + ",玩家位置：(" + data.playerPosition.x + "," + data.playerPosition.y + ")" +"，已打开宝箱：" +data.openedChestIds.Count +"，已击败敌人：" +data.defeatedEnemyIds.Count);
        SaveNetwork saveNetwork = new SaveNetwork(resourceSystem.apiSettings);
        StartCoroutine(SaveCoroutineWrap(saveNetwork, data));
    }

    private IEnumerator SaveCoroutineWrap(SaveNetwork saveNetwork, PlayerSaveData data)
    {
        yield return saveNetwork.SaveGame(data);

        Debug.Log("云端存档保存完成，播放存档CG");
        CGControl runtimeCg = Object.FindObjectOfType<CGControl>();

        if (runtimeCg != null)
        {
            runtimeCg.PlaySaveCg(() =>
            {
                Debug.Log("存档CG播放结束，解除存档锁定");
                _isSaving = false;
            });
        }
        else
        {
            Debug.LogError("找不到CGControl");
            _isSaving = false;
        }
    }

    public void LoadGame()
    {
        SaveNetwork saveNetwork = new SaveNetwork(resourceSystem.apiSettings);
        StartCoroutine(saveNetwork.LoadGame(OnLoadGameSuccess));
    }

    private void OnLoadGameSuccess(PlayerSaveData data)
    {
        //加载玩家位置
        player.position = new Vector3(data.playerPosition.x, data.playerPosition.y,player.position.z);
        //加载资源
        if (resourceSystem != null && resourceSystem.Manager != null)
        {
            resourceSystem.Manager.LoadFromSave(data.resources);
            Debug.Log("SaveManager：资源加载完成，数量：" + resourceSystem.Manager.GetAllResource().Count
            );
        }
        else
        {
            Debug.LogError("SaveManager：ResourceSystem 或 Manager 为空");
        }
        //加载世界状态
        if (worldStateManager != null)
        {
            worldStateManager.LoadWorldState( data.openedChestIds, data.defeatedEnemyIds);
        }
    }
    public void ClearGameData(System.Action<bool> onComplete = null)
    {
        Debug.Log("开始执行 ClearGameData");
        SaveNetwork saveNetwork =new SaveNetwork(resourceSystem.apiSettings);
        StartCoroutine(
            saveNetwork.ClearSave(saveSuccess =>
            {
                if (!saveSuccess)
                {
                    onComplete?.Invoke(false);
                    return;
                }
                Debug.Log("save清理成功，开始清理resources");
                StartCoroutine(
                    saveNetwork.ClearResources(resourceSuccess =>
                    {
                        if (resourceSuccess)
                        {
                            Debug.Log("resources清理成功");
                            //清Unity缓存
                            resourceSystem.Manager.ClearResources();
                            if (worldStateManager != null)
                            {
                                worldStateManager.ClearWorldState();
                            }
                            onComplete?.Invoke(true);
                        }
                        else
                        {
                            onComplete?.Invoke(false);
                        }
                    })
                );
            })
        );
    }
    public void CompleteCurrentSave()
    {
        PlayerSaveData data = new PlayerSaveData();
        data.resources = resourceSystem.Manager.GetAllResource();
        data.playerPosition = new PlayerPosition();
        // 通关后固定出生位置
        data.playerPosition.x = 45f;
        data.playerPosition.y = -2.716f;
        data.openedChestIds = worldStateManager.GetOpenedChestIds();
        data.defeatedEnemyIds = worldStateManager.GetDefeatedEnemyIds();
        data.completed = true;
        SaveNetwork saveNetwork = new SaveNetwork(resourceSystem.apiSettings);
        StartCoroutine(SaveCompletedCoroutine(saveNetwork, data));
    }
    private IEnumerator SaveCompletedCoroutine(
    SaveNetwork saveNetwork,
    PlayerSaveData data)
    {
        yield return saveNetwork.SaveGame(data);
        Debug.Log("当前存档通关状态已保存，存档槽："+ SaveSlotManager.CurrentSlot);
    }
}
