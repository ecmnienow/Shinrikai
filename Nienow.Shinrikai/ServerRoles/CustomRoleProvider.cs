using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.ServerRoles
{
    public class CustomRoleProvider : RoleProvider
    {
        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();

            var list = new Dictionary<string, string>();
            list.Add("everton", "administrator");
            list.Add("kanzi", "teacher");
            list.Add("elisa", "studenty");

            var user =  list.FirstOrDefault(u => u.Key == username && u.Value == roleName);

            if (user.Equals(default(KeyValuePair<string,string>)))
                return false;

            return true;

            //using (var usersContext = new UsersContext())
            //{
            //    var user = usersContext.Users.SingleOrDefault(u => u.UserName == username);
            //    if (user == null)
            //        return false;
            //    return user.UserRoles != null && user.UserRoles.Select(u => u.Role).Any(r => r.RoleName == roleName);
            //}
        }

        public override string[] GetRolesForUser(string username)
        {
            var userMng = new UserManager();
            var user = userMng.GetUser(username);
            
            switch(user.Type)
            {
                case "A":
                    return new string [] { "Administrator" };

                case "T":
                    return new string [] { "Teacher" };

                case "S":
                default:
                    return new string [] { "Studenty" };
            }

            //using (var usersContext = new UsersContext())
            //{
            //    var user = usersContext.Users.SingleOrDefault(u => u.UserName == username);
            //    if (user == null)
            //        return new string[]{};
            //    return user.UserRoles == null ? new string[] { } : user.UserRoles.Select(u => u.Role).Select(u => u.RoleName).ToArray();
            //}

            //return new string[] { "administrator" };
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();

            //using (var usersContext = new UsersContext())
            //{
            //    return usersContext.Roles.Select(r => r.RoleName).ToArray();
            //}
            
            return new string[] { "administrator", "teacher", "studenty" };
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string ApplicationName { get; set; }
    }
}