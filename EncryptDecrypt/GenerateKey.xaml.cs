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
using System.Windows.Shapes;

namespace EncryptDecrypt
{
    /// <summary>
    /// Interaction logic for GenerateKey.xaml
    /// </summary>
    public partial class GenerateKey : Window
    {
        public GenerateKey()
        {
            InitializeComponent();
        }

        private void GenerateKeyBrowsers_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".xml";
            Nullable<bool> result = dlg.ShowDialog();

            if (result==true)
            {
                GenKeyLocate.Text = dlg.FileName;

            }
        }
    }
}
