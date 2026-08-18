using UnityEngine;
using ARMonster.UI;
using ARMonster.Guardian;

namespace ARMonster.Core
{
    /// <summary>
    /// Управляет игровым процессом: переход от прицеливания к викторине и сохранению монстра.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Controllers & Managers")]
        [SerializeField]
        [Tooltip("Механика прицеливания и удержания монстра.")]
        private CaptureMechanic captureMechanic;

        [SerializeField]
        [Tooltip("Менеджер викторины.")]
        private QuizManager quizManager;

        [SerializeField]
        [Tooltip("Менеджер коллекции пойманных монстров.")]
        private CollectionManager collectionManager;

        [Header("Test / MVP Data")]
        [SerializeField]
        [Tooltip("Заглушка данных монстра для MVP.")]
        private MonsterData testMonsterData;

        private MonsterData _currentCapturedData;

        private void OnEnable()
        {
            if (captureMechanic != null)
            {
                captureMechanic.OnCaptureCompleted += HandleCaptureSuccess;
            }

            if (quizManager != null)
            {
                quizManager.OnQuizFinished += HandleQuizFinished;
            }
        }

        private void OnDisable()
        {
            if (captureMechanic != null)
            {
                captureMechanic.OnCaptureCompleted -= HandleCaptureSuccess;
            }

            if (quizManager != null)
            {
                quizManager.OnQuizFinished -= HandleQuizFinished;
            }
        }

        /// <summary>
        /// Обработчик успешного удержания прицела на монстре.
        /// </summary>
        private void HandleCaptureSuccess(MonsterData capturedData)
        {
            if (captureMechanic != null)
            {
                captureMechanic.SetSearching(false);
            }

            _currentCapturedData = capturedData;

            MonsterData dataToUse = capturedData != null ? capturedData : testMonsterData;

            if (quizManager != null && dataToUse != null)
            {
                quizManager.StartQuiz(dataToUse);
            }
            else if (dataToUse == null)
            {
                Debug.LogWarning("[GameFlowController] Monster Data is null and Test Monster Data is not assigned.");
            }
        }

        /// <summary>
        /// Обработчик завершения викторины. (ЕДИНСТВЕННЫЙ МЕТОД)
        /// </summary>
        /// <param name="success">True, если дан правильный ответ.</param>
        private void HandleQuizFinished(bool success)
        {
            if (success)
            {
                MonsterData dataToUse = _currentCapturedData != null ? _currentCapturedData : testMonsterData;

                if (collectionManager != null && dataToUse != null)
                {
                    // Проверяем, не ультра-эпический ли это монстр
                    bool isUltraEpic = CheckIfUltraEpic(dataToUse.monsterName);

                    if (isUltraEpic)
                    {
                        Debug.Log($"⭐ ИГРОК ПОЙМАЛ УЛЬТРА-ЭПИЧЕСКОГО МОНСТРА: {dataToUse.monsterName} ⭐");

                        if (UIManager.Instance != null)
                            UIManager.Instance.ShowUltraEpicNotification($"⭐ ВЫ ПОЙМАЛИ УЛЬТРА-ЭПИЧЕСКОГО {dataToUse.monsterName}! ⭐");
                    }

                    collectionManager.CatchMonster(dataToUse.monsterName);
                }
                else if (collectionManager == null)
                {
                    Debug.LogWarning("[GameFlowController] Collection Manager is not assigned.");
                }
            }

            if (captureMechanic != null)
            {
                captureMechanic.SetSearching(true);
            }
        }

        /// <summary>
        /// Проверяет, является ли монстр ультра-эпическим
        /// </summary>
        private bool CheckIfUltraEpic(string monsterName)
        {
            // Ищем GuardianQRGenerator на сцене
            var guardianGen = FindObjectOfType<GuardianQRGenerator>();
            if (guardianGen != null)
            {
                // === ИСПРАВЛЕНО: Используем публичный метод вместо прямого доступа к полю ===
                var ultraEpicList = guardianGen.GetUltraEpicMonsters();
                if (ultraEpicList != null)
                {
                    foreach (var monster in ultraEpicList)
                    {
                        if (monster != null && monster.monsterName == monsterName)
                            return true;
                    }
                }
            }
            return false;
        }
    }
}