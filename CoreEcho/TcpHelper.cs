using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace CoreEcho
{
    public class TcpHelper 
    {
        private static TcpListener listener { get; set; }  
        private static bool accept { get; set; } = false;  
   
        private static List<String> dic { get; set; }

        private static List<String> midasi { get; set; }

        public static void StartServer(int port) 
        {  
            IPAddress address = IPAddress.Parse("127.0.0.1");  
            listener = new TcpListener(address, port);  
   
            listener.Start();  
            accept = true;  
   
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            String [] ls = File.ReadAllLines("/usr/share/skk/SKK-JISYO.L", Encoding.GetEncoding("EUC-JP"));
            if (ls != null)
            {
                dic = new List<string>(ls);
                dic.Sort();
                 midasi = new List<String>();
                foreach (String i in dic)
                {
                    midasi.Add(i.Split(' ')[0]);
                }
            }
            Console.WriteLine($"Server started. Listening to TCP clients at 127.0.0.1:{port}");  
       }  
   
        public static void Listen()
       {  
            if(listener != null && accept) 
            {  
   
                // Continue listening.  
                while (true)
                {  
                    Console.WriteLine("Waiting for client...");  
                    var clientTask = listener.AcceptTcpClientAsync(); // Get the client  
   
                    if(clientTask.Result != null)
                    {  
                        Console.WriteLine("Client connected. Waiting for data.");  
                        var client = clientTask.Result;  
                        string message = "";
                        while (message != null && !message.StartsWith("quit"))
                        {  
                            byte[] data = Encoding.ASCII.GetBytes("Send next data: [enter 'quit' to terminate] ");  
                            client.GetStream().Write(data, 0, data.Length);  

                            byte[] resBytes = new byte[1024];  
                            int resSize = client.GetStream().Read(resBytes, 0, resBytes.Length);
                            if (resSize == 0) { break; }
                            // message = Encoding.ASCII.GetString(buffer);
                            message = Encoding.UTF8.GetString(resBytes, 0, resSize);
                            if (!message.StartsWith("quit")) 
                            {
                                message = message.TrimEnd('\n');
                                message = message.TrimEnd('\r');
                                int index = midasi.BinarySearch(message);
                                if (index > -1)
                                {
                                    data = Encoding.UTF8.GetBytes(dic[index].Split(' ')[1]);
                                }
                                else
                                {
                                    data = Encoding.UTF8.GetBytes("Not found...");
                                }
                                client.GetStream().Write(data, 0, data.Length);
                            }
                        }  
                        Console.WriteLine("Closing connection.");  
                        client.GetStream().Dispose();  
                    }  
                }  
            }  
        }  
    }   
}