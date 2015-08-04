using J2i.Net.XInputWrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using uPLibrary.Networking.M2Mqtt;
using rChordata;
using Newtonsoft.Json;
using NDesk.Options;

namespace JoyBridge2
{
  
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MqttClient Mqtt { get; private set; }
        XboxController _selectedController;
        DiamondPoint DiamondPoint;
        string mqttBroker;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            mqttBroker = "127.0.0.1";

            var p = new OptionSet
            {
                   { "mqtt=", (v) => { mqttBroker = v; } },
            };

            p.Parse(Environment.GetCommandLineArgs());

            joyStick1.JoystickMovedListeners += joystickMoved;

            _selectedController = XboxController.RetrieveController(0);
            XboxController.UpdateFrequency = 10;
            _selectedController.StateChanged += _selectedController_StateChanged;
            XboxController.StartPolling();

        }

        private void joystickMoved(DiamondPoint p)
        {
            //throw new NotImplementedException();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            XboxController.StopPolling();
            if (Mqtt != null && Mqtt.IsConnected)
                Mqtt.Disconnect();
            base.OnClosing(e);
        }

        public static double Scale(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        int changeDebounce = 10;
        void _selectedController_StateChanged(object sender, XboxControllerStateChangedEventArgs e)
        {
            OnPropertyChanged("SelectedController");

            var p = new DoublePoint(-SelectedController.RightThumbStick.X,
                SelectedController.RightThumbStick.Y);

            System.Diagnostics.Trace.WriteLine(string.Format("game x({0}) y({1})", p.X, p.Y));

            if (Math.Abs(p.X) > 10000 || Math.Abs(p.Y) > 10000)
            {
                changeDebounce = 0;

                DiamondPoint = DiamondToolbox.CartesianToDiamond(p, 32767);
                DiamondPoint.Left = Scale(DiamondPoint.Left, -32767, 32767, -100, 100);
                DiamondPoint.Right = Scale(DiamondPoint.Right, -32767, 32767, -100, 100);

                System.Diagnostics.Trace.WriteLine(string.Format("DiamondPoint left({0}) right({1})",
                    DiamondPoint.Left, DiamondPoint.Right));

                string jsn = JsonConvert.SerializeObject(new { Cmd = "MOV", M1 = (float)DiamondPoint.Left, M2 = (float)DiamondPoint.Right });
                if (Mqtt?.IsConnected ?? false)
                    Mqtt.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(jsn));
            }
            else
            {
                if (++changeDebounce < 10 )
                {
                    string jsn = JsonConvert.SerializeObject(new { Cmd = "MOV", M1 = (float)0, M2 = (float)0 });
                    if (Mqtt?.IsConnected ?? false)
                        Mqtt.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(jsn));
                }
            }
        }

        public XboxController SelectedController
        {
            get { return _selectedController; }
        }

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                Action a = ()=>{PropertyChanged(this, new PropertyChangedEventArgs(name));};
                Dispatcher.BeginInvoke(a, null);
                
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void SelectedControllerChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedController = XboxController.RetrieveController(((ComboBox)sender).SelectedIndex);
            OnPropertyChanged("SelectedController");
        }

        private void SendVibration_Click(object sender, RoutedEventArgs e)
        {
            double leftMotorSpeed = LeftMotorSpeed.Value;
            double rightMotorSpeed = RightMotorSpeed.Value;
            _selectedController.Vibrate(leftMotorSpeed, rightMotorSpeed, TimeSpan.FromSeconds(2));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Mqtt = new MqttClient(mqttBroker);
            Mqtt.Connect("JoyBridge");
        }

        private void CheckboxAButton_Checked(object sender, RoutedEventArgs e)
        {
            string jsn = JsonConvert.SerializeObject(new { Cmd = "ESC", Value = 1 });
            if (Mqtt?.IsConnected ?? false)
                Mqtt.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(jsn));
        }

        private void CheckboxBButton_Checked(object sender, RoutedEventArgs e)
        {
            string jsn = JsonConvert.SerializeObject(new { Cmd = "ESC", Value = 0 });
            if (Mqtt?.IsConnected ?? false)
                Mqtt.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(jsn));
        }

    }
}

