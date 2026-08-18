using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using ARMonster.Core;
using ARMonster.Models;

namespace ARMonster.UI
{
    public class ProfileManager : MonoBehaviour
    {
        public static ProfileManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadSavedAvatar();
        }

        [Header("User Info UI")]
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private TMP_Text userGroupText;
        [SerializeField] private TMP_Text gagarikiBalanceText;
        [SerializeField] private Image avatarImage;

        [Header("Collection Progress UI")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;

        [Header("Inventory UI")]
        [SerializeField] private Transform baytikiContentParent;
        [SerializeField] private GameObject baytikPrefab;
        [SerializeField] private MonsterData[] availableMonsters;

        [Header("Navigation & Logout")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject profilePanel;
        [SerializeField] private Button logoutButton;

        private void Start()
        {
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        private void OnDestroy()
        {
            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }

        public void OnLogoutClicked()
        {
            Debug.Log("[ProfileManager] Выход из аккаунта студента.");
            if (AuthUIController.Instance != null)
            {
                AuthUIController.Instance.Logout();
            }
            else if (DatabaseManager.Instance != null)
            {
                _ = DatabaseManager.Instance.Logout();
            }
        }

        public async void LoadProfile()
        {
            var userRecord = DatabaseManager.Instance?.CurrentUser;
            if (userRecord == null)
            {
                Debug.LogWarning("[ProfileManager] Пользователь не авторизован.");
                return;
            }

            try
            {
                if (userNameText != null) userNameText.text = string.IsNullOrEmpty(userRecord.FirstName) ? userRecord.StudentNumber : userRecord.FirstName;
                if (userGroupText != null) userGroupText.text = string.IsNullOrEmpty(userRecord.StudentGroup) ? "Группа AR" : userRecord.StudentGroup;
                if (gagarikiBalanceText != null) gagarikiBalanceText.text = userRecord.Balance.ToString();

                LoadInventory();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProfileManager] Ошибка загрузки профиля: {ex.Message}");
            }
        }

        private async void LoadInventory()
        {
            // Очищаем старые карточки
            if (baytikiContentParent != null)
            {
                foreach (Transform child in baytikiContentParent)
                {
                    Destroy(child.gameObject);
                }
            }

            var catchList = DatabaseManager.Instance?.CatchHistory ?? new List<string>();

            // === ОБНОВЛЯЕМ ШКАЛУ ПРОГРЕССА ===
            int totalCaught = catchList.Count;
            int totalAvailable = (availableMonsters != null && availableMonsters.Length > 0) ? availableMonsters.Length : 20;

            if (progressBar != null)
            {
                progressBar.maxValue = totalAvailable;
                progressBar.value = totalCaught;
            }

            if (progressText != null)
            {
                progressText.text = $"{totalCaught} / {totalAvailable}";
            }

            // === СОЗДАЕМ КАРТОЧКИ МОНСТРОВ ===
            if (baytikPrefab != null && baytikiContentParent != null)
            {
                foreach (var monsterId in catchList)
                {
                    GameObject go = Instantiate(baytikPrefab, baytikiContentParent);
                    BaytikUIItem itemUI = go.GetComponent<BaytikUIItem>();
                    
                    if (itemUI != null)
                    {
                        Sprite mSprite = GetMonsterSprite(monsterId);
                        itemUI.Setup(monsterId, "Пойман в игре", mSprite);

                        string savedAvatarId = PlayerPrefs.GetString("SelectedAvatar", "");
                        if (monsterId == savedAvatarId && mSprite != null && avatarImage != null)
                        {
                            avatarImage.sprite = mSprite;
                        }
                    }
                }
            }
        }

        public void OpenProfile()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (profilePanel != null) profilePanel.SetActive(true);
            
            LoadProfile();
        }

        public void CloseProfile()
        {
            if (profilePanel != null) profilePanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void SetAvatar(Sprite newAvatar, string mId)
        {
            Debug.Log($"[ProfileManager] SetAvatar() called with ID: {mId}, Sprite is null: {newAvatar == null}");
            if (avatarImage != null && newAvatar != null)
            {
                avatarImage.sprite = newAvatar;
                PlayerPrefs.SetString("SelectedAvatar", mId);
                PlayerPrefs.Save();
            }
        }

        private Sprite GetMonsterSprite(string monsterId)
        {
            // 1. Ищем в массиве, если он заполнен в Инспекторе
            if (availableMonsters != null)
            {
                foreach (var monster in availableMonsters)
                {
                    if (monster != null && monster.monsterName == monsterId) 
                    {
                        return monster.monsterAvatar;
                    }
                }
            }

            // 2. Точечная загрузка спрайта по ID из Resources/Monsters/
            Debug.Log($"Ищем спрайт по пути: 'Monsters/{monsterId}'");
            Sprite s = Resources.Load<Sprite>($"Monsters/{monsterId}");
            if (s != null)
            {
                return s;
            }

            Debug.LogError($"Спрайт НЕ НАЙДЕН! Проверьте, что файл лежит строго в Assets/Resources/Monsters/ и называется '{monsterId}' без расширений.");
            return null;
        }

        private void LoadSavedAvatar()
        {
            string savedAvatarId = PlayerPrefs.GetString("SelectedAvatar", "");
            if (!string.IsNullOrEmpty(savedAvatarId))
            {
                Sprite savedSprite = GetMonsterSprite(savedAvatarId);
                if (savedSprite != null && avatarImage != null)
                {
                    avatarImage.sprite = savedSprite;
                    Debug.Log($"[ProfileManager] Loaded saved avatar: {savedAvatarId}");
                }
            }
        }
    }
}
