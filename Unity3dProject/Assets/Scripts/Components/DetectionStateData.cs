using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
    {

    public enum TargetTrackingState
    {
        inactive = 0,
        active = 1,
        searching = 2,
        lost = 3
    }

    public struct TargetDetectionData
    {   
        public TargetTrackingState targetTrackingState;
        public Vector3 lastKnownPosition;
        public float targetDetectedTimer; 
    }

    public class DetectionStateData : MonoBehaviour
    {
        public float playerTargetDetectedTimer = 2f;
        private float targetRegainVisibilityTime = 8f;
        [SerializeField] private float _targetRegainVisibilityTimer = 8f;
        public float targetRegainVisibilityTimer {
            get => _targetRegainVisibilityTimer;
            set => _targetRegainVisibilityTimer = value;
        }

        public float targetSearchTime = 16f;
        [SerializeField] private float _targetSearchTimer = 16f;
        public float targetSearchTimer {
            get => _targetSearchTimer;
            set => _targetSearchTimer = value;
        }
        [SerializeField] private TargetTrackingState _targetTrackingState = 0;
        public TargetTrackingState targetTrackingState {
            get => _targetTrackingState;
            set => _targetTrackingState = value;
        }

        public void UpdateTimer_TargetRegainVisibility() => targetRegainVisibilityTimer -= Time.deltaTime;
        public void ResetTimer_TargetRegainVisibility() => targetRegainVisibilityTimer = targetRegainVisibilityTime;
        public void UpdateTimer_TargetSearch() => targetSearchTimer -= Time.deltaTime;
        public void ResetTimer_TargetSearch() => targetSearchTimer = targetSearchTime;
    }
}