using UnityEngine;

public class UIDonDestroy : MonoBehaviour
{
    private static UIDonDestroy m_Instance;


    private void Awake()
    {
        if(m_Instance != null)
        {
            Destroy(this);
            return;
        }

        m_Instance = this;
        DontDestroyOnLoad(this);
    }
}
