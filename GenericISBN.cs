using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using Newtonsoft.Json;
using ScrapySharp.Network;

namespace TCore.Scrappy
{
    public class GenericISBN
    {
        //static string sRequestTemplate = "http://isbndb.com/api/books.xml?access_key={0}&index1=isbn&value1={1}";
        static string sRequestTemplate = "https://api.isbndb.com/book/{0}";

        class ISBNBook
        {
            public string publisher { get; set; }
            public string language { get; set; }
            public string overview { get; set; }
            public string title_long { get; set; }
            public string dimensions { get; set; }
            public string dewey_decimal { get; set; }
            public string[] subjects { get; set; }
            public string[] authors { get; set; }
            public string title { get; set; }
            public string isbn13 { get; set; }
            public string isbn { get; set; }
        }

        class ISBNQueryResponse
        {
            public ISBNBook book { get; set; }
        }

        public static string FetchTitleFromISBN13(string sCode, string sIsbnDbAccessKey)
        {
            string sTitle = "!!NO TITLE FOUND";
            string sReq = String.Format(sRequestTemplate, sCode);

            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(sReq);

            req.Headers.Add(String.Format("Authorization: {0}", sIsbnDbAccessKey));
            if (req != null)
            {
                HttpWebResponse resp;

                try
                {
                    resp = (HttpWebResponse) req.GetResponse();
                }
                catch (Exception exc)
                {
                    sTitle = String.Format("!!NO TITLE FOUND: Exception: {0}", exc.Message);
                    resp = null;
                }

                if (resp != null)
                {
                    Stream stm = resp.GetResponseStream();

                    try
                    {
                        if (stm != null)
                        {
                            StreamReader stmr = new StreamReader(resp.GetResponseStream());
                            string sJson = stmr.ReadToEnd();
                            stmr.Close();
                            stm.Close();

                            
                            ISBNQueryResponse qr = JsonConvert.DeserializeObject<ISBNQueryResponse>(sJson);

                            if (qr == null || qr.book.title == null)
                            {
                                // try again scraping from bn.com...this is notoriously fragile, so its our last resort.
                                sTitle = "!!NO TITLE FOUND" + sReq + ":" + sJson;
                            }
                            else
                            {
                                sTitle = qr.book.title;
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        sTitle = "!!NO TITLE FOUND: (" + exc.Message + ")";
                    }
                }
            }

            return sTitle;
        }
    }
}