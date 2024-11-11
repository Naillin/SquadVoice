using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SquadVoice
{
	/// <summary>
	/// Логика взаимодействия для OptionsWindow.xaml
	/// </summary>
	public partial class OptionsWindow : Window
	{
		public OptionsWindow()
		{
			InitializeComponent();
		}

		private string filePathConfig = "configClient.txt";
		private void windowOptions_Loaded(object sender, RoutedEventArgs e)
		{
			SoundPlayer sndTic = new SoundPlayer(Properties.Resources.Tic);
			sndTic.Play();

			textBoxIP.Text = LoginWindow.SERVER_IP.ToString();
			textBoxPortTech.Text = LoginWindow.PORT_TECH.ToString();
			textBoxPortChat.Text = LoginWindow.PORT_CHAT.ToString();
			textBoxPortVoice.Text = LoginWindow.PORT_VOICE.ToString();
			textBoxPortVideo.Text = LoginWindow.PORT_VIDEO.ToString();
			textBoxPortDesk.Text = LoginWindow.PORT_DESK.ToString();
		}

		private void buttonApply_Click(object sender, RoutedEventArgs e)
		{
			string configTextDefault = $"ip={textBoxIP.Text}\r\n" +
									   $"port_tech={textBoxPortTech.Text}\r\n" +
									   $"port_chat={textBoxPortChat.Text}\r\n" +
									   $"port_voice={textBoxPortVoice.Text}\r\n" +
									   $"port_video={textBoxPortVideo.Text}\r\n" +
									   $"port_desk={textBoxPortDesk.Text}";
			File.WriteAllText(filePathConfig, configTextDefault);

			LoginWindow.SERVER_IP = IPAddress.Parse(textBoxIP.Text);
			LoginWindow.PORT_TECH = Convert.ToInt32(textBoxPortTech.Text);
			LoginWindow.PORT_CHAT = Convert.ToInt32(textBoxPortChat.Text);
			LoginWindow.PORT_VOICE = Convert.ToInt32(textBoxPortVoice.Text);
			LoginWindow.PORT_VIDEO = Convert.ToInt32(textBoxPortVideo.Text);
			LoginWindow.PORT_DESK = Convert.ToInt32(textBoxPortDesk.Text);

			this.Close();
		}

		private void windowOptions_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}
	}
}
