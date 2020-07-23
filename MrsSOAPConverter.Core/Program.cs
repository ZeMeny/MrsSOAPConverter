using System;
using System.IO;
using System.Xml.Serialization;
using SOAPConverter.Standard;

namespace MrsSOAPConverter.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = ReadConfig(AppDomain.CurrentDomain.BaseDirectory + "config.xml");
            SoapConverter converter = new SoapConverter(configuration);

            converter.Start();
            Console.WriteLine("Press Esc to exit");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
            }

            converter.Stop();
        }

        private static Configuration ReadConfig(string path)
        {
            var config = new Configuration
            {
                DeviceIP = "127.0.0.1",
                DevicePort = 13001,
                DeviceNotificationIP = "127.0.0.1",
                DeviceNotificationPort = 11001,
                ListenIP = "127.0.0.1",
                ListenPort = 41000,
                RequestorID = "MrsSOAPConverter",
                ValidateMessages = true
            };

            if (File.Exists(path))
            {
                try
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                        return (Configuration)serializer.Deserialize(stream);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return config;
        }
    }
}
