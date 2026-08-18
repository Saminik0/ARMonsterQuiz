using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARMonster.Core
{
    /// <summary>
    /// Менеджер викторины для отображения вопросов и проверки ответов при поимке монстра.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Панель викторины.")]
        private GameObject quizPanel;

        [SerializeField]
        [Tooltip("Контейнер интро (приветствие).")]
        private GameObject introContainer;

        [SerializeField]
        [Tooltip("Контейнер ответов.")]
        private GameObject answersContainer;

        [SerializeField]
        [Tooltip("Контейнер для финального экрана (результат).")]
        private GameObject resultContainer;

        [SerializeField]
        [Tooltip("Кнопка 'Готов!' (переход к вопросам).")]
        private Button buttonReady;

        [SerializeField]
        [Tooltip("Кнопка закрытия викторины после результата.")]
        private Button buttonClose;

        [SerializeField]
        [Tooltip("Текстовое поле для имени монстра.")]
        private TMP_Text monsterNameText;

        [SerializeField]
        [Tooltip("Изображение для аватара монстра.")]
        private Image monsterAvatarImage;

        [SerializeField]
        [Tooltip("Текстовое поле для отображения вопроса монстра.")]
        private TMP_Text questionText;

        [SerializeField]
        [Tooltip("Массив из 4 текстовых полей на кнопках ответов.")]
        private TMP_Text[] answerButtonsText = new TMP_Text[4];

        [Header("Button Visuals & Colors")]
        [SerializeField]
        [Tooltip("Массив из 4-х Image кнопок ответов для изменения их цвета.")]
        private Image[] answerButtonImages = new Image[4];

        [SerializeField]
        [Tooltip("Цвет по умолчанию для кнопок ответов.")]
        private Color defaultColor = new Color(0f, 0.2f, 0.5f, 0.7f);

        [SerializeField]
        [Tooltip("Цвет кнопки при правильном ответе.")]
        private Color correctColor = Color.green;

        [SerializeField]
        [Tooltip("Цвет кнопки при неверном ответе.")]
        private Color wrongColor = Color.red;

        private MonsterData currentMonster;

        /// <summary>
        /// Срабатывает при завершении викторины (true — ответ правильный, false — ответ неверный).
        /// </summary>
        public event Action<bool> OnQuizFinished;

        private void Awake()
        {
            if (buttonReady != null)
            {
                buttonReady.onClick.AddListener(OnReadyClicked);
            }
            if (buttonClose != null)
            {
                buttonClose.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDestroy()
        {
            if (buttonReady != null)
            {
                buttonReady.onClick.RemoveListener(OnReadyClicked);
            }
            if (buttonClose != null)
            {
                buttonClose.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private GameObject foundNavIsland;

        /// <summary>
        /// Запускает викторину для указанного монстра, активируя панель и отображая вопрос.
        /// </summary>
        /// <param name="data">Данные монстра с вопросом и вариантами ответов.</param>
        public void StartQuiz(MonsterData data)
        {
            foundNavIsland = GameObject.Find("NavIsland");
            if (foundNavIsland != null)
            {
                foundNavIsland.SetActive(false);
            }

            if (data == null)
            {
                Debug.LogError("[QuizManager] StartQuiz called with null MonsterData.");
                return;
            }

            if (quizPanel != null)
            {
                quizPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[QuizManager] Quiz Panel is not assigned.");
            }

            SetupEncounter(data);
        }

        /// <summary>
        /// Настраивает UI элементы викторины (имя, аватар, текст вопроса в кавычках и кнопки ответов).
        /// </summary>
        /// <param name="monster">Данные монстра.</param>
        public void SetupQuiz(MonsterData monster)
        {
            SetupEncounter(monster);
        }

        /// <summary>
        /// Настраивает начальный этап диалога с монстром.
        /// </summary>
        public void SetupEncounter(MonsterData monster)
        {
            if (monster == null) return;
            currentMonster = monster;

            if (introContainer != null) introContainer.SetActive(true);
            if (answersContainer != null) answersContainer.SetActive(false);
            if (resultContainer != null) resultContainer.SetActive(false);

            if (monsterNameText != null)
            {
                monsterNameText.text = monster.monsterName;
            }

            if (questionText != null)
            {
                questionText.text = $"Привет! Я {monster.monsterName}. Чтобы я подчинился тебе, докажи свои знания!";
            }

            if (monsterAvatarImage != null)
            {
                bool hasAvatar = monster.monsterAvatar != null;
                monsterAvatarImage.gameObject.SetActive(hasAvatar);

                if (hasAvatar)
                {
                    monsterAvatarImage.sprite = monster.monsterAvatar;
                }
            }
        }

        private void OnReadyClicked()
        {
            if (introContainer != null) introContainer.SetActive(false);
            if (answersContainer != null) answersContainer.SetActive(true);

            if (currentMonster != null)
            {
                DisplayQuestion(currentMonster);
            }
        }

        /// <summary>
        /// Отображает вопрос монстра, сбрасывает цвета кнопок и включает их интерактивность.
        /// </summary>
        /// <param name="monster">Данные монстра.</param>
        public void DisplayQuestion(MonsterData monster)
        {
            if (monster == null)
            {
                Debug.LogError("[QuizManager] DisplayQuestion called with null MonsterData.");
                return;
            }

            currentMonster = monster;

            // Подставляем имя монстра
            if (monsterNameText != null)
            {
                monsterNameText.text = monster.monsterName;
            }

            // Проверяем наличие аватара: включаем и назначаем спрайт или выключаем объект
            if (monsterAvatarImage != null)
            {
                bool hasAvatar = monster.monsterAvatar != null;
                monsterAvatarImage.gameObject.SetActive(hasAvatar);

                if (hasAvatar)
                {
                    monsterAvatarImage.sprite = monster.monsterAvatar;
                }
            }

            // Подставляем текст вопроса, оборачивая строку в кавычки как прямую речь
            if (questionText != null)
            {
                questionText.text = $"«{monster.questionText}»";
            }

            // Заполняем тексты кнопок из массива answers
            if (answerButtonsText != null && monster.answers != null)
            {
                for (int i = 0; i < answerButtonsText.Length; i++)
                {
                    if (answerButtonsText[i] != null && i < monster.answers.Length)
                    {
                        answerButtonsText[i].text = monster.answers[i];
                    }
                }
            }

            // Сбрасываем цвет кнопок на defaultColor и включаем интерактивность
            if (answerButtonImages != null)
            {
                for (int i = 0; i < answerButtonImages.Length; i++)
                {
                    if (answerButtonImages[i] != null)
                    {
                        answerButtonImages[i].color = defaultColor;

                        Button button = answerButtonImages[i].GetComponent<Button>();
                        if (button != null)
                        {
                            button.interactable = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает выбор ответа игроком, проверяет результат и запускает задержку закрытия окна.
        /// </summary>
        /// <param name="selectedIndex">Индекс выбранного ответа (0-3).</param>
        public void OnAnswerClicked(int selectedIndex)
        {
            if (currentMonster == null)
            {
                Debug.LogWarning("[QuizManager] No active quiz data (currentMonster is null).");
                OnCloseClicked();
                return;
            }

            // Отключаем кликабельность всех кнопок, чтобы игрок не мог нажать дважды
            if (answerButtonImages != null)
            {
                for (int i = 0; i < answerButtonImages.Length; i++)
                {
                    if (answerButtonImages[i] != null)
                    {
                        Button button = answerButtonImages[i].GetComponent<Button>();
                        if (button != null)
                        {
                            button.interactable = false;
                        }
                    }
                }
            }

            bool isCorrect = (selectedIndex == currentMonster.correctAnswerIndex);

            if (answersContainer != null) answersContainer.SetActive(false);
            if (resultContainer != null) resultContainer.SetActive(true);

            if (isCorrect)
            {
                Debug.Log("Победа! Монстр пойман.");
                if (answerButtonImages != null && selectedIndex >= 0 && selectedIndex < answerButtonImages.Length && answerButtonImages[selectedIndex] != null)
                {
                    answerButtonImages[selectedIndex].color = correctColor;
                }

                if (questionText != null)
                {
                    questionText.text = "Молодец! Ты ответил абсолютно правильно. Теперь я признаю твою силу и буду служить тебе верой и правдой!";
                }

                // Здесь позже добавим выдачу валюты (гагариков)
                OnQuizFinished?.Invoke(true);
            }
            else
            {
                Debug.Log("Ошибка! Монстр сбежал.");
                if (answerButtonImages != null && selectedIndex >= 0 && selectedIndex < answerButtonImages.Length && answerButtonImages[selectedIndex] != null)
                {
                    answerButtonImages[selectedIndex].color = wrongColor;
                }

                // Покрасим правильную кнопку в зеленый, чтобы показать правильный вариант
                if (answerButtonImages != null &&
                    currentMonster.correctAnswerIndex >= 0 &&
                    currentMonster.correctAnswerIndex < answerButtonImages.Length &&
                    answerButtonImages[currentMonster.correctAnswerIndex] != null)
                {
                    answerButtonImages[currentMonster.correctAnswerIndex].color = correctColor;
                }

                if (questionText != null)
                {
                    questionText.text = "Ошибка! Твоих знаний пока недостаточно, чтобы подчинить меня. Возвращайся, когда выучишь материал!";
                }

                OnQuizFinished?.Invoke(false);
            }
        }

        private void OnCloseClicked()
        {
            if (foundNavIsland != null)
            {
                foundNavIsland.SetActive(true);
            }

            if (quizPanel != null)
            {
                quizPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            // Сбрасываем состояния контейнеров
            if (introContainer != null) introContainer.SetActive(false);
            if (answersContainer != null) answersContainer.SetActive(false);
            if (resultContainer != null) resultContainer.SetActive(false);
        }
    }
}
