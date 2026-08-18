using UnityEngine;

namespace ARMonster.Core
{
    /// <summary>
    /// Компонент для AR-монстра:
    /// 1. Всегда плавно поворачивает спрайт/модель лицом к AR-камере (Billboard).
    /// 2. Добавляет эффект живого парения в воздухе (Hover bobbing).
    /// 3. Фиксирует и стабилизирует положение монстра в мировом AR-пространстве.
    /// </summary>
    public class ARMonsterBillboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [Tooltip("Фиксировать поворот только по горизонтали (монстр стоит вертикально).")]
        [SerializeField] private bool lockVerticalAxis = true;

        [Header("Hover Animation")]
        [Tooltip("Скорость покачивания в воздухе.")]
        [SerializeField] private float hoverSpeed = 2.5f;

        [Tooltip("Амплитуда покачивания (высота в метрах).")]
        [SerializeField] private float hoverHeight = 0.04f;

        private Camera _mainCamera;
        private Vector3 _anchorPosition;
        private bool _isInitialized = false;

        private void Start()
        {
            InitializeBillboard();
        }

        public void InitializeBillboard()
        {
            _mainCamera = Camera.main;
            _anchorPosition = transform.position;
            _isInitialized = true;
        }

        public void SetAnchorPosition(Vector3 newPos)
        {
            _anchorPosition = newPos;
            transform.position = newPos;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // 1. Поворот спрайта лицом к игроку
            Vector3 directionToCamera = _mainCamera.transform.position - transform.position;

            if (lockVerticalAxis)
            {
                directionToCamera.y = 0;
            }

            if (directionToCamera.sqrMagnitude > 0.0001f)
            {
                // В Unity спрайты в 2D смотрят вдоль оси +Z при повороте LookRotation
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }

            // 2. Плавное покачивание (парение в воздухе)
            if (_isInitialized)
            {
                float verticalOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
                transform.position = new Vector3(_anchorPosition.x, _anchorPosition.y + verticalOffset, _anchorPosition.z);
            }
        }
    }
}
