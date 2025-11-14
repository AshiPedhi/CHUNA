using System;
using System.Collections.Generic;
using UnityEngine;

namespace CHUNA.PoseData
{
    /// <summary>
    /// 포즈 데이터 관련 공통 클래스 정의
    /// HandPosePlayer, HandPoseRecorder에서 공유
    /// </summary>

    /// <summary>
    /// 단일 조인트의 포즈 데이터
    /// </summary>
    [System.Serializable]
    public class PoseData
    {
        public Vector3 position;
        public Quaternion rotation;

        public PoseData()
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        public PoseData(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }

        public PoseData Clone()
        {
            return new PoseData(position, rotation);
        }
    }

    /// <summary>
    /// 단일 프레임의 포즈 데이터 (양손 포함)
    /// </summary>
    [System.Serializable]
    public class PoseFrame
    {
        public int frameIndex;
        public float timestamp;

        // 왼손 조인트 데이터
        public Dictionary<int, PoseData> leftLocalPoses = new Dictionary<int, PoseData>();

        // 오른손 조인트 데이터
        public Dictionary<int, PoseData> rightLocalPoses = new Dictionary<int, PoseData>();

        // 손 전체 위치/회전 (월드 좌표)
        public Vector3 leftHandWorldPosition;
        public Quaternion leftHandWorldRotation;
        public Vector3 rightHandWorldPosition;
        public Quaternion rightHandWorldRotation;

        public PoseFrame()
        {
            frameIndex = 0;
            timestamp = 0f;
            leftHandWorldPosition = Vector3.zero;
            leftHandWorldRotation = Quaternion.identity;
            rightHandWorldPosition = Vector3.zero;
            rightHandWorldRotation = Quaternion.identity;
        }

        public PoseFrame Clone()
        {
            var clone = new PoseFrame
            {
                frameIndex = this.frameIndex,
                timestamp = this.timestamp,
                leftHandWorldPosition = this.leftHandWorldPosition,
                leftHandWorldRotation = this.leftHandWorldRotation,
                rightHandWorldPosition = this.rightHandWorldPosition,
                rightHandWorldRotation = this.rightHandWorldRotation
            };

            // 딕셔너리 복사
            foreach (var kvp in leftLocalPoses)
            {
                clone.leftLocalPoses[kvp.Key] = kvp.Value.Clone();
            }
            foreach (var kvp in rightLocalPoses)
            {
                clone.rightLocalPoses[kvp.Key] = kvp.Value.Clone();
            }

            return clone;
        }

        /// <summary>
        /// 특정 손의 포즈 데이터 가져오기
        /// </summary>
        public Dictionary<int, PoseData> GetHandPoses(HandType handType)
        {
            return handType == HandType.Left ? leftLocalPoses : rightLocalPoses;
        }

        /// <summary>
        /// 특정 손의 월드 위치 가져오기
        /// </summary>
        public Vector3 GetHandWorldPosition(HandType handType)
        {
            return handType == HandType.Left ? leftHandWorldPosition : rightHandWorldPosition;
        }

        /// <summary>
        /// 특정 손의 월드 회전 가져오기
        /// </summary>
        public Quaternion GetHandWorldRotation(HandType handType)
        {
            return handType == HandType.Left ? leftHandWorldRotation : rightHandWorldRotation;
        }
    }

    /// <summary>
    /// 손 타입 열거형
    /// </summary>
    public enum HandType
    {
        Left,
        Right
    }

    /// <summary>
    /// CSV 헤더 정의
    /// </summary>
    public static class CSVHeaders
    {
        public const string FRAME_INDEX = "FrameIndex";
        public const string HAND_TYPE = "HandType";
        public const string JOINT_ID = "JointID";
        public const string LOCAL_POS_X = "LocalPosX";
        public const string LOCAL_POS_Y = "LocalPosY";
        public const string LOCAL_POS_Z = "LocalPosZ";
        public const string LOCAL_ROT_X = "LocalRotX";
        public const string LOCAL_ROT_Y = "LocalRotY";
        public const string LOCAL_ROT_Z = "LocalRotZ";
        public const string LOCAL_ROT_W = "LocalRotW";
        public const string TIMESTAMP = "Timestamp";
        public const string WORLD_POS_X = "WorldPosX";
        public const string WORLD_POS_Y = "WorldPosY";
        public const string WORLD_POS_Z = "WorldPosZ";
        public const string WORLD_ROT_X = "WorldRotX";
        public const string WORLD_ROT_Y = "WorldRotY";
        public const string WORLD_ROT_Z = "WorldRotZ";
        public const string WORLD_ROT_W = "WorldRotW";

        public static string GetHeaderLine()
        {
            return $"{FRAME_INDEX},{HAND_TYPE},{JOINT_ID}," +
                   $"{LOCAL_POS_X},{LOCAL_POS_Y},{LOCAL_POS_Z}," +
                   $"{LOCAL_ROT_X},{LOCAL_ROT_Y},{LOCAL_ROT_Z},{LOCAL_ROT_W}," +
                   $"{TIMESTAMP}," +
                   $"{WORLD_POS_X},{WORLD_POS_Y},{WORLD_POS_Z}," +
                   $"{WORLD_ROT_X},{WORLD_ROT_Y},{WORLD_ROT_Z},{WORLD_ROT_W}";
        }
    }

    /// <summary>
    /// 포즈 시퀀스 데이터 (전체 녹화/재생 시퀀스)
    /// </summary>
    [System.Serializable]
    public class PoseSequence
    {
        public List<PoseFrame> frames = new List<PoseFrame>();
        public string fileName;
        public float totalDuration;
        public int totalFrames;
        public bool hasLeftHand;
        public bool hasRightHand;
        public DateTime createdTime;

        public PoseSequence()
        {
            frames = new List<PoseFrame>();
            fileName = "";
            totalDuration = 0f;
            totalFrames = 0;
            hasLeftHand = false;
            hasRightHand = false;
            createdTime = DateTime.Now;
        }

        /// <summary>
        /// 시퀀스 메타데이터 업데이트
        /// </summary>
        public void UpdateMetadata()
        {
            totalFrames = frames.Count;
            if (frames.Count > 0)
            {
                totalDuration = frames[frames.Count - 1].timestamp;

                // 왼손/오른손 데이터 존재 여부 확인
                foreach (var frame in frames)
                {
                    if (frame.leftLocalPoses.Count > 0) hasLeftHand = true;
                    if (frame.rightLocalPoses.Count > 0) hasRightHand = true;

                    if (hasLeftHand && hasRightHand) break;
                }
            }
        }

        /// <summary>
        /// 시퀀스 정보 문자열
        /// </summary>
        public override string ToString()
        {
            return $"PoseSequence[{fileName}] - Frames: {totalFrames}, Duration: {totalDuration:F2}s, " +
                   $"Left: {hasLeftHand}, Right: {hasRightHand}";
        }
    }
}
