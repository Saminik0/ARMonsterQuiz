using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using ARMonster.Models;

namespace ARMonster.Core
{
    public class GameEconomyManager : MonoBehaviour
    {
        public static GameEconomyManager Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text balanceText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public async Task RefreshBalanceUI()
        {
            var currentUser = DatabaseManager.Instance.Client?.Auth.CurrentUser;
            if (currentUser == null) return;

            try
            {
                var userRecord = await DatabaseManager.Instance.Client.From<UserModel>()
                    .Where(u => u.Id == currentUser.Id)
                    .Single();

                if (userRecord != null && balanceText != null)
                {
                    balanceText.text = $"Монеты: {userRecord.Balance}";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameEconomyManager] Ошибка при обновлении баланса: {ex.Message}");
            }
        }

        public async Task<bool> TryPayForScan(string monsterId, int cost)
        {
            Debug.Log($"[GameEconomyManager] Начинаем оплату за {monsterId}, стоимость: {cost}");
            var currentUser = DatabaseManager.Instance.Client?.Auth.CurrentUser;
            if (currentUser == null)
            {
                Debug.LogError("[GameEconomyManager] Текущий пользователь равен null. Сессия не активна.");
                return false;
            }

            try
            {
                Debug.Log($"[GameEconomyManager] Запрос данных пользователя (ID: {currentUser.Id}) из базы...");
                var userRecord = await DatabaseManager.Instance.Client.From<UserModel>()
                    .Where(u => u.Id == currentUser.Id)
                    .Single();

                if (userRecord == null)
                {
                    Debug.LogError("[GameEconomyManager] Запись UserModel не найдена в базе!");
                    return false;
                }

                Debug.Log($"[GameEconomyManager] Текущий баланс пользователя: {userRecord.Balance}");
                if (userRecord.Balance < cost)
                {
                    Debug.LogWarning($"[GameEconomyManager] Недостаточно средств. Баланс: {userRecord.Balance}, Требуется: {cost}.");
                    return false;
                }

                // Списываем средства
                Debug.Log($"[GameEconomyManager] Списание средств. Было: {userRecord.Balance}, Станет: {userRecord.Balance - cost}");
                userRecord.Balance -= cost;
                await DatabaseManager.Instance.Client.From<UserModel>().Update(userRecord);
                Debug.Log("[GameEconomyManager] Обновление баланса в базе успешно.");

                // Создаем лог транзакции
                var actionLog = new ActionLogModel
                {
                    UserId = currentUser.Id,
                    ActionType = "spawn_" + monsterId,
                    Details = monsterId,
                    Amount = -cost,
                    CreatedAt = System.DateTime.UtcNow
                };

                await DatabaseManager.Instance.Client.From<ActionLogModel>().Insert(actionLog);
                Debug.Log("[GameEconomyManager] Лог транзакции SCAN_MONSTER успешно записан.");

                // Обновляем UI баланса после успешной оплаты сканирования
                _ = RefreshBalanceUI();

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameEconomyManager] Ошибка при оплате: {ex.Message}\nСтек: {ex.StackTrace}");
                return false;
            }
        }
    }
}
