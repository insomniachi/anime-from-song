using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project;

class Program {

    static async Task Main(string[] args)
    {
        Console.InputEncoding = Encoding.Unicode;
        Console.OutputEncoding = Encoding.Unicode;
        if(args.Length > 0) {
            await TagFile.RunAsync(args);
        } else {
            await Interactive.RunAsync();
        }
    }

}
