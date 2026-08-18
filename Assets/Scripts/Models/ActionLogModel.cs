using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace ARMonster.Models
{
    /// <summary>
    /// Модель данных логов действий студентов в таблице Action_Logs.
    /// </summary>
    [Table("action_logs")]
    public class ActionLogModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("action_type")]
        public string ActionType { get; set; }

        [Column("details")]
        public string Details { get; set; }

        [Column("amount")]
        public int Amount { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
