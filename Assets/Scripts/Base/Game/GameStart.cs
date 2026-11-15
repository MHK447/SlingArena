using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameStart : MonoBehaviour
{
    IEnumerator Start()
    {
        var init = Addressables.InitializeAsync(false);
        yield return init;
        bool initialized = init.Status == AsyncOperationStatus.Succeeded;
        Addressables.Release(init);
        if (!initialized)
        {
            Debug.LogError("Addressables initialization failed.", this);
            yield break;
        }

        GameRoot.Load();
    }
}
