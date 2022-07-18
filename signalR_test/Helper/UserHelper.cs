using signalR_server.Hubs;
using signalR_server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Helper
{
    public static class UserHelper
    {
        public static User FindUser(List<User> clients, string connectionId)
        { return clients.Where(x => x.ConnectionId == connectionId).FirstOrDefault(); }

        public static bool UserExists(List<User> users, User usr)
        { return users.Contains(usr); }

       
    }
}
