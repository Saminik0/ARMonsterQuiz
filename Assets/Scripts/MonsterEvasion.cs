using UnityEngine;
using System.Collections;

/// <summary>
/// Автономный скрипт уклонения монстра.
/// Хаотично перемещает монстра в заданном радиусе от точки спавна.
/// Работает в локальных координатах относительно AR-маркера (родителя).
/// Компенсирует масштаб родителя, чтобы радиус задавался в реальных метрах.
/// </summary>
public class MonsterEvasion : MonoBehaviour
{
    [Header("Настройки бега")]
    [Tooltip("Радиус бега в метрах (реальное расстояние). 0.3 = 30 см от центра маркера.")]
    public float moveRadius = 0.3f;

    [Tooltip("Скорость перемещения в метрах/сек (реальная скорость). 0.4 = 40 см/сек.")]
    public float moveSpeed = 0.4f;

    private Vector3 _startLocalPos;
    private Vector3 _targetLocalPos;
    private float _localRadius;
    private float _localSpeed;
    private bool _isReady = false;

    void Start()
    {
        StartCoroutine(InitDelay());
    }

    IEnumerator InitDelay()
    {
        // Ждем долю секунды, чтобы AR-маркер зафиксировался
        yield return new WaitForSeconds(0.3f);

        // Запоминаем ЛОКАЛЬНУЮ позицию (относительно QR-кода)
        _startLocalPos = transform.localPosition;

        // Компенсация масштаба родителя: переводим метры в локальные единицы.
        float parentScale = 1f;
        if (transform.parent != null)
        {
            parentScale = transform.parent.lossyScale.x;
        }
        if (parentScale < 0.0001f) parentScale = 1f;

        _localRadius = moveRadius / parentScale;
        _localSpeed = moveSpeed / parentScale;

        PickNewTarget();
        _isReady = true;
    }

    void PickNewTarget()
    {
        // Выбираем новую точку в пределах радиуса (в локальных единицах)
        Vector2 randomFlat = Random.insideUnitCircle * _localRadius;

        // Фиксируем ось Y: смещение только по X и Z
        _targetLocalPos = new Vector3(
            _startLocalPos.x + randomFlat.x,
            _startLocalPos.y, // <-- Строгая фиксация высоты
            _startLocalPos.z + randomFlat.y
        );
    }

    void Update()
    {
        if (!_isReady) return;

        // Двигаем монстра в локальных координатах
        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            _targetLocalPos,
            _localSpeed * Time.deltaTime
        );

        // Если добрались до цели — выбираем новую
        if (Vector3.Distance(transform.localPosition, _targetLocalPos) < 0.01f)
        {
            PickNewTarget();
        }
    }
}