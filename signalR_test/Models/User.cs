using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Models
{
    public class User
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public User(string ConnectionId, string Username)
        {
            this.ConnectionId = ConnectionId;
            this.Username = Username;
        }
        public User() { }
        public User(string ConnectionId)
        {
            this.ConnectionId = ConnectionId;
        }      
    }
}
