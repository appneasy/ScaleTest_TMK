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
        private ConfigModel? _config;

        public SerialPortConfigWindow()
        {
            InitializeComponent();
            LoadAvailableComPorts();   // 🔥 1. โหลด COM Ports มาที่ ComboBox
            LoadConfig();              // 🔥 2. โหลด config.json
            LoadConfigToComboBoxes();  // 🔥 3. เซ็ตค่าจาก config เข้า ComboBox
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
            try
            {
                _config = ConfigManager.LoadConfig();
            }
            catch
            {
                _config = new ConfigModel(); // fallback
            }
        }

        private void LoadConfigToComboBoxes()
        {
            if (_config == null) return;
            // ต้องแน่ใจว่าในรายการ ComboBox มีค่าที่ต้องการแล้ว
            // ถ้ายังไม่มี COM Port ที่ต้องการ อาจต้องเพิ่มลงไป
            if (!string.IsNullOrEmpty(_config.ComPort) && !cmbComPort_config.Items.Contains(_config.ComPort))
            {
                cmbComPort_config.Items.Add(_config.ComPort);
            }

            SelectComboBoxItemByContent(cmbComPort_config, _config.ComPort);
            SelectComboBoxItemByContent(cmbBaudRate_config, _config.BaudRate.ToString());
            SelectComboBoxItemByContent(cmbParity_config, _config.Parity);
            SelectComboBoxItemByContent(cmbDataBits_config, _config.DataBits.ToString());
            SelectComboBoxItemByContent(cmbStopBits_config, _config.StopBits);
            SelectComboBoxItemByContent(cmbHandshake_config, _config.Handshake);
            chkAutoConnect_config.IsChecked = _config.AutoConnect;

        }

        private void SelectComboBoxItemByContent(ComboBox comboBox, string content)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem)
                {
                    if (comboBoxItem.Content?.ToString() == content)
                    {
                        comboBox.SelectedItem = comboBoxItem;
                        return;
                    }
                }
                else if (item is string strItem)
                {
                    if (strItem == content)
                    {
                        comboBox.SelectedItem = strItem;
                        return;
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _config.ComPort = cmbComPort_config.SelectedItem?.ToString() ?? "";
                _config.BaudRate = int.Parse((cmbBaudRate_config.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1200");
                _config.Parity = (cmbParity_config.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
                _config.DataBits = int.Parse((cmbDataBits_config.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "8");
                _config.StopBits = (cmbStopBits_config.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "One";
                _config.Handshake = (cmbHandshake_config.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
                _config.AutoConnect = chkAutoConnect_config.IsChecked == true;

                // ✨ บันทึกค่าการตั้งค่ารูปแบบข้อมูลน้ำหนัก
                if (int.TryParse(txtWeightStartPos.Text, out int startPos))
                    _config.WeightStartPosition = startPos;

                if (int.TryParse(txtWeightEndPos.Text, out int endPos))
                    _config.WeightEndPosition = endPos;

                if (int.TryParse(txtWeightDigits.Text, out int digits))
                    _config.WeightDigits = digits;

                if (int.TryParse(txtWeightStableDelay.Text, out int delay))
                    _config.WeightStableDelay = delay;

                if (double.TryParse(txtWeightMaxValue.Text, out double maxVal))
                    _config.WeightMaxValue = maxVal;

                ConfigManager.SaveConfig(_config);
                MessageBox.Show("บันทึกการตั้งค่าสำเร็จ", "สำเร็จ", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ผิดพลาด: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
