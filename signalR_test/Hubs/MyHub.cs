using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using signalR_server.Interfaces;
using signalR_server.Interfaces.Enums;
using signalR_server.Models;
using signalR_server.Response;
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
        static int maxGroupCount = 6;
        int firstConnect = 0;
        // client will use SendMessageAsync to send messages
        // server will use receiveMessage(event on the client)
        public async Task SendMessageAsync(string message)
        {
            // find username
            var userName = "";
            foreach (User usr in clients)
            {
                if (usr.getConnectionId() == Context.ConnectionId)
                    userName = usr.getUserName();
            }
            await Clients.All.SendAsync("receiveMessage", message, Context.ConnectionId, userName);
        }

        public async Task SendMessageToGroupAsync(string message, string groupName)
        {
            // find username
            var userName = "";
            foreach (User usr in clients)
            {
                if (usr.getConnectionId() == Context.ConnectionId)
                    userName = usr.getUserName();
            }
            await Clients.Group(groupName).SendAsync("receiveGroupMessage", message, Context.ConnectionId, userName);
        }

        // when a client connects to the server this method awakes
        public override async Task OnConnectedAsync()
        {
            // notify the users that a client has joined
            // userJoined : an event in the client
            await Clients.Caller.SendAsync("getConnectionId", Context.ConnectionId);
            clients.Add(new User(Context.ConnectionId)); // add to clients list
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
            foreach (Group grp in groups)
            {
                if (grp.getGroupName() != null)
                    groupNames.Add(grp.getGroupName());
            }
            await Clients.Caller.SendAsync("updateGroups", groupNames);
        }

        // when a client disconnects from the server this method awakes
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // notify the users that a client has left
            // userLeft : an event in the client

            User disconUser = clients.Find(x => String.Equals(x.getConnectionId(), Context.ConnectionId));
            clients.Remove(disconUser); // remove from the clients list

            List<string> userNames = new List<string>();
            foreach (User usr in clients)
            {
                if (usr.getUserName() != null)
                    userNames.Add(usr.getUserName());
            }
            await Clients.All.SendAsync("clients", userNames);
            await Clients.All.SendAsync("userLeft", Context.ConnectionId);
            // after the IMessageClient iterface created 
            // instead of the lines above we can use the lines below:
            //await Clients.All.Clients(clients);
            //await Clients.All.UserLeft(Context.ConnectionId);
        }

        //public async Task AddGroup(string connectionId, string groupName)
        //{
        //    bool isGroupExist = false;
        //    //if (groups.Where(o=>o.members.Where(x=>x.getConnectionId() == connectionId).Any()))
        //    //{

        //    //}
        //    if (groups.Where(o => o.getGroupName() == groupName).Any() && groups.Count != 0 )
        //         isGroupExist = true;
        //    else
        //    {
        //        if (groups.Count < (int)GroupEnum.maxGroupCount)
        //        {
        //            await Groups.AddToGroupAsync(connectionId, groupName);
        //            groups.Add(new Group(groupName, connectionId));
        //        }
        //        else
        //            isGroupExist = true;
        //    }
        //    await Clients.All.SendAsync("checkAddGroup", isGroupExist, groupName);
        //}

        public async Task AddGroup(string connectionId, string groupName)
        {
            var groupAlreadyExists = true;
            // if there isn't already a group with that groupName
            // create group and add the creator to the group
            if (!groups.Where(o => o.getGroupName() == groupName).Any())
            {
                groupAlreadyExists = false;

                await Groups.AddToGroupAsync(connectionId, groupName);
                User User = clients.Where(o => o.getConnectionId() == connectionId).FirstOrDefault();
                Group newGroup = new Group(groupName, connectionId);
                newGroup.addMember(User);
                groups.Add(newGroup);
            }
            await Clients.All.SendAsync("checkAddGroup", groupAlreadyExists, groupName, connectionId);
        }

        public async Task JoinGroup(string connectionId, string groupName)
        {
            GroupResponse response = new GroupResponse();
            string userName="";
            var alreadyInGroup = true;
            var theGroup=groups.First();
            if (groups.Where(o => o.getGroupName() == groupName).Any()) // if there's a group with that groupName :: aslında gerekli degil ama her ihtimale karsı
            {
                User usr = clients.Where(o => o.getConnectionId() == connectionId).FirstOrDefault();
                userName = usr.getUserName();
                 theGroup = groups.Where(o => o.getGroupName() == groupName).FirstOrDefault();
                // if the user is not already in the group, add the user to the group
                if (!theGroup.members.Contains(usr))
                {
                    alreadyInGroup = false;
                    await Groups.AddToGroupAsync(connectionId, groupName);
                    theGroup.members.Add(usr);
                }
                response = new GroupResponse
                {
                    ClientId = connectionId,
                    GroupName = groupName,
                    members = theGroup.members,//members gitmemekte bakıyorum çözünce burayı silicem
                    ClienInGroup = theGroup.members.Contains(usr)
                };
            }
           

            await Clients.Caller.SendAsync("checkJoinGroup", JsonConvert.SerializeObject(response), theGroup.members);
            await Clients.Group(groupName).SendAsync("notificationJoinGroup", userName);
        }

        public async Task RemoveGroup(string connectionId, string groupName)
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
        }

        public async Task AddUserName(string userName, string connectionId)
        {
            int returnVal; // -1 : name already taken
                           // 0 : invalid name
                           // 1 : success

            // if userName is already taken
            if(clients.Where(o => o.getUserName() == userName).Any())
            {
                returnVal = -1;
            }
            // userName should only contain letters or digits
            else if(!userName.All(c => Char.IsLetterOrDigit(c)))
            {
                returnVal = 0;
            }
            else
            {
                clients.Find(x => String.Equals(x.getConnectionId(), connectionId)).setUserName(userName);
                List<string> userNames = new List<string>();
                foreach (User usr in clients)
                {
                    if (usr.getUserName() != null)
                        userNames.Add(usr.getUserName());
                }
                returnVal = 1;
                await Clients.All.SendAsync("clients", userNames);
            }

            await Clients.Caller.SendAsync("checkUserName", returnVal, userName);

        }
    }
}
/*
 * 30.06.2022 de silinecek.
                if (String.Equals(grp.getGroupName(), groupName) == true)
                {
                    // if user is not already in the group
                    var client = clients.FirstOrDefault(o => o.getConnectionId() == connectionId);
                    if (!grp.members.Contains(client))
                    {
                        grp.members.Add(client);
                        //groups.Find(grp).addMember(connectionId);
                        await Groups.AddToGroupAsync(connectionId, groupName);
                    }

                    return;
                }
 */
