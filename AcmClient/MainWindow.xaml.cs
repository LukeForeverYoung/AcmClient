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
using System.ComponentModel;
using AcmClient;
using AngleSharp.Dom.Html;
using System.Threading;
using ToastNotifications;
using ToastNotifications.Position;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;

namespace AcmClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        hduUser user;
        problemInfomation problem;
        Queue<String> submitQueryQueue;
        Thread queueSubmitStateThread;
        judgeStateToast toast = new judgeStateToast();
        public MainWindow()
        {
            
            user = hduUser.readUserJson();
            InitializeComponent();
            
            tab.SelectionChanged += Tab_SelectionChangedAsync;
            submitQueryQueue = new Queue<string>();
            queueSubmitStateThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    
                    lock (submitQueryQueue)
                    {
                        if (submitQueryQueue.Count == 0) break;
                        Console.WriteLine(submitQueryQueue.Count);
                        String nowId = submitQueryQueue.Dequeue();
                        submitInfo Item = hduHttpHelper.checkSubmitState(nowId, user);
                        if (Item.State != "Queuing" && Item.State != "Compiling" && Item.State != "Running")
                        {
                            if(Item.State=="Accepted")
                            {
                                
                            }
                            Console.WriteLine(Item.State);
                            
                        }
                        else
                            submitQueryQueue.Enqueue(nowId);
                    }
                    Thread.Sleep(500);
                }
            }));
        }

        private async void Tab_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            TabControl x = sender as TabControl;
            switch (x.SelectedIndex)
            {
                case 0:
                    {
                        
                        if (user == null)
                        {
                            LoginDialogData result = await this.ShowLoginAsync("请先登录hdu账号", "输入账号与密码", new LoginDialogSettings { ColorScheme = this.MetroDialogOptions.ColorScheme, InitialUsername = "UserName" });
                            String username = result.Username;
                            String password = result.Password;
                            user = new hduUser(username, password);
                            hduUser.setUserJson(user);
                        }

                        hduHttpHelper.getPersonalInfo(user, this);

                    }
                    break;
                case 2:
                    {
                        
                        if (user == null)
                        {
                            Console.WriteLine("no user error!");
                            return;
                        }
                        problem = new problemInfomation();
                        hduHttpHelper.getProblemInfo(user, problem, "1001", this);

                    }
                    break;
            }
        }

        private void setInfoValueLine(object sender, EventArgs e)
        {
            var height = ValueInfo.ActualHeight;
            var width = ValueInfo.ActualWidth;
            Console.WriteLine(height + "  " + width);
            Line InfoValueLine = new Line();
            InfoValueLine.Height = height;
            InfoValueLine.X1 = InfoValueLine.X2 = width;
            InfoValueLine.Y1 = 10;
            InfoValueLine.Y2 = height - 10;
            InfoValueLine.Stroke = Brushes.LightGray;
            ValueInfo.Children.Add(InfoValueLine);
        }

        private void CopyInput(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(problem.sampleInput, true);
        }

        private void SubmitAction(object sender, RoutedEventArgs e)
        {
            SubmitWindow subWindow = new SubmitWindow(user, this);
            subWindow.Show();
        }
        public void addSubmitQueryQueue()
        {
            String RunId = hduHttpHelper.getSubmitRunId(user);
            lock (submitQueryQueue)
            {
                submitQueryQueue.Enqueue(RunId);
                if (submitQueryQueue.Count == 1)
                {
                    queueSubmitStateThread.Start();
                    //Console.WriteLine("123");
                }
            }
        }
    }
}
public class hduUser
{
    public String UserName;
    public String Password;
    public hduUser(String u, String p)
    {
        UserName = u;
        Password = p;
    }
    static public hduUser readUserJson()
    {
        String str;
        try
        {
            str = System.IO.File.ReadAllText(@"user.json");
        }
        catch (Exception e)
        {
            return null;
        }

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
    static String submitUrl = "http://acm.hdu.edu.cn/submit.php?action=submit";
    static HttpClient client;
    static HttpClient initClient()
    {
        HttpClient client = new HttpClient();
        client.MaxResponseContentBufferSize = 256000;
        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36");
        return client;
    }
    static void login(hduUser user)
    {
        client = initClient();
        HttpResponseMessage response;
        List<KeyValuePair<String, String>> form = new List<KeyValuePair<string, string>>();
        form.Add(new KeyValuePair<string, string>("username", user.UserName));
        form.Add(new KeyValuePair<string, string>("userpass", user.Password));
        form.Add(new KeyValuePair<string, string>("login", "Sign In"));
        response = client.PostAsync(new Url(loginUrl), new FormUrlEncodedContent(form)).Result;
        //Console.WriteLine(response);
    }
    static public void submit(hduUser user, String problemId, String userCode)
    {
        login(user);
        HttpResponseMessage response;
        List<KeyValuePair<String, String>> form = new List<KeyValuePair<string, string>>();
        form.Add(new KeyValuePair<string, string>("check", "0"));
        form.Add(new KeyValuePair<string, string>("problemid", problemId));
        form.Add(new KeyValuePair<string, string>("language", "0"));
        form.Add(new KeyValuePair<string, string>("usercode", userCode));
        response = client.PostAsync(new Url(submitUrl), new FormUrlEncodedContent(form)).Result;
        //Console.WriteLine(response);
    }
    static async public void getPersonalInfo(hduUser user, MainWindow mainWindow)
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
        var td = table.GetElementsByTagName("td").First();
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
        var sub = Int32.Parse(valueTable[2].LastElementChild.Text());
        var sov = Int32.Parse(valueTable[3].LastElementChild.Text());
        nowUser.setSubmitValue(rk, sub, sov);
        mainWindow.DataBinding.DataContext = nowUser;
    }
    static async public void getProblemInfo(hduUser user, problemInfomation problem, String problemID, MainWindow mainWindow)
    {
        problemInfomation nowProblem = new problemInfomation();

        login(user);
        String problemUrl = "http://acm.hdu.edu.cn/showproblem.php?pid=" + problemID;
        HttpResponseMessage response;
        response = await client.GetAsync(new Url(problemUrl));
        var responseString = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = parser.Parse(responseString);
        var problemMain = document.All.Where(m => m.LocalName == "tbody").First().Children[3].Children[0];
        //Console.WriteLine(problemMain.ToHtml());
        problem.problemName = problemMain.FirstChild.Text();
        problem.problemValue = getTextWithWrap(problemMain.Children[1].FirstChild.FirstChild.ChildNodes);
        problem.Description = getTextWithWrap(problemMain.Children[5].ChildNodes);
        problem.Input = getTextWithWrap(problemMain.Children[9].ChildNodes);
        problem.Output = getTextWithWrap(problemMain.Children[13].ChildNodes);
        problem.sampleInput = getTextWithWrap(problemMain.Children[17].FirstChild.ChildNodes);
        problem.sampleOutput = getTextWithWrap(problemMain.Children[21].FirstChild.ChildNodes);
        mainWindow.ProblemPage.DataContext = problem;

        //Console.WriteLine(problem.problemValue);
    }
    static private String getTextWithWrap(AngleSharp.Dom.INodeList NodeList)
    {
        var tempString = "";
        foreach (var ele in NodeList)
        {
            //Console.WriteLine(ele.NodeName);
            if (ele.NodeName == "BR")
                //tempString += "\r\n";
                tempString += Environment.NewLine;
            else
            {
                String AddedWrap = ele.Text();
                for (int i = 0; i < AddedWrap.Length; i++)
                {
                    if (AddedWrap[i] != '\n')
                        tempString += AddedWrap[i];
                    else
                        tempString += Environment.NewLine;
                }

            }

        }
        return tempString;
    }
    static private IHtmlDocument connectAsync(String url)
    {
        client = initClient();
        HttpResponseMessage response;
        response = client.GetAsync(new Url(url)).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;
        var parser = new HtmlParser();
        var document = parser.Parse(responseString);
        return document;
    }

    static public submitInfo checkSubmitState(string RunId, hduUser user)
    {
        String url = "http://acm.hdu.edu.cn/status.php?first=+" + RunId + "+&user=" + user.UserName;
        var document = connectAsync(url);
        var submitItem = document.GetElementsByClassName("table_text")[0].Children[0].Children[2];
        submitInfo Item = new submitInfo();
        Item.runId = RunId;
        Item.ProblemId = submitItem.Children[3].FirstChild.Text();
        Item.Time = submitItem.Children[4].Text();
        Item.Memory = submitItem.Children[5].Text();
        Item.State = submitItem.Children[2].FirstChild.Text();
        return Item;
    }

    static public String getSubmitRunId(hduUser user)
    {
        //didn't use login();
        String url = "http://acm.hdu.edu.cn/status.php?user=" + user.UserName;
        var document = connectAsync(url);
        var submitItem = document.GetElementsByClassName("table_text")[0].Children[0].Children[2];
        var runId = submitItem.Children[0].Text();
        return runId;
    }
    hduHttpHelper()
    {
        client = initClient();
    }
}
class submitInfo
{
    public String runId;
    public String ProblemId;
    public String State;
    public String Time;
    public String Memory;
    public String Compiler;
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
            _registerDate = value;
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
            return year.ToString("0000") + "/" + month.ToString("00") + "/" + day.ToString("00");
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
        if (propertyName != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
class problemInfomation : INotifyPropertyChanged
{
    String _problemName;
    public String problemName
    {
        get { return _problemName; }
        set
        {
            _problemName = value;
        }
    }
    String _problemValue;
    public String problemValue
    {
        get { return _problemValue; }
        set { _problemValue = value; }
    }
    String _Description;
    public String Description
    {
        get { return _Description; }
        set
        {
            _Description = value;
        }
    }
    String _Input;
    public String Input
    {
        get { return _Input; }
        set
        {
            _Input = value;
        }
    }
    String _Output;
    public String Output
    {
        get { return _Output; }
        set
        {
            _Output = value;
        }
    }
    String _sampleInput;
    public String sampleInput
    {
        get { return _sampleInput; }
        set
        {
            _sampleInput = value;
        }
    }
    String _sampleOutput;
    public String sampleOutput
    {
        get { return _sampleOutput; }
        set
        {
            _sampleOutput = value;
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    protected void PropertyChangedNotify(string propertyName)
    {
        if (propertyName != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
class judgeStateToast
{
    private Notifier notifier;
    public judgeStateToast()
    {
        notifier = new Notifier(cfg =>
        {
            if(cfg.PositionProvider==null)
            {
                cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 30,
                offsetY: 30);

            }
            if(cfg.LifetimeSupervisor==null)
            {
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
               notificationLifetime: TimeSpan.FromSeconds(3),
               maximumNotificationCount: MaximumNotificationCount.FromCount(5));
            }
           

            cfg.Dispatcher = Application.Current.Dispatcher;
        });
    }
    public void  Accepted(String s)
    {
        notifier.ShowSuccess(s);
    }
}


