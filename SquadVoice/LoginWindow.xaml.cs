using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
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
		public static string login = string.Empty; 
		private void buttonLogin_Click(object sender, RoutedEventArgs e)
		{
			login = textBoxLogin.Text;
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

		private void windowLogin_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (string.IsNullOrEmpty(textBoxLogin.Text))
			{
				textBoxLogin.Foreground = new SolidColorBrush(Colors.Silver);
				textBoxLogin.Text = "Login";
			}
			if (string.IsNullOrEmpty(textBoxPassword.Text))
			{
				TogglePasswordVisibility(sender, e);
				textBoxPassword.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
				textBoxPassword.Text = "Password";
			}
			//buttonLogin.Select();
		}

		private void textBoxLogin_MouseEnter(object sender, MouseEventArgs e)
		{
			if (textBoxLogin.Text.Equals("Login"))
			{
				textBoxLogin.Text = "";
				textBoxLogin.Foreground = new SolidColorBrush(Colors.Black);
			}
		}

		private void textBoxLogin_MouseLeave(object sender, MouseEventArgs e)
		{
			if (string.IsNullOrEmpty(textBoxLogin.Text))
			{
				textBoxLogin.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
				textBoxLogin.Text = "Login";
			}
		}

		private void textBoxPassword_MouseEnter(object sender, MouseEventArgs e)
		{
			if (textBoxPassword.Text.Equals("Password"))
			{
				TogglePasswordVisibility(sender, e);
				textBoxPassword.Text = "";
				textBoxPassword.Foreground = new SolidColorBrush(Colors.Black);
			}
		}

		private void textBoxPassword_MouseLeave(object sender, MouseEventArgs e)
		{
			if (string.IsNullOrEmpty(textBoxPassword.Text))
			{
				TogglePasswordVisibility(sender, e);
				textBoxPassword.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
				textBoxPassword.Text = "Password";
			}
		}

		private bool isPasswordHidden = true;  // Флаг для скрытого текста
		private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
		{
			if (isPasswordHidden)
			{
				textBoxPassword.Text = new string('•', textBoxPassword.Text.Length); // Скрыть символы
				isPasswordHidden = false;
			}
			else
			{
				textBoxPassword.Text = originalText; // Показать текст
				isPasswordHidden = true;
			}
		}

		private string originalText = string.Empty;
		private void textBoxPassword_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (isPasswordHidden)
			{
				originalText = textBoxPassword.Text;
			}
		}
	}
}
