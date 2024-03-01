namespace X100_Message
{
    public partial class Form1 : Form
    {
        private string version = "2.1.0";
        Uart uart = new();
        Extend extend = new();

        private bool isWaitMessage = false;
        private bool isRestart = false;
        List<string> controlNamesToSave = new List<string>() { "isLogFileOutput", "comComboBox", "fontSizeComboBox", "fontComboBox" };

        /**
         * Form�n
         */
        public Form1()
        {
            InitializeComponent();
            uart.DataReceived += DataReceived;
            this.FormClosing += Form1_FormClosing;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            InitComPort();
            InitFont();
            InitLogOutput();
            this.Text = "DJ-X100 ���b�Z�[�W���K�[ Ver" + version;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseConnection();
            SaveFormContentToIniFile();
            Application.Exit();
        }
        private void �I��ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseConnection();
            SaveFormContentToIniFile();
            Application.Exit();
        }

        // COM�|�[�g�ꗗ�̏���������
        private void InitComPort()
        {
            // �ݒ�t�@�C������I�����ꂽCOM�|�[�g���擾���܂��B
            string selectedComPort = IniFileHandler.ReadValue("Settings", "comComboBox", ".\\settings.ini");

            foreach (String portName in Uart.GetPortLists())
            {
                comComboBox.Items.Add(portName);

                // �ǂݎ����COM�|�[�g�ƈ�v������̂�����΁A�����I�����܂��B
                if (portName == selectedComPort)
                {
                    comComboBox.SelectedItem = portName;
                }
            }

            // �ݒ�t�@�C���Ɉ�v����COM�|�[�g��������Ȃ������ꍇ�A�ŏ��̃|�[�g��I�����܂��B
            if (comComboBox.SelectedItem == null && comComboBox.Items.Count > 0)
            {
                comComboBox.SelectedIndex = 0;
            }
        }

        private void InitFont()
        {
            // ini�t�@�C������ݒ��ǂݍ���
            string selectedFont = IniFileHandler.ReadValue("Settings", "fontComboBox", ".\\settings.ini");
            string selectedFontSize = IniFileHandler.ReadValue("Settings", "fontSizeComboBox", ".\\settings.ini");
            float fontSize = 8.0f;

            // �t�H���g�T�C�Y�̃R���{�{�b�N�X��������
            fontSizeComboBox.Items.AddRange(new string[] { "7", "8", "10", "12", "14", "16", "18", "20", "24" });
            if (!string.IsNullOrEmpty(selectedFontSize) && float.TryParse(selectedFontSize, out fontSize))
            {
                fontSizeComboBox.SelectedItem = selectedFontSize;
            }
            else
            {
                fontSizeComboBox.SelectedItem = "8";
            }

            foreach (FontFamily font in FontFamily.Families)
            {
                fontComboBox.Items.Add(font.Name);

                if (font.Name == selectedFont)
                {
                    fontComboBox.SelectedItem = font.Name;
                }
            }

            if (fontComboBox.SelectedItem == null || fontSize <= 0)
            {
                logTextBox.Font = new Font("BIZ UDP�S�V�b�N", 8.0f);
            }
            else
            {
                logTextBox.Font = new Font(selectedFont, fontSize);
            }
        }

        private void InitLogOutput()
        {
            string checkBoxValueString = IniFileHandler.ReadValue("Settings", "isLogFileOutput", ".\\settings.ini");
            bool checkBoxValue = false;
            bool.TryParse(checkBoxValueString, out checkBoxValue);
            isLogFileOutput.Checked = checkBoxValue;
        }



        private void FontSizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedFontSize = (string)fontSizeComboBox.SelectedItem;
            float fontSize = 8.0f;
            if (float.TryParse(selectedFontSize, out fontSize))
            {
                float.Parse(selectedFontSize);
            }
            logTextBox.Font = new Font(logTextBox.Font.FontFamily, fontSize);
        }

        private void FontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedFont = (string)fontComboBox.SelectedItem;
            logTextBox.Font = new Font(selectedFont, logTextBox.Font.Size);
        }

        private void SaveFormContentToIniFile()
        {
            string filePath = ".\\settings.ini";
            IniFileHandler.WriteValue("Settings", "isLogFileOutput", isLogFileOutput.Checked.ToString(), ".\\settings.ini");


            foreach (string controlName in controlNamesToSave)
            {
                Control control = this.Controls.Find(controlName, true).FirstOrDefault();
                IniFileHandler.WriteValue("Version", "Ver", version, filePath);

                if (control is TextBox)
                {
                    TextBox textBox = (TextBox)control;
                    IniFileHandler.WriteValue("Settings", textBox.Name, textBox.Text, filePath);
                }
                else if (control is ComboBox)
                {
                    ComboBox comboBox = (ComboBox)control;
                    IniFileHandler.WriteValue("Settings", comboBox.Name, comboBox.Text, filePath);
                }
            }
        }


        /**
         * �R�}���h�n
         */
        private String SendCmd(string cmd)
        {
            return uart.SendCmd(cmd).Replace("\r\n", "");
        }

        private bool SendCmd(string cmd, string expectResponse)
        {
            String response = uart.SendCmd(cmd).Replace("\r\n", "");

            if (response.Equals(Command.NG))
            {
                MessageBox.Show("�����ُ�", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }


            if (response.Equals(expectResponse)) return true;
            return false;
        }

        private String SendRawdCmd(String cmd)
        {
            String response = uart.SendRawCmd(cmd).Replace("\r\n", "");

            if (response.Equals(Command.NG))
            {
                MessageBox.Show("�����ُ�", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "���X�|���X�ُ�";
            }

            return response;
        }

        private void ConnectX100()
        {
            if (uart.InitSerialPort(comComboBox.Text))
            {
                if (SendCmd(Command.WHO, "DJ-X100"))
                {
                    String isPower = SendCmd(Command.DSPTHRU);

                    if (!isRestart && isPower.Equals("  SLEEP"))
                    {
                        MessageBox.Show("�d���������Ă��܂���", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        CloseConnection();
                        return;
                    }
                    if (isPower.Equals("���񂵂Ă��Ȃ�"))
                    {
                        CloseConnection();
                        return;
                    }

                    connectBtn.Text = "�ؒf";
                    msgOutputBtn.Enabled = true;
                    extMenuItem.Enabled = true;
                    restartBtn.Enabled = true;
                    searchBtn.Enabled = true;

                    warnLabel.Text = "DJ-X100�ڑ��ς�";

                    GetX100Info();
                }
                else
                {
                    warnLabel.Text = "�ڑ����s";
                    MessageBox.Show("�ڑ��G���[���������܂���: ", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CloseConnection();
                }

            }
        }

        // DJ-X100�ɐڑ�
        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            if (connectBtn.Text.Equals("�ؒf"))
            {
                CloseConnection();
                return;
            }

            ConnectX100();

        }

        private void GetX100Info()
        {
            mcuLabel.Text = SendCmd(Command.VER);

            // �����t�@�[���ȊO�͒��ዾ�񊈐���
            if (!mcuLabel.Text.Equals("1.00 - 003"))
            {
                searchBtn.Enabled = false;
            }

            // �g���@�\1�`�F�b�N
            String ext1Status = SendCmd(Command.EXT1_IS_VAILD);
            switch (ext1Status)
            {
                case "0001":
                    ext1Label.Text = "�g���@�\1:�L��";
                    break;

                case "0000":
                    ext1Label.Text = "�g���@�\1:����";
                    break;

                default:
                    ext1Label.Text = "�g���@�\1:�s��";
                    break;
            }

            // �g���@�\2�`�F�b�N
            String ext2Status = SendCmd(Command.EXT2_IS_VAILD);
            switch (ext2Status)
            {
                case "0001":
                    ext2Label.Text = "�g���@�\2:�L��";
                    break;

                case "0000":
                    ext2Label.Text = "�g���@�\2:����";
                    break;

                default:
                    ext2Label.Text = "�g���@�\2:�s��";
                    break;
            }
        }




        //�񓯊��ŃR�}���h��M�ҋ@���Ă���i�C�x���g�n���h���j
        private void DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!isWaitMessage) return;

            String response = e.Data;
            if (response.Equals("\r\nOK\r\n")) return;

            // ��M�f�[�^���e�L�X�g�{�b�N�X�ɕ\���iUI�X���b�h�Ŏ��s�j
            this.Invoke(new Action(() =>
            {
                DateTime now = DateTime.Now;

                if (isDisplayReceiverNameOnly.Checked)
                {
                    if (!response.Contains("sn���M�Җ�")) return;

                }

                logTextBox.AppendText($"{now} >> {response}\r\n");

                if (isLogFileOutput.Checked)
                {
                    try
                    {
                        File.AppendAllText($"received_message_{now.ToString("yyyyMMdd")}.txt", $"{now} >> {response}" + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("���b�Z�[�W���O�o�̓G���[ " + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
            }));
        }

        private void CloseConnection()
        {
            if (isWaitMessage)
            {
                SendRawdCmd("QUIT\r\n");
                isWaitMessage = false;
            }
            uart.Close();
            warnLabel.Text = "�ڑ����Ă��܂���";
            connectBtn.Text = "�ڑ�";

            msgOutputBtn.Enabled = false;
            msgOutputBtn.Text = "���b�Z�[�W�o�͊J�n";
            extMenuItem.Enabled = false;
            restartBtn.Enabled = false;
            searchBtn.Enabled = false;

            mcuLabel.Text = "ver";
            ext1Label.Text = "�g���@�\1:";
            ext2Label.Text = "�g���@�\2:";
        }


        private void MsgOutputBtn_Click(object sender, EventArgs e)
        {
            if (isWaitMessage)
            {
                if (SendRawdCmd("QUIT\r\n").Equals(Command.OK))
                {
                    msgOutputBtn.Text = "���b�Z�[�W�o�͊J�n";
                    warnLabel.Text = "DJ-X100�ڑ��ς�";
                    extMenuItem.Enabled = true;
                    restartBtn.Enabled = true;

                    // �����t�@�[���ȊO�͒��ዾ�񊈐���
                    if (!mcuLabel.Text.Equals("1.00 - 003"))
                    {
                        searchBtn.Enabled = false;
                    }
                    else
                    {
                        searchBtn.Enabled = true;
                    }

                    isWaitMessage = false;
                }
                return;
            }

            if (SendCmd(Command.OUTLINE, Command.OK))
            {
                msgOutputBtn.Text = "���b�Z�[�W�o�͏I��";

                // �����t�@�[���͎��g���ύX����
                if (mcuLabel.Text.Equals("1.00 - 003"))
                {
                    warnLabel.Text = "���b�Z�[�W�ҋ@���c(���g�����̕ύX����)";
                }
                else
                {
                    warnLabel.Text = "���b�Z�[�W�ҋ@���c(�X�L������)";
                }

                extMenuItem.Enabled = false;
                restartBtn.Enabled = false;
                searchBtn.Enabled = false;
                isWaitMessage = true;
            }
        }





        private void ���O�N���ACToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logTextBox.ResetText();
        }

        private void �o�[�W�������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("DJ-X100���b�Z�[�W���K�[\nVer" + version + "\nCopyright(C) 2023 by kaz", "�o�[�W�������", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void ext1EnableBtn_Click(object sender, EventArgs e)
        {
            extend.IsExtendAccept(sender);
        }

        private void ext1DisableBtn_Click(object sender, EventArgs e)
        {
            if (extend.IsExtendAccept(sender))
            {

                if ((SendCmd(Command.EXT2_DISABLE, Command.OK) && SendCmd(Command.EXT1_DISABLE, Command.OK)))
                {
                    MessageBox.Show("�g���@�\1,2�𖳌������܂���", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                ext1Label.Text = SendCmd(Command.EXT1_IS_VAILD, Command.ENABLE) ? "�g���@�\1:�L��" : "�g���@�\1:����";
                ext2Label.Text = SendCmd(Command.EXT2_IS_VAILD, Command.ENABLE) ? "�g���@�\2:�L��" : "�g���@�\2:����";

            }
        }

        private void ext2EnableBtn_Click(object sender, EventArgs e)
        {
            if (extend.IsExtendAccept(sender))
            {
                if (SendCmd(Command.EXT2_ENABLE, Command.OK))
                {
                    MessageBox.Show("�g���@�\2��L�������܂���", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                ext2Label.Text = SendCmd(Command.EXT2_IS_VAILD, Command.ENABLE) ? "�g���@�\2:�L��" : "�g���@�\2:����";
            }
        }
        private void ext2DisableBtn_Click(object sender, EventArgs e)
        {
            if (extend.IsExtendAccept(sender))
            {
                if (SendCmd(Command.EXT2_DISABLE, Command.OK))
                {
                    MessageBox.Show("�g���@�\2�𖳌������܂���", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                ext2Label.Text = SendCmd(Command.EXT2_IS_VAILD, Command.ENABLE) ? "�g���@�\2:�L��" : "�g���@�\2:����";
            }
        }

        private async void restartBtn_Click(object sender, EventArgs e)
        {
            if (SendCmd(Command.RESTART, Command.OK))
            {
                isRestart = true;
                CloseConnection();
                restartBtn.Text = "�ċN�����c";
                warnLabel.Text = "�ċN�����c";
                await Task.Delay(5000);

                ConnectX100();
                restartBtn.Text = "DJ-X100�ċN��";
            }
        }

        private void search_Click(object sender, EventArgs e)
        {
            SendCmd(Command.FUNC);
            SendCmd(Command.KEY_0);
        }
    }


}