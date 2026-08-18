using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARMonster.Core
{
    /// <summary>
    /// Механика прицеливания и удержания монстра в центре экрана с помощью Raycast.
    /// </summary>
    public class CaptureMechanic : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Ссылка на главную AR-камеру.")]
        private Camera arCamera;

        [SerializeField]
        [Tooltip("Маска слоя для монстров (чтобы игнорировать остальные объекты).")]
        private LayerMask targetLayer;

        [SerializeField]
        [Tooltip("Ссылка на менеджер викторины.")]
        private QuizManager quizManager;

        [Header("Capture Settings")]
        [Tooltip("Требуемое время удержания прицела на монстре (в секундах).")]
        public float captureTime = 7.0f;

        [Tooltip("Время полного сброса прогресса при потере цели (в секундах).")]
        public float decayTime = 5.0f;

        [Header("UI")]
        [SerializeField]
        [Tooltip("UI Image с типом Filled для отображения прогресса удержания.")]
        private Image progressBar;

        /// <summary>
        /// Срабатывает, когда таймер удержания достигает holdDuration секунд.
        /// Передает данные захваченного монстра.
        /// </summary>
        public event Action<MonsterData> OnCaptureCompleted;

        private float _currentProgress = 0f;
        private bool _isSearching = true;

        /// <summary>
        /// Ссылка на QuizManager для доступа из внешних скриптов.
        /// </summary>
        public QuizManager QuizManager => quizManager;

        private void Awake()
        {
            // Автоматически находим главную камеру, если ссылка не назначена в инспекторе
            if (arCamera == null)
            {
                arCamera = Camera.main;
                if (arCamera == null)
                {
                    Debug.LogWarning("[CaptureMechanic] AR Camera is not assigned and Camera.main was not found.");
                }
            }

            if (progressBar == null)
            {
                Debug.LogWarning("[CaptureMechanic] Progress Bar UI Image is not assigned.");
            }

            ResetProgress();
        }

        private void Start()
        {
            if (quizManager == null)
            {
                quizManager = FindAnyObjectByType<QuizManager>();
                if (quizManager == null)
                {
                    Debug.LogWarning("[CaptureMechanic] QuizManager was not found in the scene.");
                }
            }
        }

        private void Update()
        {
            if (!_isSearching || arCamera == null)
                return;

            // Пускаем луч ровно из центра экрана в Viewport-координатах (0.5, 0.5, 0)
            Ray ray = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hitInfo, float.PositiveInfinity, targetLayer))
            {
                _currentProgress += Time.deltaTime / captureTime;

                if (_currentProgress >= 1.0f)
                {
                    _currentProgress = 1.0f;
                    
                    MonsterData capturedData = null;
                    var entity = hitInfo.collider.GetComponentInParent<MonsterEntity>();
                    if (entity != null)
                    {
                        capturedData = entity.Data;
                    }

                    ResetProgress();
                    OnCaptureCompleted?.Invoke(capturedData);
                    return; // Прерываем Update после поимки
                }
            }
            else
            {
                _currentProgress -= Time.deltaTime / decayTime;
            }

            _currentProgress = Mathf.Clamp01(_currentProgress);

            if (progressBar != null)
            {
                progressBar.fillAmount = _currentProgress;
            }
        }

        /// <summary>
        /// Выключает/включает обработку Raycast (пригодится, когда ставим игру на паузу во время викторины).
        /// </summary>
        /// <param name="active">True — включить поиск, False — остановить поиск и сбросить прогресс.</param>
        public void SetSearching(bool active)
        {
            _isSearching = active;
            if (!active)
            {
                ResetProgress();
            }
        }

        /// <summary>
        /// Сбрасывает прогресс удержания и заполнение UI-ползунка.
        /// </summary>
        public void ResetProgress()
        {
            _currentProgress = 0f;
            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }
        }
    }
}
