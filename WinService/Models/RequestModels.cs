namespace WinService.Models;

public class RequestModels
{
    public class ComputerRequest
    {
        public string Name { get; set; }
        public int Room { get; set; }
        public string Mac { get; set; }
        public string Ip { get; set; }
        public DateTime LastSeen { get; set; }
    }
}