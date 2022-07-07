using signalR_server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Helper
{
    public static class UserHelper
    {
        public static User FindUserName(string connectionId,string Username) { return new User("",""); }
    }
}
