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
using System.Xml;
using MaterialDesignThemes.Wpf;

namespace AcmClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow//主窗体类
    {
        hduUser user;//用户信息维护
        Queue<String> submitQueryQueue;//提交等待队列
        Thread queueSubmitStateThread;//提交结果检查线程
        judgeStateToast toast = new judgeStateToast();//提交结果Toast
        List<String> problemHistoryArray;//历史题目列表
        String pageId;//页面号
        XmlNodeList ProblemNodeList;//题目列表
        public MainWindow()//主窗体初始化
        {
            problemHistoryArray = new List<string>();
            user = hduUser.readUserJson();//导入用户文件，因为第一屏需要用户信息，所以在前端初始化之前
            InitializeComponent();//前端初始化
            loadProblemXml();//导入XML文件
            ProblemHistorySelector.ItemsSource = problemHistoryArray;//数据源设定
            tab.SelectionChanged += Tab_SelectionChangedAsync;//绑定Tab切换事件
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
                            if (Item.State == "Accepted")
                            {
                                this.Invoke(new Action(() =>
                                {
                                    judgeStateToast toast = new judgeStateToast();
                                    toast.Accepted(Item.ProblemId+'\t'+Item.Time+'\t'+Item.Memory);
                                }));
                            }
                            else if(Item.State== "Presentation Error")
                            {
                                this.Invoke(new Action(() =>
                                {
                                    judgeStateToast toast = new judgeStateToast();
                                    toast.Warning(Item.ProblemId + '\t' + Item.Time + '\t' + Item.Memory);
                                }));
                            }
                            else
                            {
                                this.Invoke(new Action(() =>
                                {
                                    judgeStateToast toast = new judgeStateToast();
                                    toast.Error(Item.ProblemId + '\t' + Item.Time + '\t' + Item.Memory);
                                }));
                            }
                            Console.WriteLine(Item.State);

                        }
                        else
                            submitQueryQueue.Enqueue(nowId);
                    }
                    Thread.Sleep(500);
                }
            }))//申明线程及其运行时匿名函数
            {
                IsBackground = true
            };
            
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
                case 1:
                    {
                        SetProblemListPage(0);
                    }
                    break;
                case 2:
                    {
                        if (user == null)
                        {
                            Console.WriteLine("no user error!");
                            return;
                        }
                    }
                    break;
            }
        }//Tab切换事件，完成各Tab页的生成操作
        //private void setInfoValueLine(object sender, EventArgs e)
        //{
        //    var height = ValueInfo.ActualHeight;
        //    var width = ValueInfo.ActualWidth;
        //    Console.WriteLine(height + "  " + width);
        //    Line InfoValueLine = new Line();
        //    InfoValueLine.Height = height;
        //    InfoValueLine.X1 = InfoValueLine.X2 = width;
        //    InfoValueLine.Y1 = 10;
        //    InfoValueLine.Y2 = height - 10;
        //    InfoValueLine.Stroke = Brushes.LightGray;
        //    ValueInfo.Children.Add(InfoValueLine);
        //}
        private void CopyInput(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(SampleInputTextBox.Text, true);
        }//一键赋值按钮函数
        private void SubmitAction(object sender, RoutedEventArgs e)
        {
         
            SubmitWindow subWindow = new SubmitWindow(user,pageId, this);
            subWindow.Show();
        }//弹出提交代码窗口
        private void searchProblemClick(object sender, RoutedEventArgs e)
        {
            hduHttpHelper.getProblemInfo(user, SearchProblemText.Text, this);
            addProblemHistory(SearchProblemText.Text);
        }//跳转题目按钮点击函数
        private void addProblemHistory(String s)
        {
            pageId = s;
            for(int i=0;i<problemHistoryArray.Count;i++)
            {
                if(problemHistoryArray.ElementAt(i).CompareTo(s)==0)
                {
                    ProblemHistorySelector.SelectedIndex = i;
                    return;
                }
            }
            if (problemHistoryArray.Count >= 5)
            {
                for (int i = 1; i < problemHistoryArray.Count; i++)
                    problemHistoryArray[i - 1] = problemHistoryArray[i];
                problemHistoryArray[problemHistoryArray.Count - 1] = s;
               
            }
            else
                problemHistoryArray.Add(s);
            foreach (String ss in problemHistoryArray)
                Console.Write(ss + " ");
            Console.WriteLine();
            //ProblemHistorySelector.
        }//加入历史纪录
        private void SelectChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            String id = (sender as ComboBox).SelectedItem.ToString();
            pageId = id;
            hduHttpHelper.getProblemInfo(user, id, this);
        }//历史纪录Select切换
        private void loadProblemXml()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(@"../../Sources/ProblemList.xml");
            ProblemNodeList = xml.GetElementsByTagName("Problem");
        }//导入XML
        private void SetProblemListPage(int pageNum)
        {
            const int PageContainSize = 20;
            int startIndex = pageNum*PageContainSize;
            int endIndex = startIndex+20;
            if(startIndex>=ProblemNodeList.Count)
                return;
            if (endIndex > ProblemNodeList.Count)
                endIndex = ProblemNodeList.Count;
            ProblemSet.Children.Clear();
            for(int i=startIndex;i<endIndex;i++)
            {
                var nowProblem = ProblemNodeList.Item(i);
                Card ProblemCard = new Card();
                ProblemCard.Tag = nowProblem.Attributes["id"].Value;
                Canvas CardContent = new Canvas();
                ProblemCard.Width = this.ActualWidth * 0.8;
                ProblemCard.Height = 100;
                TextBlock Problem=new TextBlock();
                TextBlock Rate = new TextBlock();
                TextBlock PassOrSubmit = new TextBlock();
                Problem.Text = nowProblem.Attributes["Name"].Value;
                Problem.FontSize = 20;
                Problem.Margin = new Thickness(20, 10,0,0);
                CardContent.Children.Add(Problem);
                int ac = Int32.Parse(nowProblem.Attributes["acSum"].Value);
                int sub = Int32.Parse(nowProblem.Attributes["subSum"].Value);
               // Console.WriteLine((ac * 1.0 / sub).ToString());
                Rate.Text ="通过率 : "+(ac*1.0/sub).ToString("f2");
                Rate.FontSize = 15;Rate.Margin = new Thickness(20, 50, 0, 0);Rate.Foreground = Brushes.LightGray;
                CardContent.Children.Add(Rate);
                PassOrSubmit.Text ="通过人数:  "+ac.ToString() + "/" + sub.ToString();
                PassOrSubmit.FontSize = 15;PassOrSubmit.Margin = new Thickness(200, 50, 0, 0);PassOrSubmit.Foreground = Brushes.LightBlue;
                CardContent.Children.Add(PassOrSubmit);
                ProblemCard.MouseLeftButtonUp += EnterProblem;
                ProblemCard.Content = CardContent;
                ProblemCard.Margin = new Thickness(0, 20, 0, 0);
                ProblemSet.Children.Add(ProblemCard);
                
                Console.WriteLine("setok");
            }
            
        }//导入题目列表
        private void EnterProblem(object sender, MouseButtonEventArgs e)
        {
            Card card = sender as Card;
            SearchProblemText.Text = card.Tag.ToString();
            searchProblemClick(GoToProblemButton, null);
            tab.SelectedIndex = 2;
        }//构建题目页面
        public void addSubmitQueryQueue()
        {
            String RunId = hduHttpHelper.getSubmitRunId(user);
            lock (submitQueryQueue)
            {
                submitQueryQueue.Enqueue(RunId);
                if (submitQueryQueue.Count == 1)
                {
                    queueSubmitStateThread.Start();
                }
            }
        }//添加提交信息进入等待队列
    }
}
public class hduUser//用户信息类
{
    public String UserName;//用户名
    public String Password;//密码
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
    }//读Json信息
    static public void setUserJson(hduUser user)
    {
        String str = JsonConvert.SerializeObject(user);
        Console.WriteLine(str);
        System.IO.File.WriteAllText(@"user.json", str, Encoding.UTF8);
    }//写Json信息
}
class hduHttpHelper//封装所有关于杭电的Http操作的代码
{
    static String url = "http://acm.hdu.edu.cn/";
    static String loginUrl = "http://acm.hdu.edu.cn/userloginex.php?action=login";
    static String userStateUrl = "http://acm.hdu.edu.cn/userstatus.php?user=";
    static String submitUrl = "http://acm.hdu.edu.cn/submit.php?action=submit";
    static HttpClient client;//Http请求发送端
    static HttpClient initClient()
    {
        HttpClient client = new HttpClient();
        client.MaxResponseContentBufferSize = 256000;
        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36");
        return client;
    }//初始化client
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
    }//登录，用于保持用户在线
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
    }//模拟http post请求发送代码
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
    }//获取用户信息
    static async public void getProblemInfo(hduUser user, String problemID, MainWindow mainWindow)
    {
        problemInfomation problem = new problemInfomation();
        problem.problemId = problemID;
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
    }//获取题目信息
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
    }//对获取到的换行不规则文本进行修正
    static private IHtmlDocument connectAsync(String url)
    {
        client = initClient();
        HttpResponseMessage response;
        response = client.GetAsync(new Url(url)).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;
        var parser = new HtmlParser();
        var document = parser.Parse(responseString);
        return document;
    }//连接封装
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
    }//检查提交状态
    static public String getSubmitRunId(hduUser user)
    {
        //didn't use login();
        String url = "http://acm.hdu.edu.cn/status.php?user=" + user.UserName;
        var document = connectAsync(url);
        var submitItem = document.GetElementsByClassName("table_text")[0].Children[0].Children[2];
        var runId = submitItem.Children[0].Text();
        return runId;
    }//获取提交条目的RunId
    hduHttpHelper()
    {
        client = initClient();
    }//初始化
}
class submitInfo
{
    public String runId;
    public String ProblemId;
    public String State;
    public String Time;
    public String Memory;
    //public String Compiler;
}
class userInfomation : INotifyPropertyChanged//用户信息类，为了便于数据绑定，关键信息都使用get\set方法
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
class problemInfomation : INotifyPropertyChanged//题目信息类，和用户信息类似
{
    String _problemId;
    public String problemId
    {
        get { return _problemId; }
        set { _problemId = value; }
    }
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
class judgeStateToast//封装了判题结果返回的Toast各类效果以及初始化
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
                offsetX: 40,
                offsetY: 110);

            }
            if(cfg.LifetimeSupervisor==null)
            {
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
               notificationLifetime: TimeSpan.FromSeconds(5),
               maximumNotificationCount: MaximumNotificationCount.FromCount(5));
            }
            cfg.Dispatcher = Application.Current.Dispatcher;
        });
    }
    public void Accepted(String s)
    {
        notifier.ShowSuccess(s);
    }
    public void Warning(String s)
    {
        notifier.ShowWarning(s);
    }
    public void Error(String s)
    {
        notifier.ShowError(s);
    }
}