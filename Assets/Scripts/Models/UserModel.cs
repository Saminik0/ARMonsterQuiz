using Postgrest.Attributes;
using Postgrest.Models;

namespace ARMonster.Models
{
    /// <summary>
    /// Модель данных пользователя (студента) в таблице Users.
    /// </summary>
    [Table("users")]
    public class UserModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("student_number")]
        public string StudentNumber { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("student_group")]
        public string StudentGroup { get; set; }

        [Column("is_approved")]
        public bool IsApproved { get; set; }

        [Column("balance")]
        public int Balance { get; set; }

        [Column("role")]
        public string Role { get; set; } = "student";
    }
}
