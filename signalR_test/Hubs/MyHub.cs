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
        // client will use SendMessageAsync to send messages
        // server will use receiveMessage(event on the client)
        public async Task SendMessageAsync(string message)
        {
            // find username
            var userName = "";
            foreach (User usr in clients)
            {
                if (usr.connectionId == Context.ConnectionId)
                    userName = usr.userName;
            }
            await Clients.All.SendAsync("receiveMessage", message, Context.ConnectionId, userName);
        }

        public async Task SendMessageToGroupAsync(string message, string groupName)
        {
            GroupMessageResponse resp = new GroupMessageResponse();
            // find username
            foreach (User usr in clients)
            {
                if (usr.connectionId == Context.ConnectionId)
                    resp = new GroupMessageResponse(message, Context.ConnectionId, usr.userName, groupName);
            }
            await Clients.Group(groupName).SendAsync("receiveGroupMessage", JsonConvert.SerializeObject(resp));
        }

        // when a client connects to the server this method awakes
        public override async Task OnConnectedAsync()
        {
            // notify the users that a client has joined
            // userJoined : an event in the client
            await Clients.Caller.SendAsync("getConnectionId", Context.ConnectionId);
            clients.Add(new User(Context.ConnectionId)); // add to clients list
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
            await Task.Delay(3000);
            
            User disconnectUser = clients.Find(x => String.Equals(x.connectionId, Context.ConnectionId));
            clients.Remove(disconnectUser); // remove from the clients list
            List<string> userNames = new List<string>();
            foreach (User usr in clients)
            {
                if (usr.userName != null)
                    userNames.Add(usr.userName);
            }
            //userNames = clients.Where(o => o.userName != null).Select(o=>o.userName).ToList();
            // delete the user from the groups
            foreach (Group grp in groups)
            {
                foreach (User usr in grp.members)
                {
                    if (usr.connectionId == Context.ConnectionId)
                    {
                        grp.members.Remove(usr);
                        break;
                    }

                }
            }
            await Clients.All.SendAsync("clients", userNames);
            await Clients.All.SendAsync("userLeft", disconnectUser.userName);
        }

        public async Task AddGroup(string connectionId, string groupName)
        {
            var groupAlreadyExists = true;
            if (groups.Count >= 6)
            {
                await Clients.All.SendAsync("groupLimitReached");
                return;
            }
            // if there isn't already a group with that groupName
            // create group and add the creator to the group
            if (!groups.Where(o => o.getGroupName() == groupName).Any())
            {
                groupAlreadyExists = false;

                await Groups.AddToGroupAsync(connectionId, groupName);
                User User = clients.Where(o => o.connectionId == connectionId).FirstOrDefault();
                Group newGroup = new Group(groupName, connectionId);
                newGroup.addMember(User);
                groups.Add(newGroup);
            }
            await Clients.All.SendAsync("checkAddGroup", groupAlreadyExists, groupName, connectionId);
        }

        public async Task JoinGroup(string connectionId, string groupName)
        {
            GroupResponse response = new GroupResponse();
            string userName = "";
            var theGroup = groups.First();
            if (groups.Where(o => o.getGroupName() == groupName).Any()) // if there's a group with that groupName :: aslında gerekli degil ama her ihtimale karsı
            {
                User usr = clients.Where(o => o.connectionId == connectionId).FirstOrDefault();
                userName = usr.userName;
                theGroup = groups.Where(o => o.getGroupName() == groupName).FirstOrDefault();
                // if the user is not already in the group, add the user to the group
                if (!theGroup.members.Contains(usr))
                {
                    await Groups.AddToGroupAsync(connectionId, groupName);
                    theGroup.members.Add(usr);
                }
                response = new GroupResponse
                {
                    ClientId = connectionId,
                    GroupName = groupName,
                    members = theGroup.members,
                    ClienInGroup = theGroup.members.Contains(usr)
                };
            }

            await Clients.Caller.SendAsync("checkJoinGroup", JsonConvert.SerializeObject(response));
            await Clients.Group(groupName).SendAsync("notificationJoinGroup", userName);
        }

        public async Task RemoveGroup(string connectionId, string groupName)
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
        }

        public async Task AddUserName(string userName, string connectionId)
        {
            if (string.IsNullOrEmpty(userName))
            {
                await Clients.Caller.SendAsync("checkUserName", userName);
                return;
            };

            var client = clients.FirstOrDefault(o => o.connectionId == connectionId);
            if (client == null)
            {
                await Clients.Caller.SendAsync("checkUserName", userName);
                return;
            };

            if (clients.Where(o => o.userName == userName).Count() > 0)
            {
                await Clients.Caller.SendAsync("checkUserName", userName);
                return;
            };

            client.userName = userName;
            await Clients.All.SendAsync("userJoined", userName);

            await Clients.All.SendAsync("clients", clients.Where(o => o.userName != null).Select(o => o.userName));



            /*  
                Cem : 
                1- kullanıcı cıkış yaptıgında username bilgisi üstte gözükmeli  (şu an connectionId gozukuyor)
                2- kullanıcı sayfayı yenıledıgınde sistemden çıkış yapıp tekrar giriş yapmakta, bu durumu engellemek amaclı kullanıcı sayfayı yenilediğinde connection
             */

            /*
                Halime : 
                1- Kurulan gruba otomatik olarak üye olunmakta üye olunan grubun adı yeşil yanmalı ve yeni üye olunacak grubun da arka planı yeşil olmalı.
                2- Join grup dendiğinde açılan mesaj kutusu o seçilen grubun mesajlarına özgü olmalı.
                3- Response içerisinde members gitmemekte  -- JSON ile alakalı olabilir. Client tarafında JSON.Parse() ?
             */
        }

    }
}

