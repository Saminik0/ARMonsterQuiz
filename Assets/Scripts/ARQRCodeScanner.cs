using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using ZXing;
using ARMonster.Models;
using ARMonster.Guardian;
using ARMonster.UI;

namespace ARMonster.Core
{
    /// <summary>
    /// Высокопроизводительный считыватель QR-кодов через камеру AR Foundation (ARCameraManager)
    /// с асинхронным декодированием через ZXing для ультра-эпических монстров Хранителя.
    /// </summary>
    [RequireComponent(typeof(ARCameraManager))]
    public class ARQRCodeScanner : MonoBehaviour
    {
        [Header("AR Camera Dependencies")]
        [SerializeField]
        [Tooltip("Ссылка на ARCameraManager (если не указана, найдется автоматически).")]
        private ARCameraManager cameraManager;

        [SerializeField]
        [Tooltip("Главная AR-камера для расчета точки спавна монстра.")]
        private Camera arCamera;

        [Header("Scan Configuration")]
        [SerializeField]
        [Tooltip("Интервал между попытками сканирования кадра (в секундах).")]
        private float scanInterval = 0.25f;

        [SerializeField]
        [Tooltip("Ширина кадра для распознавания (уменьшение ускоряет декодирование в разы).")]
        private int targetScanWidth = 480;

        [SerializeField]
        [Tooltip("Дистанция спавна монстра перед камерой (в метрах).")]
        private float spawnDistance = 1.2f;

        [SerializeField]
        [Tooltip("Масштаб ультра-эпического монстра в AR (комфортный размер для смартфона).")]
        private float monsterScale = 0.55f;

        [Header("Monster Database")]
        [SerializeField]
        [Tooltip("Список доступных монстров для сопоставления данных.")]
        private List<MonsterData> availableMonsters;

        [SerializeField]
        [Tooltip("Запасной префаб, если у монстра не задан собственный.")]
        private GameObject fallbackMonsterPrefab;

        private IBarcodeReader _barcodeReader;
        private bool _isScanning = false;
        private float _lastScanTime = 0f;
        private string _lastProcessedToken = string.Empty;
        private float _tokenCooldownTime = 5.0f;
        private float _lastTokenProcessedTimestamp = 0f;
        private bool _isProcessingQR = false;

        // Текущий активный заспавненный ультра-эпический монстр
        private GameObject _activeUltraEpicMonster;

        private void Awake()
        {
            if (cameraManager == null)
            {
                cameraManager = GetComponent<ARCameraManager>();
            }

            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            if (availableMonsters == null || availableMonsters.Count == 0)
            {
                var loaded = Resources.LoadAll<MonsterData>("");
                if (loaded != null && loaded.Length > 0)
                {
                    availableMonsters = new List<MonsterData>(loaded);
                    Debug.Log($"[ARQRCodeScanner] Автоматически загружено {availableMonsters.Count} карточек монстров из Resources.");
                }
            }

            // Инициализация BarcodeReader из ZXing с оптимизацией под QR-коды
            _barcodeReader = new BarcodeReader
            {
                AutoRotate = false,
                Options = new ZXing.Common.DecodingOptions
                {
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                    TryHarder = false
                }
            };
        }

        private void OnEnable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        private void OnDisable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Удобный шорткат для тестирования сканирования Хранителя в Редакторе Unity (новый Input System)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                Debug.Log("[ARQRCodeScanner] [EDITOR DEBUG] Симуляция сканирования QR-кода Хранителя...");
                SimulateEditorQRScan();
            }
#endif
        }

        /// <summary>
        /// Вызывается AR Foundation при поступлении нового кадра камеры.
        /// </summary>
        private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            if (_isScanning || _isProcessingQR || Time.time - _lastScanTime < scanInterval)
                return;

            // Проверка авторизации
            if (DatabaseManager.Instance == null || !DatabaseManager.Instance.IsUserApproved)
                return;

            // Если уже есть заспавненный активный ультра-монстр перед камерой — пропускаем
            if (_activeUltraEpicMonster != null)
                return;

            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                _lastScanTime = Time.time;
                _isScanning = true;

                StartCoroutine(ProcessImageRoutine(image));
            }
        }

        /// <summary>
        /// Асинхронная конвертация и распознавание QR-кода из кадра камеры.
        /// </summary>
        private IEnumerator ProcessImageRoutine(XRCpuImage image)
        {
            Color32[] colorPixels = null;
            int width = 0;
            int height = 0;

            using (image)
            {
                // Рассчитываем пропорциональный даунскейл для мгновенного парсинга
                float aspectRatio = (float)image.height / image.width;
                width = targetScanWidth;
                height = Mathf.RoundToInt(width * aspectRatio);

                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(width, height),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.None
                };

                int size = image.GetConvertedDataSize(conversionParams);
                var rawBuffer = new NativeArray<byte>(size, Allocator.Temp);

                image.Convert(conversionParams, rawBuffer);

                // Преобразуем байты RGBA в Color32
                colorPixels = new Color32[width * height];
                for (int i = 0; i < colorPixels.Length; i++)
                {
                    int byteIndex = i * 4;
                    colorPixels[i] = new Color32(
                        rawBuffer[byteIndex],
                        rawBuffer[byteIndex + 1],
                        rawBuffer[byteIndex + 2],
                        rawBuffer[byteIndex + 3]
                    );
                }

                rawBuffer.Dispose();
            }

            yield return null;

            // Декодируем QR через ZXing
            Result decodeResult = null;
            try
            {
                decodeResult = _barcodeReader.Decode(colorPixels, width, height);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ARQRCodeScanner] Ошибка декодера ZXing: {ex.Message}");
            }

            _isScanning = false;

            if (decodeResult != null && !string.IsNullOrEmpty(decodeResult.Text))
            {
                string scannedToken = decodeResult.Text.Trim();
                OnQRCodeDetected(scannedToken);
            }
        }

        /// <summary>
        /// Обработчик успешного распознавания строки QR-кода.
        /// </summary>
        private async void OnQRCodeDetected(string token)
        {
            // Защита от спама одним и тем же токеном
            if (token == _lastProcessedToken && Time.time - _lastTokenProcessedTimestamp < _tokenCooldownTime)
                return;

            _lastProcessedToken = token;
            _lastTokenProcessedTimestamp = Time.time;
            _isProcessingQR = true;

            Debug.Log($"[ARQRCodeScanner] 🎯 ОБНАРУЖЕН QR-КОД: {token}");

            string playerID = PlayerPrefs.GetString("saved_studentId", SystemInfo.deviceUniqueIdentifier);
            MonsterData targetMonster = null;

            // 1. Проверяем облачный реестр Supabase (кросс-девайс)
            string monsterName = null;
            if (DatabaseManager.Instance != null)
            {
                monsterName = await DatabaseManager.Instance.ValidateAndScanGuardianQRAsync(token, playerID);
            }

            // 2. Локальный фолбэк (для тестирования на одном ПК / локальном реестре)
            if (string.IsNullOrEmpty(monsterName))
            {
                if (GuardianQRRegistry.TryScanQR(token, playerID, out string localMonsterName))
                {
                    monsterName = localMonsterName;
                    var guardianGen = FindFirstObjectByType<GuardianQRGenerator>();
                    if (guardianGen != null) guardianGen.OnQRScannedByPlayer(playerID);
                }
            }

            if (!string.IsNullOrEmpty(monsterName))
            {
                if (availableMonsters != null && availableMonsters.Count > 0)
                {
                    targetMonster = availableMonsters.Find(m => m != null && string.Equals(m.monsterName, monsterName, StringComparison.OrdinalIgnoreCase));
                    if (targetMonster == null)
                    {
                        targetMonster = availableMonsters[0];
                    }
                }

                if (targetMonster != null)
                {
                    // Проверяем, не пойман ли уже
                    if (CollectionManager.Instance != null && CollectionManager.Instance.IsMonsterCaught(targetMonster.monsterName))
                    {
                        Debug.Log($"[ARQRCodeScanner] Монстр {targetMonster.monsterName} уже пойман игроком.");
                        UIManager.Instance?.ShowMessage($"Монстр {targetMonster.monsterName} уже есть в вашей коллекции!");
                        _isProcessingQR = false;
                        return;
                    }

                    // Спавним ультра-эпического монстра в AR
                    SpawnUltraEpicMonsterInAR(targetMonster);
                }
                else
                {
                    Debug.LogWarning($"[ARQRCodeScanner] Монстр '{monsterName}' не найден в списке availableMonsters!");
                }
            }
            else
            {
                Debug.LogWarning($"[ARQRCodeScanner] QR-код '{token}' недействителен, истёк или лимит сканирований исчерпан.");
            }

            _isProcessingQR = false;
        }

        /// <summary>
        /// Создает 3D-монстра в мировых координатах перед AR-камерой.
        /// </summary>
        private void SpawnUltraEpicMonsterInAR(MonsterData data)
        {
            if (arCamera == null)
            {
                arCamera = Camera.main;
                if (arCamera == null) return;
            }

            // Точка спавна — ровно перед камерой на расстоянии spawnDistance
            Vector3 forward = arCamera.transform.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.01f) forward = arCamera.transform.forward;
            forward.Normalize();

            Vector3 spawnPos = arCamera.transform.position + (forward * spawnDistance);
            spawnPos.y = arCamera.transform.position.y - 0.15f;

            Quaternion spawnRot = Quaternion.LookRotation(forward);

            GameObject prefab = data.monsterPrefab != null ? data.monsterPrefab : fallbackMonsterPrefab;
            if (prefab != null)
            {
                _activeUltraEpicMonster = Instantiate(prefab, spawnPos, spawnRot);
            }
            else
            {
                // Процедурный объект, если префаб не задан
                _activeUltraEpicMonster = new GameObject("UltraEpicMonster_" + data.monsterName);
                _activeUltraEpicMonster.transform.position = spawnPos;
                _activeUltraEpicMonster.transform.rotation = spawnRot;
                var sr = _activeUltraEpicMonster.AddComponent<SpriteRenderer>();
                if (data.monsterAvatar != null) sr.sprite = data.monsterAvatar;
            }

            // Добавляем MonsterEntity для связи с CaptureMechanic
            var entity = _activeUltraEpicMonster.GetComponent<MonsterEntity>();
            if (entity == null)
            {
                entity = _activeUltraEpicMonster.AddComponent<MonsterEntity>();
            }
            entity.Data = data;

            // Добавляем динамический билборд и стабилизатор парения в AR
            var billboard = _activeUltraEpicMonster.GetComponent<ARMonsterBillboard>();
            if (billboard == null)
            {
                billboard = _activeUltraEpicMonster.AddComponent<ARMonsterBillboard>();
            }
            billboard.InitializeBillboard();

            // Настройка коллайдера для захвата прицелом
            SpriteRenderer spriteRenderer = _activeUltraEpicMonster.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                BoxCollider boxCollider = spriteRenderer.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = spriteRenderer.gameObject.AddComponent<BoxCollider>();
                }
                Bounds bounds = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
                boxCollider.center = bounds.center;
                boxCollider.size = new Vector3(Mathf.Max(0.6f, bounds.size.x), Mathf.Max(0.6f, bounds.size.y), Mathf.Max(0.8f, bounds.size.z));
            }
            else
            {
                var box = _activeUltraEpicMonster.GetComponent<BoxCollider>();
                if (box == null) box = _activeUltraEpicMonster.AddComponent<BoxCollider>();
                box.size = new Vector3(0.8f, 0.8f, 0.8f);
            }

            _activeUltraEpicMonster.transform.localScale = Vector3.one * monsterScale;

            // Эффекты появления
            TriggerUltraEpicEffects(data.monsterName);
        }

        private void TriggerUltraEpicEffects(string monsterName)
        {
            Debug.Log($"💥 [ARQRCodeScanner] ⭐ УЛЬТРА-ЭПИЧЕСКИЙ {monsterName} ЗАСПАВНЕН В AR!");

#if UNITY_ANDROID
            Handheld.Vibrate();
#endif

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowUltraEpicNotification($"⭐ УЛЬТРА-ЭПИЧЕСКИЙ {monsterName} ПОЯВИЛСЯ! НАВЕДИТЕ ПРИЦЕЛ! ⭐");
            }
        }

#if UNITY_EDITOR
        private void SimulateEditorQRScan()
        {
            if (availableMonsters != null && availableMonsters.Count > 0)
            {
                var monster = availableMonsters[0];
                SpawnUltraEpicMonsterInAR(monster);
            }
        }
#endif
    }
}
