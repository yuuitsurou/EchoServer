using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    /// <summary>
    /// 参考: http://smdn.jp/programming/netfx/tips/echo_server/
    /// </summary>
    class Program
    {
        // ローカルループバックアドレス(127.0.0.1)のポート22222番を使用する
        private static readonly IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 22222);

        static void Main()
        {
            // TCP/IPでの通信を行うソケットを作成する
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // TIME_WAIT状態のソケットを再利用する
                server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // ソケットをアドレスにバインドする
                server.Bind(endPoint);

                // 接続の待機を開始する
                server.Listen(10);

                Console.WriteLine("server started ({0})", server.LocalEndPoint);

                var connectionCount = 0;

                for (; ; )
                {
                    // クライアントからの接続要求を待機する
                    var client = server.Accept();

                    connectionCount++;

                    Console.WriteLine("client accepted (#{0}, {1})", connectionCount, client.RemoteEndPoint);

#if true
                    // 新しくスレッドを作成してクライアントを処理する
                    StartSessionInNewThread(connectionCount, client);
#else
            // アプリケーションドメインを作成してクライアントを処理する
            StartSessionInNewAppDomain(connectionCount, client);
#endif
                }
            }
        }

        private static void StartSessionInNewThread(int clientId, Socket client)
        {
            var session = new Session(clientId, client);

            session.Start();
        }

        private static void StartSessionInNewAppDomain(int clientId, Socket client)
        {
            // ソケットの複製を作成し、このプロセスのソケットを閉じる
            var duplicatedSocketInfo = client.DuplicateAndClose(Process.GetCurrentProcess().Id);

            // 新しいアプリケーションドメインを作成してクライアントを処理する
            var newAppDomain = AppDomain.CreateDomain(string.Format("client #{0}", clientId));

            var session = (Session)newAppDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,
                                                                       typeof(Session).FullName,
                                                                       false,
                                                                       BindingFlags.Default,
                                                                       null,
                                                                       new object[] { clientId, duplicatedSocketInfo },
                                                                       null,
                                                                       null);

            session.Start();
        }
    }

// セッションを処理するクラス
// (MarshalByRefObjectの継承はアプリケーションドメインをまたがってインスタンスを使用するために必要)
    public class Session : MarshalByRefObject
    {
        private readonly int id;
        private Socket client;

        public Session(int id, SocketInformation socketInfo)
        {
            this.id = id;
            this.client = new Socket(socketInfo);
        }

        public Session(int id, Socket socket)
        {
            this.id = id;
            this.client = socket;
        }

        public void Start()
        {
            // スレッドを作成し、クライアントを処理する
            var t = new Thread(SessionProc);

            t.Start();
        }

        private void SessionProc()
        {
            Console.WriteLine("#{0} session started", id);

            try
            {
                var buffer = new byte[0x100];

                for (; ; )
                {
                    // クライアントから送信された内容を受信する
                    var len = client.Receive(buffer);

                    if (0 < len)
                    {
                        // 受信した内容を表示する
                        Console.Write("#{0}> {1}", id, Encoding.Default.GetString(buffer, 0, len));

                        // 受信した内容した内容をそのままクライアントに送信する
                        client.Send(buffer, len, SocketFlags.None);
                    }
                    else
                    {
                        // 切断された
                        client.Close();

                        break;
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.ConnectionReset)
                    // 切断された以外の場合では例外を再スローする
                    throw;
            }

            Console.WriteLine("#{0} session closed", id);
        }
    }
}
