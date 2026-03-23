using UnityEngine;

namespace Algorytm.Dane
{
    /// <summary>
    /// Śledzi statystyki liczby klatek na sekundę podczas działania wizualizacji.
    /// </summary>
    public class FpsTracker : MonoBehaviour
    {
        private float _fpsSum;
        private float _minFps = float.MaxValue;
        private float _maxFps;
        private float _durationSeconds;
        private int _frameCount;
        private bool _isTracking;

        /// <summary>
        /// Zwraca średnią liczbę klatek na sekundę z zarejestrowanego okresu pomiaru.
        /// </summary>
        public float AverageFps => _frameCount > 0 ? _fpsSum / _frameCount : 0f;

        /// <summary>
        /// Zwraca minimalną liczbę klatek na sekundę zarejestrowaną podczas pomiaru.
        /// </summary>
        public float MinFps => _frameCount > 0 ? _minFps : 0f;

        /// <summary>
        /// Zwraca maksymalną liczbę klatek na sekundę zarejestrowaną podczas pomiaru.
        /// </summary>
        public float MaxFps => _frameCount > 0 ? _maxFps : 0f;

        /// <summary>
        /// Zwraca łączną liczbę zarejestrowanych klatek.
        /// </summary>
        public int TotalFrames => _frameCount;

        /// <summary>
        /// Zwraca całkowity czas trwania pomiaru w sekundach.
        /// </summary>
        public float DurationSeconds => _durationSeconds;

        /// <summary>
        /// Rozpoczyna nowy pomiar FPS, resetując wcześniej zapisane dane.
        /// </summary>
        public void StartTracking()
        {
            ResetTracker();
            _isTracking = true;
        }

        /// <summary>
        /// Zatrzymuje bieżący pomiar FPS.
        /// </summary>
        public void StopTracking()
        {
            _isTracking = false;
        }

        /// <summary>
        /// Resetuje wszystkie zapisane statystyki pomiaru FPS.
        /// </summary>
        public void ResetTracker()
        {
            _fpsSum = 0f;
            _minFps = float.MaxValue;
            _maxFps = 0f;
            _durationSeconds = 0f;
            _frameCount = 0;
            _isTracking = false;
        }

        /// <summary>
        /// Aktualizuje statystyki FPS dla bieżącej klatki, jeśli śledzenie jest aktywne.
        /// </summary>
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