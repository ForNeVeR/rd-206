using System;
using System.Net;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Impl;
using AvaloniaRider.Model;

namespace rdTest2Implementation
{
    class Program
    {
        private static ApplicationType _applicationType = ApplicationType.Client;
        
        private static int port = 10001;
        private static LifetimeDefinition ModelLifetimeDef { get; } = Lifetime.Eternal.CreateNested();
        private static LifetimeDefinition SocketLifetimeDef { get; } = Lifetime.Eternal.CreateNested();
        private static IProtocol Protocol { get; set; }
        private static IScheduler Scheduler { get; set; }

        private static DemoModel Model { get; set; } = null;
        
        private static Lifetime ModelLifetime { get; set; }
        private static Lifetime SocketLifetime { get; set; }
        
        static void Main(string[] args)
        {
            if (args.Length != 1) throw new ArgumentException("needs one parameter");
            if (args[0].ToLowerInvariant() == "server")
                _applicationType = ApplicationType.Server;

            ModelLifetime = ModelLifetimeDef.Lifetime;
            SocketLifetime = SocketLifetimeDef.Lifetime;
            
            Scheduler = SingleThreadScheduler.RunOnSeparateThread(SocketLifetime, "Worker", scheduler =>
            {
                IWire client;
                IdKind idKind;
                switch (_applicationType)
                {
                    case ApplicationType.Server:
                        client = new SocketWire.Server(ModelLifetime, scheduler, new IPEndPoint(IPAddress.Loopback, port),
                            "server");
                        idKind = IdKind.Server;
                        break;
                    case ApplicationType.Client:
                        client = new SocketWire.Client(ModelLifetime, scheduler, new IPEndPoint(IPAddress.Loopback, port), 
                            $"client");
                        idKind = IdKind.Client;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var serializers = new Serializers(ModelLifetime, scheduler, null);
                Protocol = new Protocol(_applicationType == ApplicationType.Server?"server":"client", serializers, 
                    new Identities(idKind), scheduler, client, SocketLifetime);
            });
            
            Scheduler.Queue(() =>
            {
                Model = new DemoModel(ModelLifetime, Protocol);
            });

            while (true)
            {
                Console.Out.WriteLine($"Press any to check type and close");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.A:
                        Scheduler.Queue(() =>
                        {
                            Model.A.Value = new A();
                        });
                        break;
                    case ConsoleKey.B:
                        Scheduler.Queue(() =>
                        {
                            Console.Out.WriteLine($"result: {(Model.A.Maybe.HasValue?Model.A.Maybe.Value.GetType():"Nothing")}");
                        });
                        break;
                }
                
            }
            
            SocketLifetimeDef.Terminate();
            ModelLifetimeDef.Terminate();
        }
    }


    enum ApplicationType
    {
        Server,
        Client
    }
}