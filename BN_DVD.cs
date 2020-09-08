using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;
using System.Xml;
using HtmlAgilityPack;
using ScrapySharp.Network;
// ReSharper disable All

namespace TCore.Scrappy.BarnesAndNoble
{

    // ============================================================================
    // D  V  D
    //
    // Scrape DVD information from B&N.
    // ============================================================================
    public class DVD
    {

        // ============================================================================
        // D V D  E L E M E N T
        //
        // Holds all information about the DVD.  When scraping, will fill in anything
        // that isn't already filled out.
        // ============================================================================
        public class DvdElement
        {
            string m_sScanCode;
            string m_sTitle;
            string m_sSummary;
            private string m_sNotes;
            private string m_sQueryUrl;
            private string m_sCoverSrc;
            private string m_sClassification;
            private string m_sMediaType;
            private List<string> m_plsClasses;

            public DvdElement(string sScanCode)
            {
                m_sScanCode = sScanCode;
                m_sTitle = m_sSummary = "";
            }

            public DvdElement()
            {
            }

            // notes are internal only and not scraped from anywhere
            public string Notes
            {
                get { return m_sNotes; }
                set { m_sNotes = value; }
            }

            // internal list of classifications (subjects), used to build the classification string
            public List<string> ClassList
            {
                get { return m_plsClasses; }
                set { m_plsClasses = value; }
            }

            // UPC scan code
            public string ScanCode
            {
                get { return m_sScanCode; }
                set { m_sScanCode = value; }
            }

            public string MediaType
            {
                get { return m_sMediaType; }
                set { m_sMediaType = value; }
            }

            public string QueryUrl
            {
                get { return m_sQueryUrl; }
                set { m_sQueryUrl = value; }
            }

            public string CoverSrc
            {
                get { return m_sCoverSrc; }
                set { m_sCoverSrc = value; }
            }

            public string Classification
            {
                get { return m_sClassification; }
                set { m_sClassification = value; }
            }

            public string Summary
            {
                get { return m_sSummary; }
                set { m_sSummary = value; }
            }
            public string Title
            {
                get { return m_sTitle; }
                set { m_sTitle = value; }
            }
        }

        static bool FUpdateTitle(DvdElement dvd, WebPage wp, ref string sError)
        {
            dvd.Title = Core.GetSimpleStringField(dvd.Title, "//h1[@itemprop='name']", wp, Sanitize.SanitizeTitle, ref sError, out bool fSetValue);
            return fSetValue;
        }

        static bool FUpdateSummary(DvdElement dvd, WebPage wp, ref string sError)
        {
            dvd.Summary = Core.GetComplexTextField(dvd.Summary, "//div[contains(@class,'text--medium overview-content')]", wp, Sanitize.SanitizeSummary, ref sError, out bool fSetValue);
            return fSetValue;
        }

        [Flags]
        public enum ScrapeSet
        {
            Summary = 0x0001,
            CoverSrc = 0x0002,
            Title = 0x0004,
            MediaType = 0x0008,
            Categories = 0x0010
        }

        /*----------------------------------------------------------------------------
        	%%Function: FScrapeDvd
        	%%Qualified: TCore.Scrappy.BarnesAndNoble.DVD.FScrapeDvd
        	%%Contact: rlittle
        	
            Given a DvdElement, go to B&N and scrape all the parts of the DvdElement
            that aren't already filled out.  If anything goes wrong, fill in a
            description in sError and return false.

            (NOTE: just because we failed to scrape certain elements, like subjects,
            we won't return failure. some things won't always be there)
        ----------------------------------------------------------------------------*/
        public static bool FScrapeDvd(DvdElement dvd, out DVD.ScrapeSet set, out string sError)
        {
            string sCode;

            sCode = dvd.ScanCode;
            sError = "";
            set = 0;

            // 0044004783521
            try
            {
                WebPage wp = Core.BrowseToSearch(sCode);

                if (FUpdateTitle(dvd, wp, ref sError))
                    set |= ScrapeSet.Title;

                if (FUpdateSummary(dvd, wp, ref sError))
                    set |= ScrapeSet.Summary;

                if (FUpdateMediaType(dvd, wp, ref sError))
                    set |= ScrapeSet.MediaType;

                // don't fail if we can't get a cover src
                if (FUpdateCoverSrc(dvd, wp))
                    set |= ScrapeSet.CoverSrc;

                // don't fail if we can't get any subjects
                if (FUpdateCategories(dvd, wp))
                    set |= ScrapeSet.Categories;
            }
            catch (Exception exc)
            {
                sError = exc.Message;
                if (exc.InnerException != null)
                    sError += " + " + exc.InnerException.Message;

                return false;
            }

            if (set == 0)
                return false;

            return true;
        }

        class BN_DigitalAdvertData
        {
#pragma warning disable 649
            public class BN_ProductData
            {
                public class BN_CategoryData
                {
                    public string primaryCategory;
                    public List<string> subCategory;
                    public string productType;
                }

                public BN_CategoryData category;
            }
#pragma warning restore 649

            public List<BN_ProductData> product;
        }

        private static bool FUpdateCategories(DvdElement dvd, WebPage wp)
        {
            // to figure out the category, we're going to go scraping the scripts
            // looking for the advertising metadata. they may not care about showing the customer
            // the genre of the dvd, but the care deeply about telling advertisers about what you are 
            // browsing, so that is sure to be up to date

            if (String.IsNullOrEmpty(dvd.Classification))
            {
                HtmlNode node = wp.Html.SelectSingleNode("//script[text()[contains(., 'subCategory')]]");

                if (node == null)
                    return false;

                string sRaw = node.InnerText;

                BN_DigitalAdvertData digitalData = ScrapeScript.ExtractJsonValue<BN_DigitalAdvertData>(sRaw, "var digitalData");

                if (digitalData == null || digitalData.product.Count == 0)
                    return false;

                List<string> subjects = new List<string>();

                foreach (string subject in digitalData.product[0].category.subCategory)
                {
                    subjects.Add(subject);
                }

                if (subjects.Count == 0)
                    return false;

                subjects = Sanitize.SanitizeClassList(subjects);
                if (subjects.Count == 0)
                    return false;

                dvd.Classification = string.Join(",", subjects);
                return true;
            }

            return false;
        }

#if no
        static bool FUpdateRawCoverUrl(BookElement book, WebPage wp, ref string sError)
        {
            if (String.IsNullOrEmpty(book.RawCoverUrl))
            {

                HtmlNode node = wp.Html.SelectSingleNode("//img[@id='pdpMainImage']");

                if (node == null)
                {
                    sError = "Couldn't find release date";
                    return false;
                }

                book.RawCoverUrl = Sanitize.SanitizeCoverUrl(node.Attributes["src"].Value);
                return true;
            }

            return false;
        }
#endif

        private static bool FUpdateCoverSrc(DvdElement dvd, WebPage wp)
        {
            if (String.IsNullOrEmpty(dvd.CoverSrc))
            {
                HtmlNode node = wp.Html.SelectSingleNode("//img[@id='pdpMainImage']");

                if (node == null)
                    return false;

                dvd.CoverSrc = Sanitize.SanitizeCoverUrl(node.GetAttributeValue("src", ""));
                return true;
            }

            return false;
        }


        private static bool FUpdateMediaType(DvdElement dvd, WebPage wp, ref string sError)
        {
            dvd.MediaType = Core.GetSimpleStringField(
                dvd.MediaType,
                "//h2[@id='pdp-info-format']",
                wp,
                Sanitize.SanitizeMediaType,
                ref sError,
                out bool fSetValue);

            return fSetValue;
        }

        private static bool FUpdateSummary2(DvdElement dvd, WebPage wp)
        {
            if (String.IsNullOrEmpty(dvd.Summary))
            {
                HtmlNode nodeSummary = wp.Html.SelectSingleNode("//div[@id='productInfoOverview']");

                if (nodeSummary == null)
                    return false;

                dvd.Summary = Sanitize.SanitizeSummary(nodeSummary.InnerText);
            }

            return true;
        }

        private static bool UpdateTitleFromSummary(HtmlNode nodeSummary, DvdElement dvd, ref string sError)
        {
            // fill in only those parts that are empty
            if (String.IsNullOrEmpty(dvd.Title))
            {
                // title is in the <h1> element
                HtmlNode nodeTitle = nodeSummary.SelectSingleNode("h1");

                // confirm that there is @itemprop='name'
                if (nodeTitle.GetAttributeValue("itemprop", "") != "name")
                {
                    sError = "can't find title. prodsummary[@itemprop != 'name']";
                    return false;
                }

                dvd.Title = Sanitize.SanitizeTitle(nodeTitle.InnerText);
            }

            return true;
        }
    }
}