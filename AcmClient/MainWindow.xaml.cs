﻿using System;
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
using System.ComponentModel;
using AcmClient;

namespace AcmClient
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
    /// 
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
                    hduHttpHelper.getPersonalInfo(user,this);
                }
            }
        }
     
        private void setInfoValueLine(object sender,EventArgs e)
        {
            var height = ValueInfo.ActualHeight;
            var width = ValueInfo.ActualWidth;
            Console.WriteLine(height + "  " + width);
            Line InfoValueLine = new Line();
            InfoValueLine.Height = height;
            InfoValueLine.X1=InfoValueLine.X2 = width;
            InfoValueLine.Y1 = 10;
            InfoValueLine.Y2 = height - 10;
            InfoValueLine.Stroke = Brushes.LightGray;
            ValueInfo.Children.Add(InfoValueLine);
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
    static async public void getPersonalInfo(hduUser user , MainWindow mainWindow)
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
        nowUser = new userInfomation();
        nowUser.nickName = elements[0].Text();
        Regex schoolRex = new Regex(@"^.*registered");
        String temp = schoolRex.Match(elements[1].Text()).Value;
        nowUser.School = temp.Substring(6, temp.Length - 20);
        temp = elements[1].Text().Substring(elements[1].Text().Length - 10);
        nowUser.regData = new userInfomation.Date(temp);
        nowUser.registerDate = nowUser.regData.ToString();
        //Console.WriteLine(nowUser.regData.ToString());
        nowUser.Motto = elements[3].Text();
        var valueTable = elements[5].Children.First().Children;
        var rk = Int32.Parse(valueTable[1].LastElementChild.Text());
        var sub= Int32.Parse(valueTable[2].LastElementChild.Text());
        var sov= Int32.Parse(valueTable[3].LastElementChild.Text());
        nowUser.setSubmitValue(rk, sub, sov);
        mainWindow.DataBinding.DataContext = nowUser;
    }
}
class userInfomation : INotifyPropertyChanged
{ 
    String _nickName;
    String _School;
    String _Motto;
    String _registerDate;
    public String nickName
    {
        get { return _nickName; }
        set
        {
            _nickName = value;
           // OnPropertyChanged("nickName");
        }
    }
    public String School
    {
        get { return _School; }
        set
        {
            _School = value;
            //OnPropertyChanged("School");

        }
    }
    public String Motto
    {
        get { return _Motto; }
        set
        {
            _Motto = value;
           // OnPropertyChanged("Motto");
        }
    }
    public String registerDate
    {
        get { return _registerDate; }
        set
        {
            _registerDate=value;
            //OnPropertyChanged("registerDate");
        }
    }
    public class Date
    {
        public int year, month, day;
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
            return year.ToString("0000")+"/"+month.ToString("00")+"/"+day.ToString("00");
        }
    }
    public Date regData;
    public int Rank
    {
        get { return _Rank; }
        set
        {
            _Rank = value;
            //OnPropertyChanged("Rank");
        }
    }
    public int Submitted
    {
        get { return _Submitted; }
        set
        {
            _Submitted = value;
            //OnPropertyChanged("Submitted");
        }
    }
    public int Solved
    {
        get { return _Solved; }
        set
        {
            _Solved = value;
           // OnPropertyChanged("Solved");
        }
    }
    int _Rank;
    int _Submitted;
    int _Solved;
    public void setSubmitValue(int rk, int sub, int sov)
    {
        Rank = rk; Submitted = sub; Solved = sov;
    }
    public event PropertyChangedEventHandler PropertyChanged;
    protected void PropertyChangedNotify(string propertyName)
    {
        if(propertyName!=null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}