﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

class Program {

    static async Task Main(string[] args) {
        Console.WriteLine("SPACE - tag, S - source, Q - quit");
        CaptureSourceHelper.Set(false);

        while(true) {
            var key = Console.ReadKey(true);

            if(Char.ToLower(key.KeyChar) == 'q')
                break;

            if(Char.ToLower(key.KeyChar) == 's') {
                CaptureSourceHelper.Toggle();
                continue;
            }

            if(key.Key == ConsoleKey.Spacebar) {
                Console.Write("Listening... ");

                try {
                    var result = await CaptureAndTagAsync();

                    if(result.Success) {
                        Console.CursorLeft = 0;
                        Console.WriteLine(result.Url);
                        Process.Start("explorer", result.Url);
                    } else {
                        Console.WriteLine(":(");
                    }
                } catch(Exception x) {
                    Console.WriteLine("error: " + x.Message);
                }
            }
        }

    }

    static async Task<ShazamResult> CaptureAndTagAsync() {
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        using var captureHelper = new WasapiCaptureHelper(new WaveFormat(Analysis.SAMPLE_RATE, 16, 1));
        captureHelper.Start();

        var chunk = new float[Analysis.CHUNK_SIZE];
        var retryMs = 3000;
        var tagId = Guid.NewGuid().ToString();

        while(true) {
            ReadChunk(captureHelper.SampleProvider, chunk);

            analysis.AddChunk(chunk);

            if(analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
                finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);

            if(analysis.ProcessedMs >= retryMs) {
                //new Painter(analysis, finder).Paint("c:/temp/spectro.png");
                //new Synthback(analysis, finder).Synth("c:/temp/synthback.raw");

                var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
                var result = await ShazamApi.SendRequestAsync(tagId, analysis.ProcessedMs, sigBytes);
                if(result.Success)
                    return result;

                retryMs = result.RetryMs;
                if(retryMs == 0)
                    return result;
            }
        }
    }

    static void ReadChunk(ISampleProvider sampleProvider, float[] chunk) {
        var offset = 0;
        var expectedCount = chunk.Length;

        while(true) {
            var actualCount = sampleProvider.Read(chunk, offset, expectedCount);

            if(actualCount == expectedCount)
                return;

            offset += actualCount;
            expectedCount -= actualCount;

            Thread.Sleep(100);
        }
    }
}
