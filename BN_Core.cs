using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;
using System.Xml;
using HtmlAgilityPack;
using ScrapySharp.Network;

namespace TCore.Scrappy.BarnesAndNoble
{
    public class Core
    {
        // this will throw an exception on any errors
        public static WebPage BrowseToSearch(string sCode)
        {
            ScrapingBrowser sbr = new ScrapingBrowser();
            sbr.AllowAutoRedirect = true;
            sbr.AllowMetaRedirect = true;
            sbr.AvoidAsyncRequests = true;
            sbr.AutoDetectCharsetEncoding = false;
            sbr.Encoding = Encoding.UTF8;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WebPage wp = sbr.NavigateToPage(new Uri("https://www.barnesandnoble.com/s/" + sCode));

            return wp;
        }

        public static string GetSimpleStringField(
            string sField,
            string sXPath,
            WebPage wp,
            Sanitize.SanitizeStringDelegate delSanitize,
            ref string sError,
            out bool fSetValue)
        {
            fSetValue = false;
            if (String.IsNullOrEmpty(sField))
            {
                HtmlNode node = wp.Html.SelectSingleNode(sXPath);

                if (node == null)
                {
                    sError = $"Couldn't find XPath({sXPath}: {wp.Html.InnerHtml}";
                    return sField;
                }

                fSetValue = true;
                return delSanitize(node.InnerText);
            }

            return sField;
        }

        static void ExtractTextFromNode(HtmlNode node, StringBuilder sb)
        {
            //            if (node.Name.ToLower() == "b")
            //                sb.Append("*");

            if (node.Name.ToLower() == "u")
                sb.Append("_");

            if (node.HasChildNodes)
            {
                foreach (HtmlNode child in node.ChildNodes)
                    ExtractTextFromNode(child, sb);
            }

            if (node.NodeType == HtmlNodeType.Text)
            {
                string s = node.InnerText;

                if (s == "\n")
                    s = " ";

                if (s == " "
                    && (sb.Length == 0 || Char.IsWhiteSpace(sb[sb.Length - 1])))
                {
                    s = "";
                }

                sb.Append(s);
            }

            if (node.Name.ToLower() == "p")
                sb.Append("\n");

            if (node.Name.ToLower() == "br")
                sb.Append("&#10;");

            if (node.Name.ToLower() == "u")
                sb.Append("_");

            //            if (node.Name.ToLower() == "b")
            //                sb.Append("*");
        }

        public static string GetComplexTextField(
            string sField,
            string sXPath,
            WebPage wp,
            Sanitize.SanitizeStringDelegate delSanitize,
            ref string sError,
            out bool fSetValue)
        {
            fSetValue = false;

            if (String.IsNullOrEmpty(sField))
            {
                HtmlNode node = wp.Html.SelectSingleNode(sXPath);

                if (node == null)
                {
                    sError = "Couldn't find summary";
                    return sField;
                }

                StringBuilder sb = new StringBuilder();
                ExtractTextFromNode(node, sb);

                fSetValue = true;
                return sb.ToString();
            }

            return sField;
        }
    }
}