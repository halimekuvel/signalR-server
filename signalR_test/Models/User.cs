using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Models
{
    public class User
    {
        private string userName;
        private string connectionId;

        public User(string connectionId)
        {
            this.connectionId = connectionId;
            userName = null;
        }

        public void setUserName(string userName)
        {
            this.userName = userName;
        }

        public void setConnectionId(string connectionId)
        {
            this.connectionId = connectionId;
        }

        public string getUserName()
        {
            return userName;
        }

        public string getConnectionId()
        {
            return connectionId;
        }
    }
}
