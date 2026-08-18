using System;
using UnityEngine;
using System.Threading.Tasks;
using ARMonster.Models;

namespace ARMonster.Core
{
    /// <summary>
    /// Управляет коллекцией пойманных монстров через базу данных Supabase.
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        // ========== ДОБАВЛЯЕМ SINGLETON ==========
        public static CollectionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // ========== КОНЕЦ ДОБАВЛЕНИЯ ==========

        /// <summary>
        /// Проверяет, был ли пойман монстр с указанным именем в текущей сессии БД.
        /// </summary>
        public bool IsMonsterCaught(string monsterName)
        {
            if (string.IsNullOrEmpty(monsterName) || DatabaseManager.Instance == null)
                return false;

            return DatabaseManager.Instance.HasCaughtMonster(monsterName);
        }

        /// <summary>
        /// Добавляет имя монстра в коллекцию игрока (в Supabase при онлайне и в локальный кэш).
        /// </summary>
        public async void CatchMonster(string monsterName)
        {
            if (string.IsNullOrEmpty(monsterName))
                return;

            if (IsMonsterCaught(monsterName))
            {
                Debug.Log($"[CollectionManager] Монстр {monsterName} уже есть в коллекции.");
                return;
            }

            if (DatabaseManager.Instance == null || !DatabaseManager.Instance.IsUserApproved)
            {
                Debug.LogError("[CollectionManager] Невозможно сохранить монстра. Пользователь не авторизован.");
                return;
            }

            // Добавляем в локальную историю в памяти
            DatabaseManager.Instance.CatchHistory.Add(monsterName);

            // Если есть онлайн-соединение с Supabase Auth — пишем в облако
            if (DatabaseManager.Instance.Client?.Auth.CurrentUser != null)
            {
                try
                {
                    var currentUser = DatabaseManager.Instance.Client.Auth.CurrentUser;

                    var newRecord = new MonsterCollectionModel
                    {
                        UserId = currentUser.Id,
                        MonsterId = monsterName,
                        CapturedAt = DateTime.UtcNow
                    };

                    await DatabaseManager.Instance.Client.From<MonsterCollectionModel>().Insert(newRecord);

                    var actionLog = new ActionLogModel
                    {
                        UserId = currentUser.Id,
                        ActionType = "catch_" + monsterName,
                        Details = monsterName,
                        Amount = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    await DatabaseManager.Instance.Client.From<ActionLogModel>().Insert(actionLog);

                    Debug.Log($"[CollectionManager] Успех! Монстр {monsterName} сохранен в БД Supabase.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CollectionManager] Ошибка онлайн-записи в БД: {ex.Message} (сохранен локально).");
                }
            }
            else
            {
                Debug.Log($"[CollectionManager] Монстр {monsterName} успешно добавлен в коллекцию (Автономный режим).");
            }
        }
    }
}