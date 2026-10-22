using Photon.Pun;
using UnityEngine;

namespace Wang.Test
{
    public sealed class PhotonTestLauncher : MonoBehaviour
    {
        private void Start()
        {
            Application.runInBackground = true;
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("로비에서 방에 입장한 뒤 게임을 시작하세요.", this);
                return;
            }

            foreach (Player player in GameManager.m_Instance.Players)
            {
                if (player != null && player.gameObject.activeInHierarchy && player.photonView.IsMine)
                    return;
            }

            if (!GameSceneManager.m_Instance.TryGetSpawnOrigin(out Vector3 spawn))
            {
                Debug.LogError("GameSceneManager에 현재 씬의 SOSceneData를 연결하세요.", this);
                return;
            }

            if (PhotonNetwork.Instantiate("Player", spawn, Quaternion.identity) == null)
                Debug.LogError("플레이어 생성에 실패했습니다. Resources/Player 프리팹을 확인하세요.", this);
        }
    }
}
