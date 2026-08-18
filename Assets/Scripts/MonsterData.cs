using UnityEngine;

namespace ARMonster.Core
{
    /// <summary>
    /// Данные монстра и вопрос для викторины.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMonster", menuName = "ARMonster/MonsterData")]
    public class MonsterData : ScriptableObject
    {
        [Tooltip("Имя монстра.")]
        public string monsterName;

        [Tooltip("Аватар монстра (иконка или портрет).")]
        public Sprite monsterAvatar;

        [Tooltip("Префаб 3D-монстра, который будет заспавнен при наведении на маркер.")]
        public GameObject monsterPrefab;

        [Tooltip("Стоимость сканирования/появления монстра.")]
        public int spawnCost = 10;

        [Tooltip("Текст вопроса викторины.")]
        public string questionText;

        [Tooltip("Варианты ответов (строго 4 варианта).")]
        public string[] answers = new string[4];

        [Range(0, 3)]
        [Tooltip("Индекс правильного ответа (от 0 до 3).")]
        public int correctAnswerIndex;
    }
}
