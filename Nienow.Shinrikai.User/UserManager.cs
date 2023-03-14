using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.User.DB;
using Nienow.Shinrikai.User.Model;
using System;
using System.Text.RegularExpressions;

namespace Nienow.Shinrikai.User
{
    public class UserManager
    {
        #region Attributes and Properties

        public TUser _currentUser;
        public TUser CurrentUser
        {
            get { return this._currentUser; }
            set { this._currentUser = value; }
        }

        #endregion

        #region Private Fields

        private readonly DBUser _db = new DBUser();

        #endregion

        #region Public Methods

        public Response CheckAvailability(string login, string email)
        {
            using (this._db.Connect())
            {
                var freeLogin = this._db.GetUser(login) == null;
                var freeEmail = this._db.GetUserByEmail(email) == null;

                var response = new Response() { OK = true };

                if (!freeLogin)
                {
                    response.OK = false;
                    response.Message = "O login \"" + login + "\" já esta em uso.";
                }
                if (!freeEmail)
                {
                    response.OK = false;
                    if (!string.IsNullOrEmpty(response.Message))
                        response.Message += Environment.NewLine;

                    response.Message += "O email \"" + email + "\" já esta em uso.";
                }

                return response;
            }
        }

        public void DeclareUnderstanding(int userId, int msgId)
        {
            using (this._db.Connect())
                this._db.DeclareUnderstanding(userId, msgId);
        }

        public string GetNotification(int userId, int notificationId, bool force)
        {
            using (this._db.Connect())
            {
                var notification = this._db.GetNotification(userId, notificationId, force);
                return notification;
            }
        }

        public TUser GetUser(string userName)
        {
            using (this._db.Connect())
            {
                var user = this._db.GetUser(userName);
                return user;
            }
        }

        public bool Login(string userName, string password)
        {
            using (this._db.Connect())
            {
                var user = this._db.Login(userName, password);

                if (user != null)
                {
                    this.CurrentUser = user;
                    return true;
                }
            }

            return false;
        }

        public Response SignUp(TUser user)
        {
            if (string.IsNullOrEmpty(user.Name) || user.Name.Length == 0)
                return new Response() { OK = false, Message = "Nome de usuário inválido." };

            if (!Regex.IsMatch(user.Email, @"^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                return new Response() { OK = false, Message = "Email inválido." };

            if (string.IsNullOrEmpty(user.Login) || user.Login.Length == 0)
                return new Response() { OK = false, Message = "Login inválido." };

            if (string.IsNullOrEmpty(user.Password) || user.Password.Length == 0)
                return new Response() { OK = false, Message = "Senha inválida." };

            if (string.IsNullOrEmpty(user.Password) || user.Password.Length == 0)
                return new Response() { OK = false, Message = "Senha inválida." };

            if (string.IsNullOrEmpty(user.RepeatPassword) || user.RepeatPassword.Length == 0)
                return new Response() { OK = false, Message = "A senha repetida não é a mesma de senha informada." };

            using (this._db.Connect())
                this._db.SignUp(user);

            return new Response() { OK = true, Message = "Usuário cadastrado com sucesso!" };
        }

        #endregion
    }
}
