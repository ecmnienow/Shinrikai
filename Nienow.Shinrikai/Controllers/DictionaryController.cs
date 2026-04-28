using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Dictionary.Models;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nienow.Shinrikai.Controllers
{
    public class DictionaryController : Controller
    {
        public DictionaryController(DictionaryManager dictionaryManager, MonitoringManager monitoringManager, UserManager userManager)
        {
            this._dictionaryManage = dictionaryManager;
            this._monitoringManager = monitoringManager;
            this._userManager = userManager;
        }

        #region Managers

        private DictionaryManager _dictionaryManage;

        private MonitoringManager _monitoringManager;

        private UserManager _userManager;

        #endregion

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Dicionario", "Pagina inicial", null, HttpContext.Session.Id);

            return View();
        }

        public FileContentResult GetImage(int imageId)
        {
            var image = this._dictionaryManage.GetImage(imageId);

            return File(image, "image/jpeg");
        }

        public FileContentResult GetAudio(int audioId)
        {
            var image = this._dictionaryManage.GetAudio(audioId);

            return File(image, "audio/mpeg");
        }

        public ActionResult Search(string search, ESearchType type)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Dicionario", "Pesquisa", search, HttpContext.Session.Id);

            if (string.IsNullOrWhiteSpace(search))
                return View(new GResult() { Term = search, Results = new List<GSearchWord>() });

            var results = this._dictionaryManage.SearchWord(search, type);
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