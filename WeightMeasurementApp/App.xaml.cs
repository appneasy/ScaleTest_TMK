using System.Configuration;
using System.Data;
using System.Windows;

namespace WeightMeasurementApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // เมธอดนี้จะถูกเรียกเมื่อแอปพลิเคชันเริ่มต้น
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // เพิ่มการจัดการข้อผิดพลาดที่ไม่ได้รับการจัดการ
            Application.Current.DispatcherUnhandledException += (s, ex) =>
            {
                // แสดง MessageBox เมื่อเกิดข้อผิดพลาด
                MessageBox.Show(ex.Exception.ToString(), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true; // บอกว่าเราได้จัดการข้อผิดพลาดแล้ว
            };
        }
    }

}
