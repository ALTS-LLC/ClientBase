using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneTracer : MonoBehaviour
{
    public Animator TargetAnimator = null;
    private Animator _thisAnimator = null;

    private void Start()
    {
        _thisAnimator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        foreach (HumanBodyBones item in Enum.GetValues(typeof(HumanBodyBones)))
        {
            try
            {
                _thisAnimator.GetBoneTransform(item).eulerAngles = TargetAnimator.GetBoneTransform(item).eulerAngles;
            }
            catch (Exception)
            {
                
            }


            if (item == HumanBodyBones.Hips)
            {
                _thisAnimator.GetBoneTransform(item).localPosition = TargetAnimator.GetBoneTransform(item).localPosition;
            }

            if (item == HumanBodyBones.LeftUpperArm || item == HumanBodyBones.LeftLowerArm || item == HumanBodyBones.LeftHand ||
                item == HumanBodyBones.LeftThumbProximal || item == HumanBodyBones.LeftThumbIntermediate || item == HumanBodyBones.LeftThumbDistal ||
                item == HumanBodyBones.LeftIndexProximal || item == HumanBodyBones.LeftIndexIntermediate || item == HumanBodyBones.LeftIndexDistal ||
                item == HumanBodyBones.LeftMiddleProximal || item == HumanBodyBones.LeftMiddleIntermediate || item == HumanBodyBones.LeftMiddleDistal ||
                item == HumanBodyBones.LeftRingProximal || item == HumanBodyBones.LeftRingIntermediate || item == HumanBodyBones.LeftRingDistal ||
                item == HumanBodyBones.LeftLittleProximal || item == HumanBodyBones.LeftLittleIntermediate || item == HumanBodyBones.LeftLittleDistal)
            {
                Quaternion fixQ = new Quaternion()
                {
                    x = -TargetAnimator.GetBoneTransform(item).localRotation.y,
                    y = TargetAnimator.GetBoneTransform(item).localRotation.x,
                    z = TargetAnimator.GetBoneTransform(item).localRotation.z,
                    w = TargetAnimator.GetBoneTransform(item).localRotation.w,
                };
                _thisAnimator.GetBoneTransform(item).localRotation = fixQ;
            }
            if (item == HumanBodyBones.RightUpperArm || item == HumanBodyBones.RightLowerArm || item == HumanBodyBones.RightHand ||
                item == HumanBodyBones.RightThumbProximal || item == HumanBodyBones.RightThumbIntermediate || item == HumanBodyBones.RightThumbDistal ||
                item == HumanBodyBones.RightIndexProximal || item == HumanBodyBones.RightIndexIntermediate || item == HumanBodyBones.RightIndexDistal ||
                item == HumanBodyBones.RightMiddleProximal || item == HumanBodyBones.RightMiddleIntermediate || item == HumanBodyBones.RightMiddleDistal ||
                item == HumanBodyBones.RightRingProximal || item == HumanBodyBones.RightRingIntermediate || item == HumanBodyBones.RightRingDistal ||
                item == HumanBodyBones.RightLittleProximal || item == HumanBodyBones.RightLittleIntermediate || item == HumanBodyBones.RightLittleDistal)
            {
                Quaternion fixQ = new Quaternion()
                {
                    x = TargetAnimator.GetBoneTransform(item).localRotation.y,
                    y = -TargetAnimator.GetBoneTransform(item).localRotation.x,
                    z = TargetAnimator.GetBoneTransform(item).localRotation.z,
                    w = TargetAnimator.GetBoneTransform(item).localRotation.w,
                };
                _thisAnimator.GetBoneTransform(item).localRotation = fixQ;
            }

            if (item == HumanBodyBones.LeftShoulder)
            {
                _thisAnimator.GetBoneTransform(item).Rotate(0, 0, -90);
            }
            if (item == HumanBodyBones.RightShoulder)
            {
                _thisAnimator.GetBoneTransform(item).Rotate(0, 0, 90);
            }

            if (item == HumanBodyBones.LeftThumbProximal)
            {
                _thisAnimator.GetBoneTransform(item).Rotate(13.069f, -30.654f, -7.681f);
            }
            if (item == HumanBodyBones.RightThumbProximal)
            {
                _thisAnimator.GetBoneTransform(item).Rotate(13.183f, 30.898f, 7.48f);
            }
        }
    }
}
