using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace auctionsrv
{
    public class Program
    {
        private static int PORT = 11000;
        private static int TAM = 1024;
        private static string data = null;

        public static void StartServer()
        {
            Task t1, t2;
            bool inicioSubasta = false;
            bool finSubasta = false;
            List<Socket> listaClientes = new List<Socket>();
            List<int> listaPujas = new List<int>();

            byte[] bytes = new Byte[TAM];

            IPAddress ipAddress = GetLocalIpAddress();
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Blocking = false;

            Console.WriteLine("Esperando conexiones en {0}:{1}", ipAddress, PORT);

            Console.WriteLine("Pulse INTRO cuando desee comenzar la subasta.");

            t1 = Task.Run(() =>
            {
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    while (!inicioSubasta)
                    {
                        try
                        {
                            Socket s = listener.Accept();
                            if (s != null)
                            {
                                listaClientes.Add(s);
                                Console.WriteLine("Conectado el cliente {0}", listaClientes.Count);
                                Console.WriteLine("Recuerde que al pulsar INTRO comenzará la subasta.");
                            }
                        }
                        catch (Exception) { } // !!! Blocking = false
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            });

            t2 = Task.Run(() =>
            {
                Console.ReadLine();
                inicioSubasta = true;
                listener.Close();
            });

            t1.Wait();
            Console.WriteLine("COMIENZA LA SUBASTA");

            while (!finSubasta)
            {
                Console.WriteLine("Introduzca el producto a subastar (FIN para acabar)");
                string articulo = Console.ReadLine();
                if ((articulo.Trim().Length > 0) && (!articulo.Trim().ToUpper().Equals("FIN")))
                {
                    Console.WriteLine("Esperando las pujas de los clientes...");
                    listaPujas.Clear();
                    byte[] data = Encoding.ASCII.GetBytes(articulo);
                    foreach (Socket s in listaClientes)
                    {
                        s.Blocking = true; // !!! 
                        s.Send(data);
                        int bytesRec = s.Receive(bytes);
                        Program.data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        listaPujas.Add(int.Parse(Program.data));
                    }
                    int maxPrecio = 0;
                    int maxCliente = -1;
                    int j = 0;
                    foreach (int i in listaPujas)
                    {
                        j++;
                        if (i > maxPrecio)
                        {
                            maxPrecio = i;
                            maxCliente = j;
                        }
                    }
                    string msgSell = $"El producto ha sido vendido a {maxCliente} por el precio {maxPrecio}";
                    if (maxCliente == -1)
                    {
                        msgSell = "El producto NO ha sido vendido a nadie";
                    }
                    Console.WriteLine(msgSell);
                    j = 0;
                    string msgWin = $"Enhorabuena has adquirido el producto por {maxPrecio}";
                    foreach (Socket s in listaClientes)
                    {
                        j++;
                        string msg;
                        if (j == maxCliente)
                        {
                            msg = msgWin;
                        }
                        else
                        {
                            msg = msgSell;
                        }
                        byte[] dataMsg = Encoding.ASCII.GetBytes(msg);
                        s.Send(dataMsg);
                    }

                }
                else
                {
                    finSubasta = true;
                }
            }

            Console.WriteLine("Fin de la subasta");
            foreach (Socket s in listaClientes)
            {
                s.Shutdown(SocketShutdown.Both);
                s.Close();
            }

        }

        private static IPAddress GetLocalIpAddress()
        {
            List<IPAddress> ipAddressList = new List<IPAddress>();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            int t = ipHostInfo.AddressList.Length;
            string ip;
            for (int i = 0; i < t; i++)
            {
                ip = ipHostInfo.AddressList[i].ToString();
                if (ip.Contains(".") && !ip.Equals("127.0.0.1")) ipAddressList.Add(ipHostInfo.AddressList[i]);
            }
            if (ipAddressList.Count == 1)
            {
                return ipAddressList[0];
            }
            else
            {
                int i = 0;
                foreach (IPAddress ipa in ipAddressList)
                {
                    Console.WriteLine($"[{i++}]: {ipa}");
                }
                t = ipAddressList.Count - 1;
                System.Console.Write($"Opción [0-{t}]: ");
                string s = Console.ReadLine();
                if (Int32.TryParse(s, out int j))
                {
                    if ((j >= 0) && (j <= t))
                    {
                        return ipAddressList[j];
                    }
                }
                return null;
            }
        }

        public static int Main(String[] args)
        {
            StartServer();
            return 0;
        }
    }
}
