using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public sealed class PersistentGameplayCanvas : MonoBehaviour
{
    private static PersistentGameplayCanvas instance;
    private Canvas canvas;
    private EventSystem eventSystem;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        canvas = GetComponent<Canvas>();
        eventSystem = GetComponentInChildren<EventSystem>(true);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool inGameplay = scene.name != "LobyScene";
        canvas.enabled = inGameplay;
        if (eventSystem == null)
            return;

        eventSystem.gameObject.SetActive(inGameplay);
        if (!inGameplay)
            return;

        foreach (EventSystem other in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (other != eventSystem)
                other.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}
