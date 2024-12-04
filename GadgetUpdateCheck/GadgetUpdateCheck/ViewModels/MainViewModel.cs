using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GadgetUpdateCheck.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	private string LastModifiedFileName { get; } = "last-modified.json";
	private PeriodicTimer UpdateCheckTimer { get; } = new(TimeSpan.FromMinutes(5));
	private PeriodicTimer CountdownTimer { get; } = new(TimeSpan.FromSeconds(1));
	private JsonSerializerOptions Options { get; } = new()
	{
		WriteIndented = true,
		TypeInfoResolver = SourceGenerationContext.Default,
	};

	[ObservableProperty]
	private int _secondsUntilNextUpdate;

	[ObservableProperty]
	private string _countdownText = "Update check idle";

	[ObservableProperty]
	private bool _checkingForUpdates;

	[ObservableProperty]
	private bool _noCache;

	[ObservableProperty]
	private string? _proxy;

	public ObservableCollection<string> Log { get; } = new();

	private HttpClient MakeHttpClient()
	{
		if (string.IsNullOrEmpty(Proxy))
		{
			return new();
		}

		WebProxy proxy = new(Proxy);

		HttpClientHandler httpClientHandler = new()
		{
			Proxy = proxy,
			UseProxy = true,
		};

		return new(httpClientHandler);
	}

	[RelayCommand(IncludeCancelCommand = true)]
	private async Task Update(CancellationToken cancellationToken)
	{
		try
		{
			FileStream lastModifiedStream = File.OpenRead(LastModifiedFileName);
			Dictionary<string, string> lastModifiedData = await JsonSerializer
				.DeserializeAsync(lastModifiedStream, SourceGenerationContext.Default.DictionaryStringString, cancellationToken)
				?? throw new NotImplementedException();

			await lastModifiedStream.DisposeAsync();

			foreach ((string url, string lastModifiedString) in lastModifiedData)
			{
				DateTime lastModified = DateTime.Parse(lastModifiedString);

				if (await FetchFile(url, lastModified, cancellationToken) is DateTime newDate)
				{
					lastModifiedData[url] = newDate.ToString("R");
				}
			}

			string outputText = JsonSerializer.Serialize(lastModifiedData, typeof(Dictionary<string, string>), Options);
			await File.WriteAllTextAsync(LastModifiedFileName, outputText, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			// don't need to do anything here
		}
		catch (Exception ex)
		{
			AddLog(ex.GetBaseException().Message + ex.StackTrace);
		}
	}

	private async Task<DateTime?> FetchFile(string url, DateTime lastModified, CancellationToken cancellationToken)
	{
		string path = url.Replace("http://w00g.kancolle-server.com/", "");

		try
		{
			using HttpClient httpClient = MakeHttpClient();

			httpClient.DefaultRequestHeaders.Add("if-modified-since", lastModified.ToString("R"));
			httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue()
			{
				NoCache = NoCache,
			};

			using HttpResponseMessage res = await httpClient.GetAsync(url, cancellationToken);

			if (res.IsSuccessStatusCode)
			{
				await using Stream ms = await res.Content.ReadAsStreamAsync(cancellationToken);
				await using FileStream fs = File.Create(path);
				ms.Seek(0, SeekOrigin.Begin);
				await ms.CopyToAsync(fs, cancellationToken);

				AddLog($"{path}: {res.Content.Headers.LastModified}");

				return res.Content.Headers.LastModified?.Date;
			}

			AddLog($"{path}: {res.StatusCode}");
		}
		catch (Exception ex)
		{
			AddLog($"{path}: {ex.GetBaseException().Message}{ex.StackTrace}");
		}

		return null;
	}

	[RelayCommand]
	private async Task GetIp()
	{
		HttpClient client = MakeHttpClient();

		HttpResponseMessage response = await client.GetAsync("https://api.ipify.org");

		AddLog(await response.Content.ReadAsStringAsync());
	}

	[RelayCommand(IncludeCancelCommand = true)]
	private async Task StartPeriodicCheck(CancellationToken cancellationToken)
	{
		_ = StartCountdown(cancellationToken);

		try
		{
			while (true)
			{
				SecondsUntilNextUpdate = (int)UpdateCheckTimer.Period.TotalSeconds;
				CheckingForUpdates = true;
				CountdownText = "Checking for updates...";

				await Update(cancellationToken);
				await GitPush();

				CheckingForUpdates = false;

				await UpdateCheckTimer.WaitForNextTickAsync(cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			CountdownText = "Update check idle";
			AddLog("Stopped checking for updates.");
		}
	}

	private async Task StartCountdown(CancellationToken cancellationToken)
	{
		try
		{
			while (await CountdownTimer.WaitForNextTickAsync(cancellationToken))
			{
				SecondsUntilNextUpdate--;

				if (!CheckingForUpdates)
				{
					CountdownText = $"Next update: {SecondsUntilNextUpdate}";
				}
			}
		}
		catch (OperationCanceledException)
		{
			// don't need to do anything here
		}
	}

	[RelayCommand]
	private async Task GitPush()
	{
		string gitCommand = "git";
		string gitAddArgument = "add -A";
		string gitCommitArgument = $"commit -m \"{DateTime.UtcNow:yyyy/MM/dd}\"";
		string gitPushArgument = "push origin master";

		await Process.Start(gitCommand, gitAddArgument).WaitForExitAsync();
		await Process.Start(gitCommand, gitCommitArgument).WaitForExitAsync();
		await Process.Start(gitCommand, gitPushArgument).WaitForExitAsync();
	}

	private void AddLog(string message)
	{
		Log.Add($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}]: {message}");
	}
}
