using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using CameraToolkits;
using HkvsRecordDemo.Viewmodels;
using HkvsRecordDemo.DataClass;
using System.Diagnostics;
using System.Threading;

namespace HkvsRecordDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Camera Connect Settings
        private bool m_bInitSDK = false;
        private uint iLastErr = 0;
        private Int32 m_lUserID = -1;
        private Int32 m_lFindHandle = -1;
        private Int32 m_lPlayHandle = -1;
        private Int32 m_lDownHandle = -1;
        private string str;
        private string str1;
        private string str2;
        private string str3;
        private string sPlayBackFileName = null;
        private Int32 i = 0;
        private Int32 m_lTree = 0;

        private bool m_bPause = false;
        private bool m_bReverse = false;
        private bool m_bSound = false;

        private long iSelIndex = 0;
        private uint dwAChanTotalNum = 0;
        private uint dwDChanTotalNum = 0;
        public CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo;
        public CHCNetSDK.NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;
        public CHCNetSDK.NET_DVR_GET_STREAM_UNION m_unionGetStream;
        public CHCNetSDK.NET_DVR_IPCHANINFO m_struChanInfo;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 96, ArraySubType = UnmanagedType.U4)]
        private int[] iChannelNum;

        #region Camera User Info
        private string cIPAddress;
        private string cPortNumber;
        private string cUserName;
        private string cPassword;
        #endregion

        #region Record Info
        private string rRecordPath;

        private DateTime rStartTime;
        private CHCNetSDK.NET_DVR_TIME rStrucStartTime;
        private DateTime rEndTime;
        private CHCNetSDK.NET_DVR_TIME rStrucEndTime;

        private bool isRecording = false;

        private List<int> mDownloadHanelerList;
        #endregion


        WindowViewModel windowViewModel;

        // 1. Init SDK
        // 2. User Login
        // 3. User Logout
        // 4. Release SDk

        public MainWindow()
        {
            InitializeComponent();

            windowViewModel = new WindowViewModel();
            windowViewModel.CameraInfos = new System.Collections.ObjectModel.ObservableCollection<CameraInfo>();
            this.DataContext = windowViewModel;

            // Init Camera SDK
            InitSDK();

            // Load Config from xml
            LoadConfig();

            // 2. User Login
            LoginDevices();

            this.Closed += MainWindow_Closed;



        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            LogoutDevices();
        }

        // Init Camera SDK
        public void InitSDK()
        {
            // 1. Init SDK
            m_bInitSDK = CHCNetSDK.NET_DVR_Init();

            if (m_bInitSDK == false)
            {
                LogHandler.WriteLog("NET_DVR_Init error!");
                return;
            }
            else
            {
                //保存SDK日志
                CHCNetSDK.NET_DVR_SetLogToFile(3, "\\log\\CameraSdkLog\\", true);
                iChannelNum = new int[96];
            }
        }

        // Load Config from xml
        public void LoadConfig()
        {
            // Open xml file
            System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.Load("Config/CameraConfig.xml");

            // Get Camera Info
            cIPAddress = xmlDoc.SelectSingleNode("CameraConfig/CameraIP").InnerText;
            cPortNumber = xmlDoc.SelectSingleNode("CameraConfig/CameraPort").InnerText;
            cUserName = xmlDoc.SelectSingleNode("CameraConfig/CameraUserName").InnerText;
            cPassword = xmlDoc.SelectSingleNode("CameraConfig/CameraPassword").InnerText;

            rRecordPath = xmlDoc.SelectSingleNode("CameraConfig/VideoPath").InnerText;
            
        }

        // Login Devices
        public void LoginDevices()
        {
            // 2. User Login
            if(cIPAddress == "" || cPortNumber == "" || cUserName == "" || cPassword == "")
            {
                LogHandler.WriteLog("Camera Info Valid!");
                return;
            }
            if (m_lUserID < 0)
            {
                string DVRIPAddress = cIPAddress; //设备IP地址或者域名
                Int16 DVRPortNumber = Int16.Parse(cPortNumber);//设备服务端口号
                string DVRUserName = cUserName;//设备登录用户名
                string DVRPassword = cPassword;//设备登录密码

                //登录设备 Login the device
                m_lUserID = CHCNetSDK.NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo);

                if (m_lUserID < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str1 = "NET_DVR_Login_V30 failed, error code= " + iLastErr; //登录失败，输出错误号
                    LogHandler.WriteLog(str1);
                    return;
                }
                else
                {
                    //登录成功
                    LogHandler.WriteLog("Login Success!");

                    dwAChanTotalNum = (uint)DeviceInfo.byChanNum;
                    dwDChanTotalNum = (uint)DeviceInfo.byIPChanNum + 256 * (uint)DeviceInfo.byHighDChanNum;

                    if (dwDChanTotalNum > 0)
                    {
                        InfoIPChannel();
                    }
                    else
                    {
                        for (i = 0; i < dwAChanTotalNum; i++)
                        {
                            //ListAnalogChannel(i + 1, 1);
                            iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                        }
                        // MessageBox.Show("This device has no IP channel!");
                    }
                }

            }
        }

        public void InfoIPChannel()
        {
            uint dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40);

            IntPtr ptrIpParaCfgV40 = Marshal.AllocHGlobal((Int32)dwSize);
            Marshal.StructureToPtr(m_struIpParaCfgV40, ptrIpParaCfgV40, false);

            uint dwReturn = 0;
            int iGroupNo = 0; //该Demo仅获取第一组64个通道，如果设备IP通道大于64路，需要按组号0~i多次调用NET_DVR_GET_IPPARACFG_V40获取
            if (!CHCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_GET_IPPARACFG_V40, iGroupNo, ptrIpParaCfgV40, dwSize, ref dwReturn))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str1 = "NET_DVR_GET_IPPARACFG_V40 failed, error code= " + iLastErr; //获取IP资源配置信息失败，输出错误号
                MessageBox.Show(str1);
            }
            else
            {
                // succ
                m_struIpParaCfgV40 = (CHCNetSDK.NET_DVR_IPPARACFG_V40)Marshal.PtrToStructure(ptrIpParaCfgV40, typeof(CHCNetSDK.NET_DVR_IPPARACFG_V40));

                for (i = 0; i < dwAChanTotalNum; i++)
                {
                    //ListAnalogChannel(i + 1, m_struIpParaCfgV40.byAnalogChanEnable[i]);
                    iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                }

                byte byStreamType;
                uint iDChanNum = 64;

                if (dwDChanTotalNum < 64)
                {
                    iDChanNum = dwDChanTotalNum; //如果设备IP通道小于64路，按实际路数获取
                }

                for (i = 0; i < iDChanNum; i++)
                {
                    iChannelNum[i + dwAChanTotalNum] = i + (int)m_struIpParaCfgV40.dwStartDChan;

                    byStreamType = m_struIpParaCfgV40.struStreamMode[i].byGetStreamType;
                    m_unionGetStream = m_struIpParaCfgV40.struStreamMode[i].uGetStream;

                    switch (byStreamType)
                    {
                        //目前NVR仅支持0- 直接从设备取流一种方式
                        case 0:
                            dwSize = (uint)Marshal.SizeOf(m_unionGetStream);
                            IntPtr ptrChanInfo = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_unionGetStream, ptrChanInfo, false);
                            m_struChanInfo = (CHCNetSDK.NET_DVR_IPCHANINFO)Marshal.PtrToStructure(ptrChanInfo, typeof(CHCNetSDK.NET_DVR_IPCHANINFO));

                            //列出IP通道
                            //ListIPChannel(i + 1, m_struChanInfo.byEnable, m_struChanInfo.byIPID);
                            //Add Camera
                            CameraInfo cameraInfo = new CameraInfo();
                            cameraInfo.channelNo = i;

                            if (m_struChanInfo.byIPID == 0)
                            {
                                str2 = "X"; //通道空闲，没有添加前端设备                 
                            }
                            else
                            {
                                if (m_struChanInfo.byEnable == 0)
                                {
                                    str2 = "offline"; //通道不在线
                                }
                                else
                                    str2 = "online"; //通道在线
                            }

                            cameraInfo.status = str2;
                            windowViewModel.CameraInfos.Add(cameraInfo);


                            Marshal.FreeHGlobal(ptrChanInfo);
                            break;

                        default:
                            break;
                    }
                }
            }
            Marshal.FreeHGlobal(ptrIpParaCfgV40);
        }

        // 3. User Logout
        public void LogoutDevices()
        {
            //停止回放 Stop playback
            if (m_lPlayHandle >= 0)
            {
                CHCNetSDK.NET_DVR_StopPlayBack(m_lPlayHandle);
                m_lPlayHandle = -1;
            }

            //停止下载 Stop download
            if (m_lDownHandle >= 0)
            {
                CHCNetSDK.NET_DVR_StopGetFile(m_lDownHandle);
                m_lDownHandle = -1;
            }

            //注销登录 Logout the device
            if (m_lUserID >= 0)
            {
                CHCNetSDK.NET_DVR_Logout(m_lUserID);
                m_lUserID = -1;
            }

            if (m_bInitSDK == true)
            {
                CHCNetSDK.NET_DVR_Cleanup();
            }
        }

        public void ListAnalogChannel(Int32 iChanNo, byte byEnable)
        {
            str1 = String.Format("Camera {0}", iChanNo);
            m_lTree++;

            if (byEnable == 0)
            {
                str2 = "Disabled"; //通道已被禁用 This channel has been disabled               
            }
            else
            {
                str2 = "Enabled"; //通道处于启用状态
            }

        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {

            if(!isRecording)
            {
                //Get Device ID
            
                //Get Device Time
                CHCNetSDK.NET_DVR_TIME struNetDVRTime = new CHCNetSDK.NET_DVR_TIME();
                uint dwSize = (uint)Marshal.SizeOf(struNetDVRTime);
                IntPtr ptrNetDVRTime = Marshal.AllocHGlobal((Int32)dwSize);
                Marshal.StructureToPtr(struNetDVRTime, ptrNetDVRTime, false);
                uint dwReturn = 0;

                
                if(!CHCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_GET_TIMECFG, -1, ptrNetDVRTime, dwSize, ref dwReturn))
                {

                }
                else
                {
                    struNetDVRTime = (CHCNetSDK.NET_DVR_TIME)Marshal.PtrToStructure(ptrNetDVRTime, typeof(CHCNetSDK.NET_DVR_TIME));
                    DateTime nvrTime = new DateTime(Convert.ToInt32(struNetDVRTime.dwYear), Convert.ToInt32(struNetDVRTime.dwMonth), Convert.ToInt32(struNetDVRTime.dwDay), Convert.ToInt32(struNetDVRTime.dwHour), Convert.ToInt32(struNetDVRTime.dwMinute), Convert.ToInt32(struNetDVRTime.dwSecond));
                    NvrTimeText.Text = "Start Time: " +  nvrTime.ToString("yyyy-MM-dd HH:mm:ss");

                    rStartTime = nvrTime;
                    rStrucStartTime = struNetDVRTime;

                    isRecording = true;

                    RecordButton.Content = "Stop Recording";

                }

                Marshal.FreeHGlobal(ptrNetDVRTime);

            }
            else
            {
                isRecording = false;
                RecordButton.Content = "Start Recording";

                //Get Device Time
                CHCNetSDK.NET_DVR_TIME struNetDVRTime = new CHCNetSDK.NET_DVR_TIME();
                uint dwSize = (uint)Marshal.SizeOf(struNetDVRTime);
                IntPtr ptrNetDVRTime = Marshal.AllocHGlobal((Int32)dwSize);
                Marshal.StructureToPtr(struNetDVRTime, ptrNetDVRTime, false);
                uint dwReturn = 0;

                
                if(!CHCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_GET_TIMECFG, -1, ptrNetDVRTime, dwSize, ref dwReturn))
                {

                }
                else
                {
                    struNetDVRTime = (CHCNetSDK.NET_DVR_TIME)Marshal.PtrToStructure(ptrNetDVRTime, typeof(CHCNetSDK.NET_DVR_TIME));
                    DateTime nvrTime = new DateTime(Convert.ToInt32(struNetDVRTime.dwYear), Convert.ToInt32(struNetDVRTime.dwMonth), Convert.ToInt32(struNetDVRTime.dwDay), Convert.ToInt32(struNetDVRTime.dwHour), Convert.ToInt32(struNetDVRTime.dwMinute), Convert.ToInt32(struNetDVRTime.dwSecond));
                    NvrTimeText.Text = nvrTime.ToString("yyyy-MM-dd HH:mm:ss");

                    rEndTime = nvrTime;
                    rStrucEndTime = struNetDVRTime;

                    

                }

                Marshal.FreeHGlobal(ptrNetDVRTime);

                DownloadVideoAsync();

            }    
        }

    
        public async Task DownloadVideoFromNVR(string filename, CHCNetSDK.NET_DVR_PLAYCOND struDownPara)
        {
            //按时间下载 Download by time
            m_lDownHandle = CHCNetSDK.NET_DVR_GetFileByTime_V40(m_lUserID, filename, ref struDownPara);
            if (m_lDownHandle < 0)
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "NET_DVR_GetFileByTime_V40 failed, error code= " + iLastErr;
                LogHandler.WriteLog(str);
                return;
            }
            uint iOutValue = 0;
            if (!CHCNetSDK.NET_DVR_PlayBackControl_V40(m_lDownHandle, CHCNetSDK.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "NET_DVR_PLAYSTART failed, error code= " + iLastErr; //下载控制失败，输出错误号
                LogHandler.WriteLog(str);
                return;
            }

            //Get Download Progress
            int iPos = 0;
            await Task.Run(() => {
                while (true)
                {
                    iPos = CHCNetSDK.NET_DVR_GetDownloadPos(m_lDownHandle);
                    
                    if(iPos > 0 && iPos < 100)
                    {
                        Debug.WriteLine("The current download progress is " + iPos + "%");
                    }

                    if (iPos == 100)  //下载完成
                    {
                        
                        if (!CHCNetSDK.NET_DVR_StopGetFile(m_lDownHandle))
                        {
                            iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                            str = "NET_DVR_StopGetFile failed, error code= " + iLastErr; //下载控制失败，输出错误号
                            LogHandler.WriteLog(str);
                            return;
                        }
                        m_lDownHandle = -1;
                    }

                    if (iPos == 200) //网络异常，下载失败
                    {
                        LogHandler.WriteLog("The downloading is abnormal for the abnormal network!");
                        return;
                    }

                    Thread.Sleep(1000);
                }
            });

        }

        public async Task DownloadVideoAsync()
        {
            //Get All Online Cameras
            List<CameraInfo> onlineCameras = new List<CameraInfo>();
            foreach (CameraInfo cameraInfo in windowViewModel.CameraInfos)
            {
                if (cameraInfo.status == "online")
                {
                    onlineCameras.Add(cameraInfo);
                }
            }

            //Download Each Camera Video
            foreach (CameraInfo cameraInfo in onlineCameras)
            {
                //Get Camera ID
                int currCam = cameraInfo.channelNo;

                string sVideoFilename = "";
                sVideoFilename = rRecordPath + "Channel" + cameraInfo.channelNo + "_" + rStartTime.ToString("yyyyMMddHHmmss") + ".mp4";

                CHCNetSDK.NET_DVR_PLAYCOND struDownPara = new CHCNetSDK.NET_DVR_PLAYCOND();
                struDownPara.dwChannel = (uint)iChannelNum[currCam]; //通道号 Channel number  
                struDownPara.struStartTime = rStrucStartTime; //开始时间 Start time
                struDownPara.struStopTime = rStrucEndTime; //结束时间 End time

                await DownloadVideoFromNVR(sVideoFilename, struDownPara);

            }
        }
    }
}
