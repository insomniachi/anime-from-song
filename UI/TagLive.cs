using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

internal static class TagLive
{
    public static async Task RunAsync(bool auto)
    {
        var prevTitle = default(string);

        while (true)
        {
            var startTime = DateTime.Now;

            try
            {
                using var captureHelper = CreateCaptureHelper();
                captureHelper.Start();

                var result = await CaptureAndTag.RunAsync(captureHelper);

                if (result.Success)
                {
                    var isNewTitle = result.Title != prevTitle;

                    if (isNewTitle)
                    {
                        Console.WriteLine($"Song: {result.Title}");
                        _ = Task.Run(async () =>
                        {
                            HashSet<string> results = [];
                            await foreach (var anime in AniDb.SearchSong(result.Title))
                            {
                                if (results.Add(anime))
                                {
                                    Console.WriteLine($"Anime : {anime}");
                                }
                            }
                        });
                    }

                    prevTitle = result.Title;
                }
                else
                {
                    if (!auto)
                    {
                        Console.WriteLine(":(");
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("error: " + x.Message);
            }

            if (!auto)
            {
                break;
            }

            var nextStartTime = startTime + TimeSpan.FromSeconds(15);
            while (DateTime.Now < nextStartTime)
            {
                await Task.Delay(100);
            }
        }
    }

    private static void Navigate(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            using var proc = Process.Start("explorer", '"' + url + '"');
            proc.WaitForExit();
        }
    }

    private static ICaptureHelper CreateCaptureHelper()
    {
#if WASAPI_CAPTURE
        return new WasapiCaptureHelper();
#else
        if(!OperatingSystem.IsWindows()) {
            return new SoxCaptureHelper();
        }

        return new MciCaptureHelper();
#endif
    }
}