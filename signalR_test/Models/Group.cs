using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Models
{
    public class Group
    {
        private string groupName;
        private string createdByConnId;
        private List<string> members = new List<string>();

        public Group(string groupName, string createdByConnId)
        {
            this.groupName = groupName;
            this.createdByConnId = createdByConnId;
            //members.Add(createdByConnId);
        }

        public string getGroupName()
        {
            return groupName;
        }

        public string getCreatedByConnId()
        {
            return createdByConnId;
        }

        public List<string> getMembers()
        {
            return members;
        }

        public void setGroupName(string groupName)
        {
            this.groupName = groupName;
        }

        public void addMember(string connectionId)
        {
            members.Add(connectionId);
        }

        public void removeMember(string connectionId)
        {
            members.Remove(connectionId);
        }
    }
}
