﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SA
{
    public class Helper : MonoBehaviour
    {
        [Range(-1, 1)]
        public float vertical;
        [Range(-1, 1)]
        public float horizontal;
        public bool playAnim;
        public string[] oh_attacks;
        public string[] th_attacks;
        public bool twoHanded;
        public bool enableRM;
        public bool useItem;
        public bool interacting;
        public bool lockon;
        Animator anim;
        // Use this for initialization
        void Start()
        {
            vertical = 0;
            horizontal = 0;
            anim = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            enableRM = !anim.GetBool("canMove");
            anim.applyRootMotion = enableRM;

            interacting = anim.GetBool("interacting");

            if (!lockon)
            {
            	horizontal = 0;
				vertical = Mathf.Clamp01(vertical);
            }

            if (enableRM)
                return;
            if (useItem)
            {
                anim.Play("use_item");
                useItem = false;
            }
            if (interacting)
            {
                playAnim = false;
                vertical = Mathf.Clamp(vertical, 0, 0.5f);
            }
			anim.SetBool("lockon", lockon);

            anim.SetBool("twoHanded", twoHanded);
            if (playAnim)
            {
                string targetAnim;
                if (twoHanded)
                {
                    int r = Random.Range(0, th_attacks.Length);
                    targetAnim = th_attacks[r];
                }
                else
                {
                    int r = Random.Range(0, oh_attacks.Length);
                    targetAnim = oh_attacks[r];
                    if (vertical > 0.5f)
                    {
                        targetAnim = "oh_attack_3";
                    }
                }

                //vertical = 0;
                anim.CrossFade(targetAnim, 0.2f);
                playAnim = false;
            }
            anim.SetFloat("vertical", vertical);
			anim.SetFloat("horizontal", horizontal);
        }
    }
}

