using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
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

		private WaveOutEvent waveOut; // Для воспроизведения аудио
		private void testButton_Click(object sender, RoutedEventArgs e)
		{
			// Инициализация воспроизведения
			waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1kHz, моно
			waveOut = new WaveOutEvent();
			waveOut.Init(waveProvider);
			waveOut.Play();

			// Запускаем поток для получения аудио
			Task.Run(() => ReceiveAudio());

			// Запускаем захват и отправку аудио
			Task.Run(() => StartAudioCapture());
		}

		//Close
		private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			StopAudio();
		}

		private WaveInEvent waveIn; // Для захвата аудио
		private BufferedWaveProvider waveProvider; // Буфер для воспроизведения
		private void StartAudioCapture()
		{
			waveIn = new WaveInEvent();
			waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, моно
			waveIn.DataAvailable += OnDataAvailable;
			waveIn.StartRecording();
		}

		private void OnDataAvailable(object sender, WaveInEventArgs e)
		{
			// Отправляем аудио данные на сервер
			_stream.Write(e.Buffer, 0, e.BytesRecorded);
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

					// Добавляем полученные данные в буфер воспроизведения
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
	}
}
