﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncChatClient
{
    public partial class MainForm : Form
    {
        private bool isExit = false;
        private TcpClient client;
        private BinaryReader br;
        private BinaryWriter bw;

        public MainForm()
        {
            InitializeComponent();
            Random r = new Random((int)DateTime.Now.Ticks);
            textBoxUserName.Text = "user" + r.Next(100, 999);
            listBoxOnlineStatus.HorizontalScrollbar = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            buttonConnect.Enabled = false;
            try
            {
                //此处为方便演示，实际使用时要讲Dns.GetHostName()改成服务器域名
                client = new TcpClient(Dns.GetHostName(),51888);
                AddTalkMessage("连接成功");
            }
            catch 
            {
                AddTalkMessage("连接失败");
                buttonConnect.Enabled = true;
                return;
            }
            //获取网络流
            NetworkStream networkStream = client.GetStream();
            //将网络流作为二进制读写对象
            br = new BinaryReader(networkStream);
            bw = new BinaryWriter(networkStream);
            SendMessage("Login," + textBoxUserName.Text);
            Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
            threadReceive.IsBackground = true;
            threadReceive.Start();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (listBoxOnlineStatus.SelectedIndex!=-1)
            {
                SendMessage("Talk," + listBoxOnlineStatus.SelectedItem + "," + textBoxSend.Text + "\r\n");
                textBoxSend.Clear();
            }
            else
            {
                MessageBox.Show("请现在[当前在线]中选择一个对话者");
            }
        }

        /// <summary>
        /// 关闭窗口时触发的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client!=null)
            {
                SendMessage("Logout," + textBoxUserName.Text);
                isExit = true;
                br.Close();
                bw.Close();
                client.Close();
            }
        }

        /// <summary>
        /// 在发送消息文本框中按下【Enter】键触发的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar==(char)Keys.Return)
            {
                //触发buttonSend的click事件
                buttonSend.PerformClick();
            }
        }

        /// <summary>
        /// 处理接收的服务器端数据
        /// </summary>
        private void ReceiveData()
        {
            string receiveString = null;
            while (isExit==false)
            {
                try
                {
                    //从网络流中读出字符串
                    //此方法会自动判断字符串长度前缀,并跟进表长度前缀读出字符串
                    receiveString = br.ReadString();
                }
                catch 
                {
                    if (isExit==false)
                    {
                        MessageBox.Show("与服务器失去联系");
                    }
                    break;
                }
                string[] splitString = receiveString.Split(',');
                string command = splitString[0].ToLower();
                switch (command)
                {
                    case "login":   //格式:login,用户名
                        AddOnline(splitString[1]);
                        break;
                    case "logout":  //格式:logout,用户名
                        RemoveUserName(splitString[1]);
                        break;
                    case "talk":    //格式:talk,用户名,对话信息
                        AddTalkMessage(splitString[1] + ":\r\n");
                        AddTalkMessage(receiveString.Substring(splitString[0].Length + splitString[1].Length + 2));
                        break;
                    default:
                        AddTalkMessage("什么意思啊：" + receiveString);
                        break;
                }
            }
            Application.Exit();
        }

        /// <summary>
        /// 向服务器端发送消息
        /// </summary>
        /// <param name="message">消息内容</param>
        private void SendMessage(string message)
        {
            try
            {
                //将字符串写入网络流，此方法会自动附加字符串长度前缀
                bw.Write(message);
                bw.Flush();
            }
            catch
            {
                AddTalkMessage("发送失败！");
            }
        }

        private delegate void MessageDelegate(string message);
        /// <summary>
        /// 在richTextBoxTalkInfo中追加聊天信息
        /// </summary>
        /// <param name="message"></param>
        private void AddTalkMessage(string message)
        {
            if (richTextBoxTalkInfo.InvokeRequired)
            {
                MessageDelegate d = new MessageDelegate(AddTalkMessage);
                richTextBoxTalkInfo.Invoke(d, new object[] { message });
            }
            else
            {
                richTextBoxTalkInfo.AppendText(message + Environment.NewLine);
                richTextBoxTalkInfo.ScrollToCaret();
            }
        }

        private delegate void AddOnlineDelegate(string message);
        /// <summary>
        /// 在listBoxOnlineStatus中添加在线的其它客户端信息
        /// </summary>
        /// <param name="userName"></param>
        private void AddOnline(string userName)
        {
            if (listBoxOnlineStatus.InvokeRequired)
            {
                AddOnlineDelegate d = new AddOnlineDelegate(AddOnline);
                listBoxOnlineStatus.Invoke(d, new object[] { userName });
            }
            else
            {
                listBoxOnlineStatus.Items.Add(userName);
                listBoxOnlineStatus.SelectedIndex = listBoxOnlineStatus.Items.Count - 1;
                listBoxOnlineStatus.ClearSelected();
            }
        }

        private delegate void RemoveUserNameDelegate(string userName);
        /// <summary>
        /// 在listBoxOnlineStatus中移除不在线的其他客户端信息
        /// </summary>
        /// <param name="userName"></param>
        private void RemoveUserName(string userName)
        {
            if (listBoxOnlineStatus.InvokeRequired)
            {
                RemoveUserNameDelegate d = RemoveUserName;
                listBoxOnlineStatus.Invoke(d, userName);
            }
            else
            {
                listBoxOnlineStatus.Items.Remove(userName);
                listBoxOnlineStatus.SelectedIndex = listBoxOnlineStatus.Items.Count - 1;
                listBoxOnlineStatus.ClearSelected();
            }
        }

        
    }
}
