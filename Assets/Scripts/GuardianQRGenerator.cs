using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ARMonster.Core;
using ARMonster.UI;

// === Правильные using для ZXing ===
using ZXing;
using ZXing.QrCode;

namespace ARMonster.Guardian
{
    public class GuardianQRGenerator : MonoBehaviour
    {
        [Header("UI Хранителя")]
        [SerializeField] private RawImage qrDisplayImage;
        [SerializeField] private TMP_Text monsterNameText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scanCountText;
        [SerializeField] private Button nextMonsterButton;
        [SerializeField] private Button lockMonsterButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private GameObject ultraEpicParticleEffect;

        [Header("Доступные ультра-эпические монстры")]
        [SerializeField] public List<MonsterData> ultraEpicMonsters;

        [Header("Настройки QR")]
        [SerializeField] private float minQRInterval = 10f;
        [SerializeField] private float maxQRInterval = 20f;
        [SerializeField] private int maxScansPerQR = 5;

        private string _currentQRToken;
        private Coroutine _qrLifecycleCoroutine;
        private float _timeUntilNextChange;
        private int _scanCount;
        private bool _isQRUnlocked = true;
        private MonsterData _currentMonster;
        private bool _isGenerating = false;

        public event System.Action<MonsterData, string> OnQRScanned;

        private void Start()
        {
            if (nextMonsterButton != null)
                nextMonsterButton.onClick.AddListener(ForceNextMonster);

            if (lockMonsterButton != null)
                lockMonsterButton.onClick.AddListener(ToggleQRUnlock);

            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);

            StartQRLifecycle();
        }

        private void OnDestroy()
        {
            if (nextMonsterButton != null)
                nextMonsterButton.onClick.RemoveListener(ForceNextMonster);
            if (lockMonsterButton != null)
                lockMonsterButton.onClick.RemoveListener(ToggleQRUnlock);
            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }

        private void OnLogoutClicked()
        {
            Debug.Log("[Guardian] Выход из режима Хранителя.");
            if (_qrLifecycleCoroutine != null)
                StopCoroutine(_qrLifecycleCoroutine);

            if (AuthUIController.Instance != null)
            {
                AuthUIController.Instance.Logout();
            }
            else if (DatabaseManager.Instance != null)
            {
                _ = DatabaseManager.Instance.Logout();
            }
        }

        public void StartQRLifecycle()
        {
            if (_qrLifecycleCoroutine != null)
                StopCoroutine(_qrLifecycleCoroutine);

            _qrLifecycleCoroutine = StartCoroutine(QRLifecycleRoutine());
        }

        public List<MonsterData> GetUltraEpicMonsters()
        {
            return ultraEpicMonsters;
        }

        private IEnumerator QRLifecycleRoutine()
        {
            while (true)
            {
                yield return GenerateNewGuardianQR();

                float lifeTime = Random.Range(minQRInterval * 60f, maxQRInterval * 60f);
                _timeUntilNextChange = lifeTime;

                Debug.Log($"[Guardian] QR для {_currentMonster?.monsterName} активен {lifeTime / 60:F1} минут");

                while (_timeUntilNextChange > 0)
                {
                    _timeUntilNextChange -= Time.deltaTime;
                    UpdateTimerUI(_timeUntilNextChange);

                    if (_scanCount >= maxScansPerQR && _isQRUnlocked)
                    {
                        Debug.Log($"[Guardian] QR достиг лимита сканирований!");
                        break;
                    }

                    yield return null;
                }

                if (_isQRUnlocked)
                {
                    _currentMonster = GetRandomUltraEpicMonster();
                }
            }
        }

        private IEnumerator GenerateNewGuardianQR()
        {
            while (_isGenerating)
                yield return null;

            _isGenerating = true;

            if (_isQRUnlocked)
            {
                _currentMonster = GetRandomUltraEpicMonster();
            }

            if (_currentMonster == null)
            {
                Debug.LogError("[Guardian] Нет доступных ультра-эпических монстров!");
                _isGenerating = false;
                yield break;
            }

            _currentQRToken = System.Guid.NewGuid().ToString();
            _scanCount = 0;

            RegisterGuardianQR(_currentQRToken, _currentMonster.monsterName);

            // === ГЕНЕРАЦИЯ QR-КОДА ЧЕРЕЗ ZXing ===
            Texture2D qrTexture = GenerateQRCode(_currentQRToken, 512, 512);

            if (qrTexture != null)
            {
                qrDisplayImage.texture = qrTexture;
            }
            else
            {
                Debug.LogError("[Guardian] Не удалось сгенерировать QR-код!");
            }

            monsterNameText.text = $"⭐ {_currentMonster.monsterName} ⭐";
            rarityText.text = "✨ УЛЬТРА-ЭПИЧЕСКИЙ ✨";
            scanCountText.text = $"0 / {maxScansPerQR}";

            if (ultraEpicParticleEffect != null)
                ultraEpicParticleEffect.SetActive(true);

            Debug.Log($"[Guardian] Новый QR создан для {_currentMonster.monsterName}, токен: {_currentQRToken}");

            _isGenerating = false;
            yield return null;
        }

        /// <summary>
        /// Генерация QR-кода через ZXing (ИСПРАВЛЕНАЯ ВЕРСИЯ)
        /// </summary>
        private Texture2D GenerateQRCode(string text, int width, int height)
        {
            try
            {
                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Width = width,
                        Height = height,
                        Margin = 1,
                        CharacterSet = "UTF-8"
                    }
                };

                // В версии ZXing для Unity Write() возвращает Color32[]
                Color32[] pixels = writer.Write(text);

                Texture2D texture = new Texture2D(width, height);
                texture.filterMode = FilterMode.Point;

                // Устанавливаем пиксели напрямую
                texture.SetPixels32(pixels);
                texture.Apply();

                return texture;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Guardian] Ошибка генерации QR: {ex.Message}");
                return null;
            }
        }

        private void RegisterGuardianQR(string qrToken, string monsterName)
        {
            // 1. Локальный реестр (быстрый кэш / оффлайн)
            GuardianQRRegistry.RegisterQR(qrToken, monsterName, maxScansPerQR);

            // 2. Облачный реестр в Supabase (для кросс-девайс сканирования игроками)
            if (DatabaseManager.Instance != null)
            {
                float durationMinutes = (_timeUntilNextChange > 0f) ? (_timeUntilNextChange / 60f) : maxQRInterval;
                _ = DatabaseManager.Instance.PublishGuardianQRAsync(qrToken, monsterName, maxScansPerQR, durationMinutes);
            }
        }

        private MonsterData GetRandomUltraEpicMonster()
        {
            if (ultraEpicMonsters == null || ultraEpicMonsters.Count == 0)
                return null;

            List<MonsterData> available = new List<MonsterData>(ultraEpicMonsters);
            available.Remove(_currentMonster);

            if (available.Count == 0)
                return ultraEpicMonsters[0];

            return available[Random.Range(0, available.Count)];
        }

        private void UpdateTimerUI(float timeLeft)
        {
            if (timerText == null) return;

            if (timeLeft > 60)
            {
                timerText.text = $"⏱️ {Mathf.FloorToInt(timeLeft / 60)}м {Mathf.FloorToInt(timeLeft % 60)}с";
            }
            else
            {
                timerText.text = $"⏱️ {Mathf.FloorToInt(timeLeft)}с";
            }
        }

        public void ForceNextMonster()
        {
            Debug.Log("[Guardian] Хранитель принудительно меняет монстра!");
            _currentMonster = GetRandomUltraEpicMonster();
            StartQRLifecycle();
        }

        public void ToggleQRUnlock()
        {
            _isQRUnlocked = !_isQRUnlocked;
            Debug.Log($"[Guardian] QR-генерация {(_isQRUnlocked ? "РАЗБЛОКИРОВАНА" : "ЗАБЛОКИРОВАНА")}");

            if (lockMonsterButton != null)
            {
                Text buttonText = lockMonsterButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = _isQRUnlocked ? "🔓 ЗАКРЕПИТЬ" : "🔒 ОТКРЕПИТЬ";
                }
            }
        }

        public void OnQRScannedByPlayer(string playerID)
        {
            _scanCount++;
            scanCountText.text = $"{_scanCount} / {maxScansPerQR}";

            Debug.Log($"[Guardian] Игрок {playerID} отсканировал QR! ({_scanCount}/{maxScansPerQR})");

            OnQRScanned?.Invoke(_currentMonster, playerID);

            StartCoroutine(ScanFeedbackEffect());
        }

        private IEnumerator ScanFeedbackEffect()
        {
            if (qrDisplayImage != null)
            {
                qrDisplayImage.color = Color.green;
                yield return new WaitForSeconds(0.3f);
                qrDisplayImage.color = Color.white;
            }
        }
    }

    public static class GuardianQRRegistry
    {
        private static Dictionary<string, GuardianQRData> _activeQRCodes = new Dictionary<string, GuardianQRData>();

        public class GuardianQRData
        {
            public string monsterName;
            public int maxScans;
            public int currentScans;
            public System.DateTime expiryTime;
            public List<string> scannedPlayers = new List<string>();
            public bool isActive = true;
        }

        public static void RegisterQR(string qrToken, string monsterName, int maxScans)
        {
            var data = new GuardianQRData
            {
                monsterName = monsterName,
                maxScans = maxScans,
                currentScans = 0,
                expiryTime = System.DateTime.Now.AddMinutes(20),
                isActive = true
            };

            _activeQRCodes[qrToken] = data;
            Debug.Log($"[GuardianRegistry] QR {qrToken} зарегистрирован для {monsterName}");
        }

        public static GuardianQRData GetQRData(string qrToken)
        {
            if (_activeQRCodes.TryGetValue(qrToken, out GuardianQRData data))
                return data;
            return null;
        }

        public static bool TryScanQR(string qrToken, string playerID, out string monsterName)
        {
            monsterName = null;

            var data = GetQRData(qrToken);
            if (data == null || !data.isActive)
                return false;

            if (System.DateTime.Now > data.expiryTime)
            {
                data.isActive = false;
                return false;
            }

            if (data.currentScans >= data.maxScans)
                return false;

            if (data.scannedPlayers.Contains(playerID))
                return false;

            data.currentScans++;
            data.scannedPlayers.Add(playerID);

            if (data.currentScans >= data.maxScans)
                data.isActive = false;

            monsterName = data.monsterName;
            return true;
        }

        public static void ClearExpired()
        {
            var toRemove = new List<string>();
            foreach (var kvp in _activeQRCodes)
            {
                if (!kvp.Value.isActive || System.DateTime.Now > kvp.Value.expiryTime)
                    toRemove.Add(kvp.Key);
            }

            foreach (var key in toRemove)
                _activeQRCodes.Remove(key);
        }
    }
}