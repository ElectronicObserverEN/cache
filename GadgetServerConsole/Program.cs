// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

string LastModified = "last-modified.json";
string LastModifiedText = File.ReadAllText(LastModified);
bool isModified = false;
JsonNode? JsonRoot = JsonNode.Parse(LastModifiedText);
JsonObject? parseDocument = JsonSerializer.Deserialize<JsonObject>(LastModifiedText);
PeriodicTimer timer = new(period: TimeSpan.FromMinutes(5));
async Task Update()
{
	foreach (string? line in parseDocument!.Select(line => line.Key))
	{
		var path = line.Replace("http://203.104.209.7/", "");
		try
		{
			using HttpClient httpClient = new();
			httpClient.DefaultRequestHeaders.Add("if-modified-since", JsonRoot![line].AsValue().ToString());
			using HttpResponseMessage res = await httpClient.GetAsync(line);
			if (res.IsSuccessStatusCode)
			{
				await using var ms = await res.Content.ReadAsStreamAsync();
				await using var fs = File.Create(path);
				ms.Seek(0, SeekOrigin.Begin);
				ms.CopyTo(fs);
				Console.WriteLine(path + ":" + res.Content.Headers.LastModified);
				JsonRoot![line] = res.Content.Headers.LastModified;
				isModified = true;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(path + ":" + ex.Message);
		}
		if (isModified)
		{
			string outputjson = JsonSerializer.Serialize(JsonRoot);
			File.WriteAllText(LastModified, outputjson);
		}
	}
}

void GitPush()
{
	DateTime now = DateTime.UtcNow + new TimeSpan(9, 0, 0);
	string gitCommand = "git";
	string gitAddArgument = "add -A";
	string gitCommitArgument = $"commit -m \"{now:dd-mm-yyyy}\" --author=\"gre4bee <gre4bee@address.com>\"";
	string gitPushArgument = "push origin dev";

	Process.Start(gitCommand, gitAddArgument);
	Process.Start(gitCommand, gitCommitArgument);
	Process.Start(gitCommand, gitPushArgument);
}
await Update();
GitPush();
while (await timer.WaitForNextTickAsync())
{
	await Update();
	GitPush();
}