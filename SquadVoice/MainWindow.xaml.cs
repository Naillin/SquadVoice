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
		private TcpClient techClient;
		public MainWindow(TcpClient client)
		{
			InitializeComponent();

			techClient = client;
		}

		TcpClient chatClient;
		TcpClient voiceClient;
		TcpClient videoClient;
		TcpClient deskClient;
		private void windowMain_Loaded(object sender, RoutedEventArgs e)
		{
			chatClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_CHAT); Thread.Sleep(100);
			voiceClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_VOICE); Thread.Sleep(100);
			videoClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_VIDEO); Thread.Sleep(100);
			deskClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_DESK); Thread.Sleep(100);

			FillListView(new NetworkTools(techClient).TakeBytes().GetString());
		}

		//Close
		private void windowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//new NetworkTools().TryDisconnect(techClient);
			StopAll();
		}

		private void FillListView(string channelsString)
		{
			// Разделение строки на массив имен каналов
			string[] stringsNames = channelsString.Split(';');

			foreach (string s in stringsNames)
			{
				// Добавление элементов в ListView
				listViewChannels.Items.Add(s);
			}
		}

		private BufferedWaveProvider waveProvider; // Буфер для воспроизведения
		private WaveOutEvent waveOut; // Для воспроизведения аудио
		CancellationTokenSource cancellationTokenSource;
		private void listViewChannels_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// Отменяем предыдущие операции, если они еще выполняются
			cancellationTokenSource?.Cancel();

			// Инициализация нового CancellationTokenSource
			cancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = cancellationTokenSource.Token;

			// Отправка данных
			new NetworkTools(techClient).SendString(listViewChannels.SelectedItem.ToString());
			Thread.Sleep(100); // Небольшая задержка, чтобы данные точно дошли

			// Инициализация воспроизведения
			waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1kHz, моно
			waveOut = new WaveOutEvent();
			waveOut.Init(waveProvider);
			waveOut.Play();

			// Запускаем поток для получения аудио
			Task.Run(async () => await ReceiveAudioAsync(token), token);

			// Запускаем захват и отправку аудио
			Task.Run(() => StartAudioCapture(token), token);

			// Получение сообщений
			Task.Run(async () => await ReceiveMessageAsync(token), token);
		}

		//сообщение
		private void testButton1_Click(object sender, RoutedEventArgs e)
		{
			NetworkTools networkTools = new NetworkTools(chatClient);
			networkTools.SendString(textBoxActiveField.Text); //нельзя отправть пустое сообщение!
		}

		private WaveInEvent waveIn; // Для захвата аудио
		private void StartAudioCapture(CancellationToken token)
		{
			waveIn = new WaveInEvent();
			waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, моно
			waveIn.DataAvailable += OnDataAvailable;
			waveIn.StartRecording();

			token.Register(() => StopAudioCapture()); // Остановка при срабатывании токена
		}

		private async void OnDataAvailable(object sender, WaveInEventArgs e)
		{
			if (cancellationTokenSource.Token.IsCancellationRequested)
				return; // Прекратить отправку данных, если есть запрос на отмену

			try
			{
				NetworkStream voiceStream = voiceClient.GetStream();
				if (voiceStream != null && voiceStream.CanWrite)
				{
					// Отправляем аудио данные на сервер асинхронно
					await voiceStream.WriteAsync(e.Buffer, 0, e.BytesRecorded, cancellationTokenSource.Token);
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
				MessageBox.Show("Error receiving audio11: " + ex.Message, "айя!");
			}
		}


		private async Task ReceiveAudioAsync(CancellationToken token)
		{
			byte[] buffer = new byte[4096]; // Размер буфера
			try
			{
				NetworkStream voiceStream = voiceClient.GetStream();
				while (!token.IsCancellationRequested)
				{
					int bytesRead = await voiceStream.ReadAsync(buffer, 0, buffer.Length, token);
					if (bytesRead == 0) break; // Если нет данных, выходим из цикла

					//Добавляем полученные данные в буфер воспроизведения
					waveProvider.AddSamples(buffer, 0, bytesRead);
				}
			}
			catch (OperationCanceledException)
			{
				// Операция была отменена, выходим из метода
				//MessageBox.Show("ReceiveAudio operation was canceled.", "айя!");
			}
			catch (Exception ex)
			{
				// Логирование ошибки или обработка
				MessageBox.Show("Error receiving audio22: " + ex.Message, "айя!");
			}
			finally
			{
				StopAudioPlayback();
			}
		}

		private bool isMicrophoneEnabled = true;
		public void ToggleMicrophone()
		{
			if (isMicrophoneEnabled)
			{
				// Остановить захват аудио
				waveIn?.StopRecording();
				isMicrophoneEnabled = false;
			}
			else
			{
				// Включить захват аудио
				waveIn?.StartRecording();
				isMicrophoneEnabled = true;
			}
		}

		private void StopAudioCapture()
		{
			if (waveIn != null)
			{
				waveIn?.StopRecording(); // Остановить захват аудио
				waveIn?.Dispose(); // Освободить ресурсы
				waveIn = null;
			}

			if (voiceClient != null && voiceClient.Connected)
			{
				voiceClient.GetStream().Close(); // Закрыть сетевой поток
				voiceClient.Close(); // Закрыть соединение
			}
		}

		// Инициализация остановки воспроизведения
		private void StopAudioPlayback()
		{
			waveOut?.Stop();
			waveOut?.Dispose();
			waveOut = null;
		}

		// Метод для остановки задач
		public void StopTasks()
		{
			cancellationTokenSource.Cancel();
		}

		private void StopNet()
		{
			waveIn?.StopRecording();
			waveIn?.Dispose();
			waveOut.Stop();
			waveOut.Dispose();

			//// Уничтожаем сетевые потоки
			//techClient?.GetStream()?.Close();
			//chatClient?.GetStream()?.Close();
			//voiceClient?.GetStream()?.Close();
			//videoClient?.GetStream()?.Close();
			//deskClient?.GetStream()?.Close();

			//// Освобождаем ресрурсы соединения с клиентом
			//techClient?.Dispose();
			//chatClient?.Dispose();
			//voiceClient?.Dispose();
			//videoClient?.Dispose();
			//deskClient?.Dispose();

			//// Закрываем соединение с клиентом
			//techClient?.Close();
			//chatClient?.Close();
			//voiceClient?.Close();
			//videoClient?.Close();
			//deskClient?.Close();
		}

		public void StopAll()
		{
			StopTasks();
			StopNet();

			Application.Current.Shutdown();
		}

		private async Task ReceiveMessageAsync(CancellationToken token)
		{
			try
			{
				NetworkTools networkTools = new NetworkTools(chatClient);
				while (!token.IsCancellationRequested)
				{
					// Используем асинхронное чтение с токеном отмены
					await networkTools.TakeBytesAsync();
					string message = networkTools.GetString();
					textBoxAllChat.Dispatcher.Invoke(() =>
					{
						textBoxAllChat.Text += " " + message;
					});
				}
			}
			catch (OperationCanceledException)
			{
				// Обработка отмены
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error receiving message: " + ex.Message, "Error");
			}
			finally
			{
				StopNet();
			}
		}
	}
}
