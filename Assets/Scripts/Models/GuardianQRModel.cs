using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace ARMonster.Models
{
    /// <summary>
    /// Модель активного QR-кода Хранителя в таблице active_guardian_qrs.
    /// </summary>
    [Table("active_guardian_qrs")]
    public class GuardianQRModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("token")]
        public string Token { get; set; }

        [Column("monster_name")]
        public string MonsterName { get; set; }

        [Column("max_scans")]
        public int MaxScans { get; set; }

        [Column("current_scans")]
        public int CurrentScans { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_by")]
        public string CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
