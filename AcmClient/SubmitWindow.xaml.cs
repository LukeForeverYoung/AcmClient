using MahApps.Metro.Controls;
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
using AcmClient;
namespace AcmClient
{
    /// <summary>
    /// SubmitWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SubmitWindow : MetroWindow
    {
        hduUser user;
        MainWindow mainWindow;
        
        public SubmitWindow(hduUser user,String Id, MainWindow mainWindow)
        {
            this.user = user;
           
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.ProblemId.Text = Id;
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            TextRange tr = new TextRange(Code.Document.ContentStart, Code.Document.ContentEnd);
            hduHttpHelper.submit(user,ProblemId.Text, tr.Text);
            mainWindow.addSubmitQueryQueue();
            this.Close();
        }
    }
}
