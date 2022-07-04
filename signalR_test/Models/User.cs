using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Models
{
    public class User
    {
        internal object connectionName;

        public string userName { get; set; }
        public string connectionId { get; set; }
        public User(string connectionId, string userName)
        {
            this.connectionId = connectionId;
            this.userName = userName;
        }
        public User(string connectionId)
        {
            this.connectionId = connectionId;
        }
    }
}
