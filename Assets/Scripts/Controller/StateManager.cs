using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SA
{
    public class StateManager : MonoBehaviour
    {
        [Header("Init")]
        public GameObject activeModel;
        [Header("Inputs")]
        public float vertical;
        public float horizontal;
        public float moveAmount;
        public Vector3 moveDir;
        public bool rt, lt, rb, lb;
        public bool rollInput;

        [Header("Stats")]
        public float moveSpeed = 2;
        public float runSpeed = 3.5f;
        public float rotateSpeed = 5;
        public float toGround = 0.5f;
        public float rollSpeed = 1;

        [Header("States")]
        public bool onGround;
        public bool run;
        public bool lockOn;
        public bool inAction;
        public bool canMove;
        public bool isTwoHanded;

        [Header("Other")]
        public EnemyTarget lockonTarget;
        public AnimationCurve roll_curve;


        [HideInInspector]
        public Animator anim;
        [HideInInspector]
        public Rigidbody rigid;
        [HideInInspector]
        public AnimatorHook a_hook;
        [HideInInspector]
        public float delta;
        [HideInInspector]
        public LayerMask ignoredLayer;
        float _actionDelay;

        public void Init()
        {
            SetupAnimator();
            rigid = GetComponent<Rigidbody>();
            rigid.angularDrag = 999;
            rigid.drag = 4;
            rigid.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            a_hook = activeModel.AddComponent<AnimatorHook>();
            a_hook.Init(this);

            gameObject.layer = 8; //Controller layer
            ignoredLayer = ~(1 << 9); //DamageColliders Layer

            anim.SetBool("onGround", true);
        }

        void SetupAnimator()
        {
            if (activeModel == null)
            {
                anim = GetComponentInChildren<Animator>();
                if (anim == null)
                {
                    Debug.Log("no model found");
                }
                else
                {
                    activeModel = anim.gameObject;
                }
            }
            if (anim == null)
            {
                anim = activeModel.GetComponent<Animator>();
            }
            anim.applyRootMotion = false;
        }

        public void FixedTick(float d)
        {
            delta = d;
            DetectAction();
            if (inAction)
            {
                anim.applyRootMotion = true;
                _actionDelay += delta;
                if (_actionDelay > 0.3f)
                {
                    inAction = false;
                    _actionDelay = 0;
                }
                else
                {
                    return;
                }
            }
            canMove = anim.GetBool("canMove");
            if (!canMove)
            {
                return;
            }

            a_hook.CloseRolling();
            HandleRolls();

            anim.applyRootMotion = false;
            //if in ACTION, no need for moving
            rigid.drag = (moveAmount > 0 || !onGround) ? 0 : 4;
            float targetSpeed = moveSpeed;
            if (run)
            {
                targetSpeed = runSpeed;
            }
            if (onGround)
                rigid.velocity = moveDir * (targetSpeed * moveAmount);

            if (run)
            {
                lockOn = false;
            }

            Vector3 targetDir = (lockOn) ? lockonTarget.transform.position - transform.position : moveDir;
            targetDir.y = 0;
            if (targetDir == Vector3.zero)
            {
                targetDir = transform.forward;
            }
            Quaternion tr = Quaternion.LookRotation(targetDir);
            Quaternion targetRotation = Quaternion.Slerp(transform.rotation, tr, delta * moveAmount * rotateSpeed);
            transform.rotation = targetRotation;

            anim.SetBool("lockon", lockOn);
            if (!lockOn)
            {
                HandleMovementAnimations();
            }
            else
            {
                HandleLockOnAnimations(moveDir);
            }
        }

        public void DetectAction()
        {
            if (!canMove)
                return;
            if (rb == false && rt == false && lt == false && lb == false)
            {
                return;
            }
            string targetAnimation = null;
            if (rb)
                targetAnimation = "oh_attack_1";
            if (rt)
                targetAnimation = "oh_attack_2";
            if (lb)
                targetAnimation = "th_attack_1";
            if (lt)
                targetAnimation = "oh_attack_3";
            if (string.IsNullOrEmpty(targetAnimation))
                return;
            canMove = false;
            inAction = true;
            anim.CrossFade(targetAnimation, 0.2f);
            //rigid.velocity = Vector3.zero;
        }

        public void Tick(float d)
        {
            delta = d;
            onGround = OnGround();
            anim.SetBool("onGround", onGround);
        }

        void HandleRolls()
        {
            if (!rollInput)
            {
                return;
            }
            float v = vertical;
            float h = horizontal;
            if (!lockOn)
            {
                v = (moveAmount > 0.3f) ? 1 : 0;
                h = 0;
            }
            else
            {
                if (Mathf.Abs(v) < 0.3f)
                {
                    v = 0;
                }
                if (Mathf.Abs(h) < 0.3f)
                {
                    h = 0;
                }
            }

            // if (v != 0)
            // {
            //     if (moveDir == Vector3.zero)
            //         moveDir = transform.forward;
            //     Quaternion targetRot = Quaternion.LookRotation(moveDir);
            //     transform.rotation = targetRot;
            //     a_hook.InitRolling();
            //     a_hook.rm_multiplier = rollSpeed;
            // }
            // else
            // {
            //     a_hook.rm_multiplier = 1.3f;
            // }


            anim.SetFloat("vertical", v);
            anim.SetFloat("horizontal", h);
            canMove = false;
            inAction = true;
            anim.CrossFade("Rolls", 0.2f);

        }

        void OnDrawGizmosSelected()
        {
            // Draws a blue line from this transform to the target
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, moveDir);
        }

        void HandleMovementAnimations()
        {
            anim.SetBool("run", run);
            anim.SetFloat("vertical", moveAmount, 0.4f, delta);
        }

        void HandleLockOnAnimations(Vector3 moveDir)
        {
            Vector3 relativeDir = transform.InverseTransformDirection(moveDir);
            float h = relativeDir.x;
            float v = relativeDir.z;
            anim.SetFloat("vertical", v, 0.2f, delta);
            anim.SetFloat("horizontal", h, 0.2f, delta);
        }

        public bool OnGround()
        {
            bool r = false;
            Vector3 origin = transform.position + Vector3.up * toGround;
            Vector3 dir = Vector3.down;
            float dist = toGround + 0.3f;
            RaycastHit hit;
            if (Physics.Raycast(origin, dir, out hit, dist, ignoredLayer))
            {
                r = true;
                Vector3 targetPosition = hit.point;
                transform.position = targetPosition;
            }
            return r;
        }

        public void HandleTwoHanded()
        {
            anim.SetBool("twoHanded", isTwoHanded);
        }
    }
}