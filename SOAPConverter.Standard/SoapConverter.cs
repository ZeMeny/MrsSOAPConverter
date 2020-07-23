using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using MarsDeviceManager;
using MarsDeviceManager.Extensions;
using SensorStandard;
using SensorStandard.MrsTypes;
using File = System.IO.File;
using Sensor = MrsSensor.Core.Sensor;

namespace SOAPConverter.Standard
{
    public class SoapConverter
    {
        private readonly Configuration _configuration;
        private Sensor _sensor;
        private DeviceConfiguration _deviceConfiguration;
        private DeviceStatusReport _statusReport;
        private Device _device;

        public SoapConverter(Configuration configuration)
        {
            _configuration = configuration;
            Globals.ValidateMessages = false;
        }

        public void Start()
        {
            _device = new Device(_configuration.DeviceIP, _configuration.DevicePort,
                _configuration.DeviceNotificationIP, _configuration.DeviceNotificationPort,
                _configuration.RequestorID);
            _device.MessageReceived += Device_MessageReceived;
            _device.MessageSent += Device_MessageSent;
            _device.Disconnected += Device_Disconnected;
            _device.Connected += Device_Connected;

            Console.WriteLine("Connecting...");
            _device.Connect();

        }

        public void Stop()
        {
            Console.WriteLine("Terminating...");
            _device.Disconnect();
            _sensor?.Stop();
        }

        private void Device_Connected(object sender, EventArgs e)
        {
            Device dvc = (Device) sender;
            Console.WriteLine($"Device ({dvc.DeviceIP}:{dvc.DevicePort}) Connected");
        }

        private void Device_Disconnected(object sender, EventArgs e)
        {
            Device dvc = (Device) sender;
            Console.WriteLine($"Device ({dvc.DeviceIP}:{dvc.DevicePort}) Diconnected");
        }

        private void Device_MessageSent(object sender, MrsMessage e)
        {
            Device dvc = (Device) sender;
            Console.WriteLine($"{e.MrsMessageType} sent to {dvc.DeviceIP}:{dvc.DevicePort}");
        }

        private void Device_MessageReceived(object sender, MrsMessage e)
        {
            Device dvc = (Device) sender;
            Console.WriteLine($"{e.MrsMessageType} received from {dvc.DeviceIP}:{dvc.DevicePort}");
            switch (e.MrsMessageType)
            {
                case MrsMessageTypes.DeviceConfiguration:
                    // save config
                    _deviceConfiguration = (DeviceConfiguration) e;
                    // override ip and port
                    _deviceConfiguration.NotificationServiceIPAddress = _configuration.ListenIP;
                    _deviceConfiguration.NotificationServicePort = _configuration.ListenPort.ToString();
                    break;
                case MrsMessageTypes.DeviceStatusReport:
                    // save status
                    // if status contain already values - update
                    _statusReport =
                        (DeviceStatusReport) (_statusReport == null
                            ? e
                            : _statusReport.UpdateValues(e));
                    if (_sensor == null)
                    {
                        // now that we have config and status, start the sensor (listening side)
                        _sensor = new Sensor(_deviceConfiguration, _statusReport) {ValidateMessages = false};
                        _sensor.MessageSent += Sensor_MessageSent;
                        _sensor.MessageReceived += Sensor_MessageReceived;
                        _sensor.ValidationErrorOccured += Sensor_ValidationErrorOccured;
                        Console.WriteLine("Starting sensor...");
                        _sensor.Start();
                        Console.WriteLine($"Sensor started on {_sensor.IP}:{_sensor.Port}");
                    }

                    break;
                case MrsMessageTypes.DeviceIndicationReport:
                    var indicationReport = (DeviceIndicationReport) e;
                    _sensor?.RegisterIndications(ExtractIndications(indicationReport));
                    break;
            }
        }

        private void Sensor_ValidationErrorOccured(object sender, MrsSensor.Core.InvalidMessageException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Validation error on {e.MarsMessage.MrsMessageType}\n{e.InnerException}");
            Console.ResetColor();
        }

        private void Sensor_MessageReceived(MrsMessage message, string marsName)
        {
            Console.WriteLine($"{message.MrsMessageType} received from {marsName}");
            if (message.MrsMessageType == MrsMessageTypes.CommandMessage)
            {
                if (message is CommandMessage commandMessage &&
                    (SimpleCommandType) commandMessage.Command.Item != SimpleCommandType.KeepAlive)
                {
                    // find the sensor the command is for
                    var deviceSensor = _device.Sensors.FirstOrDefault(s =>
                        s.SensorIdentification.Equals(commandMessage.SensorIdentification));
                    _device.SendCommandMessage(commandMessage.Command, deviceSensor);
                }
            }
        }

        private void Sensor_MessageSent(MrsMessage message, string marsName)
        {
            Console.WriteLine($"{message.MrsMessageType} sent to {marsName}");
        }

        private IndicationType[] ExtractIndications(DeviceIndicationReport indicationReport)
        {
            List<IndicationType> indications = new List<IndicationType>();
            foreach (var deviceReport in indicationReport.Items.OfType<DeviceIndicationReport>())
            {
                indications.AddRange(ExtractIndications(deviceReport));
            }

            foreach (var sensorReport in indicationReport.Items.OfType<SensorIndicationReport>())
            {
                indications.AddRange(sensorReport.IndicationType);
            }

            return indications.ToArray();
        }
    }
}
