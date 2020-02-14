using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Dictionary.Models;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using System.Linq;

namespace Nienow.Shinrikai.Controllers
{
    public class DictionaryController : Controller
    {
        #region Managers

        private DictionaryManager DictionaryMng
        {
            get
            {
                if (Session["DictionaryManager"] == null)
                    Session["DictionaryManager"] = new DictionaryManager();

                return Session["DictionaryManager"] as DictionaryManager;
            }
        }

        private MonitoringManager MonitoringMng
        {
            get
            {
                if (Session["MonitoringManager"] == null)
                    Session["MonitoringManager"] = new MonitoringManager();

                return Session["MonitoringManager"] as MonitoringManager;
            }
        }

        private UserManager UserMng
        {
            get
            {
                if (Session["UserManager"] == null)
                    Session["UserManager"] = new UserManager();

                return Session["UserManager"] as UserManager;
            }
        }

        #endregion

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Dicionario", "Pagina inicial", null, HttpContext.Session.SessionID);

            return View();
        }

        public FileContentResult GetImage(int imageId)
        {
            var image = this.DictionaryMng.GetImage(imageId);
            
            return File(image, "image/jpeg");
        }

        public FileContentResult GetAudio(int audioId)
        {
            var image = this.DictionaryMng.GetAudio(audioId);
            
            return File(image, "audio/mpeg");
        }

        public ActionResult Search(string search, ESearchType type)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Dicionario", "Pesquisa", search, HttpContext.Session.SessionID);

            if (string.IsNullOrWhiteSpace(search))
                return View(new GResult() { Term = search, Results = new List<GSearchWord>() });

            var results = this.DictionaryMng.SearchWord(search, type);
            var temp = new List<GSearchWord>();

            temp.AddRange(results.Results.Where(r => r.FoundTerm == search));
            temp.AddRange(results.Results.Where(r => r.FoundTerm.StartsWith(search) && r.FoundTerm != search).OrderBy(t => t.FoundTerm));
            temp.AddRange(results.Results.Where(r => r.FoundTerm.IndexOf(search) > 0).OrderBy(t => t.FoundTerm));

            if (results.Results.Count != temp.Count)
                throw new Exception("Missing results on search.");

            results.Results = temp;

            return View(results);
        }
    }
}