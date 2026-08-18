using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARMonster.UI
{
    public class BaytikUIItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private Button selectButton;

        private string monsterId;
        private Sprite monsterSprite;

        private void Awake()
        {
            // Железобетонная привязка внутри префаба
            Button[] allChildButtons = GetComponentsInChildren<Button>(true);
            bool found = false;
            foreach (var btn in allChildButtons)
            {
                if (btn.name == "Button_Options" || btn.name == "Btn_Options" || btn.name.Contains("Options"))
                {
                    btn.onClick.AddListener(SelectThisAvatar);
                    found = true;
                    Debug.Log($"[BaytikUIItem] АВТО-ПРИВЯЗКА: Кнопка {btn.name} успешно привязана!");
                }
            }

            if (!found)
            {
                Debug.LogError("[BaytikUIItem] ОШИБКА: Не удалось найти кнопку Options внутри карточки!");
            }
        }

        // ЭТОТ МЕТОД НУЖНО ВЫЗЫВАТЬ ИЗ КНОПКИ В ИНСПЕКТОРЕ
        public void SelectThisAvatar()
        {
            Debug.Log($"===> [BaytikUIItem] Нажата карточка монстра: {monsterId}");
            if (ProfileManager.Instance != null)
            {
                ProfileManager.Instance.SetAvatar(monsterSprite, monsterId);
            }
            else
            {
                Debug.LogError("[BaytikUIItem] Ошибка: ProfileManager.Instance == null!");
            }
        }

        public void Setup(string mName, string captureDate, Sprite mSprite)
        {
            Debug.Log($"[BaytikUIItem] Setup() called for: {mName}, sprite is null: {mSprite == null}");
            monsterId = mName;
            monsterSprite = mSprite;
            if (nameText != null) nameText.text = mName;
            if (dateText != null) dateText.text = captureDate;
            
            if (icon != null)
            {
                if (mSprite != null)
                {
                    icon.sprite = mSprite;
                    icon.gameObject.SetActive(true);
                }
                else
                {
                    // Если спрайт не передан, можно выключить иконку или оставить заглушку
                    icon.gameObject.SetActive(false);
                }
            }
        }
    }
}
