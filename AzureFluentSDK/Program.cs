using System;

namespace AzureFluentSDK
{
    class Program
    {
        static void Main(string[] args)
        {
            AzureVM vm = new AzureVM();
            vm.CreateAzureWindowsVM();
            Console.WriteLine("Hello World!");
        }
    }
}
