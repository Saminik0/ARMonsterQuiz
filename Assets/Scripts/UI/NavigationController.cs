using UnityEngine;

namespace ARMonster.UI
{
    public class NavigationController : MonoBehaviour
    {
        public static NavigationController Instance { get; private set; }

        [Header("Панели (ОБЯЗАТЕЛЬНО ПЕРЕТАЩИТЬ!)")]
        public GameObject profilePanel;
        public GameObject cameraPanel; // Ваш HUD_Panel

        [Header("Менеджеры (ОБЯЗАТЕЛЬНО ПЕРЕТАЩИТЬ!)")]
        public ProfileManager profileManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Железобетонная привязка: ищем ВСЕ кнопки на сцене
            UnityEngine.UI.Button[] allButtons = Object.FindObjectsOfType<UnityEngine.UI.Button>(true);
            
            int camCount = 0;
            int profCount = 0;
            
            foreach (var btn in allButtons)
            {
                if (btn.name == "Btn_Camera" || btn.name == "Button_Camera" || btn.name.Contains("Camera"))
                {
                    btn.onClick.AddListener(ShowCamera);
                    camCount++;
                    Debug.Log($"[NavigationController] Успешно привязана кнопка: {btn.name} к ShowCamera");
                }
                else if (btn.name == "Btn_Profile" || btn.name == "Button_Profile" || btn.name.Contains("Profile"))
                {
                    btn.onClick.AddListener(ShowProfile);
                    profCount++;
                    Debug.Log($"[NavigationController] Успешно привязана кнопка: {btn.name} к ShowProfile");
                }
            }
            
            Debug.Log($"[NavigationController] АВТО-ПРИВЯЗКА ЗАВЕРШЕНА: Привязано {camCount} кнопок Камеры и {profCount} кнопок Профиля.");
        }

        public void ShowProfile()
        {
            Debug.Log("===> [NavigationController] Открываем Профиль!");
            if (cameraPanel != null) cameraPanel.SetActive(false);
            if (profilePanel != null) profilePanel.SetActive(true);

            if (profileManager != null) profileManager.LoadProfile();
        }

        public void ShowCamera()
        {
            Debug.Log("===> [NavigationController] Открываем Камеру!");
            if (profilePanel != null) profilePanel.SetActive(false);
            if (cameraPanel != null) cameraPanel.SetActive(true);
        }
    }
}
