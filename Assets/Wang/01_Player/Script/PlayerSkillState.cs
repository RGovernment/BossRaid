using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static FSM;
using static InputManager;
using static SOSkillState;

public sealed class PlayerSkillState : PlayerState
{
    private readonly SkillSpawner m_refSpawner;
    private int m_iCurrentSkill = -1;

    public PlayerSkillState(FSM _refOwner, SkillSpawner _refSpawner)
        : base(_refOwner, eStateType.Skill)
    {
        m_refSpawner = _refSpawner;
    }

    public override void Init()
    {
        base.Init();
        
        InputManager.m_Instance.Skill0Action.started += RequestSkill0;
        InputManager.m_Instance.Skill1Action.started += RequestSkill1;
    }

    public override void Dispose()
    {
        InputManager.m_Instance.Skill0Action.started -= RequestSkill0;
        InputManager.m_Instance.Skill1Action.started -= RequestSkill1;

        base.Dispose();
    }
    
    //여기 좀 어떻게 해야할듯
    private void RequestSkill0(InputAction.CallbackContext _tContext) 
    { 
         if(m_refOwner.ChangeState(eStateType.Skill))
         {
             m_iCurrentSkill = 1001;
             m_refSpawner.UseSkill(m_iCurrentSkill);
            m_refOwner.Animator.SetTrigger("Test");
         }
    }
    private void RequestSkill1(InputAction.CallbackContext _tContext)
    {
        if (m_refOwner.ChangeState(eStateType.Skill))
        {
            m_iCurrentSkill = 1002;
            m_refSpawner.UseSkill(m_iCurrentSkill);
            m_refOwner.Animator.SetTrigger("Test2");
        }
    }

    private bool CanStartSkill()
    {
        eStateType? eCurrent = m_refOwner.CurrentState?.Type;
        return m_refOwner.HasGameplayInput &&
            (eCurrent == eStateType.Idle || eCurrent == eStateType.Walk || eCurrent == eStateType.Run);
    }

    public override bool CanEnter() => CanStartSkill();

    public override void Enter()
    {
        m_refOwner.Rigidbody.rotation = m_refOwner.ViewRotation;
    }

    public override void UpdateState()
    {
       if(!m_refSpawner.IsSkillActive)
            m_refOwner.ChangeState(PlayerMoveState.SelectState());
    }

    public override void Exit()
    {
        m_iCurrentSkill = -1;
    }

}
