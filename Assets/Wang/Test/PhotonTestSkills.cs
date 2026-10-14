using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Wang.Test
{
    [RequireComponent(typeof(PhotonView), typeof(Player))]
    public sealed class PhotonTestSkills : MonoBehaviourPun
    {
        [SerializeField] private GameObject slashEffect;
        [SerializeField] private GameObject bladeDanceEffect;
        [SerializeField] private AnimationClip slashClip;
        [SerializeField] private AnimationClip bladeDanceClip;

        private Player player;
        private FSM fsm;
        private Animator animator;
        private InputManager input;
        private int currentSlot = -1;
        private bool effectPlayed;
        private Coroutine finishTimer;
        private Vector3 castPosition;
        private Quaternion castRotation;

        private void Awake()
        {
            player = GetComponent<Player>();
            fsm = GetComponent<FSM>();
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (!photonView.IsMine)
                return;

            input = InputManager.m_Instance;
            input.Skill0Action.started += RequestSlash;
            input.Skill1Action.started += RequestBladeDance;
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.Skill0Action.started -= RequestSlash;
                input.Skill1Action.started -= RequestBladeDance;
                input = null;
            }
            StopAllCoroutines();
            finishTimer = null;
            if (currentSlot >= 0)
                fsm.Stop();
        }

        private void RequestSlash(InputAction.CallbackContext context)
        {
        }// player.UseSkill(0);
        private void RequestBladeDance(InputAction.CallbackContext context)
        {

        //=> player.UseSkill(1);
        }

        public void BeginSkill(int slot)
        {
            currentSlot = slot;
            effectPlayed = false;
            castPosition = transform.position;
            castRotation = photonView.IsMine ? player.ViewRotation : transform.rotation;
            if (photonView.IsMine)
            {
                Rigidbody body = GetComponent<Rigidbody>();
                body.rotation = castRotation;
                body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
            }
            animator.speed = 1f;
            animator.SetBool("Ground", true);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            animator.SetFloat("Speed", 0f);
            animator.Play(slot == 0 ? "Base Layer.SlashEffect" : "Base Layer.BladeDanceEffect2", 0, 0f);
            AnimationClip clip = slot == 0 ? slashClip : bladeDanceClip;
            // Also release movement if an imported clip loses its SkillOff event.
            finishTimer = StartCoroutine(FinishAfter(clip.length + 0.25f));
        }

        public void ReceiveAnimEvent(AnimationEvent animationEvent)
        {
            if (currentSlot < 0 || animationEvent.animatorClipInfo.clip != (currentSlot == 0 ? slashClip : bladeDanceClip))
                return;

            if (animationEvent.stringParameter == "SkillOn" && !effectPlayed)
            {
                effectPlayed = true;
                GameObject prefab = currentSlot == 0 ? slashEffect : bladeDanceEffect;
                GameObject effect = Instantiate(prefab, castPosition + castRotation * prefab.transform.localPosition,
                    castRotation * prefab.transform.localRotation);
                effect.GetComponent<ParticleSystem>().Play(true);
                Destroy(effect, 6f);
            }
            else if (animationEvent.stringParameter == "SkillOff")
            {
                FinishSkill();
            }
        }

        private IEnumerator FinishAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            finishTimer = null;
            FinishSkill();
        }

        private void FinishSkill()
        {
            if (fsm.CurrentState?.Type != FSM.eStateType.Skill)
                return;

            if (photonView.IsMine)
                fsm.ChangeState(FSM.eStateType.Idle);
            else
                fsm.Stop();
        }

        public void EndSkill()
        {
            if (finishTimer != null)
                StopCoroutine(finishTimer);
            finishTimer = null;
            currentSlot = -1;
            animator.Play("Base Layer.BaseBlend", 0, 0f);
        }
    }
}
