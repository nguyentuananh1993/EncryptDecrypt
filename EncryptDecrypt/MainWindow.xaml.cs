using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace EncryptDecrypt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.publicKey = null;
            this.freeEvent = new EventWaitHandle(true, EventResetMode.ManualReset);

        }

        private string publicKey;
        private string privateKey;
        private string manifestFilePath;
        private EventWaitHandle freeEvent;

        private static string MakePath(string plainFilePath, string newSuffix)
        {
            string encryptedFileName = System.IO.Path.GetFileNameWithoutExtension(plainFilePath) + newSuffix;
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(plainFilePath), encryptedFileName);
        }

        private void UpdateOutput(Label tb, string message, bool append)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                string newMessage = append ? tb.Content + "\r\n" + message : message;
                tb.Content = newMessage;
            }), null);
        }

        private void openFolderDialog(TextBox textbox)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = EncryptDecrypt.Properties.Resources.DialogTitle_CreateKey;
            
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textbox.Text = fbd.SelectedPath;
            }
        }
        private void openFileDialog(TextBox textbox,String filter){
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

            fileDialog.DefaultExt = "txt";
            fileDialog.Filter = filter;
            Nullable<bool> result = fileDialog.ShowDialog();
            if (result == true)
            {
                textbox.Text = fileDialog.FileName;
            }
        }

        private void EncryptOpenFile_Click(object sender, RoutedEventArgs e)
        {
            String fileFilter = "All files(*.*)|*.*";
            openFileDialog(txtBox_Encrypt,fileFilter);
        }

        private void EnterPublicKey_Click(object sender, RoutedEventArgs e)
        {
            String fileFilter = "XML Documents (*.xml)|*.xml| All files(*.*)|*.*";
            openFileDialog(txtBox_PublicKey,fileFilter);
            using (StreamReader sr = File.OpenText(txtBox_PublicKey.Text))
            {
                this.publicKey = sr.ReadToEnd();
            }
        }

        private void EnterPrivateKey_Click(object sender, RoutedEventArgs e)
        {
            String fileFilter = "XML Documents (*.xml)|*.xml| All files(*.*)|*.*";
            openFileDialog(txtBox_PrivateKey, fileFilter);
            using (StreamReader sr = File.OpenText(txtBox_PrivateKey.Text))
            {
                this.privateKey = sr.ReadToEnd();
            }
        }

        private void DecryptOpenFile_Click(object sender, RoutedEventArgs e)
        {
            String fileFilter = "Encrypted files (*.encrypted)|*.encrypted";
            openFileDialog(txtBox_Decrypt, fileFilter);
            this.manifestFilePath = txtBox_Decrypt.Text;
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!this.freeEvent.WaitOne(0))
            {
                MessageBox.Show(Properties.Resources.Backend_Busy);
                return;
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                MessageBox.Show(this, Properties.Resources.Error_Need_PublicKey);
                return;
            }

            string plainFilePath = this.txtBox_Encrypt.Text;
            string encryptedFilePath = MakePath(plainFilePath, ".encrypted");
            string manifestFilePath = MakePath(plainFilePath, ".manifest.xml");

            this.label_encryptStatus.Content = Properties.Resources.Out_msg_start_encryption;

            var t = Task.Factory.StartNew(() =>
            {
                freeEvent.Reset();
                string s = EncryptDecrypt.Encipher.Encrypt(plainFilePath,
                    encryptedFilePath,
                    manifestFilePath,
                    this.publicKey);

                freeEvent.Set();
                this.UpdateOutput(this.label_encryptStatus, Properties.Resources.Out_msg_encrypt_success + "\r\n" + s, false);
            });
        }

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!this.freeEvent.WaitOne(0))
            {
                MessageBox.Show(Properties.Resources.Backend_Busy);
                return;
            }

            string rsaKey = this.privateKey;
            string encryptedFile = this.txtBox_Decrypt.Text;
            string plainFile = MakePath(encryptedFile, ".decrypted");
            string manifestFile = this.manifestFilePath;
            XDocument doc = XDocument.Load(manifestFile);
            XElement aesKeyElement = doc.Root.XPathSelectElement("./DataEncryption/AESEncryptedKeyValue/Key");
            byte[] aesKey = EncryptDecrypt.Encipher.RSADescryptBytes(Convert.FromBase64String(aesKeyElement.Value), rsaKey);
            XElement aesIvElement = doc.Root.XPathSelectElement("./DataEncryption/AESEncryptedKeyValue/IV");
            byte[] aesIv = EncryptDecrypt.Encipher.RSADescryptBytes(Convert.FromBase64String(aesIvElement.Value), rsaKey);

            this.label_decryptStatus.Content = Properties.Resources.Out_msg_start_decryption;
            var t = Task.Factory.StartNew(() =>
            {
                freeEvent.Reset();
                EncryptDecrypt.Encipher.DecryptFile(plainFile, encryptedFile, aesKey, aesIv);
                freeEvent.Set();
                this.UpdateOutput(this.label_decryptStatus, string.Format(Properties.Resources.Out_msg_decryption_success, plainFile), false);
            });
        }

        private void KeyLocate_Click(object sender, RoutedEventArgs e)
        {
            openFolderDialog(txtBox_KeyLocate);
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            string publicKeyPath = System.IO.Path.Combine(txtBox_KeyLocate.Text, "publicKey.xml");
            string privateKeyPath = System.IO.Path.Combine(txtBox_KeyLocate.Text, "privateKey.xml");

            string publicKey;
            string privateKey;
            EncryptDecrypt.Encipher.GenerateRSAKeyPair(out publicKey, out privateKey);
            using (StreamWriter sw = File.CreateText(publicKeyPath))
            {
                sw.Write(publicKey);
            }

            using (StreamWriter sw = File.CreateText(privateKeyPath))
            {
                sw.Write(privateKey);
            }
        }

        private void ManifestFile_Click(object sender, RoutedEventArgs e)
        {
            String fileFilter = "XML Documents (*.xml)|*.xml| All files(*.*)|*.*";
            openFileDialog(txtBox_ManifestFile, fileFilter);
            this.manifestFilePath = txtBox_ManifestFile.Text;
        }
    }
}
