using UnityEngine;

namespace Algorytm.Dane
{
    public class FpsTracker : MonoBehaviour
    {
        private float _fpsSum;
        private float _minFps = float.MaxValue;
        private float _maxFps;
        private float _durationSeconds;
        private int _frameCount;
        private bool _isTracking;

        public float AverageFps => _frameCount > 0 ? _fpsSum / _frameCount : 0f;
        public float MinFps => _frameCount > 0 ? _minFps : 0f;
        public float MaxFps => _frameCount > 0 ? _maxFps : 0f;
        public int TotalFrames => _frameCount;
        public float DurationSeconds => _durationSeconds;

        public void StartTracking()
        {
            ResetTracker();
            _isTracking = true;
        }

        public void StopTracking()
        {
            _isTracking = false;
        }

        public void ResetTracker()
        {
            _fpsSum = 0f;
            _minFps = float.MaxValue;
            _maxFps = 0f;
            _durationSeconds = 0f;
            _frameCount = 0;
        }

        private void Update()
        {
            if (!_isTracking)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            float currentFps = 1f / deltaTime;

            _fpsSum += currentFps;
            _frameCount++;
            _durationSeconds += deltaTime;

            if (currentFps < _minFps)
            {
                _minFps = currentFps;
            }

            if (currentFps > _maxFps)
            {
                _maxFps = currentFps;
            }
        }
    }
}