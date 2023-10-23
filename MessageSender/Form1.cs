using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using MessageSender.Properties;

namespace MessageSender
{
    public partial class FormMain : Form
    {
        #region Variables
        private static readonly DateTime startUpTime = DateTime.Now;

        internal ConnectionType connType;
        bool Secure;

        internal ConnectionConstructor connection;
        internal ConnectionPreparer connPreparer = null;
        internal ConnectionResponse connResponse = null;

        private WebHeaderCollection HttpHeaders = new WebHeaderCollection();
        private RestrictedHeaderCollection RestrictedHeaders = new RestrictedHeaderCollection();

        private DataTable HttpHeadersDT = new DataTable();
        private readonly List<string> HttpReqHeaders = new List<string>()
        {
            "Cache-Control", "Keep-Alive", "Pragma", "Trailer",
            "Upgrade", "Via", "Warning", "Allow", "Content-Encoding",
            "Content-Language", "Content-Location","Content-MD5","Content-Range","Expires","Last-Modified",
            "Accept-Charset","Accept-Encoding","Accept-Language","Authorization","Cookie",
            "From","If-Match","If-None-Match","If-Range","If-Unmodified-Since",
            "Max-Forwards","Proxy-Authorization","TE","Translate",

            // Restricted headers
            "Accept","Connection","Content-Length","Content-Type","Date","Expect","Host",
            "If-Modified-Since","Range","Referer","Transfer-Encoding","User-Agent"
        };
        #endregion Variables

        public FormMain()
        {
            // UI
            InitializeComponent();

            LogMessage(LogTypes.INFO, "Initialize Completed");

            // Defaut to Http
            //radioBtnSecureTrue.Checked = true;
            //radioBtnHttp.Checked = true;
            //chkBoxHttpInfo.Checked = true;

            // Table for HTTP Headers
            HttpHeadersDT.TableName = "HttpHeaders";
            HttpHeadersDT.Columns.Add("Header", Type.GetType("System.String"));
            HttpHeadersDT.Columns.Add("Value", Type.GetType("System.String"));
            HttpHeadersDT.Columns.Add("Delete", Type.GetType("System.String"));

            dataGridViewHttpHeaders.DataSource = HttpHeadersDT;

            foreach (string header in HttpReqHeaders)
            {
                comboHeader.Items.Add(header);
            }

            // Other Content Types
            foreach (string contentType in MIME.ContentTypes)
            {
                comboBoxOtherContentTypes.Items.Add(contentType);
            }

            // Default Send
            radioBtnMsgTabTextBox.Checked = true;
            radioBtnTypePlainZip.Checked = true;

            // Disable Download File
            btnFileDownload.Enabled = false;

            GetSettings();

            // Logs
            Logging.LogFileName = "MS_" + startUpTime.ToString("dd-MM-yyyy HH.mm") + ".log";
            Logging.LogFilePath = Logging.DEFAULT_DIRECTORY;
            textBoxLogPath.Text = Logging.DEFAULT_DIRECTORY;

            LogMessage(LogTypes.INFO, "Default Log Path Set");
            LogMessage(LogTypes.INFO, "User Settings Loaded");
        }
        #region Form Events

        #region Click
        private void btnConnectionConstructor_Click(object sender, EventArgs e)
        {
            bool isValid = true;
            ushort? port = null;

            // Check if variables are valid
            if (string.IsNullOrEmpty(txtHost.Text))
            {
                isValid = false;
                LogMessage(LogTypes.ERROR, "Host is empty");
            }
            if (!string.IsNullOrEmpty(txtPort.Text))
            {
                if (ushort.TryParse(txtPort.Text, out ushort parsedPort))
                {
                    port = parsedPort;
                }
                else
                {
                    isValid = false;
                    LogMessage(LogTypes.ERROR, "Invalid Port");
                }
            }

            if (radioBtnSecureTrue.Checked) Secure = true;
            else if (radioBtnSecureFalse.Checked) Secure = false;

            if (isValid == true)
            {
                try
                {
                    // Create connection
                    switch (connType)
                    {
                        case ConnectionType.Socket:
                            connection = new SocketRequest(Secure, txtHost.Text, port);
                            break;
                        case ConnectionType.HttpSocket:
                            connection = new HttpSocketRequest(Secure, txtHost.Text, port);
                            break;
                        case ConnectionType.Http:
                            connection = new HttpRequest(Secure, txtHost.Text, port);
                            break;
                        case ConnectionType.AWS:
                            connection = new AWSRequest(Secure, txtHost.Text, port);
                            break;
                        default:
                            LogMessage(LogTypes.ERROR, "Invalid Connection Type");
                            return;
                    }

                    // Save Settings - ConType, Secure, Host, Port
                    if (radioBtnSocket.Checked) Settings.Default.ConnType = "Socket";
                    else if (radioBtnHttpSocket.Checked) Settings.Default.ConnType = "HttpSocket";
                    else if (radioBtnHttp.Checked) Settings.Default.ConnType = "Http";
                    else if (radioBtnAWS.Checked) Settings.Default.ConnType = "AWS";
                    if (radioBtnSecureTrue.Checked) Settings.Default.Secure = true;
                    else if (radioBtnSecureFalse.Checked) Settings.Default.Secure = false;
                    Settings.Default.txtHost = txtHost.Text;
                    Settings.Default.txtPort = txtPort.Text;
                    Settings.Default.Save();

                    // Enable/Disable Buttons
                    btnConnectionConstructor.Enabled = false;

                    groupBoxConnVar.Enabled = false;
                    groupBoxConnType.Enabled = false;
                    btnConnectionPreparer.Enabled = true;

                    LogMessage(LogTypes.INFO, "[ConstructConnectionInfo] :: Connection Created For :: "
                        + $"[Secure: {Secure}, Host: {txtHost.Text}, Port: {port}]");
                }
                catch (Exception ex)
                {
                    LogMessage(LogTypes.ERROR, "[ConstructConnectionInfo] :: " + ex);
                }
            }
        }

        private void btnCloseConn_Click(object sender, EventArgs e)
        {
            // Close connection and dispose ConnectionPreparer
            if (connPreparer != null)
            {
                if (connPreparer.Close())
                {
                    connPreparer = null;
                }
            }

            // Reset connection objects
            connection = null;

            // Enable/Disable Buttons
            btnConnectionConstructor.Enabled = true;

            groupBoxConnVar.Enabled = true;
            groupBoxConnType.Enabled = true;
            btnConnectionPreparer.Enabled = false;
            btnFileDownload.Enabled = false;

            txtTimeout.Enabled = false;

            LogMessage(LogTypes.INFO, "Connection Reset");
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(comboHeader.Text) && !string.IsNullOrEmpty(txtHeaderValue.Text))
            {
                if (HttpHeadersDT.Select().ToList().Exists(row => row["Header"].ToString() == comboHeader.Text))
                {
                    LogMessage(LogTypes.ERROR, "Header must be unique!");
                }
                else
                {
                    HttpHeadersDT.Rows.Add(comboHeader.Text, txtHeaderValue.Text, "Delete");
                    comboHeader.Items.Remove(comboHeader.Text);

                    comboHeader.Text = "";
                    txtHeaderValue.Text = "";
                }
            }
            else
            {
                LogMessage(LogTypes.ERROR, "Header and Value must not be empty!");
            }
        }

        private void dataGridViewHttpHeaders_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0)
            {
                comboHeader.Items.Add(HttpHeadersDT.Rows[e.RowIndex]["Header"]);
                HttpHeadersDT.Rows.Remove(HttpHeadersDT.Rows[e.RowIndex]);
            }
        }

        #region Additional Connection Info
        private HttpInfo? CreateHttpInfo()
        {
            HttpInfo httpInfo = new HttpInfo();

            if (chkBoxHttpInfo.Checked)
            {
                if (string.IsNullOrEmpty(txtObjectPath.Text) || string.IsNullOrEmpty(txtHttpMethod.Text))
                {
                    LogMessage(LogTypes.ERROR, "Http Object Path and Http Method requires values!");
                    return null;
                }

                httpInfo.ObjectPath = txtObjectPath.Text;
                httpInfo.HttpMethod = txtHttpMethod.Text;

                HttpHeaders.Clear();
                foreach (DataRow row in HttpHeadersDT.Rows)
                {
                    switch (row["Header"].ToString())
                    {
                        case "Accept": RestrictedHeaders.Accept = row["Value"].ToString(); break;
                        case "Connection": RestrictedHeaders.Connection = row["Value"].ToString(); break;
                        case "Content-Length": RestrictedHeaders.ContentLength = (long)Convert.ToDouble(row["Value"].ToString()); break;
                        case "Content-Type": RestrictedHeaders.ContentType = row["Value"].ToString(); break;
                        case "Date": RestrictedHeaders.Date = DateTime.Parse(row["Value"].ToString()); break;
                        case "Expect": RestrictedHeaders.Expect = row["Value"].ToString(); break;
                        case "Host": RestrictedHeaders.Host = row["Value"].ToString(); break;
                        case "If-Modified-Since": RestrictedHeaders.IfModifiedSince = DateTime.Parse(row["Value"].ToString()); break;
                        case "Range": RestrictedHeaders.Range = row["Value"].ToString(); break;
                        case "Referer": RestrictedHeaders.Referer = row["Value"].ToString(); break;
                        case "Transfer-Encoding": RestrictedHeaders.TransferEncoding = row["Value"].ToString(); break;
                        case "User-Agent": RestrictedHeaders.UserAgent = row["Value"].ToString(); break;
                        default:
                            HttpHeaders.Add(row["Header"].ToString(), row["Value"].ToString()); break;
                    }
                }
                httpInfo.HttpHeaders = HttpHeaders;
                httpInfo.RestrictedHeaders = RestrictedHeaders;
            }

            return httpInfo;
        }

        private AWSInfo? CreateAWSInfo()
        {
            AWSInfo awsInfo = new AWSInfo();

            if (chkBoxAWSInfo.Checked)
            {
                if (string.IsNullOrEmpty(txtTokenURL.Text) || string.IsNullOrEmpty(txtScope.Text) || string.IsNullOrEmpty(txtMachId.Text))
                {
                    LogMessage(LogTypes.ERROR, "AWS Information variables cannot be empty!");
                    return null;
                }

                awsInfo = new AWSInfo()
                {
                    Scope = txtScope.Text,
                    MachId = txtMachId.Text,
                    TokenURL = txtTokenURL.Text
                };
            }

            return awsInfo;
        }
        #endregion Additional Connection Info

        private void btnConnectionPreparer_Click(object sender, EventArgs e)
        {
            // Reset ConnectionPreparer & Button
            connPreparer = null;
            btnSend.Enabled = false;
            btnSendAsync.Enabled = false;

            // Create HttpInfo & AWSInfo objects
            HttpInfo? httpInfo = CreateHttpInfo();
            AWSInfo? awsInfo = CreateAWSInfo();

            if (chkBoxHttpInfo.Checked && !httpInfo.HasValue)
            {
                return;
            }

            if (chkBoxAWSInfo.Checked && !awsInfo.HasValue)
            {
                return;
            }

            // Create ConnectionPreparer
            switch (connType)
            {
                case ConnectionType.Socket:
                    connPreparer = connection.Create(this);
                    break;
                case ConnectionType.HttpSocket:
                    if (chkBoxHttpInfo.Checked)
                    {
                        connPreparer = connection.Create(this, httpInfo.Value);
                    }
                    else
                    {
                        connPreparer = connection.Create(this);
                    }
                    break;
                case ConnectionType.Http:
                    if (chkBoxHttpInfo.Checked)
                    {
                        connPreparer = connection.Create(this, httpInfo.Value);
                    }
                    else
                    {
                        connPreparer = connection.Create(this);
                    }
                    break;
                case ConnectionType.AWS:
                    if (chkBoxAWSInfo.Checked && chkBoxHttpInfo.Checked)
                    {
                        connPreparer = connection.Create(this, httpInfo.Value, awsInfo.Value);
                    }
                    else if (chkBoxAWSInfo.Checked && !chkBoxHttpInfo.Checked)
                    {
                        connPreparer = connection.Create(this, awsInfo.Value);
                    }
                    else
                    {
                        LogMessage(LogTypes.ERROR, "AWS connection requires AWSInfo object");
                        return;
                    }
                    break;
                default:
                    LogMessage(LogTypes.ERROR, "[PrepareConnection] :: Invalid ConnectionPreparer Type");
                    return;
            }

            // Save Settings - HttpInfo, AWSInfo, TokenPath, Scope, MachId, TokenURL, ObjectPath, Timeout, HttpMethod, HttpHeadersDT
            if (chkBoxHttpInfo.Checked) Settings.Default.HttpInfo = true;
            else Settings.Default.HttpInfo = false;
            if (chkBoxAWSInfo.Checked) Settings.Default.AWSInfo = true;
            else Settings.Default.AWSInfo = false;
            Settings.Default.txtTokenURL = txtTokenURL.Text;
            Settings.Default.txtScope = txtScope.Text;
            Settings.Default.txtMachId = txtMachId.Text;
            Settings.Default.txtObjectPath = txtObjectPath.Text;
            Settings.Default.txtHttpMethod = txtHttpMethod.Text;

            StringWriter writer = new StringWriter();
            HttpHeadersDT.WriteXml(writer);
            Settings.Default.HttpHeadersDT = writer.ToString();

            Settings.Default.Save();

            btnSend.Enabled = true;
            btnSendAsync.Enabled = true;
            txtTimeout.Enabled = true;

            txtTimeout.Text = connPreparer.Timeout.ToString();

            // Log Connection
            LogMessage(LogTypes.INFO, "[PrepareConnection] :: Connection Prepared");

            // Log Http Info
            if (chkBoxHttpInfo.Checked)
            {
                string logMsg = "[PrepareConnection] :: [Http Info] :: [ObjectPath: " + httpInfo.Value.ObjectPath + ", HttpMethod: " + httpInfo.Value.HttpMethod + ", HttpHeaders: {";
                foreach (DataRow row in HttpHeadersDT.Rows)
                {
                    logMsg += row["Header"].ToString() + ": " + row["Value"].ToString() + ", ";
                }

                if (HttpHeadersDT.Rows.Count > 0)
                {
                    logMsg = logMsg.Remove(logMsg.Length - 2);
                }

                logMsg += "}]";
                LogMessage(LogTypes.INFO, logMsg);
            }

            // Log AWS Info
            if (chkBoxAWSInfo.Checked)
            {
                string logMsg = $"[PrepareConnection] :: [AWS Info] :: [Scope: {awsInfo.Value.Scope}, TokenURL: {awsInfo.Value.TokenURL}, MachId: {awsInfo.Value.MachId}]";
                LogMessage(LogTypes.INFO, logMsg);
            }
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            // Reset Response
            connResponse = null;
            txtRespDisplay.Text = String.Empty;

            string sentReturnMsg = "";

            if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageMessage"])
            {
                string Message = "";
                if (radioBtnMsgTabTextBox.Checked == true)
                {
                    if (radioBtnSocket.Checked || !chkBoxHttpInfo.Checked || !(txtHttpMethod.Text == "GET"))
                    {
                        if (!CheckMessageValid(out string errMsg))
                        {
                            LogMessage(LogTypes.ERROR, errMsg);
                            return;
                        }
                    }

                    Message = txtMessage.Text;
                }
                else if (radioBtnMsgTabFile.Checked == true)
                {
                    if (!CheckFileValid(txtMessageFileLoc, out string errMsg))
                    {
                        LogMessage(LogTypes.ERROR, errMsg);
                        return;
                    }

                    Message = File.ReadAllText(txtMessageFileLoc.Text);
                }

                MessageFormat MsgFormat;
                if (radioBtnTypePlainZip.Checked) MsgFormat = MessageFormat.Plaintext;
                else if (radioBtnTypeXML.Checked) MsgFormat = MessageFormat.XML;
                else if (radioBtnTypeJSON.Checked) MsgFormat = MessageFormat.JSON;
                else if (radioBtnTypeSOAP.Checked) MsgFormat = MessageFormat.SOAP;
                else if (CheckOtherContentTypeUsed()) MsgFormat = MessageFormat.Other;
                else
                {
                    LogMessage(LogTypes.ERROR, "Invalid Message Type");
                    return;
                }

                connResponse = connPreparer.SendMessage(Message, MsgFormat, comboBoxOtherContentTypes.Text);

                if (connResponse == null)
                {
                    LogMessage(LogTypes.ERROR, "[Send Message] :: Unable to retrieve response");
                }

                // Save Settings - Message Source, MsgFileType, txtMessage, txtMessageLoc, SendType
                Settings.Default.SendType = "Message";
                if (radioBtnMsgTabTextBox.Checked) Settings.Default.MessageSource = "TextBox";
                else if (radioBtnMsgTabFile.Checked) Settings.Default.MessageSource = "File";
                Settings.Default.txtMessage = txtMessage.Text;
                Settings.Default.txtMessageLoc = txtMessageFileLoc.Text;

                sentReturnMsg = $"[Send Message] :: [Message Content Sent]\n>>>\n{Message}\n<<<";
            }
            else if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageFile"])
            {
                if (!CheckFileValid(txtFileLoc, out string errMsg))
                {
                    LogMessage(LogTypes.ERROR, errMsg);
                    return;
                }

                FileStream fs = File.Open(txtFileLoc.Text, FileMode.Open);

                FileFormat fileFormat;
                if (radioBtnTypePlainZip.Checked) fileFormat = FileFormat.ZIP;
                else if (radioBtnTypeXML.Checked) fileFormat = FileFormat.XML;
                else if (radioBtnTypeJSON.Checked) fileFormat = FileFormat.JSON;
                else if (radioBtnTypeSOAP.Checked) fileFormat = FileFormat.SOAP;
                else if (CheckOtherContentTypeUsed()) fileFormat = FileFormat.Other;
                else
                {
                    LogMessage(LogTypes.ERROR, "Invalid Message Type");
                    return;
                }

                connResponse = connPreparer.SendFile(fs, fileFormat, comboBoxOtherContentTypes.Text);

                if (connResponse == null)
                {

                    LogMessage(LogTypes.ERROR, "[Send File] :: Unable to retrieve response");
                }

                // Save Settings - MsgFileType, txtFileLoc, sendType
                Settings.Default.SendType = "File";
                Settings.Default.txtFileLoc = txtFileLoc.Text;

                fs.Position = 0;
                sentReturnMsg = $"[Send File] :: [Sent File Path] :: {fs.Name}";
                fs.Close();
            }

            btnSend.Enabled = false;
            btnSendAsync.Enabled = false;

            // Make log for updating process to user
            string contentType = String.Empty;
            string logMsg = String.Empty;

            if (radioBtnTypeXML.Checked) contentType += "[XML]";
            else if (radioBtnTypeJSON.Checked) contentType += "[JSON]";
            else if (radioBtnTypeSOAP.Checked) contentType += "[SOAP]";
            else if (CheckOtherContentTypeUsed()) contentType += $"[{comboBoxOtherContentTypes.Text}]";

            if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageMessage"])
            {
                if (radioBtnTypePlainZip.Checked) contentType += "[PlainText]";
                logMsg = $"[Send Message] :: {contentType} :: {(connResponse != null ? "Message Sent!" : "Failed to send message")}";
            }
            else if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageFile"])
            {
                if (radioBtnTypePlainZip.Checked) contentType += "[ZIP]";
                logMsg = $"[Send File] :: {contentType} :: {(connResponse != null ? "File Sent!" : "Failed to send file")}";
            }

            // Log for update progress and sent content
            LogMessage(LogTypes.INFO, logMsg);
            if (connResponse != null)
            {
                LogMessage(LogTypes.INFO, sentReturnMsg);
            }

            // Display Response
            DisplayResponse();

            // Save Settings
            SaveMediaTypeSettings();
        }

        private async void buttonSendAsync_Click(object sender, EventArgs e)
        {
            // Reset Response
            connResponse = null;
            txtRespDisplay.Text = String.Empty;

            string sentReturnMsg = "";

            if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageMessage"])
            {
                string Message = "";
                if (radioBtnMsgTabTextBox.Checked == true)
                {
                    if (radioBtnSocket.Checked || !chkBoxHttpInfo.Checked || !(txtHttpMethod.Text == "GET"))
                    {
                        if (!CheckMessageValid(out string errMsg))
                        {
                            LogMessage(LogTypes.ERROR, errMsg);
                            return;
                        }
                    }

                    Message = txtMessage.Text;
                }
                else if (radioBtnMsgTabFile.Checked == true)
                {
                    if (!CheckFileValid(txtMessageFileLoc, out string errMsg))
                    {
                        LogMessage(LogTypes.ERROR, errMsg);
                        return;
                    }

                    Message = File.ReadAllText(txtMessageFileLoc.Text);
                }

                MessageFormat MsgFormat;
                if (radioBtnTypePlainZip.Checked) MsgFormat = MessageFormat.Plaintext;
                else if (radioBtnTypeXML.Checked) MsgFormat = MessageFormat.XML;
                else if (radioBtnTypeJSON.Checked) MsgFormat = MessageFormat.JSON;
                else if (radioBtnTypeSOAP.Checked) MsgFormat = MessageFormat.SOAP;
                else if (CheckOtherContentTypeUsed()) MsgFormat = MessageFormat.Other;
                else
                {
                    LogMessage(LogTypes.ERROR, "Invalid Message Type");
                    return;
                }

                connResponse = await connPreparer.SendMessageAsync(Message, MsgFormat, comboBoxOtherContentTypes.Text);
                if (connResponse == null)
                {
                    LogMessage(LogTypes.ERROR, "[Send Async Message] :: Unable to retrieve response");
                }

                // Save Settings - Message Source, MsgFileType, txtMessage, txtMessageLoc, sendType
                Settings.Default.SendType = "Message";
                if (radioBtnMsgTabTextBox.Checked) Settings.Default.MessageSource = "TextBox";
                else if (radioBtnMsgTabFile.Checked) Settings.Default.MessageSource = "File";
                Settings.Default.txtMessage = txtMessage.Text;
                Settings.Default.txtMessageLoc = txtMessageFileLoc.Text;

                sentReturnMsg = $"[Send Async Message] :: [Message Content Sent]\n>>>\n{Message}\n<<<";
            }
            else if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageFile"])
            {
                if (!CheckFileValid(txtFileLoc, out string errMsg))
                {
                    LogMessage(LogTypes.ERROR, errMsg);
                    return;
                }

                FileStream fs = File.Open(txtFileLoc.Text, FileMode.Open);

                FileFormat fileFormat;
                if (radioBtnTypePlainZip.Checked) fileFormat = FileFormat.ZIP;
                else if (radioBtnTypeXML.Checked) fileFormat = FileFormat.XML;
                else if (radioBtnTypeJSON.Checked) fileFormat = FileFormat.JSON;
                else if (radioBtnTypeSOAP.Checked) fileFormat = FileFormat.SOAP;
                else if (CheckOtherContentTypeUsed()) fileFormat = FileFormat.Other;
                else
                {
                    LogMessage(LogTypes.ERROR, "Invalid Message Type");
                    return;
                }

                connResponse = await connPreparer.SendFileAsync(fs, fileFormat, comboBoxOtherContentTypes.Text);
                if (connResponse == null)
                {
                    LogMessage(LogTypes.ERROR, "[Send Async File] :: Unable to retrieve response");
                }

                // Save Settings - MsgFileType, txtFileLoc, SendType
                Settings.Default.SendType = "File";
                Settings.Default.txtFileLoc = txtFileLoc.Text;

                fs.Position = 0;
                sentReturnMsg = $"[Send Async File] :: [Sent File Path] :: {fs.Name}";
                fs.Close();
            }

            btnSend.Enabled = false;
            btnSendAsync.Enabled = false;

            // Make log for updating process to user
            string contentType = String.Empty;
            string logMsg = String.Empty;

            if (radioBtnTypeXML.Checked) contentType += "[XML]";
            else if (radioBtnTypeJSON.Checked) contentType += "[JSON]";
            else if (radioBtnTypeSOAP.Checked) contentType += "[SOAP]";
            else if (CheckOtherContentTypeUsed()) contentType += $"[{comboBoxOtherContentTypes.Text}]";

            if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageMessage"])
            {
                if (radioBtnTypePlainZip.Checked) contentType += "[PlainText]";
                logMsg = $"[Send Async Message] :: {contentType} :: {(connResponse != null ? "Message Sent!" : "Failed to send message")}";
            }
            else if (tabControlSend.SelectedTab == tabControlSend.TabPages["tabPageFile"])
            {
                if (radioBtnTypePlainZip.Checked) contentType += "[ZIP]";
                logMsg = $"[Send Async File] :: {contentType} :: {(connResponse != null ? "File Sent!" : "Failed to send file")}";
            }

            // Log for update progress and sent content
            LogMessage(LogTypes.INFO, logMsg);
            if (connResponse != null)
            {
                LogMessage(LogTypes.INFO, sentReturnMsg);
            }

            // Display Response
            DisplayResponse();

            // Save Settings
            SaveMediaTypeSettings();
        }

        private void btnMessageLoc_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = @"C:\";
            OFD.Title = "Browsing Files";

            OFD.CheckFileExists = true;
            OFD.CheckPathExists = true;

            OFD.DefaultExt = ".txt";
            OFD.Filter = "Text files (.txt)|*.txt|JSON (.json)|*.json|XML/SOAP (.xml)|*.xml|Log files (.log)|*.log|All files (*.*)|*.*";
            OFD.FilterIndex = 1;
            OFD.RestoreDirectory = true;

            OFD.ReadOnlyChecked = true;
            OFD.ShowReadOnly = true;

            if (OFD.ShowDialog() == DialogResult.OK)
            {
                txtMessageFileLoc.Text = OFD.FileName;
            }
            OFD.Dispose();
        }

        private void btnFileLoc_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = @"C:\";
            OFD.Title = "Browsing Files";

            OFD.CheckFileExists = true;
            OFD.CheckPathExists = true;

            OFD.DefaultExt = ".zip";
            OFD.Filter = "Text Files (.txt)|*.txt|JSON (.json)|*.json|XML/SOAP (.xml)|*.xml|ZIP Folder (.zip)|*.zip|All files (*.*)|*.*";
            OFD.FilterIndex = 1;
            OFD.RestoreDirectory = true;

            OFD.ReadOnlyChecked = true;
            OFD.ShowReadOnly = true;

            if (OFD.ShowDialog() == DialogResult.OK)
            {
                txtFileLoc.Text = OFD.FileName;
            }
            OFD.Dispose();
        }
        #endregion Click

        #region CheckedChanged
        // Radio Button - Connection Type
        private void rdbSocket_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnSocket.Checked == false) return;
            connType = ConnectionType.Socket;

            chkBoxHttpInfo.Enabled = false;
            chkBoxHttpInfo.Checked = false;

            chkBoxAWSInfo.Enabled = false;
            chkBoxAWSInfo.Checked = false;

            groupBoxMsgType.Enabled = false;
        }

        private void rdbHttpSocket_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnHttpSocket.Checked == false) return;
            connType = ConnectionType.HttpSocket;

            chkBoxHttpInfo.Enabled = true;

            chkBoxAWSInfo.Enabled = false;
            chkBoxAWSInfo.Checked = false;

            groupBoxMsgType.Enabled = true;
        }

        private void rdbHttp_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnHttp.Checked == false) return;
            connType = ConnectionType.Http;

            chkBoxHttpInfo.Enabled = true;

            chkBoxAWSInfo.Enabled = false;
            chkBoxAWSInfo.Checked = false;

            groupBoxMsgType.Enabled = true;
        }

        private void rdbAWS_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnAWS.Checked == false) return;
            connType = ConnectionType.AWS;

            chkBoxHttpInfo.Enabled = true;

            chkBoxAWSInfo.Enabled = true;
            chkBoxAWSInfo.Checked = true;

            groupBoxMsgType.Enabled = true;
        }

        // Enable configuration after HttpInfo or AwsInfo has been checked
        private void chkBoxHttpInfo_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBoxHttpInfo.Checked) groupBoxHttpInfo.Enabled = true;
            else groupBoxHttpInfo.Enabled = false;
        }

        private void chkBoxAWSInfo_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBoxAWSInfo.Checked) groupBoxAWSInfo.Enabled = true;
            else groupBoxAWSInfo.Enabled = false;
        }

        // Tab - Message - Radio Button
        private void rdbTxtBox_CheckedChanged(object sender, EventArgs e)
        {
            labelMessageFileLoc.Visible = false;
            txtMessageFileLoc.Visible = false;
            btnMessageFileLoc.Visible = false;

            txtMessage.Visible = true;
        }

        private void rdbFile_CheckedChanged(object sender, EventArgs e)
        {
            labelMessageFileLoc.Visible = true;
            txtMessageFileLoc.Visible = true;
            btnMessageFileLoc.Visible = true;

            txtMessage.Visible = false;
        }

        // Content Type Radio Button
        private void radioBtnTypePlainZip_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnTypePlainZip.Checked)
            {
                radioBtnTypeJSON.Checked = false;
                radioBtnTypeXML.Checked = false;
                radioBtnTypeSOAP.Checked = false;
                comboBoxOtherContentTypes.SelectedIndex = -1;
            }
        }

        private void radioBtnTypeJSON_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnTypeJSON.Checked)
            {
                radioBtnTypePlainZip.Checked = false;
                radioBtnTypeXML.Checked = false;
                radioBtnTypeSOAP.Checked = false;
                comboBoxOtherContentTypes.SelectedIndex = -1;
            }
        }

        private void radioBtnTypeXML_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnTypeXML.Checked)
            {
                radioBtnTypePlainZip.Checked = false;
                radioBtnTypeJSON.Checked = false;
                radioBtnTypeSOAP.Checked = false;
                comboBoxOtherContentTypes.SelectedIndex = -1;
            }
        }

        private void radioBtnTypeSOAP_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnTypeSOAP.Checked)
            {
                radioBtnTypePlainZip.Checked = false;
                radioBtnTypeJSON.Checked = false;
                radioBtnTypeXML.Checked = false;
                comboBoxOtherContentTypes.SelectedIndex = -1;
            }
        }

        private void comboBoxOtherContentTypes_TextUpdated(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(comboBoxOtherContentTypes.Text))
            {
                radioBtnTypePlainZip.Checked = false;
                radioBtnTypeJSON.Checked = false;
                radioBtnTypeXML.Checked = false;
                radioBtnTypeSOAP.Checked = false;
            }
            else if (String.IsNullOrEmpty(comboBoxOtherContentTypes.Text) && !radioBtnTypePlainZip.Checked && !radioBtnTypeJSON.Checked && !radioBtnTypeXML.Checked && !radioBtnTypeSOAP.Checked)
            {
                radioBtnTypePlainZip.Checked = true;
            }
        }
        #endregion CheckedChanged

        #region TextChanged
        private void txtTimeout_TextChanged(object sender, EventArgs e)
        {
            bool parseInt = int.TryParse(txtTimeout.Text, out int timeoutVal);

            if (parseInt && connPreparer != null && timeoutVal >= 1000)
            {
                connPreparer.Timeout = timeoutVal;

                Settings.Default.txtTimeout = txtTimeout.Text;
                Settings.Default.Save();
            }
        }
        #endregion TextChanged

        #region KeyPress
        private void txtPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !(Char.IsNumber(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Delete);
        }

        private void txtTimeout_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !(Char.IsNumber(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Delete);
        }
        #endregion KeyPress

        #region Selected
        private void tabControlSend_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage.Name == "tabPageMessage")
            {
                btnSend.Text = "Send Message";
                btnSendAsync.Text = "Send Message Async";

                groupBoxMsgType.Text = "Message Content Type";
                radioBtnTypePlainZip.Text = "PlainText";
            }
            else if (e.TabPage.Name == "tabPageFile")
            {
                btnSend.Text = "Send File";
                btnSendAsync.Text = "Send File Async";

                groupBoxMsgType.Text = "File Content Type";
                radioBtnTypePlainZip.Text = "ZIP";
            }
        }
        #endregion Selected

        #endregion Form Events

        #region Helper Functions
        private bool CheckMessageValid(out string errMsg)
        {
            errMsg = "";
            if (string.IsNullOrEmpty(txtMessage.Text))
            {
                errMsg = "Message cannot be empty";
                return false;
            }

            return true;
        }

        private bool CheckFileValid(TextBox loc, out string errMsg)
        {
            errMsg = "";
            if (File.Exists(loc.Text))
            {
                return true;
            }
            else if (string.IsNullOrEmpty(loc.Text))
            {
                errMsg = "File Location cannot be empty";
            }
            else
            {
                errMsg = "Specified file does not exist";
            }
            return false;
        }

        private bool CheckOtherContentTypeUsed()
        {
            return !radioBtnTypePlainZip.Checked && !radioBtnTypeXML.Checked && !radioBtnTypeJSON.Checked && !radioBtnTypeSOAP.Checked && !String.IsNullOrEmpty(comboBoxOtherContentTypes.Text);
        }

        private void DisplayResponse()
        {
            if (connResponse == null)
            {
                LogMessage(LogTypes.ERROR, "No response available to display!");

                // Disable file download
                btnFileDownload.Enabled = false;
                return;
            }
            else
            {
                txtRespDisplay.Text = connResponse.CastToString();
                LogMessage(LogTypes.INFO, "[DisplayResponse] :: The response has been displayed in the \"Response\" tab");
                LogMessage(LogTypes.INFO, "[DisplayResponse] :: If you would like to download the displayed response, click the \"Download Reponse\" button");

                // Enable file download
                btnFileDownload.Enabled = true;
            }
        }

        private void SaveMediaTypeSettings()
        {
            if (radioBtnTypePlainZip.Checked) Settings.Default.MsgFileType = "PlainZip";
            else if (radioBtnTypeXML.Checked) Settings.Default.MsgFileType = "XML";
            else if (radioBtnTypeJSON.Checked) Settings.Default.MsgFileType = "JSON";
            else if (radioBtnTypeSOAP.Checked) Settings.Default.MsgFileType = "SOAP";
            else if (!String.IsNullOrEmpty(comboBoxOtherContentTypes.Text)) Settings.Default.MsgFileType = "OtherContentType";

            Settings.Default.txtOtherContentTypes = comboBoxOtherContentTypes.Text;

            Settings.Default.Save();
        }

        private void GetSettings()
        {
            switch (Settings.Default.ConnType)
            {
                case "Socket": radioBtnSocket.Checked = true; break;
                case "HttpSocket": radioBtnHttpSocket.Checked = true; break;
                case "Http": radioBtnHttp.Checked = true; break;
                case "AWS": radioBtnAWS.Checked = true; break;
            }
            switch (Settings.Default.HttpInfo)
            {
                case true: chkBoxHttpInfo.Checked = true; break;
                case false: chkBoxHttpInfo.Checked = false; break;
            }
            switch (Settings.Default.AWSInfo)
            {
                case true: chkBoxAWSInfo.Checked = true; break;
                case false: chkBoxAWSInfo.Checked = false; break;
            }
            switch (Settings.Default.MessageSource)
            {
                case "TextBox": radioBtnMsgTabTextBox.Checked = true; break;
                case "File": radioBtnMsgTabFile.Checked = true; break;
            }
            switch (Settings.Default.MsgFileType)
            {
                case "PlainZip": radioBtnTypePlainZip.Checked = true; break;
                case "XML": radioBtnTypeXML.Checked = true; break;
                case "JSON": radioBtnTypeJSON.Checked = true; break;
                case "SOAP": radioBtnTypeSOAP.Checked = true; break;
                case "OtherContentType": comboBoxOtherContentTypes.Text = Settings.Default.txtOtherContentTypes; break;
            }
            switch (Settings.Default.SendType)
            {
                case "Message":
                    tabControlSend.SelectedTab = tabControlSend.TabPages["tabPageMessage"];

                    btnSend.Text = "Send Message";
                    btnSendAsync.Text = "Send Message Async";

                    groupBoxMsgType.Text = "Message Type";
                    break;
                case "File":
                    tabControlSend.SelectedTab = tabControlSend.TabPages["tabPageFile"];

                    btnSend.Text = "Send File";
                    btnSendAsync.Text = "Send File Async";
                    radioBtnTypePlainZip.Text = "ZIP";

                    groupBoxMsgType.Text = "File Type";
                    break;
            }
            txtMessage.Text = Settings.Default.txtMessage;
            txtMessageFileLoc.Text = Settings.Default.txtMessageLoc;
            txtFileLoc.Text = Settings.Default.txtFileLoc;

            // Configuration
            textBoxLogPath.Text = Settings.Default.textBoxLogPath;
            switch (Settings.Default.Secure)
            {
                case true: radioBtnSecureTrue.Checked = true; break;
                case false: radioBtnSecureFalse.Checked = true; break;
            }
            txtHost.Text = Settings.Default.txtHost;
            txtPort.Text = Settings.Default.txtPort;
            txtTokenURL.Text = Settings.Default.txtTokenURL;
            txtScope.Text = Settings.Default.txtScope;
            txtMachId.Text = Settings.Default.txtMachId;
            txtObjectPath.Text = Settings.Default.txtObjectPath;
            txtTimeout.Text = Settings.Default.txtTimeout;
            txtHttpMethod.Text = Settings.Default.txtHttpMethod;
            comboBoxOtherContentTypes.Text = Settings.Default.txtOtherContentTypes;

            if (!string.IsNullOrEmpty(Settings.Default.HttpHeadersDT))
            {
                StringReader reader = new StringReader(Settings.Default.HttpHeadersDT);
                HttpHeadersDT.ReadXml(reader);

                foreach (DataRow row in HttpHeadersDT.Rows)
                {
                    comboHeader.Items.Remove(row["Header"]);
                }
            }
        }
        #endregion Helper Functions

        #region Logging
        public void LogMessage(LogTypes logType, string message)
        {
            string logMsg = string.Empty;
            switch (logType)
            {
                case LogTypes.INFO: logMsg = Logging.Write(message, LogTypes.INFO); break;
                case LogTypes.WARN: logMsg = Logging.Write(message, LogTypes.WARN); break;
                case LogTypes.ERROR: logMsg = Logging.Write(message, LogTypes.ERROR); break;
                case LogTypes.DEBUG: logMsg = Logging.Write(message, LogTypes.DEBUG); break;
            }

            void TxtLog()
            {
                txtLog.AppendText(logMsg + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length - 1;
                txtLog.SelectionLength = 0;
                txtLog.ScrollToCaret();
            }

            if (InvokeRequired)
            {
                Invoke((Action)delegate
                {
                    TxtLog();
                });
            }
            else
            {
                TxtLog();
            }
        }

        public void btnClearLogs_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            txtLog.SelectionStart = 0;
            txtLog.SelectionLength = 0;
            txtLog.ScrollToCaret();
        }

        public void btnLogPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog
            {
                Description = "Select a folder you want to log file to be at"
            };
            try
            {


                if (FBD.ShowDialog() == DialogResult.OK)
                {
                    textBoxLogPath.Text = FBD.SelectedPath;
                    Logging.LogFilePath = FBD.SelectedPath;
                    Settings.Default.textBoxLogPath = FBD.SelectedPath;
                    Settings.Default.Save();

                    LogMessage(LogTypes.INFO, $"[LogPath] :: [Update] :: Log files will be saved at the folder \"{FBD.SelectedPath}\"");
                }
            }
            catch (Exception ex)
            {
                LogMessage(LogTypes.ERROR, ex.Message);
            }
            finally
            {
                FBD.Dispose();
            }
        }

        private void btnDefaultLogPath_Click(object sender, EventArgs e)
        {
            textBoxLogPath.Text = Logging.DEFAULT_DIRECTORY;
            Logging.LogFilePath = Logging.DEFAULT_DIRECTORY;
            Settings.Default.textBoxLogPath = Logging.DEFAULT_DIRECTORY;
            Settings.Default.Save();

            LogMessage(LogTypes.INFO, $"[LogPath] :: [Update] :: Log files will be saved at the folder \"{Logging.DEFAULT_DIRECTORY}\"");
        }
        #endregion Logging

        #region Files
        private void fileDownloadBtn_Click(object sender, EventArgs e)
        {
            if (connResponse == null)
            {
                LogMessage(LogTypes.ERROR, "[Download] :: Unable to download because there is no response");
                return;
            }

            FolderBrowserDialog FBD = new FolderBrowserDialog
            {
                Description = "Select a folder you want to download the response to"
            };

            try
            {
                if (FBD.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = FBD.SelectedPath;
                    string fileName = $"{DateTime.Now:dd-MM-yyyy HH.mm.ss}.txt";
                    string fileNameWithPath = Path.Combine(folderPath, fileName);

                    string responseStr = connResponse.CastToString();

                    using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(responseStr);
                        stream.Write(info, 0, info.Length);
                    }

                    LogMessage(LogTypes.INFO, $"[Download] :: [Response Saved] :: Saved to {fileNameWithPath}");
                }
            }
            catch (Exception ex)
            {
                LogMessage(LogTypes.ERROR, ex.Message);
            }
            finally { FBD.Dispose(); }
        }
        #endregion Files
    }
}