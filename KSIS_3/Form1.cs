using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace KSIS_3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public bool conn = false;
        public List<Client> clients = new List<Client>();
        public class Client
        {
            public IPEndPoint iep;
            public String name;
            public Client(IPEndPoint iepp, string str)
            {
                iep = iepp;
                name = str;
            }
        }

        class Packet
        {
            public int type;
            public int length;
            public byte[] data;

            public Packet(int typ)
            {
                type = typ;
                length = 2 * sizeof(int);
                data = null;
            }

            public Packet(int typ, int len, byte[] dat)
            {
                type = typ;
                length = len + 2 * sizeof(int);
                data = new byte[dat.Length];
                Buffer.BlockCopy(dat,0,data,0,dat.Length);
            }

            public Packet(byte[] dat)
            {
                type = BitConverter.ToInt32(dat, 0);
                length = BitConverter.ToInt32(dat, sizeof(int));
                data = new byte[dat.Length];
                Buffer.BlockCopy(dat, 0, data, 0, dat.Length);
            }
            public byte[] getBytes()
            {
                byte[] dat = new byte[length];
                Buffer.BlockCopy(BitConverter.GetBytes(type), 0, dat, 0, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(length), 0, dat, sizeof(int), sizeof(int));
                if (data != null)
                    Buffer.BlockCopy(data, 0, dat, 2 * sizeof(int), length - 2 * sizeof(int));
                return dat;
            }
        }

        public static String GetName(IPEndPoint iep, List<Client> list)
        {
            foreach(Client c in list)
            {
                if (IPEndPoint.Equals(c.iep,iep))
                {
                    return c.name;
                }
            }
            return null;
        }

        private void Connect()
        {
            UdpClient client = new UdpClient();
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("230.230.230.230"), 228);
            try
            {
                Packet msg = new Packet(0, Encoding.Unicode.GetBytes(textBox2.Text).Length, Encoding.Unicode.GetBytes(textBox2.Text));
                client.Send(msg.getBytes(), msg.length, iep);
                client.Close();

                if (!conn)
                {
                    Task UdpRec = new Task(UdpReceive);
                    Task TcpRec = new Task(TcpReceive);
                    UdpRec.Start();
                    TcpRec.Start();
                }
                conn = true;
                MessageBox.Show("Вы подключились к чату");
            }
            catch (Exception ex)
            {
                client.Close();
                MessageBox.Show(ex.Message);
            }
        }
        private void UdpReceive()
        {
            UdpClient client = new UdpClient(228);
            client.JoinMulticastGroup(IPAddress.Parse("230.230.230.230"), 50);
            IPEndPoint iep = null;
            try
            {
                while (true)
                {
                    byte[] data = client.Receive(ref iep);
                    Packet msg = new Packet(data);
                    MessageBox.Show(Encoding.Unicode.GetString(msg.data));
                    switch (msg.type)
                    {
                        case 0:
                            bool flag = true;
                            foreach (Client c in clients)
                            { 
                                if (clients.Contains(c))
                                {
                                    flag = false;
                                }
                            }
                            if (flag)
                            {
                                Client c = new Client(iep, Encoding.Unicode.GetString(msg.data));
                                clients.Add(c);
                            }
                            TcpAnswer();
                            break;
                        case 1:
                            foreach (Client c in clients)
                            {
                                if (clients.Contains(c))
                                {
                                    clients.Remove(c);
                                    listChat.Items.Add(c.name + " покинул чат");
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        private void TcpAnswer()
        {
            IPEndPoint remoteIp = null;
            TcpClient sender = new TcpClient(remoteIp);
            NetworkStream stream = sender.GetStream();
            Packet msg = new Packet(Encoding.Unicode.GetBytes(textBox2.Text));
            stream.Write(msg.getBytes(), 0, msg.length);
            stream.Close();
            sender.Close();
        }

        private void TcpReceive()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse(GetLocalIPAddress()), 50);
            listener.Start();
            byte[] data = new byte[65536];

            while (conn)
            {
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                stream.Read(data, 0, data.Length);
                Packet msg = new Packet(data);
                IPEndPoint iep = (IPEndPoint)client.Client.RemoteEndPoint;
                switch (msg.type)
                {
                    case 0:
                        Client c = new Client(iep, Encoding.Unicode.GetString(msg.data));
                        if (!clients.Contains(c))
                        {
                            clients.Add(c);
                            listChat.Items.Add(GetName(iep, clients) + " присоединился");
                        }
                        break;
                    case 1:
                        listChat.Items.Add(GetName(iep,clients) + ":" + Encoding.Unicode.GetString(msg.data));
                        break;
                }
            }
            
            listener.Stop();
        }

        private void LeaveChat()
        {
            conn = false;
            UdpClient client = new UdpClient();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("230.230.230.230"), 228);
            try
            {
                Packet msg = new Packet(1);
                client.Send(msg.getBytes(), msg.length, ep);
                client.Close();
                MessageBox.Show("Вы отключились");
            }
            catch (Exception ex)
            {
                client.Close();
                MessageBox.Show(ex.Message);
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (String.Compare(textBox2.Text, "") != 0)
            {
                btnFind.Enabled = true;
            }
        }

        private void btnLeave_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UdpClient client = new UdpClient();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("230.230.230.230"), 228);
            try
            {
                Packet msg = new Packet(1);
                client.Send(msg.getBytes(), msg.length, ep);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
