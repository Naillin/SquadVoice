using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
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
using System.Windows.Shapes;

namespace SquadVoice
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private TcpClient _client;
		private NetworkStream _stream;

		public MainWindow(TcpClient client, NetworkStream stream)
		{
			InitializeComponent();

			_client = client;
			_stream = stream;
		}

		private void mainWindow_Loaded(object sender, RoutedEventArgs e)
		{

        }

		private BufferedWaveProvider waveProvider; // Буфер для воспроизведения
		private WaveOutEvent waveOut; // Для воспроизведения аудио
		private void testButton_Click(object sender, RoutedEventArgs e)
		{
			// Инициализация воспроизведения
			waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1kHz, моно
			waveOut = new WaveOutEvent();
			waveOut.Init(waveProvider);
			waveOut.Play();

			NetworkTools networkTools = new NetworkTools(_stream);
			networkTools.sendData("General");
			Thread.Sleep(100);  // Небольшая задержка, чтобы данные точно дошли

			// Запускаем поток для получения аудио
			Task.Run(() => ReceiveAudio());

			// Запускаем захват и отправку аудио
			Task.Run(() => StartAudioCapture());

			Task.Run(() => ReceiveMessage());
		}

		private void testButton1_Click(object sender, RoutedEventArgs e)
		{
			NetworkTools networkTools = new NetworkTools(_stream);
			networkTools.sendData(activeFieldTextBox.Text);
		}

		//Close
		private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			StopAudio();
		}

		private WaveInEvent waveIn; // Для захвата аудио
		private void StartAudioCapture()
		{
			waveIn = new WaveInEvent();
			waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, моно
			waveIn.DataAvailable += OnDataAvailable;
			waveIn.StartRecording();
		}

		private void OnDataAvailable(object sender, WaveInEventArgs e)
		{
			try
			{
				if (_stream != null && _stream.CanWrite)
				{
					// Отправляем аудио данные на сервер
					_stream.Write(e.Buffer, 0, e.BytesRecorded);
				}
			}
			catch (ObjectDisposedException)
			{
				// Обработка ситуации, когда поток закрыт
				MessageBox.Show("NetworkStream is closed. Cannot send audio data.", "айя!");
			}
			catch (Exception ex)
			{
				// Обработка других ошибок
				MessageBox.Show("Error receiving audio: " + ex.Message, "айя!");
			}
		}

		private void ReceiveAudio()
		{
			byte[] buffer = new byte[4096]; // Размер буфера
			try
			{
				while (true)
				{
					int bytesRead = _stream.Read(buffer, 0, buffer.Length);
					if (bytesRead == 0) break; // Если нет данных, выходим из цикла

					//Добавляем полученные данные в буфер воспроизведения
					waveProvider.AddSamples(buffer, 0, bytesRead);
				}
			}
			catch (Exception ex)
			{
				// Логирование ошибки или обработка
				MessageBox.Show("Error receiving audio: " + ex.Message, "айя!");
			}
			finally
			{
				StopAudio(); // Вызываем StopAudio() здесь, когда мы действительно выходим из цикла
			}
		}

		private void StopAudio()
		{
			waveIn?.StopRecording();
			waveIn?.Dispose();
			waveOut.Stop();
			waveOut.Dispose();
			_stream.Close();
			_client.Close();
		}

		private void ReceiveMessage()
		{
			try
			{
				while (true)
				{
					NetworkTools networkTools = new NetworkTools(_stream);
					allChatTextBox.Text = allChatTextBox.Text + " " + networkTools.getData();
				}
			}
			catch (Exception ex)
			{
				// Логирование ошибки или обработка
				MessageBox.Show("Error receiving message: " + ex.Message, "айя!");
			}
			finally
			{

			}
		}
	}
}
