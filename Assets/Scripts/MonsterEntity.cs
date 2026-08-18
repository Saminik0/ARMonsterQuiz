using UnityEngine;

namespace ARMonster.Core
{
    /// <summary>
    /// Динамически добавляемый компонент, хранящий данные монстра на сцене.
    /// Позволяет механике прицеливания понять, на какого именно монстра мы смотрим.
    /// </summary>
    public class MonsterEntity : MonoBehaviour
    {
        public MonsterData Data;
    }
}
