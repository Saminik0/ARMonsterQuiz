using ARMonster.Guardian;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMonster.Core
{
    /// <summary>
    /// Отслеживает QR-коды и изображения через AR Foundation и управляет спавном 3D-монстров.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class ARImageTracker : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Глобальный запасной префаб (если у MonsterData он не задан).")]
        private GameObject fallbackMonsterPrefab;

        [SerializeField]
        [Tooltip("Ссылка на менеджер трекинга изображений AR Foundation.")]
        private ARTrackedImageManager trackedImageManager;

        [SerializeField]
        [Tooltip("Фиксированный масштаб монстра в метрах в AR (0.1 = 10 сантиметров).")]
        private float monsterScale = 0.1f;

        [SerializeField]
        [Tooltip("Список доступных монстров для определения стоимости по имени маркера.")]
        private List<MonsterData> availableMonsters;

        // Словарь созданных объектов: ключ — имя картинки или trackableId, значение — инстанс монстра
        private readonly Dictionary<string, GameObject> _spawnedMonsters = new Dictionary<string, GameObject>();

        private void Awake()
        {
            // Автоматически находим ARTrackedImageManager, если ссылка не назначена в инспекторе
            if (trackedImageManager == null)
            {
                trackedImageManager = GetComponent<ARTrackedImageManager>();
            }
        }

        private void OnEnable()
        {
            if (trackedImageManager != null)
            {
                trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
            }
            else
            {
                Debug.LogError("[ARImageTracker] ARTrackedImageManager is not assigned or found on this GameObject.");
            }
        }

        private void OnDisable()
        {
            if (trackedImageManager != null)
            {
                trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
            }
        }

        /// <summary>
        /// Обработчик события изменения состояния отслеживаемых изображений.
        /// </summary>
        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            if (eventArgs.added != null && eventArgs.added.Count > 0)
            {
                Debug.Log($"[ARImageTracker] Событие OnTrackedImagesChanged (Added). Найдено новых маркеров: {eventArgs.added.Count}");
            }

            // Проверка авторизации (раньше здесь был тихий return)
            if (DatabaseManager.Instance == null)
            {
                if (eventArgs.added != null && eventArgs.added.Count > 0)
                    Debug.LogWarning("[ARImageTracker] Игнорируем маркер: DatabaseManager не инициализирован.");
                return;
            }

            if (!DatabaseManager.Instance.IsUserApproved)
            {
                if (eventArgs.added != null && eventArgs.added.Count > 0)
                    Debug.LogWarning("[ARImageTracker] Игнорируем маркер: Игрок не авторизован (IsUserApproved = false). Возможно, Auto-Login еще в процессе.");
                return;
            }

            // Обработка добавленных маркеров
            if (eventArgs.added != null)
            {
                foreach (ARTrackedImage trackedImage in eventArgs.added)
                {
                    if (trackedImage == null)
                        continue;

                    string key = GetImageKey(trackedImage);
                    if (string.IsNullOrEmpty(key))
                        continue;

                    // Проверяем, что монстр для этой картинки ещё не был создан (защита от дублей)
                    if (!_spawnedMonsters.ContainsKey(key))
                    {
                        ProcessNewTrackedImage(trackedImage, key);
                    }
                }
            }

            // Обработка обновлённых маркеров (изменение состояния отслеживания)
            if (eventArgs.updated != null)
            {
                foreach (ARTrackedImage trackedImage in eventArgs.updated)
                {
                    if (trackedImage == null)
                        continue;

                    string key = GetImageKey(trackedImage);
                    if (string.IsNullOrEmpty(key))
                        continue;

                    // Включаем/выключаем видимость монстра в зависимости от видимости маркера
                    if (_spawnedMonsters.TryGetValue(key, out GameObject monsterInstance) && monsterInstance != null)
                    {
                        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
                        monsterInstance.SetActive(isTracking);
                    }
                }
            }

            // Обработка удалённых маркеров
            if (eventArgs.removed != null)
            {
                foreach (ARTrackedImage trackedImage in eventArgs.removed)
                {
                    if (trackedImage == null)
                        continue;

                    string key = GetImageKey(trackedImage);
                    if (string.IsNullOrEmpty(key))
                        continue;

                    if (_spawnedMonsters.TryGetValue(key, out GameObject monsterInstance))
                    {
                        if (monsterInstance != null)
                        {
                            Destroy(monsterInstance);
                        }

                        _spawnedMonsters.Remove(key);
                    }
                }
            }
        }

        private HashSet<string> _pendingScans = new HashSet<string>();

        private async void ProcessNewTrackedImage(ARTrackedImage trackedImage, string key)
        {
            Debug.Log($"[ARImageTracker] Начат процесс обработки маркера: {key}");

            if (_spawnedMonsters.ContainsKey(key) || _pendingScans.Contains(key))
            {
                Debug.Log($"[ARImageTracker] Монстр {key} уже заспавнен или в процессе оплаты.");
                return;
            }

            _pendingScans.Add(key);

            // ============================================================
            // ШАГ 1: Проверяем, не ультра-эпический ли это QR от Хранителя
            // ============================================================
            MonsterData data = null;
            bool isUltraEpic = false;
            string playerID = PlayerPrefs.GetString("PlayerID", SystemInfo.deviceUniqueIdentifier);

            // 1. Проверяем облачный реестр Supabase (для сканирования между разными устройствами)
            string cloudMonsterName = null;
            if (DatabaseManager.Instance != null)
            {
                cloudMonsterName = await DatabaseManager.Instance.ValidateAndScanGuardianQRAsync(key, playerID);
            }

            if (!string.IsNullOrEmpty(cloudMonsterName))
            {
                data = (availableMonsters != null) ? availableMonsters.Find(m => m != null && m.monsterName == cloudMonsterName) : null;
                if (data != null)
                {
                    isUltraEpic = true;
                    Debug.Log($"[ARImageTracker] ⭐ ОБНАРУЖЕН УЛЬТРА-ЭПИЧЕСКИЙ МОНСТР ИЗ ОБЛАКА: {data.monsterName}");
                }
            }
            else if (TryGetGuardianMonster(key, out data)) // 2. Локальный фолбэк (для тестирования на одном устройстве)
            {
                isUltraEpic = true;
                Debug.Log($"[ARImageTracker] ⭐ ОБНАРУЖЕН УЛЬТРА-ЭПИЧЕСКИЙ МОНСТР ИЗ ЛОКАЛЬНОГО РЕЕСТРА: {data.monsterName}");

                // Регистрируем сканирование в локальном реестре Хранителя
                if (GuardianQRRegistry.TryScanQR(key, playerID, out string _))
                {
                    Debug.Log($"[ARImageTracker] QR {key} успешно отсканирован игроком {playerID}");

                    // Уведомляем Хранителя (если генератор запущен в этой же сессии)
                    var guardianGen = FindObjectOfType<GuardianQRGenerator>();
                    if (guardianGen != null)
                    {
                        guardianGen.OnQRScannedByPlayer(playerID);
                    }
                }
                else
                {
                    Debug.LogWarning($"[ARImageTracker] Не удалось зарегистрировать сканирование QR {key}");
                    _pendingScans.Remove(key);
                    return;
                }
            }

            // ============================================================
            // ШАГ 2: Если не ультра-эпический — ищем среди обычных (бумажных)
            // ============================================================
            if (data == null)
            {
                if (availableMonsters == null || availableMonsters.Count == 0)
                {
                    Debug.LogError("[ARImageTracker] Список availableMonsters пуст!");
                    _pendingScans.Remove(key);
                    return;
                }

                data = availableMonsters.Find(m => m != null && m.monsterName == key);
                if (data == null)
                {
                    Debug.LogError($"[ARImageTracker] Не найдены данные MonsterData для маркера: '{key}'");
                    _pendingScans.Remove(key);
                    return;
                }

                isUltraEpic = false;
                Debug.Log($"[ARImageTracker] 📄 Обычный монстр: {data.monsterName}");
            }

            // ============================================================
            // ШАГ 3: Спавним монстра (с оплатой для обычных, бесплатно для ультра-эпических)
            // ============================================================
            bool canSpawn = true;

            if (!isUltraEpic)
            {
                // Для обычных монстров — списываем монеты
                int cost = data.spawnCost;
                canSpawn = await GameEconomyManager.Instance.TryPayForScan(key, cost);
                Debug.Log($"[ARImageTracker] Оплата для {key}: {(canSpawn ? "УСПЕШНА" : "НЕ ХВАТАЕТ МОНЕТ")}");
            }
            else
            {
                // Для ультра-эпических — бесплатно! (или дешевле)
                Debug.Log($"[ARImageTracker] ⭐ Ультра-эпический монстр — БЕСПЛАТНО!");

                // Можно добавить проверку, не поймал ли игрок уже этого монстра
                if (CollectionManager.Instance != null && CollectionManager.Instance.IsMonsterCaught(data.monsterName))
                {
                    Debug.Log($"[ARImageTracker] Игрок уже поймал {data.monsterName}!");
                    UI.UIManager.Instance?.ShowMessage($"Вы уже поймали {data.monsterName}!");
                    _pendingScans.Remove(key);
                    return;
                }
            }

            if (canSpawn && !_spawnedMonsters.ContainsKey(key))
            {
                Debug.Log($"[ARImageTracker] Спавн монстра {key}...");

                GameObject prefabToSpawn = data.monsterPrefab != null ? data.monsterPrefab : fallbackMonsterPrefab;
                SpawnMonster(trackedImage, key, prefabToSpawn, data);

                // Дополнительные эффекты для ультра-эпических
                if (isUltraEpic)
                {
                    StartCoroutine(UltraEpicSpawnEffects(trackedImage.transform.position));
                }
            }
            else if (!canSpawn)
            {
                Debug.LogWarning($"[ARImageTracker] Не удалось заспавнить монстра {key}");
            }

            _pendingScans.Remove(key);
        }

        private IEnumerator UltraEpicSpawnEffects(Vector3 position)
        {
            // Эпический эффект появления
            Debug.Log("💥 УЛЬТРА-ЭПИЧЕСКИЙ МОНСТР ПОЯВИЛСЯ!");

            // Можно добавить вибрацию
#if UNITY_ANDROID
    Handheld.Vibrate();
#endif

            // Показать уведомление на UI
            UI.UIManager.Instance?.ShowUltraEpicNotification("⭐ УЛЬТРА-ЭПИЧЕСКИЙ МОНСТР ПОЯВИЛСЯ! ⭐");

            yield return null;
        }

        /// <summary>
        /// Создаёт монстра в мировых координатах как дочерний объект маркера.
        /// </summary>
        private void SpawnMonster(ARTrackedImage trackedImage, string key, GameObject prefab, MonsterData data)
        {
            if (prefab == null)
            {
                Debug.LogError($"[ARImageTracker] ОШИБКА СПАВНА: У MonsterData '{key}' не назначен monsterPrefab, а fallbackMonsterPrefab пуст!");
                return;
            }

            Debug.Log($"[ARImageTracker] Вызов Instantiate для префаба {prefab.name} на маркере {key}...");

            // 1. Создаём монстра как дочерний объект маркера
            GameObject monsterInstance = Instantiate(prefab, trackedImage.transform);
            
            // Внедряем данные для CaptureMechanic
            var entity = monsterInstance.AddComponent<MonsterEntity>();
            entity.Data = data;

            // 2. Позиционируем в центре маркера с поднятием по Y
            monsterInstance.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            monsterInstance.transform.localRotation = Quaternion.identity;

            // 3. Рассчитываем масштаб
            float qrPhysicalWidth = (trackedImage.referenceImage != null && trackedImage.referenceImage.size.x > 0f)
                ? trackedImage.referenceImage.size.x
                : monsterScale;

            SpriteRenderer spriteRenderer = monsterInstance.GetComponentInChildren<SpriteRenderer>();
            float targetScale = monsterScale;

            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                float spriteUnscaledWidth = spriteRenderer.sprite.bounds.size.x;
                if (spriteUnscaledWidth > 0.0001f)
                {
                    targetScale = qrPhysicalWidth / spriteUnscaledWidth;
                }

                BoxCollider boxCollider = spriteRenderer.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = spriteRenderer.gameObject.AddComponent<BoxCollider>();
                }

                Bounds spriteBounds = spriteRenderer.sprite.bounds;
                boxCollider.center = spriteBounds.center;

                float zThickness = Mathf.Max(0.15f / targetScale, 0.2f);
                boxCollider.size = new Vector3(spriteBounds.size.x, spriteBounds.size.y, zThickness);
            }

            monsterInstance.transform.localScale = new Vector3(targetScale, targetScale, targetScale);

            _spawnedMonsters.Add(key, monsterInstance);
        }

        /// <summary>
        /// Возвращает уникальный ключ для маркера.
        /// </summary>
        private string GetImageKey(ARTrackedImage trackedImage)
        {
            if (trackedImage == null)
                return string.Empty;

            return trackedImage.referenceImage != null && !string.IsNullOrEmpty(trackedImage.referenceImage.name)
                ? trackedImage.referenceImage.name
                : trackedImage.trackableId.ToString();
        }
        /// <summary>
        /// Проверяет, является ли QR-код ультра-эпическим (от Хранителя)
        /// </summary>
        private bool TryGetGuardianMonster(string key, out MonsterData monsterData)
        {
            monsterData = null;

            // Проверяем, зарегистрирован ли QR в реестре Хранителей
            var guardianData = GuardianQRRegistry.GetQRData(key);
            if (guardianData == null)
                return false;

            // Проверяем, не истёк ли QR
            if (!guardianData.isActive || System.DateTime.Now > guardianData.expiryTime)
            {
                Debug.Log($"[ARImageTracker] QR {key} истёк или неактивен");
                return false;
            }

            // Ищем монстра в availableMonsters по имени
            monsterData = availableMonsters.Find(m => m != null && m.monsterName == guardianData.monsterName);

            if (monsterData == null)
            {
                Debug.LogError($"[ARImageTracker] Монстр {guardianData.monsterName} не найден в availableMonsters!");
                return false;
            }

            return true;
        }
    }
}
