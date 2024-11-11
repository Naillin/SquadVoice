using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
//Media
using System.Media;

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
			SoundPlayer sndVoscl = new SoundPlayer(Properties.Resources.Voscl);
			sndVoscl.Play();

			chatClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_CHAT); Thread.Sleep(100);
			voiceClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_VOICE); Thread.Sleep(100);
			videoClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_VIDEO); Thread.Sleep(100);
			deskClient = new NetworkTools().TryConnection(LoginWindow.SERVER_IP, LoginWindow.PORT_DESK); Thread.Sleep(100);

			FillListView(ref listViewChannels, new NetworkTools(techClient).TakeBytes().GetString());

			// Инициализация нового CancellationTokenSource
			cancellationTokenSource = new CancellationTokenSource();

			Task.Run(() => Animation()); // Перезапуск задач
		}

		//Close
		private void windowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
				new NetworkTools().TryDisconnect(techClient);
			}
			catch { }
			finally
			{
				StopAll();
			}
		}

		private BufferedWaveProvider waveProvider; // Буфер для воспроизведения
		private WaveOutEvent waveOut; // Для воспроизведения аудио
		CancellationTokenSource cancellationTokenSource;
		bool firstFlag = true;
		private void listViewChannels_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SoundPlayer sndTic = new SoundPlayer(Properties.Resources.Tic);
			sndTic.Play();

			if (firstFlag)
			{
				new NetworkTools(techClient).SendString(listViewChannels.SelectedItem.ToString());
				Thread.Sleep(100); // Небольшая задержка для отправки данных
				messagesBuffer = new NetworkTools(chatClient).TakeBytes().GetString() + Environment.NewLine;
				textBoxAllChat.Text = messagesBuffer;

				StartTasks();

				// Инициализация воспроизведения
				waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1kHz, моно
				waveOut = new WaveOutEvent();
				waveOut.Init(waveProvider);
				waveOut.Play();

				firstFlag = false;
			}
			else
			{
				RestartPlayback();
				ChangeChannel();
			}

			//sndTic.Stop();
		}

		//сообщение
		string messagesBuffer = string.Empty;
		private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
		{
			if(!string.IsNullOrEmpty(textBoxActiveField.Text))
			{
				string message = textBoxActiveField.Text;
				textBoxActiveField.Text = string.Empty;
				messagesBuffer = messagesBuffer + LoginWindow.login + ": " + message + Environment.NewLine;
				textBoxAllChat.Text = messagesBuffer;

				new NetworkTools(chatClient).SendString(message);
			}
		}

		//Отправка сообщения на enter
		private void textBoxActiveField_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				buttonSendMessage_Click(sender, e);
				textBoxActiveField.Focus();
			}
		}

		private void buttonMicro_Click(object sender, RoutedEventArgs e)
		{
			ToggleMicrophone();
		}

		private void ConnectedClients(CancellationToken token) //не работает тварь
		{
			while (!token.IsCancellationRequested)
			{
				string connectedClients = new NetworkTools(techClient).TakeBytes().GetString();
				listViewClients.Dispatcher.Invoke(() =>
				{
					FillListView(ref listViewClients, connectedClients);
				});
				Thread.Sleep(2000);
			}
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
				//MessageBox.Show("NetworkStream is closed. Cannot send audio data.", "айя!");
			}
			catch (Exception ex)
			{
				// Обработка других ошибок
				//MessageBox.Show("Error receiving audio11: " + ex.Message, "айя!");
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
				//MessageBox.Show("Error receiving audio22: " + ex.Message, "айя!");
			}
			finally
			{
				StopAudioPlayback();
			}
		}

		private async Task ReceiveMessageAsync(CancellationToken token)
		{
			SoundPlayer sndWhu = new SoundPlayer(Properties.Resources.Whu);
			try
			{
				NetworkTools networkTools = new NetworkTools(chatClient);
				while (!token.IsCancellationRequested)
				{
					// Используем асинхронное чтение с токеном отмены
					await networkTools.TakeBytesAsync();
					string message = networkTools.GetString();

					sndWhu.Play();

					messagesBuffer = messagesBuffer + message + Environment.NewLine;
					textBoxAllChat.Dispatcher.Invoke(() =>
					{
						textBoxAllChat.Text = messagesBuffer;
					});
				}
			}
			catch (OperationCanceledException)
			{
				// Обработка отмены
			}
			catch (Exception ex)
			{
				MessageBox.Show(Properties.Resources.errorReceivingMessageString + ex.Message, Properties.Resources.errorString);
			}
			finally
			{
				StopNet();
			}
		}

		private bool isMicrophoneEnabled = true;
		public void ToggleMicrophone()
		{
			if (isMicrophoneEnabled)
			{
				// Остановить захват аудио
				waveIn?.StopRecording();
				buttonMicro.Content = Properties.Resources.microOffString;
				gradientStopMicroButton.Color = Colors.Red;
				isMicrophoneEnabled = false;
			}
			else
			{
				// Включить захват аудио
				waveIn?.StartRecording();
				buttonMicro.Content = Properties.Resources.microOnString;
				gradientStopMicroButton.Color = Colors.Lime;
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

		private void StartTasks()
		{
			CancellationToken token = cancellationTokenSource.Token;

			Task.Run(() => ConnectedClients(token), token);
			Task.Run(async () => await ReceiveAudioAsync(token), token);
			Task.Run(() => StartAudioCapture(token), token);
			Task.Run(async () => await ReceiveMessageAsync(token), token);
		}

		private string channelChangeCode = "ChannelChange";
		public void ChangeChannel() //нихуя не работает
		{
			StopTasks(); // Отменяем все задачи без закрытия соединений
						 // Прерываем задачи, но оставляем соединения
			NetworkTools networkTools = new NetworkTools(techClient);
			networkTools.SendString(channelChangeCode);
			Thread.Sleep(3000); // Небольшая задержка для отправки данных
			networkTools.SendString(listViewChannels.SelectedItem.ToString());
			Thread.Sleep(100); // Небольшая задержка для отправки данных

			// Запускаем новые задачи после смены канала
			StartTasks();
		}

		// Метод для остановки задач
		private void StopTasks()
		{
			cancellationTokenSource.Cancel(); // Завершаем задачи через токен отмены
			cancellationTokenSource.Dispose(); // Очищаем токен для последующих операций
			cancellationTokenSource = new CancellationTokenSource(); // Инициализируем новый токен для следующих задач
		}

		private void RestartPlayback()
		{
			// Остановить текущее воспроизведение, если оно уже запущено
			if (waveOut != null)
			{
				waveOut.Stop();
				waveOut.Dispose();
				waveOut = null;
			}

			// Освободить предыдущий waveProvider
			waveProvider = null;

			// Небольшая задержка перед переинициализацией, чтобы освободить ресурсы
			Thread.Sleep(200);

			// Переинициализация воспроизведения
			waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1kHz, моно
			waveOut = new WaveOutEvent();
			waveOut.Init(waveProvider);
			waveOut.Play();
		}



		private void StopNet()
		{
			//waveIn?.StopRecording();
			//waveIn?.Dispose();
			//waveOut?.Stop();
			//waveOut?.Dispose();

			// Уничтожаем сетевые потоки
			//techClient?.GetStream()?.Close();
			chatClient?.GetStream()?.Close();
			voiceClient?.GetStream()?.Close();
			videoClient?.GetStream()?.Close();
			deskClient?.GetStream()?.Close();

			// Освобождаем ресрурсы соединения с клиентом
			//techClient?.Dispose();
			//chatClient?.Dispose();
			//voiceClient?.Dispose();
			//videoClient?.Dispose();
			//deskClient?.Dispose();

			// Закрываем соединение с клиентом
			//techClient?.Close();
			//chatClient?.Close();
			//voiceClient?.Close();
			//videoClient?.Close();
			//deskClient?.Close();
		}

		public void StopAll()
		{
			StopTasks();
			//StopNet();

			Application.Current.Shutdown();
		}

		private void FillListView(ref ListView listView, string strings)
		{
			listView.Items.Clear();

			// Разделение строки на массив имен каналов
			string[] stringsNames = strings.Split(';');

			foreach (string s in stringsNames)
			{
				// Добавление элементов в ListView
				listView.Items.Add(s);
			}
		}

		private void Animation()
		{
			try
			{
				while (true)
				{
					rotateTransformMicroButton.Dispatcher.Invoke(() =>
					{
						rotateTransformMicroButton.Angle = rotateTransformMicroButton.Angle + 10;
					});
					Thread.Sleep(100);
				}
			}
			catch
			{

			}
		}
	}
}
