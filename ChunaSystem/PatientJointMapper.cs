using UnityEngine;

namespace CHUNA.Patient
{
    /// <summary>
    /// 환자 모델의 조인트를 자동으로 매핑하는 헬퍼 클래스
    /// Animator 또는 이름 기반으로 조인트를 찾아 매핑
    /// </summary>
    public class PatientJointMapper
    {
        private readonly GameObject patientModel;
        private readonly Animator patientAnimator;
        private readonly bool useAnimator;
        private readonly bool useFallbackNaming;

        // 매핑된 조인트들
        public Transform Spine { get; private set; }
        public Transform Neck { get; private set; }
        public Transform Head { get; private set; }
        public Transform Chest { get; private set; }
        public Transform UpperChest { get; private set; }
        public Transform LeftShoulder { get; private set; }
        public Transform LeftUpperArm { get; private set; }
        public Transform LeftLowerArm { get; private set; }
        public Transform LeftHand { get; private set; }
        public Transform RightShoulder { get; private set; }
        public Transform RightUpperArm { get; private set; }
        public Transform RightLowerArm { get; private set; }
        public Transform RightHand { get; private set; }

        public PatientJointMapper(GameObject model, Animator animator, bool useAnimator = true, bool useFallbackNaming = true)
        {
            this.patientModel = model;
            this.patientAnimator = animator;
            this.useAnimator = useAnimator;
            this.useFallbackNaming = useFallbackNaming;
        }

        /// <summary>
        /// 자동 조인트 매핑 수행
        /// </summary>
        public bool AutoMapJoints()
        {
            Debug.Log("<color=cyan>[PatientJointMapper] 자동 조인트 매핑 시작...</color>");

            bool success = false;

            // 1. Animator를 통한 매핑 시도
            if (useAnimator && patientAnimator != null)
            {
                success = MapJointsFromAnimator();
                if (success)
                {
                    Debug.Log("<color=green>[PatientJointMapper] Animator 매핑 성공!</color>");
                    ValidateMapping();
                    return true;
                }
            }

            // 2. 이름 기반 매핑 시도
            if (useFallbackNaming)
            {
                success = MapJointsByName();
                if (success)
                {
                    Debug.Log("<color=green>[PatientJointMapper] 이름 기반 매핑 성공!</color>");
                    ValidateMapping();
                    return true;
                }
            }

            Debug.LogWarning("[PatientJointMapper] 자동 매핑 실패. 수동으로 조인트를 할당해주세요.");
            return false;
        }

        /// <summary>
        /// Animator의 휴먼 본 구조를 이용한 매핑
        /// </summary>
        private bool MapJointsFromAnimator()
        {
            if (patientAnimator == null || !patientAnimator.isHuman)
            {
                Debug.LogWarning("[PatientJointMapper] Animator가 없거나 Humanoid가 아닙니다.");
                return false;
            }

            int mappedCount = 0;

            // 상체 조인트 매핑
            Spine = patientAnimator.GetBoneTransform(HumanBodyBones.Spine);
            if (Spine != null) mappedCount++;

            Chest = patientAnimator.GetBoneTransform(HumanBodyBones.Chest);
            if (Chest != null) mappedCount++;

            UpperChest = patientAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (UpperChest != null) mappedCount++;

            Neck = patientAnimator.GetBoneTransform(HumanBodyBones.Neck);
            if (Neck != null) mappedCount++;

            Head = patientAnimator.GetBoneTransform(HumanBodyBones.Head);
            if (Head != null) mappedCount++;

            // 왼팔 조인트
            LeftShoulder = patientAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            if (LeftShoulder != null) mappedCount++;

            LeftUpperArm = patientAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (LeftUpperArm != null) mappedCount++;

            LeftLowerArm = patientAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            if (LeftLowerArm != null) mappedCount++;

            LeftHand = patientAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (LeftHand != null) mappedCount++;

            // 오른팔 조인트
            RightShoulder = patientAnimator.GetBoneTransform(HumanBodyBones.RightShoulder);
            if (RightShoulder != null) mappedCount++;

            RightUpperArm = patientAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (RightUpperArm != null) mappedCount++;

            RightLowerArm = patientAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (RightLowerArm != null) mappedCount++;

            RightHand = patientAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            if (RightHand != null) mappedCount++;

            Debug.Log($"[PatientJointMapper] Animator에서 {mappedCount}/13개 조인트 매핑됨");

            return mappedCount >= 5; // 최소 5개 이상 매핑되어야 성공
        }

        /// <summary>
        /// 이름 기반 조인트 매핑 (Fallback)
        /// </summary>
        private bool MapJointsByName()
        {
            if (patientModel == null)
            {
                Debug.LogWarning("[PatientJointMapper] 환자 모델이 없습니다.");
                return false;
            }

            Transform[] allTransforms = patientModel.GetComponentsInChildren<Transform>();
            int mappedCount = 0;

            foreach (Transform t in allTransforms)
            {
                string name = t.name.ToLower();

                // Spine
                if ((name.Contains("spine") || name.Contains("척추")) && !name.Contains("chest"))
                {
                    if (Spine == null)
                    {
                        Spine = t;
                        mappedCount++;
                        Debug.Log($"  - Spine 매핑: {t.name}");
                    }
                }

                // Chest
                if ((name.Contains("chest") || name.Contains("가슴")) && !name.Contains("upper"))
                {
                    if (Chest == null)
                    {
                        Chest = t;
                        mappedCount++;
                        Debug.Log($"  - Chest 매핑: {t.name}");
                    }
                }

                // UpperChest
                if (name.Contains("upperchest") || name.Contains("upper chest") || name.Contains("상부가슴"))
                {
                    UpperChest = t;
                    mappedCount++;
                    Debug.Log($"  - UpperChest 매핑: {t.name}");
                }

                // Neck
                if (name.Contains("neck") || name.Contains("목"))
                {
                    Neck = t;
                    mappedCount++;
                    Debug.Log($"  - Neck 매핑: {t.name}");
                }

                // Head
                if (name.Contains("head") || name.Contains("머리"))
                {
                    Head = t;
                    mappedCount++;
                    Debug.Log($"  - Head 매핑: {t.name}");
                }

                // 왼쪽 어깨
                if ((name.Contains("left") || name.Contains("l_") || name.Contains("왼")) &&
                    (name.Contains("shoulder") || name.Contains("어깨")))
                {
                    LeftShoulder = t;
                    mappedCount++;
                    Debug.Log($"  - LeftShoulder 매핑: {t.name}");
                }

                // 왼쪽 상완
                if ((name.Contains("left") || name.Contains("l_") || name.Contains("왼")) &&
                    (name.Contains("upperarm") || name.Contains("upper arm") || name.Contains("상완")))
                {
                    LeftUpperArm = t;
                    mappedCount++;
                    Debug.Log($"  - LeftUpperArm 매핑: {t.name}");
                }

                // 왼쪽 하완 (팔꿈치)
                if ((name.Contains("left") || name.Contains("l_") || name.Contains("왼")) &&
                    (name.Contains("lowerarm") || name.Contains("lower arm") || name.Contains("forearm") || name.Contains("하완")))
                {
                    LeftLowerArm = t;
                    mappedCount++;
                    Debug.Log($"  - LeftLowerArm 매핑: {t.name}");
                }

                // 왼손
                if ((name.Contains("left") || name.Contains("l_") || name.Contains("왼")) &&
                    (name.Contains("hand") || name.Contains("손")))
                {
                    LeftHand = t;
                    mappedCount++;
                    Debug.Log($"  - LeftHand 매핑: {t.name}");
                }

                // 오른쪽 어깨
                if ((name.Contains("right") || name.Contains("r_") || name.Contains("오른")) &&
                    (name.Contains("shoulder") || name.Contains("어깨")))
                {
                    RightShoulder = t;
                    mappedCount++;
                    Debug.Log($"  - RightShoulder 매핑: {t.name}");
                }

                // 오른쪽 상완
                if ((name.Contains("right") || name.Contains("r_") || name.Contains("오른")) &&
                    (name.Contains("upperarm") || name.Contains("upper arm") || name.Contains("상완")))
                {
                    RightUpperArm = t;
                    mappedCount++;
                    Debug.Log($"  - RightUpperArm 매핑: {t.name}");
                }

                // 오른쪽 하완
                if ((name.Contains("right") || name.Contains("r_") || name.Contains("오른")) &&
                    (name.Contains("lowerarm") || name.Contains("lower arm") || name.Contains("forearm") || name.Contains("하완")))
                {
                    RightLowerArm = t;
                    mappedCount++;
                    Debug.Log($"  - RightLowerArm 매핑: {t.name}");
                }

                // 오른손
                if ((name.Contains("right") || name.Contains("r_") || name.Contains("오른")) &&
                    (name.Contains("hand") || name.Contains("손")))
                {
                    RightHand = t;
                    mappedCount++;
                    Debug.Log($"  - RightHand 매핑: {t.name}");
                }
            }

            Debug.Log($"[PatientJointMapper] 이름 기반 {mappedCount}/13개 조인트 매핑됨");

            return mappedCount >= 5;
        }

        /// <summary>
        /// 매핑 검증 및 경고 출력
        /// </summary>
        public void ValidateMapping()
        {
            Debug.Log("<color=yellow>===== 조인트 매핑 검증 =====</color>");

            int validCount = 0;
            int totalCount = 13;

            // 각 조인트 검증
            ValidateJoint("Spine", Spine, ref validCount);
            ValidateJoint("Chest", Chest, ref validCount);
            ValidateJoint("UpperChest", UpperChest, ref validCount);
            ValidateJoint("Neck", Neck, ref validCount);
            ValidateJoint("Head", Head, ref validCount);
            ValidateJoint("LeftShoulder", LeftShoulder, ref validCount);
            ValidateJoint("LeftUpperArm", LeftUpperArm, ref validCount);
            ValidateJoint("LeftLowerArm", LeftLowerArm, ref validCount);
            ValidateJoint("LeftHand", LeftHand, ref validCount);
            ValidateJoint("RightShoulder", RightShoulder, ref validCount);
            ValidateJoint("RightUpperArm", RightUpperArm, ref validCount);
            ValidateJoint("RightLowerArm", RightLowerArm, ref validCount);
            ValidateJoint("RightHand", RightHand, ref validCount);

            float percentage = (float)validCount / totalCount * 100f;
            string resultColor = percentage >= 80f ? "green" : percentage >= 50f ? "yellow" : "red";

            Debug.Log($"<color={resultColor}>[PatientJointMapper] 매핑 완료: {validCount}/{totalCount} ({percentage:F1}%)</color>");

            if (validCount < 5)
            {
                Debug.LogWarning("[PatientJointMapper] 필수 조인트가 부족합니다. 수동으로 할당해주세요.");
            }
        }

        private void ValidateJoint(string jointName, Transform joint, ref int validCount)
        {
            if (joint != null)
            {
                Debug.Log($"  ✓ {jointName}: {joint.name}");
                validCount++;
            }
            else
            {
                Debug.LogWarning($"  ✗ {jointName}: <color=red>매핑 안 됨</color>");
            }
        }

        /// <summary>
        /// 수동으로 조인트 설정
        /// </summary>
        public void SetJoints(
            Transform spine = null,
            Transform neck = null,
            Transform head = null,
            Transform chest = null,
            Transform upperChest = null,
            Transform leftShoulder = null,
            Transform leftUpperArm = null,
            Transform leftLowerArm = null,
            Transform leftHand = null,
            Transform rightShoulder = null,
            Transform rightUpperArm = null,
            Transform rightLowerArm = null,
            Transform rightHand = null)
        {
            if (spine != null) Spine = spine;
            if (neck != null) Neck = neck;
            if (head != null) Head = head;
            if (chest != null) Chest = chest;
            if (upperChest != null) UpperChest = upperChest;
            if (leftShoulder != null) LeftShoulder = leftShoulder;
            if (leftUpperArm != null) LeftUpperArm = leftUpperArm;
            if (leftLowerArm != null) LeftLowerArm = leftLowerArm;
            if (leftHand != null) LeftHand = leftHand;
            if (rightShoulder != null) RightShoulder = rightShoulder;
            if (rightUpperArm != null) RightUpperArm = rightUpperArm;
            if (rightLowerArm != null) RightLowerArm = rightLowerArm;
            if (rightHand != null) RightHand = rightHand;

            ValidateMapping();
        }
    }
}
