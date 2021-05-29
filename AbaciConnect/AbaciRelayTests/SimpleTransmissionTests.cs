using Microsoft.VisualStudio.TestTools.UnitTesting;
using AbaciConnect.Relay;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace AbaciConnect.RelayTests
{
    [TestClass]
    [DeploymentItem("SimpleImage.bmp")]
    public class SimpleTransmissionTests
    {
        private readonly CommandBytesFactory factory = new CommandBytesFactory();
        private readonly StructFactory structFactory = new StructFactory();
        private readonly EmissionDecoder emissionDecoder = new EmissionDecoder();
        private readonly FrameByteReceiver byteReceiver = new FrameByteReceiver();
        [TestMethod]
        public void OpenAndClose()
        {
            using (SerialRelay relay = new SerialRelay("COM4", this.byteReceiver))
            {
            }
        }
        [TestMethod]
        public void SetName()
        {
            byte[] bytes = factory.CreateSetNameFrame("XBee A");
            using (SerialRelay relay = new SerialRelay("COM4", this.byteReceiver))
            {
                relay.SendBytes(bytes);
            }
        }
        [TestMethod]
        public void SetNetworkSettings()
        {
            string port = "COM4";
            NetworkSettings settings = new NetworkSettings();
            settings.Role = DeviceRoles.Create;
            byte[] bytes = factory.CreateSetNetworkSettingsFrame(settings);
            using (SerialRelay relay = new SerialRelay(port, this.byteReceiver))
            {
                relay.SendBytes(bytes);
            }
        }
        [TestMethod]
        public void SendSimpleData()
        {
            ulong broadcast = 0x000000000000FFFF;
            ulong address = 0x0013A20041B764AD;
            string text = "xxxxx";
            byte[] data_bytes = Encoding.UTF8.GetBytes(text);
            byte[] bytes = factory.CreateSendDataFrame(address, data_bytes);
            using (SerialRelay relay = new SerialRelay("COM4", this.byteReceiver))
            {
                relay.SendBytes(bytes);
                EmissionDescriptor em = this.byteReceiver.WaitForEmission(EmissionTypes.ExtendedTransmitStatus);
                ExtendedTransmitStatus response = this.structFactory.Unpack<ExtendedTransmitStatus>(
                    em.Data.ToArray(), CONSTANTS.XTRANSMIT_RESPONSE_SIZE);
                Console.WriteLine("Frame ID: {0}; Result: {1}",
                    response.FrameID, response.DeliveryStatus);
                Assert.AreEqual(0, response.DeliveryStatus);
            }
        }
        [TestMethod]
        public void SendSimpleDataStream()
        {
            ulong broadcast = 0x000000000000FFFF;
            ulong long_address = 0x0013A20041B764AD;
            string text = "";
            for(int i=0; i<84; i++)
            {
                text+="x";
            } // looks like 84 is the magic number so that the packets dont fragment; should probably start making a transmission/packet/etc class that can hold arrays of these 84 byte transmissions
            byte[] data_bytes = Encoding.UTF8.GetBytes(text);
            byte[] bytes = factory.CreateSendDataFrame(long_address, data_bytes);
            using (SerialRelay relay = new SerialRelay("COM4", this.byteReceiver))
            {
                relay.SendBytes(bytes);
                EmissionDescriptor em = this.byteReceiver.WaitForEmission(EmissionTypes.ExtendedTransmitStatus);
                ExtendedTransmitStatus response = this.structFactory.Unpack<ExtendedTransmitStatus>(
                    em.Data.ToArray(), CONSTANTS.XTRANSMIT_RESPONSE_SIZE);
                Console.WriteLine("ID: {0}; Address: 0x{1:X2}{2:X2}; Result: 0x{3:X2}",
                    response.FrameID, response.AddressH, response.AddressL, response.DeliveryStatus);
                ushort short_address = (ushort)(((ushort)response.AddressH<<8)|response.AddressL);
                bytes = factory.CreateSendDataFrame(short_address, data_bytes);
                for (int i = 0; i < 100; i++)
                {
                    relay.SendBytes(bytes);
                    em = this.byteReceiver.WaitForEmission(EmissionTypes.ExtendedTransmitStatus);
                    response = this.structFactory.Unpack<ExtendedTransmitStatus>(
                        em.Data.ToArray(), CONSTANTS.XTRANSMIT_RESPONSE_SIZE);
                    Console.WriteLine("ID: {0}; Address: 0x{1:X2}{2:X2}; Result: 0x{3:X2}",
                        response.FrameID, response.AddressH, response.AddressL, response.DeliveryStatus);
                    Assert.AreEqual(0, response.DeliveryStatus);
                    //System.Threading.Thread.Sleep(10);
                }
            }
        }
    }
}
