using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SelectRandomTarget", story: "Select [Target] for [Self]", category: "Action", id: "547b2b95d06ae0d95e242467c9c2d851")]
public partial class SelectRandomTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeField] public string playerTag = "Player";

    protected override Status OnStart()
    {
        //가드절
        if (Self == null || Self.Value == null || Target == null)
            return Status.Failure;
        //이전타겟 초기화
        Target.Value = null;
        //범위 가져오기 
        BossRangeSettings ranges = Self.Value.GetComponent<BossRangeSettings>();
        if (ranges == null)
            return Status.Failure;
        //플레이어 테그 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        //범위안의 플레이어 넣기
        var candidates = new System.Collections.Generic.List<GameObject>();
        //범위 제곱
        float rangeSquared = ranges.DetectionRange * ranges.DetectionRange;
        //범위 검사
        foreach (GameObject player in players)
        {
            Vector3 offset = player.transform.position - Self.Value.transform.position;

            // 높이를 제외하고 바닥 기준 거리로 탐색합니다.
            offset.y = 0f;
            if (offset.sqrMagnitude <= rangeSquared)
                candidates.Add(player);
        }

        if (candidates.Count == 0)
            return Status.Failure;
        //랜덤 뽑기
        int index = UnityEngine.Random.Range(0, candidates.Count);
        Target.Value = candidates[index];

        Debug.Log($"어글자 선택: {Target.Value.name}");

        return Status.Success;
    }


}

