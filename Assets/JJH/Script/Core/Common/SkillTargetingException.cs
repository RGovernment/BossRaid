using System;
using UnityEngine;

public class SkillTargetingException : Exception
{
    // 나중에 디버깅할 때 요긴하게 쓸 추가 데이터 필드들
    public int SkillId { get; private set; }
    public string ErrorReason { get; private set; }

    public SkillTargetingException() : base("스킬 타겟팅 연산 중 알 수 없는 오류가 발생했습니다.") { }

    public SkillTargetingException(string message) : base(message) { }

    public SkillTargetingException(int skillId, string reason)
        : base($"[Skill ID: {skillId}] 타겟팅 실패 원인: {reason}")
    {
        SkillId = skillId;
        ErrorReason = reason;
    }
}
