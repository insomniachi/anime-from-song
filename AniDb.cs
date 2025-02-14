using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using NAudio.MediaFoundation;

namespace Project;

public static class AniDb
{
    public static async IAsyncEnumerable<string> SearchSong(string title)
    {
        string response;
        try
        {
            response = await "https://anidb.net/perl-bin/animedb.pl".SetQueryParams(new
                {
                    show = "json",
                    action = "search",
                    type = "song",
                    query = title,
                })
                .WithHeader("x-lcontrol", "x-no-cache")
                .WithHeader("User-Agent", "anime-from-song")
                .GetStringAsync();
        }
        catch
        {
            yield break;
        }

        var nodes = JsonNode.Parse(response)?.AsArray() ?? [];
        var songs = new HashSet<string>();
        foreach (var node in nodes)
        {
            var id = $"{node?["id"]}";
            var name = $"{node?["name"]}";
            if (string.Equals(RemoveSpaces(name), RemoveSpaces(title), StringComparison.OrdinalIgnoreCase))
            {
                songs.Add(id);
            }
        }
        
        foreach (var match in songs)
        {
            var web = new HtmlWeb();
            HtmlDocument doc = null;
            try
            {
                doc = web.Load($"https://anidb.net/song/{match}");
            }
            catch
            {
                continue;
            }

            var nameNodes = doc.DocumentNode.SelectNodes("//table[@id='animelist']//tr/td[@class='name ']/a");

            if (nameNodes is null)
            {
                continue;
            }
            
            foreach (var name in nameNodes.Select(node => node.InnerText.Trim()))
            {
                yield return name;
            }
        }
            
    }
    
    static string RemoveSpaces(string input) { return string.Concat(input.Where(c => !char.IsWhiteSpace(c))); }

}
