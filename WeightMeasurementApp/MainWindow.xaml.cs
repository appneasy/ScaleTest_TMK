using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WeightMeasurementApp.Models;     // สำหรับใช้งาน ConfigModel
using WeightMeasurementApp.Helpers;    // สำหรับใช้งาน ConfigManager


namespace WeightMeasurementApp
{
    public partial class MainWindow : Window
    {
        private SerialPort serialPort = new SerialPort(); // ✅ Initialize here
        private SmartLogic smartLogic;
        private StringBuilder buffer = new StringBuilder();
        private string? lastRawData = null;  // เก็บข้อมูลดิบล่าสุดเพื่อใช้ในการเรียนรู้

        // ======== ตัวแปรสำหรับติดตามน้ำหนัก ========
        private double lastValidWeight = -1;       // น้ำหนักที่เสถียรล่าสุด
        private const double NoiseThreshold = 10;  // เกณฑ์สำหรับการตรวจจับเสียงรบกวน
        private DateTime? lastStableTime = null;   // เวลาที่น้ำหนักเริ่มนิ่ง
        private const double stableThreshold = 0.5; // การเปลี่ยนแปลงที่ยอมรับได้
        private const int stableTimeLimit = 1;     // เวลานิ่งที่ต้องการ (วินาที)
    //    private double lastWeightDisplayed = -1;   // น้ำหนักที่แสดงล่าสุด
        private bool isCurrentlyStable = false;    // สถานะปัจจุบัน - ว่านิ่งหรือไม่

        private ConsoleLogger logger; // ✅ ประกาศตัวแปร Logger
       // private ConfigModel currentConfig; // <<== เพิ่มบรรทัดนี้
        private ConfigModel _config;
        private bool isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
          //  UpdateButtonStates(false);

            // กำหนดค่าให้กับฟิลด์ที่เป็น non-nullable ทั้งหมด
            logger = ConsoleLogger.Instance; // ✅ สร้าง shared logger ที่พร้อมใช้งานก่อน
            smartLogic = new SmartLogic(logger,_config!);
            _config = new ConfigModel();

            LoadConfig();

           
         //   currentConfig = _config;

            serialPort = new SerialPort();
            InitializeSerialPort();
            LoadAvailablePorts();

            ShowCurrentConfigInTextBox();

            // สร้าง SmartLogic และฝึกด้วยข้อมูลตัวอย่าง
            InitializeSmartLogic();

            // เพิ่มการ redirect Console output ไปยัง TextBox
            RedirectConsoleOutput();
            LogToConsole("โปรแกรมเริ่มทำงานแล้ว");

            // Initialize log displays
            InitializeLogDisplays();

            ApplyConfigToUI();

            // อัพเดทสถานะปุ่มเริ่มต้น

            LogToConsole("โปรแกรมเริ่มทำงานแล้ว");

            try
            {
             //   logger = new ConsoleLogger();
                logger.Log("MainWindow initialized.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logger error: {ex.Message}");
            }
        }

        private void ShowCurrentConfigInTextBox()
        {
            if (_config == null) return;

            string info = $"Port: {_config.ComPort}, Baud: {_config.BaudRate}, Parity: {_config.Parity}, " +
                          $"DataBits: {_config.DataBits}, StopBits: {_config.StopBits}, Handshake: {_config.Handshake}\n" +
                          $"WeightStart: {_config.WeightStartPosition}, End: {_config.WeightEndPosition}, Digits: {_config.WeightDigits}\n" +
                          $"StableDelay: {_config.WeightStableDelay} ms, Min: {_config.WeightMinValue}, Max: {_config.WeightMaxValue}";

            txtCurrentConfigInfo.Text = info;
        }

        /// <summary>
        /// Initialize the log displays
        /// </summary>
        private void InitializeLogDisplays()
        {
            try
            {
                // ล้าง log ทั้งหมด
                txtHexPatternLog.Clear();
                txtHexToAsciiLog.Clear();
                txtConsoleLog.Clear();

                // เพิ่มหัวข้อ
                txtHexPatternLog.AppendText("=== Raw Hex Pattern Log ===" + Environment.NewLine);
                txtHexToAsciiLog.AppendText("=== ASCII Conversion Log ===" + Environment.NewLine);
                txtConsoleLog.AppendText("=== Console Log ===" + Environment.NewLine);

                LogToConsole("เตรียม log displays เรียบร้อย");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"เกิดข้อผิดพลาดในการเตรียม log displays: {ex.Message}");
            }
        }

        /// <summary>
        /// สร้างและฝึก SmartLogic ด้วยข้อมูลตัวอย่าง
        /// </summary>
        private void InitializeSmartLogic()
        {
            try
            {
                // สร้าง SmartLogic instance
                smartLogic = new SmartLogic(logger,_config);

                LogToConsole("SmartLogic ได้รับการตั้งค่าเรียบร้อยแล้ว");
            }
            catch (Exception ex)
            {
                LogToConsole($"เกิดข้อผิดพลาดในการเริ่มต้น SmartLogic: {ex.Message}");
            }
        }



        private void LoadConfig()
        {
            try
            {
                _config = ConfigManager.LoadConfig();
                logger.Log("Config loaded successfully.");
            }
            catch (Exception ex)
            {
                logger.Log($"Error loading config: {ex.Message}. Using default config.");
                _config = new ConfigModel();
            }
        }
        private void ApplyConfigToUI()
        {
            try
            {
                // ตรวจสอบว่า ComboBox ไม่เป็น null และตั้งค่าตามที่โหลดมา
                if (cmbComPort != null && _config != null)
                {
                    var portNames = SerialPort.GetPortNames();
                    if (portNames.Contains(_config.ComPort))
                    {
                        cmbComPort.SelectedItem = _config.ComPort;
                    }
                    else if (cmbComPort.Items.Count > 0)
                    {
                        cmbComPort.SelectedIndex = 0;
                    }
                }

                if (cmbBaudRate != null && _config != null)
                {
                    foreach (ComboBoxItem item in cmbBaudRate.Items)
                    {
                        if (item.Content?.ToString() == _config.BaudRate.ToString())
                        {
                            cmbBaudRate.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (cmbParity != null && _config != null)
                {
                    foreach (ComboBoxItem item in cmbParity.Items)
                    {
                        if (item.Content?.ToString() == _config.Parity)
                        {
                            cmbParity.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (cmbDataBits != null && _config != null)
                {
                    foreach (ComboBoxItem item in cmbDataBits.Items)
                    {
                        if (item.Content?.ToString() == _config.DataBits.ToString())
                        {
                            cmbDataBits.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (cmbStopBits != null && _config != null)
                {
                    foreach (ComboBoxItem item in cmbStopBits.Items)
                    {
                        if (item.Content?.ToString() == _config.StopBits)
                        {
                            cmbStopBits.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (cmbHandshake != null && _config != null)
                {
                    foreach (ComboBoxItem item in cmbHandshake.Items)
                    {
                        if (item.Content?.ToString() == _config.Handshake)
                        {
                            cmbHandshake.SelectedItem = item;
                            break;
                        }
                    }
                }

                LogToConsole("อัปเดต UI ตาม Config เรียบร้อยแล้ว");
            }
            catch (Exception ex)
            {
                LogToConsole($"เกิดข้อผิดพลาดในการอัปเดต UI: {ex.Message}");
            }
        }
        //private void ApplyConfigToUI()
        //{
        //    // ตรวจสอบ currentConfig ไม่เป็น null ก่อน
        //    if (currentConfig == null)
        //    {
        //        return; // หาก currentConfig เป็น null จะไม่ทำอะไร
        //    }

        //    //// ตรวจสอบ ComboBox ไม่เป็น null และ currentConfig.ComPort ไม่เป็น null
        //    //if (cmbComPort != null && !string.IsNullOrEmpty(currentConfig.ComPort))
        //    //{
        //    //    cmbComPort.SelectedItem = currentConfig.ComPort;
        //    //}

        //    //if (cmbBaudRate != null && currentConfig.BaudRate != null)
        //    //{
        //    //    cmbBaudRate.SelectedItem = currentConfig.BaudRate.ToString();
        //    //}

        //    //if (cmbParity != null && currentConfig.Parity != null)
        //    //{
        //    //    cmbParity.SelectedItem = currentConfig.Parity;
        //    //}

        //    //if (cmbDataBits != null && currentConfig.DataBits != null)
        //    //{
        //    //    cmbDataBits.SelectedItem = currentConfig.DataBits.ToString();
        //    //}

        //    //if (cmbStopBits != null && currentConfig.StopBits != null)
        //    //{
        //    //    cmbStopBits.SelectedItem = currentConfig.StopBits;
        //    //}

        //    //if (cmbHandshake != null && currentConfig.Handshake != null)
        //    //{
        //    //    cmbHandshake.SelectedItem = currentConfig.Handshake;
        //    //}
        //}

        //private void AutoConnectIfNeeded()
        //{
        //    if (_config != null && _config.AutoConnect)
        //    {
        //        Dispatcher.InvokeAsync(() => BtnConnect_Click(null, null));
        //    }
        //}

        /// <summary>
        /// Event handler for the Serial Port Settings menu item
        /// </summary>
        //private void MenuSerialPortSettings_Click(object sender, RoutedEventArgs e)
        //{
        //    //var configWindow = new SerialPortConfigWindow();
        //    //configWindow.Owner = this;
        //    //configWindow.ShowDialog();
        //    var configWindow = new SerialPortConfigWindow();
        //    configWindow.Owner = this;
        //    bool? result = configWindow.ShowDialog();
        //    ShowCurrentConfigInTextBox();

        //    // ✅ ถ้า user กด Save และมีการเปลี่ยนแปลง → โหลด config ใหม่และอัปเดต UI
        //    if (result == true)
        //    {
        //        _config = ConfigManager.LoadConfig();
        //        currentConfig = _config;
        //        ApplyConfigToUI();
        //        ShowCurrentConfigInTextBox();
        //        LogToConsole("โหลด config ใหม่หลังจากการบันทึกสำเร็จ");
        //    }
        //}
        private void MenuSerialPortSettings_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new SerialPortConfigWindow();
            configWindow.Owner = this;
            bool? result = configWindow.ShowDialog();

            // ถ้า user กด Save และมีการเปลี่ยนแปลง → โหลด config ใหม่และอัปเดต UI
            if (result == true)
            {
                _config = ConfigManager.LoadConfig();
                ShowCurrentConfigInTextBox();

                // อัปเดต SmartLogic ด้วย Config ใหม่ถ้าจำเป็น
                if (!isConnected)
                {
                    smartLogic = new SmartLogic(logger, _config);
                }

                ApplyConfigToUI();
                LogToConsole("โหลด config ใหม่หลังจากการบันทึกสำเร็จ");
            }
        }
        /// <summary>
        /// Event handler for the View Log menu item
        /// </summary>
        private void MenuViewLog_Click(object sender, RoutedEventArgs e)
        {
            // โฟลเดอร์ Log ของโปรแกรม (ตาม AppDomain.BaseDirectory)
            string logFolder = "wlog"; // หรือชื่อโฟลเดอร์ของคุณ
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFolder);

            // สร้าง OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            openFileDialog.InitialDirectory = folderPath; // ใช้ path นี้
            openFileDialog.Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.Title = "เลือกไฟล์ Log";

            // ถ้าเลือกไฟล์แล้ว
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                // เปิดไฟล์ด้วย notepad
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{selectedFilePath}\"",
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Event Handler สำหรับเมนู Exit
        /// </summary>
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            // ปิดการเชื่อมต่อกับเครื่องชั่งก่อนออกจากโปรแกรม (ถ้ายังเชื่อมต่ออยู่)
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    DisconnectFromScale();
                    LogToConsole("ตัดการเชื่อมต่อก่อนปิดโปรแกรม");
                }
                catch (Exception ex)
                {
                    LogToConsole($"เกิดข้อผิดพลาดระหว่างการตัดการเชื่อมต่อก่อนปิดโปรแกรม: {ex.Message}");
                }
            }

            // ปิดแอปพลิเคชัน
            LogToConsole("กำลังปิดโปรแกรม...");
            Application.Current.Shutdown();
        }


        /// <summary>
        /// กำหนดค่าเริ่มต้นสำหรับ Serial Port - แก้ไขปัญหาเพี้ยน
        /// </summary>
        private void InitializeSerialPort()
        {
            try
            {
                // สร้าง Serial Port ใหม่
                serialPort = new SerialPort();

                // กำหนด timeout
                serialPort.ReadTimeout = 2000;  // 2 วินาที
                serialPort.WriteTimeout = 2000; // 2 วินาที

                // กำหนดขนาด buffer
                serialPort.ReadBufferSize = 4096;
                serialPort.WriteBufferSize = 2048;

                // กำหนด Event Handler สำหรับรับข้อมูลจาก Serial Port
                serialPort.DataReceived += SerialPort_DataReceived;

                LogToConsole("เริ่มต้น Serial Port สำเร็จ");
            }
            catch (Exception ex)
            {
                LogToConsole($"เกิดข้อผิดพลาดในการเริ่มต้น Serial Port: {ex.Message}");
            }
        }


        /// <summary>
        /// โหลด COM Ports ที่มีอยู่ในระบบ
        /// </summary>
        private void LoadAvailablePorts()
        {
            try
            {
                // ล้าง combobox ก่อน
                cmbComPort.Items.Clear();

                // รับรายการพอร์ตที่มีอยู่
                string[] availablePorts = SerialPort.GetPortNames();

                if (availablePorts.Length > 0)
                {
                    // เพิ่มพอร์ตเข้าไปใน combobox
                    foreach (var port in availablePorts)
                    {
                        cmbComPort.Items.Add(port);
                    }

                    // เลือกพอร์ตแรกเป็น default
                    cmbComPort.SelectedIndex = 0;
                    LogToConsole($"พบ {availablePorts.Length} พอร์ตบนเครื่อง");
                }
                else
                {
                    LogToConsole("ไม่พบ COM port ในระบบ");
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"เกิดข้อผิดพลาดในการโหลดพอร์ต: {ex.Message}");
            }
        }

        /// <summary>
        /// Event Handler สำหรับปุ่ม Connect
        /// </summary>
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            // ตรวจสอบว่าผู้ใช้ได้เลือกการตั้งค่าพอร์ตครบหรือไม่
            if (cmbComPort.SelectedItem == null || cmbBaudRate.SelectedItem == null ||
                cmbParity.SelectedItem == null || cmbDataBits.SelectedItem == null ||
                cmbStopBits.SelectedItem == null || cmbHandshake.SelectedItem == null)
            {
                MessageBox.Show("กรุณาเลือกการตั้งค่าพอร์ตอนุกรมให้ครบ");
                return;
            }

            try
            {
                // กำหนดค่าพอร์ตตามที่ผู้ใช้เลือก แนะนำแนวทาง "ทำให้ชัวร์" สวยๆ (พร้อม default fallback)
                serialPort.PortName = cmbComPort.SelectedItem.ToString();

                var selectedItem = cmbBaudRate.SelectedItem as ComboBoxItem;
                if (selectedItem != null && int.TryParse(selectedItem.Content?.ToString(), out int baudRate))
                {
                    serialPort.BaudRate = baudRate;
                }
                else
                {
                    serialPort.BaudRate = 1200; // หรือค่า default ที่อยากตั้ง
                }
                serialPort.Parity = (Parity)Enum.Parse(typeof(Parity),
                    ((cmbParity.SelectedItem as ComboBoxItem)?.Content?.ToString()) ?? "None");

                serialPort.DataBits = int.Parse(
                    ((cmbDataBits.SelectedItem as ComboBoxItem)?.Content?.ToString()) ?? "8");

                serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits),
                    ((cmbStopBits.SelectedItem as ComboBoxItem)?.Content?.ToString()) ?? "One");

                serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake),
                    ((cmbHandshake.SelectedItem as ComboBoxItem)?.Content?.ToString()) ?? "None");

                // เชื่อมต่อกับเครื่องชั่ง
                if (ConnectToScale())
                {
                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                    isConnected = true;
                    UpdateStatus("เชื่อมต่อกับเครื่องชั่งสำเร็จ", Colors.Green);
                    LogToConsole("เชื่อมต่อกับเครื่องชั่งสำเร็จ");
                }
                else
                {
                    throw new Exception("ไม่สามารถเชื่อมต่อกับเครื่องชั่งได้");
                }
                UpdateButtonStates(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}");
                UpdateStatus("การเชื่อมต่อล้มเหลว", Colors.Red);
                LogToConsole($"การเชื่อมต่อล้มเหลว: {ex.Message}");
            }
        }

        /// <summary>
        /// Event Handler สำหรับปุ่ม Disconnect - แก้ไขปัญหา hang
        /// </summary>
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ตัดการเชื่อมต่อจากเครื่องชั่ง
                DisconnectFromScale();

                // เปิดใช้งานปุ่ม Connect และปิดปุ่ม Disconnect
                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = false;
                isConnected = false;

                // อัพเดตสถานะ
                UpdateStatus("ยกเลิกการเชื่อมต่อแล้ว", Colors.Red);

                // รีเซ็ตสถานะเมื่อตัดการเชื่อมต่อ
                ResetStableState();

                // อัพเดตสถานะปุ่ม save หลังตัดการเชื่อมต่อ
                UpdateButtonStates(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดระหว่างยกเลิกการเชื่อมต่อ: {ex.Message}");
                Console.WriteLine($"Disconnect Error: {ex.Message}");
                logger?.Log($"Disconnect Error: {ex.Message}"); // บันทึกข้อผิดพลาดลงใน log
            }
        }

        /// <summary>
        /// เพิ่มฟังก์ชันสำหรับการบันทึก log ที่ปลอดภัยกับการทำงานข้าม thread
        /// </summary>
        private void LogMessageThreadSafe(string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    LogToConsole(message);
                    logger?.Log(message);
                });
            }
            catch
            {
                // กรณีมีปัญหากับ Dispatcher ให้บันทึกลงใน Console โดยตรง
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// ยกเลิกการเชื่อมต่อกับเครื่องชั่ง - แก้ไขปัญหา hang
        /// </summary>
        private void DisconnectFromScale()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    // ตั้งค่า timeout เพื่อป้องกันการค้าง
                    serialPort.ReadTimeout = 500;
                    serialPort.WriteTimeout = 500;

                    // ล้าง buffer และปิดการเชื่อมต่อ
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                    serialPort.Close();

                    Console.WriteLine("ปิด Serial Port แล้ว");
                    logger?.Log("ปิด Serial Port แล้ว");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"เกิดข้อผิดพลาดขณะปิด Serial Port: {ex.Message}");
                    logger?.Log($"เกิดข้อผิดพลาดขณะปิด Serial Port: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// รีเซ็ตสถานะความนิ่ง
        /// </summary>
        private void ResetStableState()
        {
            lastStableTime = null;
            isCurrentlyStable = false;
            Dispatcher.Invoke(() =>
            {
                greenCircle.Visibility = Visibility.Collapsed;
                lblWeightDisplay.Foreground = new SolidColorBrush(Colors.Black);
                lblWeightDisplay.Content = "00000";
            });
        }

        /// <summary>
        /// เชื่อมต่อกับเครื่องชั่งผ่าน Serial Port - แก้ไขปัญหาการเชื่อมต่อใหม่
        /// </summary>
        private bool ConnectToScale()
        {
            try
            {
                // ถ้าพอร์ตเปิดอยู่แล้ว ให้ปิดก่อน
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    LogToConsole("ปิด Serial Port ก่อนทำการเชื่อมต่อใหม่");
                }

                // เปิดการเชื่อมต่อ
                serialPort.Open();

                if (serialPort.IsOpen)
                {
                    Console.WriteLine("เชื่อมต่อ Serial Port สำเร็จ");
                    logger?.Log("เชื่อมต่อ Serial Port สำเร็จ"); // บันทึกการเชื่อมต่อสำเร็จลงใน log
                    return true;
                }
                else
                {
                    LogToConsole("ไม่สามารถเปิด Serial Port ได้");
                    return false;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message, Colors.Red);
                LogToConsole($"เกิดข้อผิดพลาดในการเชื่อมต่อ: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Event Handler สำหรับการรับข้อมูลจาก Serial Port
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // อ่านข้อมูลที่ได้รับจาก Serial Port
                string incoming = serialPort.ReadExisting();
                buffer.Append(incoming); // เก็บข้อมูลไว้ใน buffer

                // ตรวจสอบว่าข้อมูลครบบรรทัดหรือไม่ (มี \n หรือ \r)
                if (buffer.ToString().Contains('\n') || buffer.ToString().Contains('\r'))
                {
                    string rawData = buffer.ToString().Trim();
                    buffer.Clear(); // ล้าง buffer หลังจากอ่านข้อมูลเสร็จ

                    // Log data to all the different displays
                    Dispatcher.Invoke(() => LogDataToDisplays(rawData));

                    // ประมวลผลข้อมูลใน UI thread
                    Dispatcher.Invoke(() => ProcessWeight(rawData));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    LogToConsole($"Serial Error: {ex.Message}");
                    logger?.Log($"Serial Error: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// ตรวจสอบว่าน้ำหนักปัจจุบันนิ่งครบตามเวลาที่กำหนดหรือไม่
        /// </summary>
        private bool CheckIfStableForThreeSeconds()
        {
            if (lastStableTime == null)
                return false;

            TimeSpan stableTime = DateTime.Now - lastStableTime.Value;
            return stableTime.TotalSeconds >= stableTimeLimit;
        }

        /// <summary>
        /// ประมวลผลข้อมูลน้ำหนักที่ได้รับจากเครื่องชั่ง
        /// </summary>
        private double previousWeight = 0;
        private int consistentHighWeightCount = 0;


        // แทนที่จะใช้ค่าคงที่ ใช้เปอร์เซ็นต์การเปลี่ยนแปลงตามน้ำหนัก
        private double GetNoiseThreshold(double currentWeight)
        {
            //// น้ำหนักน้อย ให้ threshold ต่ำ
            //if (currentWeight < 100) return 50;

            //// น้ำหนักปานกลาง threshold เป็นเปอร์เซ็นต์
            //if (currentWeight < 1000) return currentWeight * 0.30; // 30% ของน้ำหนักปัจจุบัน

            //// น้ำหนักมาก threshold สูง
            //return currentWeight * 0.20; // 20% ของน้ำหนักปัจจุบัน (ยังสูงพอสำหรับรถใหญ่)

            // น้ำหนักน้อยมาก ให้ threshold ต่ำแต่พอเพียง
            if (currentWeight < 100)
                return 50;  // คงค่าเดิม - เหมาะสำหรับน้ำหนักน้อย

            // น้ำหนักน้อย-ปานกลาง
            if (currentWeight < 1000)
                return Math.Max(100, currentWeight * 0.15); // ปรับลงจาก 0.30 เป็น 0.15 และมีค่าขั้นต่ำ

            // น้ำหนักปานกลาง-มาก
            if (currentWeight < 10000)
                return Math.Max(200, currentWeight * 0.10); // 10% ของน้ำหนักปัจจุบัน และมีค่าขั้นต่ำ

            // น้ำหนักมากมาก (รถบรรทุก)
            return Math.Max(1000, currentWeight * 0.05); // 5% สำหรับน้ำหนักมากมาก และมีค่าขั้นต่ำ
        }
       // private int noiseDetectionCount = 0;


        private void ProcessWeight(string rawData)
        {
            Console.WriteLine($"ข้อมูลที่ได้รับ: \"{rawData}\" (ความยาว: {rawData.Length})");
            logger?.Log($"ข้อมูลที่ได้รับ: \"{rawData}\" (ความยาว: {rawData.Length})");

            lastRawData = rawData;

            if (string.IsNullOrWhiteSpace(rawData))
            {
                Console.WriteLine("ไม่มีข้อมูล - รักษาสถานะปัจจุบัน");
                logger?.Log("ไม่มีข้อมูล - รักษาสถานะปัจจุบัน");
                return;
            }

            double weight = smartLogic.ExtractWeightFromPattern(rawData); // ✨ ใช้เมธอดแยกน้ำหนักจาก SmartLogic
            bool isStable = smartLogic.IsReadingStable(weight);
            double extractedWeight = smartLogic.ExtractWeightFromPattern(rawData);

            Console.WriteLine($"น้ำหนักที่ประมวลผลแล้ว: {weight}, เสถียร: {isStable}");
            logger?.Log($"น้ำหนักที่ประมวลผลแล้ว: {weight}, เสถียร: {isStable}");

            if (weight <= _config.WeightMinValue)
            {
                lastValidWeight = 0;
                SetStableState(false);
                DisplayWeight(0, false);
                return;
            }

           
            bool weightChanged = Math.Abs(extractedWeight - lastValidWeight) > 0.1;

            if (weightChanged)
            {
                lastValidWeight = extractedWeight;
                lastStableTime = DateTime.Now;
                SetStableState(false);
                DisplayWeight(extractedWeight, false);
                logger?.Log($"น้ำหนักเปลี่ยนเป็น: {extractedWeight} - รีเซ็ตสถานะนิ่ง");
            }
            else
            {
                bool isStableForDelay = (DateTime.Now - lastStableTime.Value).TotalMilliseconds >= _config.WeightStableDelay;
                DisplayWeight(lastValidWeight, isStableForDelay);
                if (isStableForDelay && !isCurrentlyStable)
                {
                    SetStableState(true);
                    logger?.Log("เปลี่ยนสถานะเป็นนิ่ง");
                }
            }
          //  lastWeightDisplayed = weight;
        }

     


        private double ExtractDirectWeight(string rawData)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(rawData);
            int digitStart = -1;
            int digitCount = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] >= 0x30 && bytes[i] <= 0x39)
                {
                    if (digitStart == -1) digitStart = i;
                    digitCount++;
                    if (digitCount > 5) break;
                }
                else
                {
                    if (digitCount >= 2) break;
                    digitStart = -1;
                    digitCount = 0;
                }
            }

            if (digitStart != -1 && digitCount >= 2)
            {
                string numberStr = Encoding.ASCII.GetString(bytes, digitStart, digitCount);
                if (int.TryParse(numberStr, out int result))
                {
                    bool hasContext = rawData.Contains("(") || rawData.Contains(":") || rawData.Contains(")") || bytes.Length >= 17;
                    if (!hasContext && result < 500)
                    {
                        logger?.Log($"[MainWindow] ⛔ ปฏิเสธน้ำหนัก {result} จาก noise ไม่มี context");
                        return 0;
                    }

                    logger?.Log($"[MainWindow] ✅ น้ำหนักที่อ่านได้ตรงจาก ASCII: {result}");
                    return result;
                }
            }

            return 0;
        }

        // ปรับปรุงฟังก์ชัน IsDirectValueNoise ให้เหมาะสมกับน้ำหนักขนาดใหญ่
        private bool IsDirectValueNoise(double value)
        {
            if (previousWeight <= 0)
                return false;  // ถ้าไม่มีค่าอ้างอิง ไม่ถือว่าเป็น noise

            // คำนวณการเปลี่ยนแปลงและขีดจำกัด
            double change = Math.Abs(value - previousWeight);

            // ใช้ threshold แบบปรับเปลี่ยนตามขนาดของน้ำหนัก
            double threshold = GetNoiseThreshold(previousWeight);

            // ตรวจสอบว่าการเปลี่ยนแปลงเกินขีดจำกัดหรือไม่ และยังไม่มีการยืนยันซ้ำ
            bool isNoise = change > threshold && consistentHighWeightCount < 3;

            // บันทึกเหตุผลในการตัดสินใจ
            if (isNoise)
            {
                logger?.Log($"ตรวจพบ noise: ค่าเปลี่ยนแปลง {change} > threshold {threshold}, การนับต่อเนื่อง = {consistentHighWeightCount}");
            }

            return isNoise;
        }


        /// <summary>
        /// ตั้งค่าสถานะความนิ่ง
        /// </summary>
        private void SetStableState(bool stable)
        {
            isCurrentlyStable = stable;
            greenCircle.Visibility = stable ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// แสดงน้ำหนักใน UI พร้อมปรับสีตามสถานะความนิ่ง
        /// </summary>
        /// <param name="weight">น้ำหนักที่จะแสดง</param>
        /// <param name="isStable">สถานะความนิ่ง (true = นิ่งครบ 3 วินาที)</param>
        private void DisplayWeight(double weight, bool isStable)
        {
            try
            {
                string formattedWeight = ((int)Math.Round(weight)).ToString("D5");

                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    lblWeightDisplay.Content = formattedWeight;

                    Color textColor = isStable ? Colors.Green : Colors.Black;
                    lblWeightDisplay.Foreground = new SolidColorBrush(textColor);

                    if (weight == 0 && !isStable)
                    {
                        lblWeightDisplay.Foreground = new SolidColorBrush(Colors.Red);
                    }

                    Console.WriteLine($"UI Update: น้ำหนัก {formattedWeight}, สี: {(isStable ? "เขียว" : "ดำ")}");
                    logger?.Log($"UI Update: น้ำหนัก {formattedWeight}, สี: {(isStable ? "เขียว" : "ดำ")}");
                }));

                Console.WriteLine($"แสดงน้ำหนัก: {formattedWeight}, นิ่ง: {isStable}, สี: {(isStable ? "เขียว" : "ดำ")}");
                logger?.Log($"แสดงน้ำหนัก: {formattedWeight}, นิ่ง: {isStable}, สี: {(isStable ? "เขียว" : "ดำ")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"เกิดข้อผิดพลาดในการแสดงน้ำหนัก: {ex.Message}");
                logger?.Log($"เกิดข้อผิดพลาดในการแสดงน้ำหนัก: {ex.Message}");
            }
        }
        

        /// <summary>
        /// อัปเดตข้อความสถานะและสี
        /// </summary>
        private void UpdateStatus(string message, Color color)
        {
            statusBarText.Text = $"สถานะ: {message}";
            statusBarText.Foreground = new SolidColorBrush(color);
        }

        /// <summary>
        /// เพิ่มข้อความลงใน Console Log
        /// </summary>
        public void LogToConsole(string message)
        {
            // ใช้ Dispatcher เพื่อให้แน่ใจว่าทำงานบน UI thread
            Dispatcher.Invoke(() =>
            {
                // เพิ่มเวลาและข้อความ
                string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                txtConsoleLog.AppendText(logEntry + Environment.NewLine);
                txtConsoleLog.ScrollToEnd();
            });
        }

        private void UpdateLog(string logEntry)
        {
            // เพิ่มข้อความใหม่
            txtConsoleLog.AppendText(logEntry + Environment.NewLine);

            // ตรวจสอบจำนวนบรรทัด
            var lines = txtConsoleLog.LineCount;

            // ถ้าจำนวนบรรทัดเกิน 10 บรรทัด ให้ลบบรรทัดแรก
            if (lines > 10)
            {
                int firstLineIndex = txtConsoleLog.GetLineIndexFromCharacterIndex(0);
                int lineLength = txtConsoleLog.GetLineLength(firstLineIndex);
                txtConsoleLog.Select(0, lineLength);
                txtConsoleLog.SelectedText = ""; // ลบบรรทัดแรก
            }
        }

        /// <summary>
        /// Event Handler for Save Hex Pattern Log button
        /// </summary>
        private void BtnSaveHexPattern_Click(object sender, RoutedEventArgs e)
        {
            SaveLogContent(txtHexPatternLog.Text, "HexPattern");
        }

        /// <summary>
        /// Event Handler for Save ASCII Log button
        /// </summary>
        private void BtnSaveAsciiLog_Click(object sender, RoutedEventArgs e)
        {
            SaveLogContent(txtHexToAsciiLog.Text, "AsciiMapping");
        }

        /// <summary>
        /// Event Handler สำหรับปุ่ม Clear Log - ล้าง log ทั้ง 3 ส่วน
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ล้าง log ทั้ง 3 ส่วน
                Dispatcher.Invoke(() =>
                {
                    // ล้าง HEX Pattern Log
                    txtHexPatternLog.Clear();
                    txtHexPatternLog.AppendText("=== Raw Hex Pattern Log ===" + Environment.NewLine);

                    // ล้าง ASCII Conversion Log
                    txtHexToAsciiLog.Clear();
                    txtHexToAsciiLog.AppendText("=== ASCII Conversion Log ===" + Environment.NewLine);

                    // ล้าง Console Log หลัก
                    txtConsoleLog.Clear();
                    txtConsoleLog.AppendText("=== Console Log ===" + Environment.NewLine);

                    // แจ้งให้ทราบว่าล้างทั้งหมดแล้ว
                    LogToConsole("ล้าง log ทั้งหมดเรียบร้อยแล้ว");
                });
            }
            catch (Exception ex)
            {
                LogToConsole($"เกิดข้อผิดพลาดในการล้าง log: {ex.Message}");
            }
        }

        /// <summary>
        /// Event Handler สำหรับปุ่ม Save Log
        /// </summary>
       // private bool isConnected = false;
        // ใช้ตรวจสอบสถานะการเชื่อมต่อ
        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // สร้าง SaveFileDialog
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = $"WeightScale_Log_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                // แสดง SaveFileDialog
                bool? result = dlg.ShowDialog();

                // บันทึกไฟล์ถ้าผู้ใช้กด OK
                if (result == true)
                {
                    string filename = dlg.FileName;
                    System.IO.File.WriteAllText(filename, txtConsoleLog.Text);
                    LogToConsole($"Log saved to {filename}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving log: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Override สำหรับ Console.WriteLine เพื่อนำข้อความมาแสดงใน Console Log
        /// </summary>
        public void RedirectConsoleOutput()
        {
            // สร้าง TextWriter ที่กำหนดเองเพื่อแทนที่ Console.Out
            Console.SetOut(new ConsoleOutputRedirector(this));
        }

        /// <summary>
        /// Inner class สำหรับ redirect Console.WriteLine ไปยัง TextBox
        /// </summary>
        private class ConsoleOutputRedirector : System.IO.TextWriter
        {
            private MainWindow _window;

            public ConsoleOutputRedirector(MainWindow window)
            {
                _window = window;
            }

            public override void WriteLine(string? value)
            {
                _window.LogToConsole(value ?? string.Empty);
            }

            public override void Write(string? value)
            {
                // ไม่ต้องทำอะไร (เราใช้แค่ WriteLine)
            }

            public override System.Text.Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }
        }

        /// <summary>
        /// Log data to different log displays based on type
        /// </summary>
        private void LogDataToDisplays(string rawData)
        {
            try
            {
                // 1. Log to HEX Pattern display
                byte[] bytes = Encoding.ASCII.GetBytes(rawData);
                string hexData = BitConverter.ToString(bytes);
                Dispatcher.Invoke(() =>
                {
                    txtHexPatternLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {hexData}" + Environment.NewLine);
                    txtHexPatternLog.ScrollToEnd();
                });

                // 2. Log to ASCII conversion display - show the mapping between HEX and ASCII
                // Format: "30[0] 20[ ] 20[ ] 20[ ] 20[ ] 20[ ] 30[0] 0D[.] 0A[.] 02[.]..."
                StringBuilder asciiMapping = new StringBuilder();
                asciiMapping.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] ");

                int charsPerLine = 8; // Characters per line for readability
                for (int i = 0; i < bytes.Length; i++)
                {
                    // Format each byte as "XX[C] " where XX is hex and C is ASCII char or . for control chars
                    string hexByte = bytes[i].ToString("X2");
                    char asciiChar = bytes[i] >= 32 && bytes[i] <= 126 ? (char)bytes[i] : '.';
                    asciiMapping.Append($"{hexByte}[{asciiChar}] ");

                    // Add line break every 8 characters for readability
                    if ((i + 1) % charsPerLine == 0)
                        asciiMapping.AppendLine();
                }

                // 3. Add the raw ASCII representation after the hex mapping
                asciiMapping.AppendLine();
                asciiMapping.AppendLine($"ASCII: \"{rawData}\"");

                Dispatcher.Invoke(() =>
                {
                    txtHexToAsciiLog.AppendText(asciiMapping.ToString() + Environment.NewLine);
                    txtHexToAsciiLog.ScrollToEnd();
                });

                // 4. Regular console log remains unchanged
                LogToConsole($"ข้อมูลที่ได้รับ: \"{rawData}\" (ความยาว: {rawData.Length})");
                // Save to external HEX log file
                ConsoleLogger.Instance?.LogToFile("HexPattern", hexData);

                // Save ASCII mapping separately
                ConsoleLogger.Instance?.LogToFile("AsciiMapping", asciiMapping.ToString());

            }
            catch (Exception ex)
            {
                LogToConsole($"Error in LogDataToDisplays: {ex.Message}");
            }
        }

        /// <summary>
        /// Common function to save log content to a file
        /// </summary>
        private void SaveLogContent(string content, string filePrefix)
        {
            try
            {
                // Create SaveFileDialog
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = $"WeightScale_{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";

                // Show SaveFileDialog
                bool? result = dlg.ShowDialog();

                // Save file if the user clicks OK
                if (result == true)
                {
                    string filename = dlg.FileName;
                    System.IO.File.WriteAllText(filename, content);
                    LogToConsole($"{filePrefix} log saved to {filename}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving {filePrefix} log: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// อัพเดทสถานะปุ่มต่างๆ
        /// </summary>
        private void UpdateButtonStates(bool isConnected)
        {
            // อัพเดทสถานะปุ่ม Connect/Disconnect
            btnConnect.IsEnabled = !isConnected;
            btnDisconnect.IsEnabled = isConnected;

            // อัพเดทสถานะปุ่ม Save Logs - เปิดใช้งานเมื่อไม่ได้เชื่อมต่อ
            btnSaveLog.IsEnabled = !isConnected;

            //// ถ้าม่ปุ่ม Save ASCII Log และ Save HEX Pattern ให้อัพเดทด้วย
            if (BtnSaveAsciiLog != null )
                BtnSaveAsciiLog.IsEnabled = !isConnected;

            if (BtnSaveHexPattern != null)
                BtnSaveHexPattern.IsEnabled = !isConnected;

        }
        

    }
}