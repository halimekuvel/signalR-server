using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Response
{
    public class GroupMessageResponse
    {
        public string message;
        public string connectionId;
        public string sender;
        public string groupName;

        public GroupMessageResponse(string message, string connectionId, string sender, string groupName)
        {
            this.message = message;
            this.connectionId = connectionId;
            this.sender = sender;
            this.groupName = groupName;
        }
        public GroupMessageResponse() { }
        //Halime Düzenleme gerekecek
    }
}
