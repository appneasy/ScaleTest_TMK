using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using WeightMeasurementApp.Models;
using WeightMeasurementApp.Helpers;

namespace WeightMeasurementApp
{
    public partial class SerialPortConfigWindow : Window
    {
        private ConfigModel config;

        public SerialPortConfigWindow()
        {
            InitializeComponent();
            LoadConfig();
            InitializeComboBoxes();
            DisplayConfig();
        }

        private void LoadAvailableComPorts()
        {
            cmbComPort_config.Items.Clear();
            var ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            foreach (var port in ports)
            {
                cmbComPort_config.Items.Add(port);
            }
        }

        private void LoadConfig()
        {
            config = ConfigManager.LoadConfig();
        }

        private void InitializeComboBoxes()
        {
            try
            {
                // เติมข้อมูลลงใน ComboBox ด้วยวิธีที่ถูกต้อง

                // สำหรับ COM Ports
                string[] portNames = SerialPort.GetPortNames();
                foreach (string port in portNames)
                {
                    cmbComPort_config.Items.Add(port);
                }

                // สำหรับ Baud Rate
                int[] baudRates = { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
                foreach (int rate in baudRates)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = rate.ToString();
                    cmbBaudRate_config.Items.Add(item);
                }

                // สำหรับ Parity
                string[] parityValues = Enum.GetNames(typeof(Parity));
                foreach (string parity in parityValues)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = parity;
                    cmbParity_config.Items.Add(item);
                }

                // สำหรับ Data Bits
                int[] dataBits = { 5, 6, 7, 8 };
                foreach (int bits in dataBits)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = bits.ToString();
                    cmbDataBits_config.Items.Add(item);
                }

                // สำหรับ Stop Bits
                string[] stopBits = Enum.GetNames(typeof(StopBits));
                foreach (string bits in stopBits)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = bits;
                    cmbStopBits_config.Items.Add(item);
                }

                // สำหรับ Handshake
                string[] handshakes = Enum.GetNames(typeof(Handshake));
                foreach (string handshake in handshakes)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = handshake;
                    cmbHandshake_config.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการเตรียม ComboBox: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //private void LoadConfigToComboBoxes()
        //{
        //    if (config == null) return;
        //    // ต้องแน่ใจว่าในรายการ ComboBox มีค่าที่ต้องการแล้ว
        //    // ถ้ายังไม่มี COM Port ที่ต้องการ อาจต้องเพิ่มลงไป
        //    if (!string.IsNullOrEmpty(config.ComPort) && !cmbComPort_config.Items.Contains(_config.ComPort))
        //    {
        //        cmbComPort_config.Items.Add(config.ComPort);
        //    }

        //    SelectComboBoxItemByContent(cmbComPort_config, _config.ComPort);
        //    SelectComboBoxItemByContent(cmbBaudRate_config, _config.BaudRate.ToString());
        //    SelectComboBoxItemByContent(cmbParity_config, _config.Parity);
        //    SelectComboBoxItemByContent(cmbDataBits_config, _config.DataBits.ToString());
        //    SelectComboBoxItemByContent(cmbStopBits_config, _config.StopBits);
        //    SelectComboBoxItemByContent(cmbHandshake_config, _config.Handshake);
        //    chkAutoConnect_config.IsChecked = _config.AutoConnect;

        //}

        private void DisplayConfig()
        {
            try
            {
                // แสดงค่า Serial Port Settings ปัจจุบัน

                // กำหนดค่า ComPort
                if (!string.IsNullOrEmpty(config.ComPort))
                {
                    int index = -1;
                    for (int i = 0; i < cmbComPort_config.Items.Count; i++)
                    {
                        if (cmbComPort_config.Items[i].ToString() == config.ComPort)
                        {
                            index = i;
                            break;
                        }
                    }
                    if (index >= 0)
                    {
                        cmbComPort_config.SelectedIndex = index;
                    }
                    else if (cmbComPort_config.Items.Count > 0)
                    {
                        cmbComPort_config.SelectedIndex = 0;
                    }
                }

                // กำหนดค่า BaudRate
                string baudRateStr = config.BaudRate.ToString();
                for (int i = 0; i < cmbBaudRate_config.Items.Count; i++)
                {
                    ComboBoxItem item = cmbBaudRate_config.Items[i] as ComboBoxItem;
                    if (item?.Content?.ToString() == baudRateStr)
                    {
                        cmbBaudRate_config.SelectedIndex = i;
                        break;
                    }
                }

                // กำหนดค่า Parity
                for (int i = 0; i < cmbParity_config.Items.Count; i++)
                {
                    ComboBoxItem item = cmbParity_config.Items[i] as ComboBoxItem;
                    if (item?.Content?.ToString() == config.Parity)
                    {
                        cmbParity_config.SelectedIndex = i;
                        break;
                    }
                }

                // กำหนดค่า DataBits
                string dataBitsStr = config.DataBits.ToString();
                for (int i = 0; i < cmbDataBits_config.Items.Count; i++)
                {
                    ComboBoxItem item = cmbDataBits_config.Items[i] as ComboBoxItem;
                    if (item?.Content?.ToString() == dataBitsStr)
                    {
                        cmbDataBits_config.SelectedIndex = i;
                        break;
                    }
                }

                // กำหนดค่า StopBits
                for (int i = 0; i < cmbStopBits_config.Items.Count; i++)
                {
                    ComboBoxItem item = cmbStopBits_config.Items[i] as ComboBoxItem;
                    if (item?.Content?.ToString() == config.StopBits)
                    {
                        cmbStopBits_config.SelectedIndex = i;
                        break;
                    }
                }

                // กำหนดค่า Handshake
                for (int i = 0; i < cmbHandshake_config.Items.Count; i++)
                {
                    ComboBoxItem item = cmbHandshake_config.Items[i] as ComboBoxItem;
                    if (item?.Content?.ToString() == config.Handshake)
                    {
                        cmbHandshake_config.SelectedIndex = i;
                        break;
                    }
                }

                // กำหนดค่า AutoConnect
                chkAutoConnect_config.IsChecked = config.AutoConnect;

                // กำหนดค่า Weight Settings
                txtWeightStartPos.Text = config.WeightStartPosition.ToString();
                txtWeightEndPos.Text = config.WeightEndPosition.ToString();
                txtWeightDigits.Text = config.WeightDigits.ToString();
                txtWeightStableDelay.Text = config.WeightStableDelay.ToString();

                txtWeightMaxValue.Text = config.WeightMaxValue.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการแสดงค่า Config: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // อ่านค่าจาก ComboBox ที่ผู้ใช้เลือก
                if (cmbComPort_config.SelectedItem != null)
                {
                    config.ComPort = cmbComPort_config.SelectedItem.ToString();
                }

                if (cmbBaudRate_config.SelectedItem is ComboBoxItem baudRateItem && baudRateItem.Content != null)
                {
                    if (int.TryParse(baudRateItem.Content.ToString(), out int baudRate))
                    {
                        config.BaudRate = baudRate;
                    }
                }

                if (cmbParity_config.SelectedItem is ComboBoxItem parityItem && parityItem.Content != null)
                {
                    config.Parity = parityItem.Content.ToString();
                }

                if (cmbDataBits_config.SelectedItem is ComboBoxItem dataBitsItem && dataBitsItem.Content != null)
                {
                    if (int.TryParse(dataBitsItem.Content.ToString(), out int dataBits))
                    {
                        config.DataBits = dataBits;
                    }
                }

                if (cmbStopBits_config.SelectedItem is ComboBoxItem stopBitsItem && stopBitsItem.Content != null)
                {
                    config.StopBits = stopBitsItem.Content.ToString();
                }

                if (cmbHandshake_config.SelectedItem is ComboBoxItem handshakeItem && handshakeItem.Content != null)
                {
                    config.Handshake = handshakeItem.Content.ToString();
                }

                // อ่านค่า AutoConnect
                config.AutoConnect = chkAutoConnect_config.IsChecked ?? false;

                // อ่านค่า Weight Settings จาก TextBox
                if (int.TryParse(txtWeightStartPos.Text, out int startPos))
                {
                    config.WeightStartPosition = startPos;
                }

                if (int.TryParse(txtWeightEndPos.Text, out int endPos))
                {
                    config.WeightEndPosition = endPos;
                }

                if (int.TryParse(txtWeightDigits.Text, out int digits))
                {
                    config.WeightDigits = digits;
                }

                if (int.TryParse(txtWeightStableDelay.Text, out int delay))
                {
                    config.WeightStableDelay = delay;
                }


                if (double.TryParse(txtWeightMaxValue.Text, out double maxValue))
                {
                    config.WeightMaxValue = maxValue;
                }

                // ตรวจสอบความถูกต้องของค่า
                if (config.WeightEndPosition <= config.WeightStartPosition)
                {
                    throw new Exception("ตำแหน่งสิ้นสุดต้องมากกว่าตำแหน่งเริ่มต้น");
                }

                if (config.WeightMaxValue <= config.WeightMinValue)
                {
                    throw new Exception("น้ำหนักสูงสุดต้องมากกว่าน้ำหนักต่ำสุด");
                }

                // บันทึกการตั้งค่า
                ConfigManager.SaveConfig(config);

                // ปิดหน้าต่าง
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการบันทึกการตั้งค่า: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
