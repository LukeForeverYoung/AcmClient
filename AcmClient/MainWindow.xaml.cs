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
using AngleSharp;
using AngleSharp.Extensions;
using System.Web;
using System.Net.Http;
using AngleSharp.Parser.Html;
using System.Text.RegularExpressions;

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
                    hduHttpHelper.getPersonalInfo(user);
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
class hduHttpHelper
{
    static String url = "http://acm.hdu.edu.cn/";
    static String loginUrl = "http://acm.hdu.edu.cn/userloginex.php?action=login";
    static String userStateUrl = "http://acm.hdu.edu.cn/userstatus.php?user=";
    static HttpClient client;
    static HttpClient initClient()
    {
        HttpClient client = new HttpClient();
        client.MaxResponseContentBufferSize = 256000;
        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36");
        return client;
    }
    static async void login(hduUser user)
    {
        client = initClient();
        HttpResponseMessage response;
        List<KeyValuePair<String, String>> form = new List<KeyValuePair<string, string>>();
        form.Add(new KeyValuePair<string, string>("username", user.UserName));
        form.Add(new KeyValuePair<string, string>("userpass", user.Password));
        form.Add(new KeyValuePair<string, string>("login", "Sign In"));
        response = client.PostAsync(new Url(loginUrl), new FormUrlEncodedContent(form)).Result;
    }
    static async public void getPersonalInfo(hduUser user)
    {
        login(user);
        String userUrl = userStateUrl + user.UserName;
        HttpResponseMessage response;
        response = await client.GetAsync(new Url(userUrl));
        var responseString = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = parser.Parse(responseString);
        var tables = document.All.Where(m => m.LocalName == "table" && m.GetAttribute("width") == "90%");
        var table = tables.First();
        var td=table.GetElementsByTagName("td").First();
        var elements = td.Children;
      
        userInfomation nowUser = new userInfomation();
        nowUser.nickName = elements[0].Text();
        Regex schoolRex = new Regex(@"^.*registered");
        String temp = schoolRex.Match(elements[1].Text()).Value;
        nowUser.School = temp.Substring(6, temp.Length - 20);
        temp = elements[1].Text().Substring(elements[1].Text().Length - 10);
        nowUser.regData = new userInfomation.Date(temp);
        //Console.WriteLine(nowUser.regData.ToString());
        nowUser.Motto = elements[3].Text();
        var valueTable = elements[5].Children.First().Children;
        var rk = Int32.Parse(valueTable[1].LastElementChild.Text());
        var sub= Int32.Parse(valueTable[2].LastElementChild.Text());
        var sov= Int32.Parse(valueTable[3].LastElementChild.Text());
        nowUser.value = new userInfomation.learnValue(rk, sub, sov);
    }
}
class userInfomation
{ 
    public String nickName;
    public String School;
    public String Motto;
    public class Date
    {
        int year, month, day;
        public Date(String date)
        {
            String[] th = date.Split('-');
            year = Int32.Parse(th[0]);
            month = Int32.Parse(th[1]);
            day = Int32.Parse(th[2]);
        }
        override
        public String ToString()
        {
            return " "+year+" "+month+" "+day;
        }
    }
    public Date regData;
    public class learnValue
    {
        int Rank;
        int Submitted;
        int Solved;
        public learnValue(int rk,int sub,int sov)
        {
            Rank = rk;Submitted = sub;Solved = sov;
        }
    }
    public learnValue value;

}