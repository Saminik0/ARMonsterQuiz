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
        /// Добавляет имя монстра в БД Supabase (таблицы Monster_Collection и Action_Logs).
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

            if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client?.Auth.CurrentUser == null)
            {
                Debug.LogError("[CollectionManager] Невозможно сохранить монстра. Пользователь не авторизован.");
                return;
            }

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

                DatabaseManager.Instance.CatchHistory.Add(monsterName);

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
                Debug.LogError($"[CollectionManager] Ошибка при сохранении монстра в БД: {ex.Message}");
            }
        }
    }
}