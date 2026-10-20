using UnityEngine;

[CreateAssetMenu(fileName = "SOSceneData", menuName = "Game/Scene Data")]
public sealed class SOSceneData : ScriptableObject
{
    public string m_strNextSceneName;
    public Vector3 m_vSpawnPos;

    public SOAudio m_refBGMAudio;
}
