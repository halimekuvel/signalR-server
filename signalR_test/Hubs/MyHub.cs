using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using signalR_server.Helper;
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
        public static List<User> clients = new List<User>();
        public static List<Group> groups = new List<Group>();
        // client will use SendMessageAsync to send messages
        // server will use receiveMessage(event on the client)
        public async Task SendMessageAsync(string message)
        {
            // find username
            User usr = UserHelper.FindUser(clients, Context.ConnectionId);
            
            //userName = clients.Where(x => x.connectionId == Context.ConnectionId).FirstOrDefault().userName;
            await Clients.All.SendAsync("receiveMessage", message, Context.ConnectionId, usr.Username);
        }
        public async Task SendMessageToUserAsync(string message, string userName, string senderConnId)
        {
            User usr = UserHelper.FindUserByUsername(clients, userName);
            await Clients.Client(usr.ConnectionId).SendAsync("receiveDirectMessage", message, usr.ConnectionId, userName, UserHelper.FindUser(clients, senderConnId).Username);
            await Clients.Caller.SendAsync("receiveDirectMessage", message, usr.ConnectionId, userName);
        }

        public async Task SendMessageToGroupAsync(string message, string groupName)
        {

            GroupMessageResponse resp = new GroupMessageResponse();
            // find username
            User user = UserHelper.FindUser(clients, Context.ConnectionId);
            resp = new GroupMessageResponse { message = message, connectionId = Context.ConnectionId, sender = user.Username, groupName = groupName };

            groups.Where(o => o.getGroupName() == groupName).FirstOrDefault().messages.Add(resp);
            await Clients.Group(groupName).SendAsync("receiveGroupMessage", JsonConvert.SerializeObject(resp));
        }

        public async Task GetPrevGroupMsgs(string groupName)
        {
            await Clients.Caller.SendAsync("receivePrevGroupMsgs", JsonConvert.SerializeObject(groups.Where(o => o.getGroupName() == groupName).FirstOrDefault().messages));
        }

        public async Task GetPrevUserMsgs(string userName)
        {
            await Clients.Caller.SendAsync("receivePrevUserMsgs", JsonConvert.SerializeObject(, userName);
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

            User disconnectUser = clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            clients.Remove(disconnectUser); // remove from the clients list
            List<string> userNames = new List<string>();

            userNames = clients.Where(o => o.Username != null).Select(o => o.Username).ToList();
            // delete the user from the groups
            foreach (Group grp in groups)
            {
                User user = UserHelper.FindUser(grp.members, Context.ConnectionId);
                if (user.ConnectionId == Context.ConnectionId)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, grp.getGroupName());
                    grp.members.Remove(user);
                }
            }
            
            if (disconnectUser != null && disconnectUser.Username != null)
            {
                await Clients.All.SendAsync("userLeft", disconnectUser.Username);
                await Clients.All.SendAsync("clients", clients.Where(o => o.Username != null).Select(o => o.Username));
            }
        }


        public async Task AddGroup(string connectionId, string groupName)
        {
            var groupAlreadyExists = true;
            if (groups.Count >= (int)GroupEnum.maxGroupCount)
            {
                await Clients.All.SendAsync("groupLimitReached");
                return;
            }
            // if there isn't already a group with that groupName
            // create group and add the creator to the group
            if (!GroupHelper.GroupExists(groups, groupName))
            {
                groupAlreadyExists = false;
                await Groups.AddToGroupAsync(connectionId, groupName);
                groups = GroupHelper.AddGroup(groups, groupName, clients, connectionId);
            }
            await Clients.All.SendAsync("checkAddGroup", groupAlreadyExists, groupName, connectionId);
        }

        public async Task JoinGroup(string connectionId, string groupName)
        {
            GroupResponse response = new GroupResponse();
            User usr = new User();
            var theGroup = groups.First();
            if (GroupHelper.GroupExists(groups, groupName)) // if there's a group with that groupName :: aslında gerekli degil ama her ihtimale karsı
            {
                usr = UserHelper.FindUser(clients, connectionId);
                theGroup = GroupHelper.FindGroup(groups, groupName);

                response = new GroupResponse{ClientId = connectionId, GroupName = groupName, members = theGroup.members, ClienInGroup = false};

                // if the user is not already in the group, add the user to the group
                
                if (!UserHelper.UserExists(theGroup.members, usr))
                {
                    await Groups.AddToGroupAsync(connectionId, groupName);
                    theGroup.members.Add(usr);
                    response.ClienInGroup = true;
                }

            }

            await Clients.Caller.SendAsync("checkJoinGroup", JsonConvert.SerializeObject(response));
            await Clients.OthersInGroup(groupName).SendAsync("notificationJoinGroup", usr.Username);
        }
        public async Task LeaveGroup(string connectionId, string groupName)
        {
            User usr = new User();
            var theGroup = groups.First();
            if (GroupHelper.GroupExists(groups, groupName)) // if there's a group with that groupName
            {
                usr = UserHelper.FindUser(clients, connectionId);
                theGroup = GroupHelper.FindGroup(groups, groupName);
                if (UserHelper.UserExists(theGroup.members, usr))
                {
                    await Groups.RemoveFromGroupAsync(connectionId, groupName);
                    theGroup.members.Remove(usr);
                }

            }
            await Clients.Caller.SendAsync("checkLeaveGroup", groupName);
            // there should be another function 
            //await Clients.Group(groupName).SendAsync("notificationJoinGroup", usr.Username);
        }


        public async Task AddUserName(string userName, string connectionId)
        {
            var client = clients.FirstOrDefault(o => o.ConnectionId == connectionId);
            if (string.IsNullOrEmpty(userName) || client == null || clients.Where(o => o.Username == userName).Count() > 0)
            {
                await Clients.Caller.SendAsync("checkUserName", userName);
                return;
            };
            client.Username = userName;

            await Clients.Caller.SendAsync("userJoined", userName);
            await Clients.All.SendAsync("clients", clients.Where(o => o.Username != null).Select(o => o.Username));
            await Clients.Others.SendAsync("notifyUserJoined", userName);

        }

    }
}

