using System.ComponentModel.DataAnnotations;

namespace WinService.Models;

public class DbModels
{
    public class TabUsers
    {
        [Key]
        public string Id { get; set; } = null!;
        public string Class { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string SecondName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Abbreviation { get; set; } = null!;
    }
    public class TabLessons
    {
        [Key]
        public int Id { get; set; }
        public int Room { get; set; }
        public int Class { get; set; }
        public int Subject { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
    public class TabRooms
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string LongName { get; set; } = null!;
    }
    public class TabComputers
    {
        [Key]
        public string Name { get; set; } = null!;
        public int Room { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
