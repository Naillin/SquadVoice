using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
using System.IO;
using System.Runtime.CompilerServices;
using System.Net;
using System.Threading;

namespace SquadVoice
{
	/// <summary>
	/// Логика взаимодействия для LoginWindow.xaml
	/// </summary>
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			InitializeComponent();
		}

		static public IPAddress SERVER_IP = IPAddress.Parse("127.0.0.1");
		static public int PORT_TECH = 5555;
		static public int PORT_CHAT = 5656;
		static public int PORT_VOICE = 5757;
		static public int PORT_VIDEO = 5858;
		static public int PORT_DESK = 5959;
		private string filePathConfig = "configClient.txt";
		private string filePathCredentials = "credentials.txt";
		private void windowLogin_Loaded(object sender, RoutedEventArgs e)
		{
			if (File.Exists(filePathConfig))
			{
				string[] linesConfig = File.ReadAllLines(filePathConfig);
				SERVER_IP = IPAddress.Parse(linesConfig[0].Split('=')[1]);
				PORT_TECH = Convert.ToInt32(linesConfig[1].Split('=')[1]);
				PORT_CHAT = Convert.ToInt32(linesConfig[2].Split('=')[1]);
				PORT_VOICE = Convert.ToInt32(linesConfig[3].Split('=')[1]);
				PORT_VIDEO = Convert.ToInt32(linesConfig[4].Split('=')[1]);
				PORT_DESK = Convert.ToInt32(linesConfig[5].Split('=')[1]);
			}
			else
			{
				string configTextDefault = $"ip=127.0.0.1\r\n" +
										   $"port_tech=5555\r\n" +
										   $"port_chat=5656\r\n" +
										   $"port_voice=5757\r\n" +
										   $"port_video=5858\r\n" +
										   $"port_desk=5959";
				File.WriteAllText(filePathConfig, configTextDefault);
			}

			if (File.Exists(filePathCredentials))
			{
				string[] linesCredentials = File.ReadAllLines(filePathCredentials);
				textBoxLogin.Text = linesCredentials[0].Split('=')[1];  // Получаем значение логина
				textBoxPassword.Text = linesCredentials[1].Split('=')[1];  // Получаем значение пароля
			}
			else
			{
				File.WriteAllText(filePathCredentials, $"Login={123}\nPassword={123}");
			}

			// Анимация
			Task.Run(() => Animation());
		}

		private void loginWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}

		//login
		private void buttonLogin_Click(object sender, RoutedEventArgs e)
		{
			string login = textBoxLogin.Text;
			string password = textBoxPassword.Text;

			// Перезапись файла с новыми данными
			File.WriteAllText(filePathCredentials, $"Login={login}\nPassword={password}");
			SendLoginPassword(login, password);
		}

		//options
		private void buttonOptions_Click(object sender, RoutedEventArgs e)
		{
			OptionsWindow optionsWindow = new OptionsWindow();
			optionsWindow.ShowDialog();
		}

		private async void SendLoginPassword(string login, string password)
		{
			NetworkTools networkTools = new NetworkTools();
			TcpClient client = networkTools.TryConnection(SERVER_IP, PORT_TECH, true);
			// Отправляем логин и пароль
			string message = $"{login}:{password}";
			networkTools.SendString(message);

			// Читаем ответ от сервера (1 или 0)
			await networkTools.TakeBytesAsync();
			byte[] responseData = networkTools.GetBytes();
			if (responseData[0] == 1)
			{
				MainWindow mainWindow = new MainWindow(client);
				mainWindow.Show();
				this.Hide();
			}
			else
			{
				MessageBox.Show("неправильный пароль братан!", "айя!");
			}
		}

		private void Animation()
		{
			while (true)
			{
				rotateTransformLoginButton.Dispatcher.Invoke(() =>
				{
					rotateTransformLoginButton.Angle = rotateTransformLoginButton.Angle + 10;
				});
				Thread.Sleep(100);
			}
		}
	}
}
