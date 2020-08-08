using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ScrapySharp.Network;

namespace TCore.Scrappy
{
    public class GenericUPC
    {
        public static string FetchTitleFromUPC(string sCode)
        {
            if (sCode.Length == 13)
                sCode = sCode.Substring(1);

            try
            {
                ScrapingBrowser sbr = new ScrapingBrowser();
                sbr.AllowAutoRedirect = false;
                sbr.AllowMetaRedirect = false;
                sbr.AvoidAsyncRequests = true;

                WebPage wp = sbr.NavigateToPage(new Uri("http://www.searchupc.com/upc/" + sCode));


                HtmlNodeCollection nodes = wp.Html.SelectNodes("//table[@id='searchresultdata']");
                HtmlNodeCollection nodesTr = nodes[0].SelectNodes("tr");

                if (nodesTr == null || nodesTr.Count != 2)
                {
                    return "!!NO TITLE FOUND";
                }

                return nodesTr[1].ChildNodes[1].InnerText;
            }
            catch
            {
                return "!!NO TITLE FOUND";
            }
        }
    }
}