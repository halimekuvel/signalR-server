namespace signalR_server.Models
{
    public class UserBase
    {
        internal object connectionName;

        public string userName { get; set; }
        public string connectionId { get; set; }
    }
}