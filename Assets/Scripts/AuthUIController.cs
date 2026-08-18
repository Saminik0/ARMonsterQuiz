using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARMonster.Core;

namespace ARMonster.UI
{
    /// <summary>
    /// Вспомогательный UI-контроллер для связывания кнопок и полей ввода (TMP_InputField) 
    /// с методами регистрации и входа в DatabaseManager.
    /// </summary>
    public class AuthUIController : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField]
        [Tooltip("Поле ввода студенческого билета/номера.")]
        private TMP_InputField studentIdInput;

        [SerializeField]
        [Tooltip("Поле ввода пароля.")]
        private TMP_InputField passwordInput;

        [Header("Buttons")]
        [SerializeField]
        [Tooltip("Кнопка Вход.")]
        private Button loginButton;

        [SerializeField]
        [Tooltip("Кнопка Регистрация.")]
        private Button registerButton;

        [Header("Status Output")]
        [SerializeField]
        [Tooltip("Текстовое поле статуса/ошибок.")]
        private TMP_Text statusText;

        [Header("AR Controls")]
        [SerializeField]
        [Tooltip("Объект AR Session, который нужно отключать при показе UI.")]
        private GameObject arSessionObject;

        [SerializeField]
        [Tooltip("Игровая панель HUD, которая должна включиться при входе в игру.")]
        private GameObject hudPanel;

        [SerializeField]
        [Tooltip("Островок навигации (NavIsland).")]
        public GameObject navIsland;

        [Header("Guardian Panel")]
        [SerializeField]
        [Tooltip("Панель генерации QR-кодов для Хранителя (Guardian_Panel).")]
        private GameObject guardianPanel;

        public static AuthUIController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private async void Start()
        {
            if (navIsland != null) navIsland.SetActive(false);
            if (guardianPanel != null) guardianPanel.SetActive(false);

            if (arSessionObject != null)
            {
                arSessionObject.SetActive(false);
            }

            if (PlayerPrefs.HasKey("saved_studentId") && PlayerPrefs.HasKey("saved_pwd"))
            {
                string savedId = PlayerPrefs.GetString("saved_studentId");
                string savedPwd = PlayerPrefs.GetString("saved_pwd");
                
                SetStatus("Восстановление сессии...");
                SetButtonsInteractable(false);

                // Ждем инициализации Supabase максимум 4 секунды
                float waitTimer = 0f;
                while ((DatabaseManager.Instance == null || !DatabaseManager.Instance.IsInitialized) && waitTimer < 4.0f)
                {
                    waitTimer += Time.unscaledDeltaTime;
                    await System.Threading.Tasks.Task.Yield();
                }

                if (DatabaseManager.Instance != null && DatabaseManager.Instance.IsInitialized)
                {
                    bool success = await DatabaseManager.Instance.Login(savedId, savedPwd);
                    
                    if (success)
                    {
                        SetStatus("Авторизация успешна!");
                        RouteUserAfterLogin();
                        return;
                    }
                }

                // Если автоматический вход не удался или истек таймаут — разблокируем интерфейс
                SetButtonsInteractable(true);
                if (studentIdInput != null) studentIdInput.text = savedId;
                SetStatus("Введите пароль для входа.");
            }
            else
            {
                SetStatus("Введите логин и пароль.");
            }
        }

        private void OnEnable()
        {
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
            if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
        }

        private void OnDisable()
        {
            if (loginButton != null) loginButton.onClick.RemoveListener(OnLoginClicked);
            if (registerButton != null) registerButton.onClick.RemoveListener(OnRegisterClicked);
        }

        public async void OnLoginClicked()
        {
            if (string.IsNullOrEmpty(studentIdInput.text) || string.IsNullOrEmpty(passwordInput.text))
            {
                SetStatus("Заполните логин/номер билета и пароль!");
                return;
            }

            SetStatus("Выполняется вход...");
            SetButtonsInteractable(false);

            // Ждем инициализации Supabase, если еще не готов (с таймаутом 5 сек)
            float waitTimer = 0f;
            while ((DatabaseManager.Instance == null || !DatabaseManager.Instance.IsInitialized) && waitTimer < 5.0f)
            {
                waitTimer += Time.unscaledDeltaTime;
                await System.Threading.Tasks.Task.Yield();
            }

            if (DatabaseManager.Instance == null || !DatabaseManager.Instance.IsInitialized)
            {
                SetStatus("Ошибка: Нет связи с сервером базы данных.");
                SetButtonsInteractable(true);
                return;
            }

            bool success = await DatabaseManager.Instance.Login(studentIdInput.text, passwordInput.text);

            SetButtonsInteractable(true);

            if (success)
            {
                SetStatus("Успешный вход!");
                RouteUserAfterLogin();
            }
            else
            {
                SetStatus("Ошибка входа! Проверьте логин, пароль или подтверждение модератора.");
            }
        }

        /// <summary>
        /// Маршрутизация пользователя в зависимости от его роли (Хранитель или Студент).
        /// </summary>
        private void RouteUserAfterLogin()
        {
            gameObject.SetActive(false);

            if (DatabaseManager.Instance != null && DatabaseManager.Instance.IsGuardian)
            {
                // === РЕЖИМ ХРАНИТЕЛЯ ===
                Debug.Log("[AuthUIController] Вход под ролью ХРАНИТЕЛЯ / АДМИНИСТРАТОРА.");
                
                if (navIsland != null) navIsland.SetActive(false);
                if (hudPanel != null) hudPanel.SetActive(false);
                if (arSessionObject != null) arSessionObject.SetActive(false);

                if (guardianPanel != null)
                {
                    guardianPanel.SetActive(true);
                    var generator = guardianPanel.GetComponentInChildren<Guardian.GuardianQRGenerator>(true);
                    if (generator != null)
                    {
                        generator.gameObject.SetActive(true);
                        generator.StartQRLifecycle();
                    }
                }
                else
                {
                    Debug.LogWarning("[AuthUIController] guardianPanel не назначен в инспекторе!");
                }
            }
            else
            {
                // === РЕЖИМ ОБЫЧНОГО СТУДЕНТА ===
                Debug.Log("[AuthUIController] Вход под ролью СТУДЕНТА.");

                if (guardianPanel != null) guardianPanel.SetActive(false);
                if (navIsland != null) navIsland.SetActive(true);
                
                if (NavigationController.Instance != null)
                {
                    NavigationController.Instance.ShowProfile();
                }

                if (GameEconomyManager.Instance != null)
                {
                    _ = GameEconomyManager.Instance.RefreshBalanceUI();
                }
            }
        }

        /// <summary>
        /// Выход из аккаунта и возврат на экран логина.
        /// </summary>
        public async void Logout()
        {
            if (guardianPanel != null) guardianPanel.SetActive(false);
            if (navIsland != null) navIsland.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (arSessionObject != null) arSessionObject.SetActive(false);

            if (NavigationController.Instance != null && NavigationController.Instance.profilePanel != null)
            {
                NavigationController.Instance.profilePanel.SetActive(false);
            }

            if (passwordInput != null) passwordInput.text = string.Empty;

            gameObject.SetActive(true);
            SetStatus("Вы вышли из системы.");

            if (DatabaseManager.Instance != null)
            {
                await DatabaseManager.Instance.Logout();
            }
        }

        public async void OnRegisterClicked()
        {
            if (string.IsNullOrEmpty(studentIdInput.text) || string.IsNullOrEmpty(passwordInput.text))
            {
                SetStatus("Заполните номер билета и пароль!");
                return;
            }

            SetStatus("Регистрация...");
            SetButtonsInteractable(false);

            bool success = await DatabaseManager.Instance.Register(studentIdInput.text, passwordInput.text);

            SetButtonsInteractable(true);

            if (success)
            {
                SetStatus("Регистрация успешна! Аккаунт ожидает подтверждения модератором.");
            }
            else
            {
                SetStatus("Ошибка регистрации!");
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void SetButtonsInteractable(bool state)
        {
            if (loginButton != null) loginButton.interactable = state;
            if (registerButton != null) registerButton.interactable = state;
        }
    }
}
