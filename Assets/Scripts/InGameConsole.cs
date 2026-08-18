using UnityEngine;
using System.Collections.Generic;

namespace ARMonster.UI
{
    /// <summary>
    /// Простая внутриигровая консоль для вывода логов прямо на экране мобильного телефона.
    /// Позволяет отлаживать AR на реальном устройстве без подключения к Android Logcat.
    /// </summary>
    public class InGameConsole : MonoBehaviour
    {
        private struct LogMessage
        {
            public string message;
            public LogType type;
        }

        private readonly List<LogMessage> _logs = new List<LogMessage>();
        private Vector2 _scrollPosition;
        private bool _showConsole = false;
        private const int MaxLogs = 50;

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            _logs.Add(new LogMessage { message = logString, type = type });
            if (type == LogType.Exception || type == LogType.Error)
            {
                _logs.Add(new LogMessage { message = stackTrace, type = type });
            }

            if (_logs.Count > MaxLogs)
            {
                _logs.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            // Масштабируем размер текста для телефонов с высоким разрешением экрана
            GUI.skin.label.fontSize = 35;
            GUI.skin.button.fontSize = 40;

            if (GUI.Button(new Rect(10, 10, 350, 100), _showConsole ? "Скрыть логи" : "Показать логи"))
            {
                _showConsole = !_showConsole;
            }

            if (!_showConsole) return;

            // Рисуем полупрозрачный фон
            GUI.Box(new Rect(0, 120, Screen.width, Screen.height - 120), "");

            GUILayout.BeginArea(new Rect(10, 120, Screen.width - 20, Screen.height - 130));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            foreach (var log in _logs)
            {
                GUI.contentColor = log.type switch
                {
                    LogType.Error => Color.red,
                    LogType.Exception => Color.red,
                    LogType.Warning => Color.yellow,
                    _ => Color.white
                };
                
                GUILayout.Label($"[{log.type}] {log.message}");
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.contentColor = Color.white;
        }
    }
}
