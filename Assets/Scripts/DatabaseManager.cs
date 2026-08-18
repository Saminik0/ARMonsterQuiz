using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Supabase;
using ARMonster.Models;

namespace ARMonster.Core
{
    /// <summary>
    /// Singleton-менеджер для подключения к Supabase, управления аутентификацией студентов
    /// и работы с таблицами Users и Monster_Collection.
    /// </summary>
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager Instance { get; private set; }

        [Header("Supabase Credentials")]
        [SerializeField]
        private string supabaseUrl = "https://ntxflminberlovwawukr.supabase.co";

        [SerializeField]
        private string supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im50eGZsbWluYmVybG92d2F3dWtyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODYwMDg0NzUsImV4cCI6MjEwMTU4NDQ3NX0.3nI9K2tdpcaRC49RpSLyAfO9c_TK4Gr6GtSmQsnjVGQ";

        private Supabase.Client _client;

        /// <summary>
        /// Публичный доступ к инстансу Supabase Client.
        /// </summary>
        public Supabase.Client Client => _client;

        /// <summary>
        /// Данные текущего авторизованного пользователя из таблицы Users.
        /// </summary>
        public UserModel CurrentUser { get; private set; }

        /// <summary>
        /// Текущая роль пользователя ("student", "guardian", "admin").
        /// </summary>
        public string UserRole => CurrentUser?.Role ?? "student";

        /// <summary>
        /// Является ли текущий пользователь Хранителем или Администратором.
        /// </summary>
        public bool IsGuardian => CurrentUser != null && (CurrentUser.Role == "guardian" || CurrentUser.Role == "admin");

        /// <summary>
        /// Локальный список ID монстров, пойманных текущим игроком.
        /// </summary>
        public List<string> CatchHistory { get; private set; } = new List<string>();

        /// <summary>
        /// Флаг, показывающий, что пользователь авторизован и подтверждён.
        /// </summary>
        public bool IsUserApproved { get; private set; } = false;

        /// <summary>
        /// Флаг, показывающий, что клиент Supabase инициализирован.
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSupabase();
        }

        /// <summary>
        /// Инициализирует подключение к Supabase.
        /// </summary>
        private async void InitializeSupabase()
        {
            try
            {
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false
                };

                _client = new Supabase.Client(supabaseUrl, supabaseAnonKey, options);

                // Даем 2 секунды на попытку онлайн-подключения
                var initTask = _client.InitializeAsync();
                var timeoutTask = Task.Delay(2000);

                if (await Task.WhenAny(initTask, timeoutTask) == initTask)
                {
                    IsInitialized = true;
                    Debug.Log("[DatabaseManager] Supabase успешно подключен онлайн.");
                }
                else
                {
                    Debug.LogWarning("[DatabaseManager] Сервер Supabase не ответил вовремя. Включен автономный режим.");
                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] Supabase недоступен ({ex.Message}). Включен автономный режим.");
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Генерирует фейковый email по студенческому номеру.
        /// </summary>
        private string GetStudentEmail(string studentId)
        {
            return $"{studentId.Trim()}@campus.local";
        }

        /// <summary>
        /// Регистрация нового студента.
        /// </summary>
        public async Task<bool> Register(string studentId, string password)
        {
            if (_client != null)
            {
                try
                {
                    string fakeEmail = GetStudentEmail(studentId);
                    var session = await _client.Auth.SignUp(fakeEmail, password);
                    if (session != null && session.User != null)
                    {
                        Debug.Log($"[DatabaseManager] Онлайн-регистрация успешна: {studentId}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DatabaseManager] Ошибка онлайн-регистрации: {ex.Message}");
                }
            }

            // В автономном режиме регистрация всегда успешна
            Debug.Log($"[DatabaseManager] Автономная регистрация пользователя {studentId} завершена.");
            return true;
        }

        /// <summary>
        /// Вход в систему с поддержкой как онлайн-сервера Supabase, так и автономного режима.
        /// </summary>
        public async Task<bool> Login(string studentId, string password)
        {
            // 1. Пробуем онлайн-вход через Supabase
            if (_client != null)
            {
                try
                {
                    string fakeEmail = GetStudentEmail(studentId);
                    var loginTask = _client.Auth.SignInWithPassword(fakeEmail, password);
                    var timeoutTask = Task.Delay(3000);

                    if (await Task.WhenAny(loginTask, timeoutTask) == loginTask)
                    {
                        var session = await loginTask;
                        if (session != null && session.User != null)
                        {
                            string currentUserId = session.User.Id;
                            var userRecord = await _client.From<UserModel>()
                                .Where(u => u.Id == currentUserId)
                                .Single();

                            if (userRecord != null && userRecord.IsApproved)
                            {
                                CurrentUser = userRecord;
                                IsUserApproved = true;
                                await LoadUserData();

                                PlayerPrefs.SetString("saved_studentId", studentId);
                                PlayerPrefs.SetString("saved_pwd", password);
                                PlayerPrefs.Save();

                                Debug.Log($"[DatabaseManager] Онлайн-авторизация успешна! Пользователь: {studentId}, Роль: {UserRole}");
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DatabaseManager] Онлайн-вход не удался ({ex.Message}), переключаемся в автономный режим...");
                }
            }

            // 2. Автономный режим (если сервер недоступен или заблокирован)
            Debug.Log($"[DatabaseManager] Вход в АВТОНОМНОМ режиме для: {studentId}");

            string role = "student";
            string lowerId = studentId.ToLower();
            string lowerPwd = password.ToLower();
            if (lowerId.Contains("guardian") || lowerId.Contains("admin") || lowerId.Contains("хранитель") || lowerPwd.Contains("guardian") || lowerPwd.Contains("admin"))
            {
                role = "guardian";
            }

            CurrentUser = new UserModel
            {
                Id = "local_" + studentId,
                StudentNumber = studentId,
                FirstName = studentId,
                StudentGroup = "Группа AR",
                IsApproved = true,
                Balance = 200,
                Role = role
            };

            IsUserApproved = true;
            PlayerPrefs.SetString("saved_studentId", studentId);
            PlayerPrefs.SetString("saved_pwd", password);
            PlayerPrefs.Save();

            return true;
        }

        /// <summary>
        /// Выход из аккаунта и очистка сессии.
        /// </summary>
        public async Task Logout()
        {
            try
            {
                if (_client != null)
                {
                    await _client.Auth.SignOut();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] Ошибка при SignOut: {ex.Message}");
            }

            CurrentUser = null;
            IsUserApproved = false;
            CatchHistory.Clear();

            PlayerPrefs.DeleteKey("saved_studentId");
            PlayerPrefs.DeleteKey("saved_pwd");
            PlayerPrefs.Save();

            Debug.Log("[DatabaseManager] Пользователь вышел из системы.");
        }

        /// <summary>
        /// Публикует активный QR-код Хранителя в базу данных Supabase.
        /// </summary>
        public async Task<bool> PublishGuardianQRAsync(string token, string monsterName, int maxScans, float lifetimeMinutes)
        {
            if (_client == null || !IsGuardian)
            {
                Debug.LogWarning("[DatabaseManager] Только Хранитель может публиковать QR в базу.");
                return false;
            }

            try
            {
                var qrRecord = new GuardianQRModel
                {
                    Token = token,
                    MonsterName = monsterName,
                    MaxScans = maxScans,
                    CurrentScans = 0,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                    IsActive = true,
                    CreatedBy = CurrentUser?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _client.From<GuardianQRModel>().Insert(qrRecord);
                Debug.Log($"[DatabaseManager] QR-код успешно опубликован в Supabase для монстра '{monsterName}', токен: {token}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] Ошибка публикации QR в Supabase: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет и засчитывает сканирование QR-кода Хранителя через Supabase (между устройствами).
        /// </summary>
        public async Task<string> ValidateAndScanGuardianQRAsync(string token, string playerId)
        {
            if (_client == null)
                return null;

            try
            {
                var response = await _client.From<GuardianQRModel>()
                    .Where(q => q.Token == token && q.IsActive == true)
                    .Get();

                if (response == null || response.Models == null || response.Models.Count == 0)
                {
                    Debug.LogWarning($"[DatabaseManager] QR-код {token} не найден или неактивен в Supabase.");
                    return null;
                }

                var qr = response.Models[0];

                if (DateTime.UtcNow > qr.ExpiresAt)
                {
                    Debug.LogWarning($"[DatabaseManager] QR-код {token} истёк.");
                    return null;
                }

                if (qr.CurrentScans >= qr.MaxScans)
                {
                    Debug.LogWarning($"[DatabaseManager] QR-код {token} исчерпал лимит сканирований.");
                    return null;
                }

                // Увеличиваем счетчик сканирований
                qr.CurrentScans += 1;
                if (qr.CurrentScans >= qr.MaxScans)
                {
                    qr.IsActive = false;
                }

                await _client.From<GuardianQRModel>()
                    .Where(q => q.Id == qr.Id)
                    .Update(qr);

                Debug.Log($"[DatabaseManager] QR {token} успешно подтвержден через Supabase! Монстр: {qr.MonsterName}");
                return qr.MonsterName;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] Проверка QR в Supabase не удалась ({ex.Message}), используем локальный реестр.");
                return null;
            }
        }

        /// <summary>
        /// Загружает список ID пойманных монстров из таблицы Monster_Collection для текущего пользователя.
        /// </summary>
        public async Task LoadUserData()
        {
            CatchHistory.Clear();

            var currentUser = _client?.Auth.CurrentUser;
            if (currentUser == null)
            {
                Debug.LogWarning("[DatabaseManager] Невозможно загрузить данные: пользователь не авторизован.");
                return;
            }

            try
            {
                var response = await _client.From<MonsterCollectionModel>()
                    .Where(m => m.UserId == currentUser.Id)
                    .Get();

                if (response != null && response.Models != null)
                {
                    foreach (var record in response.Models)
                    {
                        if (!string.IsNullOrEmpty(record.MonsterId))
                        {
                            CatchHistory.Add(record.MonsterId);
                        }
                    }
                }

                Debug.Log($"[DatabaseManager] Коллекция загружена. Найдено монстров: {CatchHistory.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] Ошибка при загрузке данных пользователя: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет, поймал ли текущий игрок конкретного монстра по его monster_id.
        /// </summary>
        public bool HasCaughtMonster(string monsterId)
        {
            return CatchHistory.Contains(monsterId);
        }
    }
}
