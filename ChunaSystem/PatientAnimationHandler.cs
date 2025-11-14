using UnityEngine;
using CHUNA.Patient;

namespace CHUNA.Patient
{
    /// <summary>
    /// 환자 모델의 애니메이션 처리 클래스
    /// 초기 포즈 저장, 진행에 따른 포즈 업데이트, 시각적 피드백 담당
    /// </summary>
    public class PatientAnimationHandler
    {
        private readonly PatientJointMapper jointMapper;
        private readonly AnimationCurve progressCurve;
        private readonly bool useAnimationCurve;

        // 애니메이션 설정
        private float maxRotation;
        private float maxDisplacement;
        private float maxArmRaise;
        private float maxElbowBend;
        private bool enableShoulderRotation;
        private bool enableElbowBend;
        private bool moveBothArms;

        // 초기 포즈 저장
        private Vector3 initialSpinePosition;
        private Quaternion initialSpineRotation;
        private Vector3 initialChestPosition;
        private Quaternion initialChestRotation;
        private Vector3 initialUpperChestPosition;
        private Quaternion initialUpperChestRotation;
        private Vector3 initialNeckPosition;
        private Quaternion initialNeckRotation;
        private Vector3 initialHeadPosition;
        private Quaternion initialHeadRotation;

        // 왼팔 초기 포즈
        private Vector3 initialLeftShoulderPosition;
        private Quaternion initialLeftShoulderRotation;
        private Vector3 initialLeftUpperArmPosition;
        private Quaternion initialLeftUpperArmRotation;
        private Vector3 initialLeftLowerArmPosition;
        private Quaternion initialLeftLowerArmRotation;
        private Vector3 initialLeftHandPosition;
        private Quaternion initialLeftHandRotation;

        // 오른팔 초기 포즈
        private Vector3 initialRightShoulderPosition;
        private Quaternion initialRightShoulderRotation;
        private Vector3 initialRightUpperArmPosition;
        private Quaternion initialRightUpperArmRotation;
        private Vector3 initialRightLowerArmPosition;
        private Quaternion initialRightLowerArmRotation;
        private Vector3 initialRightHandPosition;
        private Quaternion initialRightHandRotation;

        public PatientAnimationHandler(
            PatientJointMapper jointMapper,
            AnimationCurve progressCurve,
            bool useAnimationCurve = true,
            float maxRotation = 30f,
            float maxDisplacement = 0.1f,
            float maxArmRaise = 60f,
            float maxElbowBend = 90f,
            bool enableShoulderRotation = true,
            bool enableElbowBend = true,
            bool moveBothArms = true)
        {
            this.jointMapper = jointMapper;
            this.progressCurve = progressCurve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
            this.useAnimationCurve = useAnimationCurve;
            this.maxRotation = maxRotation;
            this.maxDisplacement = maxDisplacement;
            this.maxArmRaise = maxArmRaise;
            this.maxElbowBend = maxElbowBend;
            this.enableShoulderRotation = enableShoulderRotation;
            this.enableElbowBend = enableElbowBend;
            this.moveBothArms = moveBothArms;
        }

        /// <summary>
        /// 초기 포즈 저장
        /// </summary>
        public void SaveInitialPose()
        {
            if (jointMapper.Spine != null)
            {
                initialSpinePosition = jointMapper.Spine.localPosition;
                initialSpineRotation = jointMapper.Spine.localRotation;
            }

            if (jointMapper.Chest != null)
            {
                initialChestPosition = jointMapper.Chest.localPosition;
                initialChestRotation = jointMapper.Chest.localRotation;
            }

            if (jointMapper.UpperChest != null)
            {
                initialUpperChestPosition = jointMapper.UpperChest.localPosition;
                initialUpperChestRotation = jointMapper.UpperChest.localRotation;
            }

            if (jointMapper.Neck != null)
            {
                initialNeckPosition = jointMapper.Neck.localPosition;
                initialNeckRotation = jointMapper.Neck.localRotation;
            }

            if (jointMapper.Head != null)
            {
                initialHeadPosition = jointMapper.Head.localPosition;
                initialHeadRotation = jointMapper.Head.localRotation;
            }

            // 왼팔 초기 포즈
            if (jointMapper.LeftShoulder != null)
            {
                initialLeftShoulderPosition = jointMapper.LeftShoulder.localPosition;
                initialLeftShoulderRotation = jointMapper.LeftShoulder.localRotation;
            }

            if (jointMapper.LeftUpperArm != null)
            {
                initialLeftUpperArmPosition = jointMapper.LeftUpperArm.localPosition;
                initialLeftUpperArmRotation = jointMapper.LeftUpperArm.localRotation;
            }

            if (jointMapper.LeftLowerArm != null)
            {
                initialLeftLowerArmPosition = jointMapper.LeftLowerArm.localPosition;
                initialLeftLowerArmRotation = jointMapper.LeftLowerArm.localRotation;
            }

            if (jointMapper.LeftHand != null)
            {
                initialLeftHandPosition = jointMapper.LeftHand.localPosition;
                initialLeftHandRotation = jointMapper.LeftHand.localRotation;
            }

            // 오른팔 초기 포즈
            if (jointMapper.RightShoulder != null)
            {
                initialRightShoulderPosition = jointMapper.RightShoulder.localPosition;
                initialRightShoulderRotation = jointMapper.RightShoulder.localRotation;
            }

            if (jointMapper.RightUpperArm != null)
            {
                initialRightUpperArmPosition = jointMapper.RightUpperArm.localPosition;
                initialRightUpperArmRotation = jointMapper.RightUpperArm.localRotation;
            }

            if (jointMapper.RightLowerArm != null)
            {
                initialRightLowerArmPosition = jointMapper.RightLowerArm.localPosition;
                initialRightLowerArmRotation = jointMapper.RightLowerArm.localRotation;
            }

            if (jointMapper.RightHand != null)
            {
                initialRightHandPosition = jointMapper.RightHand.localPosition;
                initialRightHandRotation = jointMapper.RightHand.localRotation;
            }

            Debug.Log("<color=cyan>[PatientAnimationHandler] 초기 포즈 저장 완료</color>");
        }

        /// <summary>
        /// 진행도에 따라 환자 포즈 업데이트
        /// </summary>
        public void UpdatePatientPose(float currentProgress)
        {
            float curvedProgress = useAnimationCurve ?
                progressCurve.Evaluate(currentProgress) : currentProgress;

            // Spine 회전
            if (jointMapper.Spine != null)
            {
                Quaternion targetRotation = initialSpineRotation *
                    Quaternion.Euler(maxRotation * curvedProgress, 0, 0);
                jointMapper.Spine.localRotation = Quaternion.Lerp(
                    jointMapper.Spine.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // Chest 회전
            if (jointMapper.Chest != null)
            {
                Quaternion targetRotation = Quaternion.Euler(maxRotation * curvedProgress * 0.7f, 0, 0);
                jointMapper.Chest.localRotation = Quaternion.Lerp(
                    jointMapper.Chest.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // UpperChest 회전
            if (jointMapper.UpperChest != null)
            {
                Quaternion targetRotation = Quaternion.Euler(maxRotation * curvedProgress * 0.5f, 0, 0);
                jointMapper.UpperChest.localRotation = Quaternion.Lerp(
                    jointMapper.UpperChest.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // Neck 회전
            if (jointMapper.Neck != null)
            {
                Quaternion targetRotation = initialNeckRotation *
                    Quaternion.Euler(maxRotation * curvedProgress * 0.5f, 0, 0);
                jointMapper.Neck.localRotation = Quaternion.Lerp(
                    jointMapper.Neck.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // Head 이동
            if (jointMapper.Head != null)
            {
                Vector3 targetPosition = initialHeadPosition +
                    Vector3.up * maxDisplacement * curvedProgress;
                jointMapper.Head.localPosition = Vector3.Lerp(
                    jointMapper.Head.localPosition, targetPosition, Time.deltaTime * 5f);
            }

            // 왼팔 움직임
            UpdateLeftArm(curvedProgress);

            // 오른팔 움직임
            if (moveBothArms)
            {
                UpdateRightArm(curvedProgress);
            }
        }

        /// <summary>
        /// 왼팔 애니메이션 업데이트
        /// </summary>
        private void UpdateLeftArm(float curvedProgress)
        {
            // 왼쪽 어깨
            if (jointMapper.LeftShoulder != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialLeftShoulderRotation *
                    Quaternion.Euler(0, 0, maxArmRaise * curvedProgress * 0.3f);
                jointMapper.LeftShoulder.localRotation = Quaternion.Lerp(
                    jointMapper.LeftShoulder.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 왼쪽 상완
            if (jointMapper.LeftUpperArm != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialLeftUpperArmRotation *
                    Quaternion.Euler(-maxArmRaise * curvedProgress, 0, 0);
                jointMapper.LeftUpperArm.localRotation = Quaternion.Lerp(
                    jointMapper.LeftUpperArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 왼쪽 전완 (팔꿈치)
            if (jointMapper.LeftLowerArm != null && enableElbowBend)
            {
                Quaternion targetRotation = initialLeftLowerArmRotation *
                    Quaternion.Euler(0, 0, -maxElbowBend * curvedProgress);
                jointMapper.LeftLowerArm.localRotation = Quaternion.Lerp(
                    jointMapper.LeftLowerArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 왼손
            if (jointMapper.LeftHand != null)
            {
                Quaternion targetRotation = initialLeftHandRotation *
                    Quaternion.Euler(0, 0, -15f * curvedProgress);
                jointMapper.LeftHand.localRotation = Quaternion.Lerp(
                    jointMapper.LeftHand.localRotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        /// <summary>
        /// 오른팔 애니메이션 업데이트
        /// </summary>
        private void UpdateRightArm(float curvedProgress)
        {
            // 오른쪽 어깨
            if (jointMapper.RightShoulder != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialRightShoulderRotation *
                    Quaternion.Euler(0, 0, -maxArmRaise * curvedProgress * 0.3f);
                jointMapper.RightShoulder.localRotation = Quaternion.Lerp(
                    jointMapper.RightShoulder.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 오른쪽 상완
            if (jointMapper.RightUpperArm != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialRightUpperArmRotation *
                    Quaternion.Euler(-maxArmRaise * curvedProgress, 0, 0);
                jointMapper.RightUpperArm.localRotation = Quaternion.Lerp(
                    jointMapper.RightUpperArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 오른쪽 전완
            if (jointMapper.RightLowerArm != null && enableElbowBend)
            {
                Quaternion targetRotation = initialRightLowerArmRotation *
                    Quaternion.Euler(0, 0, maxElbowBend * curvedProgress);
                jointMapper.RightLowerArm.localRotation = Quaternion.Lerp(
                    jointMapper.RightLowerArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 오른손
            if (jointMapper.RightHand != null)
            {
                Quaternion targetRotation = initialRightHandRotation *
                    Quaternion.Euler(0, 0, 15f * curvedProgress);
                jointMapper.RightHand.localRotation = Quaternion.Lerp(
                    jointMapper.RightHand.localRotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        /// <summary>
        /// 포즈를 초기 상태로 리셋
        /// </summary>
        public void ResetToInitialPose()
        {
            if (jointMapper.Spine != null)
            {
                jointMapper.Spine.localPosition = initialSpinePosition;
                jointMapper.Spine.localRotation = initialSpineRotation;
            }

            if (jointMapper.Chest != null)
            {
                jointMapper.Chest.localPosition = initialChestPosition;
                jointMapper.Chest.localRotation = initialChestRotation;
            }

            if (jointMapper.UpperChest != null)
            {
                jointMapper.UpperChest.localPosition = initialUpperChestPosition;
                jointMapper.UpperChest.localRotation = initialUpperChestRotation;
            }

            if (jointMapper.Neck != null)
            {
                jointMapper.Neck.localPosition = initialNeckPosition;
                jointMapper.Neck.localRotation = initialNeckRotation;
            }

            if (jointMapper.Head != null)
            {
                jointMapper.Head.localPosition = initialHeadPosition;
                jointMapper.Head.localRotation = initialHeadRotation;
            }

            // 왼팔 리셋
            ResetLeftArm();

            // 오른팔 리셋
            ResetRightArm();

            Debug.Log("<color=yellow>[PatientAnimationHandler] 포즈 초기화 완료</color>");
        }

        private void ResetLeftArm()
        {
            if (jointMapper.LeftShoulder != null)
            {
                jointMapper.LeftShoulder.localPosition = initialLeftShoulderPosition;
                jointMapper.LeftShoulder.localRotation = initialLeftShoulderRotation;
            }

            if (jointMapper.LeftUpperArm != null)
            {
                jointMapper.LeftUpperArm.localPosition = initialLeftUpperArmPosition;
                jointMapper.LeftUpperArm.localRotation = initialLeftUpperArmRotation;
            }

            if (jointMapper.LeftLowerArm != null)
            {
                jointMapper.LeftLowerArm.localPosition = initialLeftLowerArmPosition;
                jointMapper.LeftLowerArm.localRotation = initialLeftLowerArmRotation;
            }

            if (jointMapper.LeftHand != null)
            {
                jointMapper.LeftHand.localPosition = initialLeftHandPosition;
                jointMapper.LeftHand.localRotation = initialLeftHandRotation;
            }
        }

        private void ResetRightArm()
        {
            if (jointMapper.RightShoulder != null)
            {
                jointMapper.RightShoulder.localPosition = initialRightShoulderPosition;
                jointMapper.RightShoulder.localRotation = initialRightShoulderRotation;
            }

            if (jointMapper.RightUpperArm != null)
            {
                jointMapper.RightUpperArm.localPosition = initialRightUpperArmPosition;
                jointMapper.RightUpperArm.localRotation = initialRightUpperArmRotation;
            }

            if (jointMapper.RightLowerArm != null)
            {
                jointMapper.RightLowerArm.localPosition = initialRightLowerArmPosition;
                jointMapper.RightLowerArm.localRotation = initialRightLowerArmRotation;
            }

            if (jointMapper.RightHand != null)
            {
                jointMapper.RightHand.localPosition = initialRightHandPosition;
                jointMapper.RightHand.localRotation = initialRightHandRotation;
            }
        }

        /// <summary>
        /// 설정 업데이트
        /// </summary>
        public void UpdateSettings(
            float? maxRotation = null,
            float? maxDisplacement = null,
            float? maxArmRaise = null,
            float? maxElbowBend = null,
            bool? enableShoulderRotation = null,
            bool? enableElbowBend = null,
            bool? moveBothArms = null)
        {
            if (maxRotation.HasValue) this.maxRotation = maxRotation.Value;
            if (maxDisplacement.HasValue) this.maxDisplacement = maxDisplacement.Value;
            if (maxArmRaise.HasValue) this.maxArmRaise = maxArmRaise.Value;
            if (maxElbowBend.HasValue) this.maxElbowBend = maxElbowBend.Value;
            if (enableShoulderRotation.HasValue) this.enableShoulderRotation = enableShoulderRotation.Value;
            if (enableElbowBend.HasValue) this.enableElbowBend = enableElbowBend.Value;
            if (moveBothArms.HasValue) this.moveBothArms = moveBothArms.Value;
        }
    }
}
