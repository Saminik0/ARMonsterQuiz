using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace ARMonster.Models
{
    /// <summary>
    /// Модель данных коллекции пойманных монстров в таблице Monster_Collection.
    /// </summary>
    [Table("monster_collection")]
    public class MonsterCollectionModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("monster_id")]
        public string MonsterId { get; set; }

        [Column("captured_at")]
        public DateTime CapturedAt { get; set; }
    }
}
