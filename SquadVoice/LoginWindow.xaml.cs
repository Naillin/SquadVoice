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

		static public string SERVER_IP = "127.0.0.1";
		static public int SERVER_PORT = 5656;
		string filePathConfig = "configClient.txt";
		string filePathCredentials = "credentials.txt";
		private void loginWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if(File.Exists(filePathConfig))
			{
				string[] linesConfig = File.ReadAllLines(filePathConfig);
				SERVER_IP = linesConfig[0].Split('=')[1];  // Получаем значение ip
				SERVER_PORT = Convert.ToInt32(linesConfig[1].Split('=')[1]);  // Получаем значение port
			}
			else
			{
				File.WriteAllText(filePathConfig, $"ip={SERVER_IP}\nport={SERVER_PORT}");
			}

			if (File.Exists(filePathCredentials))
			{
				string[] linesCredentials = File.ReadAllLines(filePathCredentials);
				loginTextBox.Text = linesCredentials[0].Split('=')[1];  // Получаем значение логина
				passTextBox.Text = linesCredentials[1].Split('=')[1];  // Получаем значение пароля
			}
			else
			{
				File.WriteAllText(filePathCredentials, $"Login={123}\nPassword={123}");
			}
		}

		//login
		private void loginButton_Click(object sender, RoutedEventArgs e)
		{
			string login = loginTextBox.Text;
			string password = passTextBox.Text;

			// Перезапись файла с новыми данными
			File.WriteAllText(filePathCredentials, $"Login={login}\nPassword={password}");
			SendLoginPassword(login, password);
		}

		//options
		private void optionsButton_Click(object sender, RoutedEventArgs e)
		{
			//
		}

		private void SendLoginPassword(string login, string password)
		{
			TcpClient client = new TcpClient(SERVER_IP, SERVER_PORT);
			NetworkStream stream = client.GetStream();

			// Отправляем логин и пароль
			string message = $"{login}:{password}";
			byte[] data = Encoding.UTF8.GetBytes(message);
			stream.Write(data, 0, data.Length);

			// Читаем ответ от сервера (1 или 0)
			byte[] responseData = new byte[1];
			stream.Read(responseData, 0, responseData.Length);

			if (responseData[0] == 1)
			{
				MainWindow mainWindow = new MainWindow(client, stream);
				mainWindow.Show();
				this.Hide();
			}
			else
			{
				MessageBox.Show("неправильный пароль братан!", "айя!");
			}
		}
	}
}
