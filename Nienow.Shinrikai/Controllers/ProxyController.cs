using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Nienow.Shinrikai.Controllers
{
    public class ProxyController : ApiController
    {
        public HttpResponseMessage Get(string url)
        {
            HttpWebRequest newRequest = (HttpWebRequest)WebRequest.Create(url);
            newRequest.Method = "GET";
            newRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            newRequest.Host = "chan.sankakucomplex.com";
            newRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:36.0) Gecko/20100101 Firefox/36.0";
            newRequest.Headers.Add("Cookie", "__cfduid=d9a65d492a8bf15b3234929e5a692e9611447451391; pass_hash=4cba22eb11bc8d4a8ebc504c498d116bfadae694; __atuvc=2729%7C22%2C1527%7C23%2C1073%7C24%2C2400%7C25%2C381%7C26; mode=view; locale=en; _sankakucomplex_session=BAh7BzoMdXNlcl9pZGkCnpA6D3Nlc3Npb25faWQiJWNlMzE1MmUxN2YyZTVjMjdiZDNlYjM4NmY5N2U3ZmQ4--678596204188227473bd946d8531947cf58ac58f");

            try
            {
                string html;
                HttpWebResponse response = (HttpWebResponse)newRequest.GetResponse();

                using (var sr = new StreamReader(response.GetResponseStream()))
                    html = sr.ReadToEnd();

                var myResponse = new HttpResponseMessage();
                myResponse.Content = new StringContent(html);
                myResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

                return myResponse;
            }
            catch (WebException ex)
            {
                var myResponse = new HttpResponseMessage();

                var erro = string.Empty;

                if (ex.Response == null)
                {
                    myResponse.StatusCode = (HttpStatusCode)400;
                    myResponse.Content = new StringContent("<html><body>" + ex.ToString() + "</body></html>");
                }

                var statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;

                myResponse.StatusCode = (HttpStatusCode)statusCode;
                myResponse.Content = new StringContent("<html><body>" + ex.ToString() + "</body></html>");
                myResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return myResponse;
            }
        }
    }
}