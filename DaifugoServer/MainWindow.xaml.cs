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
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace DaifugoServer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        void a_method(string debtext)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.Write(debtext + "\r\n");
            Console.BackgroundColor = ConsoleColor.Black;
        }
        public delegate void log_add_Hand(string msg);
        public void log_add(string msg)
        {
            this.Dispatcher.Invoke(() =>
            {
                log.AppendText(msg+"\r\n");
            });
        }
        public async void sendtoallclient(string message)
        {
            for (int i = 0; i < cnt; i++)
            {
                byte[] sendbyte = enc.GetBytes(message+"\n");
                await ns[i].WriteAsync(sendbyte, 0, sendbyte.Length);
            }
        }
        public async void sendcardinftoclient(int index)
        {
            byte[] sendbyte;
            if (index == 0)
            {
                sendbyte = enc.GetBytes("cardinfo"+_0 + "\n");
            }
            else if (index == 1)
            {
                sendbyte = enc.GetBytes("cardinfo" + _1 + "\n");
            }
            else if (index == 2)
            {
                sendbyte = enc.GetBytes("cardinfo" + _2 + "\n");
            }
            else
            {
                sendbyte = enc.GetBytes("cardinfo" + _3 + "\n");
            }
            await ns[index].WriteAsync(sendbyte, 0, sendbyte.Length);
        }
        public void sendtoclient(int index)
        {

        }
        int[] resSize = new int[4];
        byte[][] resBytes = new byte[4][]{
            new byte[1024],
            new byte[1024],
            new byte[1024],
            new byte[1024]
        };
        int trim = 0;
        public async Task<string> resGetAsync(int index)
        {
            resBytes[index] = new byte[1024]; 
            ms[index].SetLength(0);
            //データの一部を受信する
            resSize[index] = ns[index].Read(resBytes[index], 0, resBytes[index].Length);
            //受信したデータを蓄積する
            await ms[index].WriteAsync(resBytes[index], 0, resSize[index]);
            string resMsg = enc.GetString(ms[index].GetBuffer(), 0, (int)ms[index].Length);
            return resMsg;
        }
        System.Text.Encoding enc = System.Text.Encoding.Unicode;
        //public string ip = "192.168.1.10";
        int cnt = 0;//接続数
        string[] names = new string[4];
        TcpListener tcplis;
        TcpClient[] tcpcli = new TcpClient[4];
            System.Net.Sockets.NetworkStream[] ns = new NetworkStream[4];
        System.IO.MemoryStream[] ms = new System.IO.MemoryStream[4];
        bool isgamestart = false;



        string _0, _1, _2, _3;//カード情報を格納/各人

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            log.AppendText("\r\n");
            log.AppendText("サーバー開始\r\n");
            tcplis = new TcpListener(IPAddress.Any, 2001);

            byte[] bufbytes = new byte[256];
            int act = 0;
            tcplis.Start();
            await Task.Run(async () =>
            {
                while (cnt<4)
                {
                    tcpcli[cnt] = await tcplis.AcceptTcpClientAsync();
                    ns[cnt] = tcpcli[cnt].GetStream();
                    ms[cnt] = new System.IO.MemoryStream();
                    act = await ns[cnt].ReadAsync(bufbytes, 0, bufbytes.Length);
                    await ms[cnt].WriteAsync(bufbytes, 0, act);
                    string mes = enc.GetString(ms[cnt].GetBuffer(), 0, (int)ms[cnt].Length);
                    ms[cnt].SetLength(0);
                    log_add($"Name: {mes} 接続されました。あと{3 - cnt}人です");
                    names[cnt] = mes;
                    cnt++;
                    if (cnt == 4) break;
                    await Task.Delay(100);
                    for (int i = 0; i < cnt; i++)
                    {
                        byte[] sendbyte = enc.GetBytes($"あと{4 - cnt}人です\n");
                        await ns[i].WriteAsync(sendbyte, 0, sendbyte.Length);
                    }

                }
                log_add("人がそろいました。");
                sendtoallclient("全員揃いました。ゲーム開始を待機しています...");
                while (true)
                {
                    if (isgamestart) break;
                }
                //ゲームスタート!
                string namelist = "";
                for(int i = 0; i<cnt;i++)
                {
                    namelist += "{"+names[i] + "}";
                }
                namelist = namelist.Replace("\r\n", "");
                //namelist = namelist.TrimEnd(' ');
                sendtoallclient("namelist["+namelist);
                //ゲーム開始のREADYを待つ
                /*
                int[] ready = new int[4];
                while (true)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        string temp = await resGetAsync(i);
                        if (temp.IndexOf("READY")>-1) ready[i] = 1;
                        bool isOK = true;
                        for (j = 0;j<4;j++)
                        {
                            if (ready[i] != 1) isOK=false;
                        }
                        if (!isOK) goto sendgamestart;
                        await Task.Delay(100);
                    }
                }
                */
            sendgamestart:
                //開始
                log_add("send gamestart!");
                sendtoallclient("gamestart!");
                string[] card = new string[53];
                //カードを初期化
                int iInit = 0;
                for (int i = 0;i<53;i++)
                {
                    if (iInit == 0)
                    {
                        card[i] = "[C]{" + (i + 1).ToString() + "}";
                        if (i == 12) iInit++;
                    }
                    else if (iInit == 1)
                    {
                        card[i] = "[H]{" + (i + 1-13).ToString() + "}";
                        if (i == 25) iInit++;
                    }
                    else if (iInit == 2)
                    {
                        card[i] = "[D]{" + (i + 1-26).ToString() + "}";
                        if (i == 38) iInit++;
                    }
                    else if (iInit == 3)
                    {
                        card[i] = "[S]{" + (i + 1-39).ToString() + "}";
                        if (i == 51) iInit++;
                    }
                    else if(iInit==4)
                    {
                        card[i] = "[J]{0}";
                        iInit++;
                    }
                }
                //カードをシャッフル
                Random r = new Random();
                for(int i = 0; i <53;i++)
                {
                    int ran = r.Next(0, 52);
                    string tmp = card[i];
                    card[i] = card[ran];
                    card[ran] = tmp;
                }
                //カードを分ける
                int cCnt = 0;
                
                _0 = _1 = _2 = _3 = "";
                for(int i = 0; i <53;i++)
                {
                    if (cCnt == 0)
                    {
                        _0+=card[i];
                        cCnt++;
                    }
                    else if (cCnt == 1)
                    {
                        _1 += card[i];
                        cCnt++;
                    }
                    else if (cCnt == 2)
                    {
                        _2 += card[i];
                        cCnt++;
                    }
                    else if (cCnt == 3)
                    {
                        _3 += card[i];
                        cCnt=0;
                    }
                }
                for(int i = 0; i<4;i++)
                {
                    sendcardinftoclient(i);
                }
                while(true)//ゲーム本体の処理ループ
                {
                    await Task.Delay(200);
                    string resget = "";
                    //カード数とか、提出とか。

                    string temp = "";
                    //await Task.Delay(800);

                    //全てのクライアントがOKを返すまで待機します
                    int[] okpac = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        okpac[i] = 0;
                    }
                    while (true)
                    {
                        for (int i = 0; i < 4; i++)
                        {//OKをいれろおおおおおお
                            temp = await resGetAsync(i);
                            Console.WriteLine($"OK待機 現在:{temp}");
                            resget += temp;
                            if (temp.IndexOf("OK") > -1)//->>デッドロック(妥協策OKpacket(クライアント
                            {
                                okpac[i] = 1;
                            }
                            await Task.Delay(100);
                        }
                        int cnt = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            if (okpac[i] == 1) cnt++;
                        }
                        Console.WriteLine($"okpac={cnt}");
                        //AllOK
                        if (cnt==4)
                        {
                            resget = resget.Replace("\r\n", "").Replace("\n", "").Replace("READY", "");
                            a_method($"packet送信packet:{resget}");
                            sendtoallclient("packet:" + resget);
                            break;
                        }
                    }
                  
                }
            });


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            isgamestart = true;
        }
    }
}
