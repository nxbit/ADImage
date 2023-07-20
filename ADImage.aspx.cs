using System;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;





public partial class ADImage : System.Web.UI.Page
{

    string PID2SAM(string idvalue)
    {
        string connstring = "server=Database.dbo.;database=MiningSwap;Network=DBMSSOCN;integrated security=SSPI; persist security info=false;Trusted_Connection=True;";
        string sqlquery = "SELECT SAMACCOUNTNAME FROM Database.dbo.PROD_WORKERS pw with(nolock) WHERE pw.ENTITYACCOUNT = '" + idvalue + "'";
        DataTable queryresults = new DataTable();
        using (SqlConnection connection = new SqlConnection(connstring))
        {
            using (SqlCommand command = new SqlCommand(sqlquery, connection))
            {
                connection.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(command))
                {
                    da.Fill(queryresults);
                }
                connection.Close();
            }
            string val = "";
            if (queryresults.Rows.Count > 0) { val = queryresults.Rows[0][0].ToString(); }
            return val;

        }
    }
    string PSID2SAM(string idvalue)
    {
        string connstring = "server=Database.dbo.;database=MiningSwap;Network=DBMSSOCN;integrated security=SSPI; persist security info=false;Trusted_Connection=True;";
        string sqlquery = "SELECT SAMACCOUNTNAME FROM Database.dbo.PROD_WORKERS pw with(nolock) WHERE pw.NETIQWORKERID = '" + idvalue + "'";
        DataTable queryresults = new DataTable();
        using (SqlConnection connection = new SqlConnection(connstring))
        {
            using (SqlCommand command = new SqlCommand(sqlquery, connection))
            {
                connection.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(command))
                {
                    da.Fill(queryresults);
                }
                connection.Close();
            }
            string val = "";
            if (queryresults.Rows.Count > 0) { val = queryresults.Rows[0][0].ToString(); }
            return val;

        }
    }
    public string inputCleanup(string input)
    {
        return input.Replace("\n", "").Replace(";", "").Replace("<", "").Replace(">", "").Replace("'", "").Replace("\"", "").Replace("\\\\", "\\");
    }

    private Image idtoTxt(String id)
    {
        Font f = new Font(FontFamily.GenericMonospace, 18, FontStyle.Bold);
        Color txtC = Color.White;
        Color bckC = Color.Black;
        // create dummy img object
        Image img = new Bitmap(1, 1);
        Graphics draw = Graphics.FromImage(img);
        SizeF textSize = draw.MeasureString(id, f);
        //clean up objects
        img.Dispose();
        draw.Dispose();

        // update img and draw with new sizes
        img = new Bitmap((int)textSize.Width, (int)(textSize.Width * .9));
        draw = Graphics.FromImage(img);

        //draw the txt
        draw.Clear(bckC);
        Brush textBrush = new SolidBrush(txtC);
        draw.DrawString(id, f, textBrush, 0, (float)(textSize.Width / 3));
        draw.Save();
        //clean up
        textBrush.Dispose();
        draw.Dispose();
        return img;
    }

    private void TexttoPage(string text)
    {
        Font f = new Font(FontFamily.GenericSansSerif, 18, FontStyle.Bold);
        MemoryStream ms = new MemoryStream();
        Image img = idtoTxt(text);
        img.Save(ms, ImageFormat.Jpeg);
        byte[] bb = ms.ToArray();

        Response.ContentType = "image/png";
        Response.Clear();
        Response.BufferOutput = true;

        Response.BinaryWrite(bb);
        Response.End();
    }

    string sAM = "";
    string pID = "";
    string pSID = "";


    protected void Page_Load(object sender, EventArgs e)
    {

        /*
            112420 - Added sAM Token
            120320 - Added pID, and pSID Tokens
            120420 - Added inputCleanup function
            120420 - Added when return is Null or <> Length Requirements

            NOTE:: We prob want to add in a SAM input check. Here we are assuming the passed SAM is valid


         */
        sAM = "";
        pID = "";
        pSID = "";
        if (!String.IsNullOrEmpty(Request.QueryString["sAM"]))
        {
            sAM = inputCleanup(Request.QueryString["sAM"].ToString());
        }
        else if (!String.IsNullOrEmpty(Request.QueryString["pID"]))
        {
            pID = inputCleanup(Request.QueryString["pID"].ToString());

            if (!(pID.Length == 8) || String.IsNullOrEmpty(PID2SAM(pID))) { TexttoPage(pID); } else { try { sAM = PID2SAM(pID); } catch { TexttoPage("No Picture"); } };
        }
        else if (!String.IsNullOrEmpty(Request.QueryString["pSID"]) || String.IsNullOrEmpty(PSID2SAM(pSID)))
        {
            pSID = inputCleanup(Request.QueryString["pSID"].ToString());

            if (!(pSID.Length == 7)) { TexttoPage(pSID); } else { try { sAM = PSID2SAM(pSID); } catch { TexttoPage("No Picture"); } };
        }
        else
        {
            sAM = User.Identity.Name.Split('\\')[1];
        }




        Response.ContentType = "image/png";
        Response.Clear();
        Response.BufferOutput = true;



        SearchResult user = null;





        //  Confirm we have a reasonable value to search
        if (sAM.Length > 0)
        {
            DirectoryEntry de = new DirectoryEntry();
            de.Path = "LDAP://OU=Accounts,OU=SPECTRUM,DC=CORP,DC=DOMAINCONTROLLER,DC=com";


            DirectorySearcher search = new DirectorySearcher();
            search.SearchRoot = de;
            search.ClientTimeout = TimeSpan.FromMilliseconds(600);
            search.Filter = "(&(samaccountname=" + sAM + "))";
            search.PropertiesToLoad.Add("samaccountname");
            search.PropertiesToLoad.Add("thumbnailPhoto");

            //  Update for the first found AD account
            user = search.FindOne();

            try
            {
                var userimg = (byte[])user.Properties["thumbnailPhoto"][0];
            }
            catch { TexttoPage("No Picture"); }

            //Render Image
            Response.BinaryWrite((byte[])user.Properties["thumbnailPhoto"][0]);
            Response.End();



            //  Cleanup
            search.Dispose(); search = null;
            de.Dispose(); de = null;
        }
        else if (user == null)
        {
            TexttoPage("No Picture");
        }
        else
        {
            try
            {
                Response.BinaryWrite((byte[])user.Properties["thumbnailPhoto"][0]);
                Response.Flush();
            }
            catch
            {

                TexttoPage("No Profile");

            }
        }

    }
}