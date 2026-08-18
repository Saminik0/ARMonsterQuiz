using UnityEngine;
using TMPro;
using System.Collections;

namespace ARMonster.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private TMP_Text notificationText;
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private float notificationDuration = 2f;

        private Coroutine _notificationCoroutine;

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

        public void ShowMessage(string message)
        {
            if (_notificationCoroutine != null)
                StopCoroutine(_notificationCoroutine);

            _notificationCoroutine = StartCoroutine(ShowNotification(message, false));
        }

        public void ShowUltraEpicNotification(string message)
        {
            if (_notificationCoroutine != null)
                StopCoroutine(_notificationCoroutine);

            _notificationCoroutine = StartCoroutine(ShowNotification(message, true));
        }

        private IEnumerator ShowNotification(string message, bool isUltraEpic)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
                notificationText.color = isUltraEpic ? Color.yellow : Color.white;
                notificationText.fontStyle = isUltraEpic ? FontStyles.Bold : FontStyles.Normal;
            }

            if (notificationPanel != null)
                notificationPanel.SetActive(true);

            yield return new WaitForSeconds(notificationDuration);

            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }
    }
}