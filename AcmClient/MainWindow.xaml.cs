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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
namespace AcmClient
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		public MainWindow()
		{
			InitializeComponent();
            tab.SelectionChanged += Tab_SelectionChangedAsync;
		}

        private async void Tab_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            TabControl x = sender as TabControl;
            if (x.SelectedIndex == 0)
            {
                hduUser user=hduUser.readUserJson();
                if(user==null)
                {
                    LoginDialogData result = await this.ShowLoginAsync("请先登录hdu账号", "输入账号与密码", new LoginDialogSettings { ColorScheme = this.MetroDialogOptions.ColorScheme, InitialUsername = "UserName" });
                    String username = result.Username;
                    String password = result.Password;
                    user = new hduUser(username, password);
                    hduUser.setUserJson(user);
                }
                else
                {
                    Console.WriteLine(user.Password);
                }
            }
        }
     
     
        
    }
}
class hduUser
{
    public String UserName;
    public String Password;
    public hduUser(String u,String p)
    {
        UserName = u;
        Password = p;
    }
    static public hduUser readUserJson()
    {
       
        String str=System.IO.File.ReadAllText(@"user.json");
        hduUser user = JsonConvert.DeserializeObject<hduUser>(str);
        return user;
    }
    static public void setUserJson(hduUser user)
    {
        String str = JsonConvert.SerializeObject(user);
        Console.WriteLine(str);
        System.IO.File.WriteAllText(@"user.json", str, Encoding.UTF8);
    }
}