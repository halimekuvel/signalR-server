using Microsoft.AspNetCore.SignalR;
using signalR_server.Interfaces;
using signalR_server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalR_server.Hubs
{
    public class MyHub : Hub
    {
        static List<User> clients = new List<User>();
        static List<Group> groups = new List<Group>();
        static int groupAmount = 0;
        int firstConnect = 0;
        // client will use SendMessageAsync to send messages
        // server will use receiveMessage(event on the client)
        public async Task SendMessageAsync(string message)
        {
            await Clients.All.SendAsync("receiveMessage", message);
        }

        public async Task SendMessageToGroupAsync(string message, string groupName)
        {
            await Clients.Group(groupName).SendAsync("receiveGroupMessage", message);
        }

        // when a client connects to the server this method awakes
        public override async Task OnConnectedAsync()
        {
            // notify the users that a client has joined
            // userJoined : an event in the client
            await Clients.Caller.SendAsync("getConnectionId", Context.ConnectionId);
            clients.Add(new User(Context.ConnectionId)); // add to clients list
            Console.WriteLine("!!grp amnt: " + groupAmount);
            //if (firstConnect == 0)
            //{
            //    // add default groups
            //    Console.WriteLine("here");
            //    await addGroup("server", "A");
            //    await addGroup("server", "B");
            //    await addGroup("server", "C");
            //    firstConnect = 1;
            //}
            //await Clients.All.SendAsync("clients", clients);
            await Clients.All.SendAsync("userJoined", Context.ConnectionId);
            // after the IMessageClient iterface created 
            // instead of the lines above we can use the lines below:
            //await Clients.All.Clients(clients);
            //await Clients.All.UserJoined(Context.ConnectionId);

            List<string> groupNames = new List<string>();
            var count = 0;
            foreach (Group grp in groups)
            {
                Console.WriteLine("count: " + count);
                count++;
                if (grp.getGroupName() != null)
                    groupNames.Add(grp.getGroupName());
            }
            await Clients.Caller.SendAsync("updateGroups", groupNames);
            Console.WriteLine("------------------");
        }

        // when a client disconnects from the server this method awakes
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // notify the users that a client has left
            // userLeft : an event in the client

            User disconUser = clients.Find(x => String.Equals(x.getConnectionId(), Context.ConnectionId) == true);
            clients.Remove(disconUser); // remove from the clients list

            List<string> userNames = new List<string>();
            foreach(User usr in clients)
            {
                if(usr.getUserName() != null)
                    userNames.Add(usr.getUserName());
            }
            await Clients.All.SendAsync("clients", userNames);
            await Clients.All.SendAsync("userLeft", Context.ConnectionId);
            // after the IMessageClient iterface created 
            // instead of the lines above we can use the lines below:
            //await Clients.All.Clients(clients);
            //await Clients.All.UserLeft(Context.ConnectionId);
        }

        public async Task addGroup(string connectionId, string groupName)
        {
            // adds the client to the group 
            // if the group doesn't exist it creates the group first 
            foreach(Group grp in groups)
            {
                // if group already exists
                if(String.Equals(grp.getGroupName(), groupName) == true)
                {
                    // if user is not already in the group
                    if(grp.getMembers().Contains(connectionId) == false)
                    {
                        //groups.Find(grp).addMember(connectionId);
                        await Groups.AddToGroupAsync(connectionId, groupName);
                    }
                    return;
                }
            }
            if(groupAmount != 6)
            {
                await Groups.AddToGroupAsync(connectionId, groupName);
                groups.Add(new Group(groupName, connectionId));
                groupAmount++;
                List<string> groupNames = new List<string>();
                foreach (Group grp in groups)
                {
                    if (grp.getGroupName() != null)
                        groupNames.Add(grp.getGroupName());
                }
                await Clients.All.SendAsync("checkAddGroupOk", true, groupName);
            }
            else
            {
                await Clients.Caller.SendAsync("checkAddGroupOk", false, "");
            }
           
        }

        public async Task addUserName(string userName, string connectionId)
        {
            clients.Find(x => String.Equals(x.getConnectionId(), connectionId) == true).setUserName(userName);
            List<string> userNames = new List<string>();
            foreach (User usr in clients)
            {
                if (usr.getUserName() != null)
                    userNames.Add(usr.getUserName());
            }
            await Clients.All.SendAsync("clients", userNames);
        }
    }
}
