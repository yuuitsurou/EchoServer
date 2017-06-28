using System;

namespace CoreEcho
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpHelper.StartServer(5678);
            TcpHelper.Listen();
        }
    }
}
