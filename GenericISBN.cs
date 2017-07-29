using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using ScrapySharp.Network;

namespace TCore.Scrappy
{
    public class GenericISBN
    {
        static string sRequestTemplate = "http://isbndb.com/api/books.xml?access_key={0}&index1=isbn&value1={1}";

        public static string FetchTitleFromISBN13(string sCode, string sIsbnDbAccessKey)
        {
            string sTitle = "!!NO TITLE FOUND";
            string sReq = String.Format(sRequestTemplate, sIsbnDbAccessKey, sCode);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(sReq);
            if (req != null)
                {
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp != null)
                    {
                    Stream stm = resp.GetResponseStream();
                    if (stm != null)
                        {
                        System.Xml.XmlDocument dom = new System.Xml.XmlDocument();

                        try
                            {
                            dom.Load(stm);

                            XmlNode node = dom.SelectSingleNode("/ISBNdb/BookList/BookData/Title");
                            if (node == null)
                                {
                                // try again scraping from bn.com...this is notoriously fragile, so its our last resort.
                                sTitle = "!!NO TITLE FOUND" + sReq + dom.InnerXml; // SScrapeISBN(sIsbn);
                                }
                            else
                                {
                                sTitle = node.InnerText;
                                }
                            }
                        catch (Exception exc)
                            {
                            sTitle = "!!NO TITLE FOUND: (" + exc.Message + ")";
                            }
                        }
                    }
                }

            return sTitle;

        }
    }

}