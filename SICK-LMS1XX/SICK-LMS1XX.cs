/*
 * A C#.NET class to communicate with SICK SENSOR LMS1xx
 * 
 * Author : beolabs.io / Benjamin Oms
 * Update : 7/23/2019, sebescudie
 * Github : https://github.com/beolabs-io/SICK-Sensor-LMS1XX
 * 
 * --- MIT LICENCE ---
 * 
 * Copyright (c) 2017 beolabs.io
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace LMS1XX
{
    public class LMS1XX
    {
        #region Enums

        public enum SocketConnectionResult { CONNECTED = 0, CONNECT_TIMEOUT = 1, CONNECT_ERROR = 2, DISCONNECTED = 3, DISCONNECT_TIMEOUT = 4, DISCONNECT_ERROR = 5 }
        public enum NetworkStreamResult { STARTED = 0, STOPPED = 1, TIMEOUT = 2, ERROR = 3, CLIENT_NOT_CONNECTED = 4 }
        public enum UserLevel { MAINTENANCE = 0, AUTHORIZED_CLIENT = 1, SERVICE = 2 }
        public enum ScanFrequency { TWENTY_FIVE_HERTZ = 0, FITY_HERTZ = 1 }
        public enum AngularResolution { ZERO_POINT_TWENTY_FIVE_DEGREES = 0, ZERO_POINT_FIFTY_DEGREES = 1 }
        public enum StatusCode { BUSY = 0, READY = 1, ERROR = 2 }
        public enum SetScanConfigStatusCode { NO_ERROR = 0, FREQUENCY_ERROR = 1, RESOLUTION_ERROR = 2, RESOLUTION_AND_SCANAREA_ERROR = 3, SCANAREA_ERROR = 4, OTHER_ERRORS = 5 }

        #endregion Enums

        #region Propriétés publiques

        public String IpAddress { get; set; }
        public int Port { get; set; }
        public int ReceiveTimeout { get; set; }
        public int SendTimeout { get; set; }

        #endregion

        #region Propriétés privées

        private TcpClient clientSocket;

        #endregion

        #region Constructeurs

        public LMS1XX()
        {
            this.clientSocket = new TcpClient() { ReceiveTimeout = 1000, SendTimeout = 1000 };
            this.IpAddress = String.Empty;
            this.Port = 0;
        }

        public LMS1XX(string ipAdress, int port, int receiveTimeout, int sendTimeout)
        {
            this.clientSocket = new TcpClient() { ReceiveTimeout = receiveTimeout, SendTimeout = sendTimeout };
            this.IpAddress = ipAdress;
            this.Port = port;
        }

        #endregion

        #region Methodes de base pour le pilotage du capteur

        public bool IsSocketConnected()
        {
            return clientSocket.Connected;
        }

        #region Connect
        /// <summary>
        /// Connects to the socket
        /// </summary>
        /// <returns></returns>
        public SocketConnectionResult Connect()
        {
            SocketConnectionResult status = (clientSocket.Connected) ? SocketConnectionResult.CONNECTED : SocketConnectionResult.DISCONNECTED;
            if (status == SocketConnectionResult.DISCONNECTED)
            {
                try
                {
                    clientSocket.Connect(this.IpAddress, this.Port);
                    status = SocketConnectionResult.CONNECTED;
                }
                catch (TimeoutException) { status = SocketConnectionResult.CONNECT_TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException) { status = SocketConnectionResult.CONNECT_ERROR; this.Disconnect(); return status; }
            }
            return status;
        }

        /// <summary>
        /// Connects to the socket
        /// </summary>
        /// <returns></returns>
        public async Task<SocketConnectionResult> ConnectAsync()
        {
            SocketConnectionResult status = (clientSocket.Connected) ? SocketConnectionResult.CONNECTED : SocketConnectionResult.DISCONNECTED;
            if (status == SocketConnectionResult.DISCONNECTED)
            {
                try
                {
                    await clientSocket.ConnectAsync(this.IpAddress, this.Port);
                    status = SocketConnectionResult.CONNECTED;
                }
                catch (TimeoutException) { status = SocketConnectionResult.CONNECT_TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException) { status = SocketConnectionResult.CONNECT_ERROR; this.Disconnect(); return status; }
            }
            return status;
        }
        #endregion Connect

        #region Disconnect
        /// <summary>
        /// Disconnects the socket
        /// </summary>
        /// <returns></returns>
        public SocketConnectionResult Disconnect()
        {
            SocketConnectionResult status = (clientSocket.Connected) ? SocketConnectionResult.CONNECTED : SocketConnectionResult.DISCONNECTED;
            if (status == SocketConnectionResult.CONNECTED)
            {
                try
                {
                    clientSocket.Close();
                    clientSocket = new TcpClient() { ReceiveTimeout = this.ReceiveTimeout };
                    status = SocketConnectionResult.DISCONNECTED;
                }
                catch (TimeoutException) { status = SocketConnectionResult.DISCONNECT_TIMEOUT; return status; }
                catch (SystemException) { status = SocketConnectionResult.DISCONNECT_ERROR; return status; }
            }
            return status;
        }
        #endregion Disconnect

        #region Start
        /// <summary>
        /// Start the laser and (unless in Standby mode) the motor of the the device.
        /// </summary>
        /// <returns></returns>
        public NetworkStreamResult Start()
        {
            byte[] cmd = new byte[18] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x61, 0x72, 0x74, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();
                    serverStream.Write(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STARTED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException) { status = NetworkStreamResult.ERROR; this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }

        /// <summary>
        /// Start the laser and (unless in Standby mode) the motor of the the device.
        /// </summary>
        /// <returns></returns>
        public async Task<NetworkStreamResult> StartAsync()
        {
            byte[] cmd = new byte[18] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x61, 0x72, 0x74, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();
                    await serverStream.WriteAsync(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STARTED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException) { status = NetworkStreamResult.ERROR; this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }
        #endregion Start

        #region Stop
        /// <summary>
        /// Shut off the laser and stop the motor of the the device.
        /// </summary>
        /// <returns></returns>
        public NetworkStreamResult Stop()
        {
            byte[] cmd = new byte[17] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x6F, 0x70, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();

                    serverStream.Write(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STOPPED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException) { status = NetworkStreamResult.ERROR; this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }

        /// <summary>
        /// Shut off the laser and stop the motor of the the device.
        /// </summary>
        /// <returns></returns>
        public async Task<NetworkStreamResult> StopAsync()
        {
            byte[] cmd = new byte[17] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x6F, 0x70, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();

                    await serverStream.WriteAsync(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STOPPED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException) { status = NetworkStreamResult.ERROR; this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }
        #endregion Stop

        #region ExecuteRaw
        /// <summary>
        /// Sends a raw command to the LIDAR
        /// </summary>
        /// <param name="streamCommand"></param>
        /// <returns></returns>
        public byte[] ExecuteRaw(byte[] streamCommand)
        {
            try
            {
                NetworkStream serverStream = clientSocket.GetStream();
                serverStream.Write(streamCommand, 0, streamCommand.Length);

                serverStream.Flush();

                byte[] inStream = new byte[clientSocket.ReceiveBufferSize];
                serverStream.Read(inStream, 0, (int)clientSocket.ReceiveBufferSize);

                return inStream;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<byte[]> ExecuteRawAsync(byte[] streamCommand)
        {
            try
            {
                NetworkStream serverStream = clientSocket.GetStream();
                await serverStream.WriteAsync(streamCommand, 0, streamCommand.Length);
                await serverStream.FlushAsync();

                byte[] inStream = new byte[clientSocket.ReceiveBufferSize];
                await serverStream.ReadAsync(inStream, 0, (int)clientSocket.ReceiveBufferSize);

                return inStream;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion ExecuteRaw

        #region SetAccessMode
        public struct SetAccessModeResult
        {
            public byte[] RawData;
        }

        public SetAccessModeResult SetAccessMode()
        {
            SetAccessModeResult result;
            byte[] command = new byte[] { 0x02, 0x73, 0x41, 0x4E, 0x20, 0x53, 0x65, 0x74, 0x41, 0x63, 0x63, 0x65, 0x73, 0x73, 0x4D, 0x6F, 0x64, 0x65, 0x20, 0x31, 0x03 };
            result.RawData = this.ExecuteRaw(command);
            return result;
        }

        public async Task<SetAccessModeResult> SetAccessModeAsync()
        {
            SetAccessModeResult result;
            byte[] command = new byte[] { 0x02, 0x73, 0x41, 0x4E, 0x20, 0x53, 0x65, 0x74, 0x41, 0x63, 0x63, 0x65, 0x73, 0x73, 0x4D, 0x6F, 0x64, 0x65, 0x20, 0x31, 0x03 };
            result.RawData = await this.ExecuteRawAsync(command);
            return result;
        }
        #endregion SetAccessMode

        #region Login
        public struct LoginResponse
        {
            public bool IsError;
            public Exception ErrorException;
            public byte[] RawData;
            public string RawDataString;
            public string CommandType;
            public string Command;
            public bool ChangedUserLevel;

            public LoginResponse(byte[] rawData)
            {
                IsError = true;
                ErrorException = null;
                RawData = rawData;
                RawDataString = Encoding.ASCII.GetString(rawData, 1, 19);
                CommandType = String.Empty;
                Command = String.Empty;
                ChangedUserLevel = false;
            }
        }

        public LoginResponse Login(UserLevel userLevel)
        {
            // Command type + command + space
            byte[] command = new byte[] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x53, 0x65, 0x74, 0x41, 0x63, 0x63, 0x65, 0x73, 0x73, 0x4D, 0x6F, 0x64, 0x65, 0x20 };
            byte[] selectedLevel = null;
            byte[] selectedPassword = null;

            byte[] maintenancePassword = new byte[] { 0x42, 0x32, 0x31, 0x41, 0x43, 0x45, 0x32, 0x36 };
            byte[] authorizedClientPassord = new byte[] { 0x46, 0x34, 0x37, 0x32, 0x34, 0x37, 0x34, 0x34 };
            byte[] servicePassword = new byte[] { 0x38, 0x31, 0x42, 0x45, 0x32, 0x33, 0x41, 0x41 };

            byte[] terminator = new byte[] { 0x03 };

            // Sets selectedLevel according to user choice. The space is included after userLevel (0x20)
            switch (userLevel)
            {
                case UserLevel.MAINTENANCE:
                    selectedLevel = new byte[] { 0x30, 0x32, 0x20 };
                    selectedPassword = maintenancePassword;
                    break;
                case UserLevel.AUTHORIZED_CLIENT:
                    selectedLevel = new byte[] { 0x30, 0x33, 0x20 };
                    selectedPassword = authorizedClientPassord;
                    break;
                case UserLevel.SERVICE:
                    selectedLevel = new byte[] { 0x30, 0x34, 0x20 };
                    selectedPassword = servicePassword;
                    break;
                default:
                    break;
            }

            // Build the final command
            // byte[] finalCommand = command.Concat(selectedLevel).Concat(Encoding.ASCII.GetBytes(password)).Concat(terminator).ToArray();
            byte[] finalCommand = command.Concat(selectedLevel).Concat(selectedPassword).Concat(terminator).ToArray();


            if (clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = this.ExecuteRaw(finalCommand);
                }
                catch (Exception ex)
                {
                    return new LoginResponse() { IsError = true, ErrorException = ex };
                }

                if (rawData != null)
                {
                    LoginResponse result = new LoginResponse(rawData);
                    result.IsError = false;
                    result.ErrorException = null;

                    string[] blocs = result.RawDataString.Split(new string[] { " " }, StringSplitOptions.None);

                    for(int i = 0; i < 3; i++)
                    {
                        switch(i)
                        {
                            case 0: result.CommandType = blocs[i]; break;
                            case 1: result.Command = blocs[i]; break;
                            case 2: result.ChangedUserLevel = blocs[i] == "1"; break;
                        }
                    }

                    return result;
                }
                else
                {
                    return new LoginResponse() { IsError = true, ErrorException = new Exception("Raw data is null") };
                }
            }
            else
            {
                return new LoginResponse() { IsError = true, ErrorException = new Exception("Socket is not connected") };
            }
        }

        #endregion Login

        #region ScanDataResult

        public struct LMDScandataResult
        {
            public bool IsError;
            public Exception ErrorException;
            public byte[] RawData;
            public String RawDataString;
            public String CommandType;
            public String Command;
            public int? VersionNumber;
            public int? DeviceNumber;
            public int? SerialNumber;
            public String DeviceStatus;
            public int? TelegramCounter;
            public int? ScanCounter;
            public uint? TimeSinceStartup;
            public uint? TimeOfTransmission;
            public String StatusOfDigitalInputs;
            public String StatusOfDigitalOutputs;
            public int? Reserved;
            public double? ScanFrequency;
            public double? MeasurementFrequency;
            public int? AmountOfEncoder;
            public int? EncoderPosition;
            public int? EncoderSpeed;
            public int? AmountOf16BitChannels;
            public String Content;
            public String ScaleFactor;
            public String ScaleFactorOffset;
            public double? StartAngle;
            public double? SizeOfSingleAngularStep;
            public int? AmountOfData;
            public List<double> DistancesData;

            public LMDScandataResult(byte[] rawData)
            {
                IsError = true;
                ErrorException = null;
                RawData = rawData;
                RawDataString = Encoding.ASCII.GetString(rawData);
                DistancesData = new List<double>();
                CommandType = String.Empty;
                Command = String.Empty;
                VersionNumber = null;
                DeviceNumber = null;
                SerialNumber = null;
                DeviceStatus = String.Empty;
                TelegramCounter = null;
                ScanCounter = null;
                TimeSinceStartup = null;
                TimeOfTransmission = null;
                StatusOfDigitalInputs = String.Empty;
                StatusOfDigitalOutputs = String.Empty;
                Reserved = null;
                ScanFrequency = null;
                MeasurementFrequency = null;
                AmountOfEncoder = null;
                EncoderPosition = null;
                EncoderSpeed = null;
                AmountOf16BitChannels = null;
                Content = String.Empty;
                ScaleFactor = String.Empty;
                ScaleFactorOffset = String.Empty;
                StartAngle = null;
                SizeOfSingleAngularStep = null;
                AmountOfData = null;
            }
        }

        /// <summary>
        /// Outputs values from last scan. 
        /// </summary>
        /// <remarks>
        /// Asking the device for the measurement values of the last valid scan. The device will respond, even if it is not running at the moment.
        /// </remarks>
        /// <returns></returns>
        public LMDScandataResult LMDScandata()
        {
            byte[] command = new byte[] { 0x02, 0x73, 0x52, 0x4E, 0x20, 0x4C, 0x4D, 0x44, 0x73, 0x63, 0x61, 0x6E, 0x64, 0x61, 0x74, 0x61, 0x03 };

            if (clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = this.ExecuteRaw(command);
                }
                catch (Exception ex)
                {
                    return new LMDScandataResult() { IsError = true, ErrorException = ex };
                }

                if (rawData != null)
                {
                    LMDScandataResult result = new LMDScandataResult(rawData);
                    result.IsError = false;
                    result.ErrorException = null;

                    int dataIndex = 0;
                    int dataBlocCounter = 0;
                    string dataBloc = String.Empty;

                    while (dataBlocCounter < 28)
                    {
                        dataIndex++;
                        if ((dataIndex < result.RawDataString.Length) && !(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            ++dataBlocCounter;
                            switch (dataBlocCounter)
                            {
                                case 1: result.CommandType = dataBloc; break;
                                case 2: result.Command = dataBloc; break;
                                case 3: result.VersionNumber = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 4: result.DeviceNumber = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 5: result.SerialNumber = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 6: result.DeviceStatus = dataBloc; break;
                                case 7: result.DeviceStatus += "-" + dataBloc; break;
                                case 8: result.TelegramCounter = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 9: result.ScanCounter = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 10: result.TimeSinceStartup = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 11: result.TimeOfTransmission = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 12: result.StatusOfDigitalInputs = dataBloc; break;
                                case 13: result.StatusOfDigitalInputs += "-" + dataBloc; break;
                                case 14: result.StatusOfDigitalOutputs = dataBloc; break;
                                case 15: result.StatusOfDigitalOutputs += "-" + dataBloc; break;
                                case 16: result.Reserved = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 17: result.ScanFrequency = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 100; break;
                                case 18: result.MeasurementFrequency = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10; break;
                                case 19: result.AmountOfEncoder = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); if (result.AmountOfEncoder <= 0) dataBlocCounter += 2; break;
                                case 20: result.EncoderPosition = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 21: result.EncoderSpeed = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 22: result.AmountOf16BitChannels = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 23: result.Content = dataBloc; break;
                                case 24: result.ScaleFactor = dataBloc; break;
                                case 25: result.ScaleFactorOffset = dataBloc; break;
                                case 26: result.StartAngle = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 27: result.SizeOfSingleAngularStep = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 28: result.AmountOfData = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                            }
                            dataBloc = String.Empty;
                            if (result.CommandType != "sRA") return result;
                        }
                    }

                    dataBloc = String.Empty;
                    while (dataBlocCounter < result.AmountOfData + 28)
                    {
                        ++dataIndex;
                        if (!(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            result.DistancesData.Add(Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 1000);
                            dataBloc = String.Empty;
                            ++dataBlocCounter;
                        }
                    }

                    return result;
                }
                else
                    return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Raw data is null.") };
            }
            else
                return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Client socket not connected.") };
        }

        /// <summary>
        /// Outputs values from last scan. 
        /// </summary>
        /// <remarks>
        /// Asking the device for the measurement values of the last valid scan. The device will respond, even if it is not running at the moment.
        /// </remarks>
        /// <returns></returns>
        public async Task<LMDScandataResult> LMDScandataAsync()
        {
            byte[] command = new byte[] { 0x02, 0x73, 0x52, 0x4E, 0x20, 0x4C, 0x4D, 0x44, 0x73, 0x63, 0x61, 0x6E, 0x64, 0x61, 0x74, 0x61, 0x03 };

            if (clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = await this.ExecuteRawAsync(command);
                }
                catch (Exception ex)
                {
                    return new LMDScandataResult() { IsError = true, ErrorException = ex };
                }

                if (rawData != null)
                {
                    LMDScandataResult result = new LMDScandataResult(rawData);
                    result.IsError = false;
                    result.ErrorException = null;

                    int dataIndex = 0;
                    int dataBlocCounter = 0;
                    string dataBloc = String.Empty;

                    while (dataBlocCounter < 28)
                    {
                        dataIndex++;
                        if ((dataIndex < result.RawDataString.Length) && !(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            ++dataBlocCounter;
                            switch (dataBlocCounter)
                            {
                                case 1: result.CommandType = dataBloc; break;
                                case 2: result.Command = dataBloc; break;
                                case 3: result.VersionNumber = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 4: result.DeviceNumber = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 5: result.SerialNumber = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 6: result.DeviceStatus = dataBloc; break;
                                case 7: result.DeviceStatus += "-" + dataBloc; break;
                                case 8: result.TelegramCounter = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 9: result.ScanCounter = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 10: result.TimeSinceStartup = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 11: result.TimeOfTransmission = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 12: result.StatusOfDigitalInputs = dataBloc; break;
                                case 13: result.StatusOfDigitalInputs += "-" + dataBloc; break;
                                case 14: result.StatusOfDigitalOutputs = dataBloc; break;
                                case 15: result.StatusOfDigitalOutputs += "-" + dataBloc; break;
                                case 16: result.Reserved = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 17: result.ScanFrequency = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 100; break;
                                case 18: result.MeasurementFrequency = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10; break;
                                case 19: result.AmountOfEncoder = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); if (result.AmountOfEncoder <= 0) dataBlocCounter += 2; break;
                                case 20: result.EncoderPosition = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 21: result.EncoderSpeed = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 22: result.AmountOf16BitChannels = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 23: result.Content = dataBloc; break;
                                case 24: result.ScaleFactor = dataBloc; break;
                                case 25: result.ScaleFactorOffset = dataBloc; break;
                                case 26: result.StartAngle = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 27: result.SizeOfSingleAngularStep = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 28: result.AmountOfData = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                            }
                            dataBloc = String.Empty;
                            if (result.CommandType != "sRA")
                                return result;
                        }
                    }

                    dataBloc = String.Empty;
                    while (dataBlocCounter < result.AmountOfData + 28)
                    {
                        ++dataIndex;
                        if (!(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            result.DistancesData.Add(Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 1000);
                            dataBloc = String.Empty;
                            ++dataBlocCounter;
                        }
                    }

                    return result;
                }
                else
                    return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Raw data is null.") };
            }
            else
                return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Client socket not connected.") };
        }

        #endregion ScanDataResult

        #region Reboot
        public struct RebootResponse
        {
            public bool IsError;
            public Exception ErrorException;
            public byte[] RawData;
            public string RawDataString;

            public RebootResponse(byte[] rawData)
            {
                IsError = true;
                ErrorException = null;
                RawData = null;
                RawDataString = Encoding.ASCII.GetString(rawData);
            }
        }

        /// <summary>
        /// Reboots the LIDAR
        /// </summary>
        /// <remarks>Only works in AUTHORIZEDCLIENT and  SERVICE user levels</remarks>
        /// <returns></returns>
        public RebootResponse Reboot()
        {
            byte[] command = new byte[] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x6D, 0x53, 0x43, 0x72, 0x65, 0x62, 0x6F, 0x6F, 0x74, 0x03 };

            if (clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = this.ExecuteRaw(command);
                }
                catch (Exception ex)
                {
                    return new RebootResponse() { IsError = true, ErrorException = ex };
                }

                if (rawData != null)
                {
                    RebootResponse result = new RebootResponse(rawData);
                    result.IsError = false;
                    result.ErrorException = null;

                    return result;
                }
                else
                    return new RebootResponse() { IsError = true, ErrorException = new Exception("Raw Data is null") };
            }
            else
                return new RebootResponse() { IsError = true, ErrorException = new Exception("Client socket not connected.") };
        }
        #endregion Reboot

        #region SetScanConfiguration

        public struct SetScanConfigurationResult
        {
            public bool IsError;
            public Exception ErrorException;
            public byte[] RawData;
            public String RawDataString;
            public String CommandType;
            public String Command;
            public SetScanConfigStatusCode StatusCode;
            public float? ScanFrequency;
            public float? NumberOfActiveSectors;
            public float? AngularResolution;
            public float? StartAngle;
            public float? StopAngle;

            public SetScanConfigurationResult(byte[] rawData)
            {
                IsError = true;
                ErrorException = null;
                RawData = rawData;
                RawDataString = Encoding.ASCII.GetString(rawData, 1, 50);
                CommandType = String.Empty;
                Command = String.Empty;
                StatusCode = SetScanConfigStatusCode.OTHER_ERRORS;
                ScanFrequency = null;
                NumberOfActiveSectors = null;
                AngularResolution = null;
                StartAngle = null;
                StopAngle = null;
            }
        }

        public SetScanConfigurationResult SetScanConfiguration(ScanFrequency scanFrequency, AngularResolution angularResolution)
        {
            // Main command fragment, with STX and final space
            byte[] command = new byte[] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x6D, 0x4C, 0x4D, 0x50, 0x73, 0x65, 0x74, 0x73, 0x63, 0x61, 0x6E, 0x63, 0x66, 0x67, 0x20 };
            byte[] chosenScanFrequency = null;
            // Sectors fragment (this is always 1 for LMS1XX LIDARS) + space
            byte[] sectors = new byte[] { 0x2B, 0x31, 0x20 };
            byte[] chosenAngularResolution = null;
            // Start angle fragment + space
            byte[] startAngle = new byte[] { 0x2D, 0x34, 0x35, 0x30, 0x30, 0x30, 0x30, 0x20 };
            // Stop angle fragment + ETX
            byte[] stopAngle = new byte[] { 0x2B, 0x32, 0x32, 0x35, 0x30, 0x30, 0x30, 0x30, 0x03 };

            switch (scanFrequency)
            {
                case ScanFrequency.TWENTY_FIVE_HERTZ:
                    // +2500d + space (0x20)
                    chosenScanFrequency = new byte[] { 0x2B, 0x32, 0x35, 0x30, 0x30, 0x20 };
                    break;
                case ScanFrequency.FITY_HERTZ:
                    // +5000d + space (0x20)
                    chosenScanFrequency = new byte[] { 0x2B, 0x35, 0x30, 0x30, 0x30, 0x20 };
                    break;
                default:
                    break;
            }

            switch (angularResolution)
            {
                case AngularResolution.ZERO_POINT_TWENTY_FIVE_DEGREES:
                    // +2500d + space (0x20)
                    chosenAngularResolution = new byte[] { 0x2B, 0x32, 0x35, 0x30, 0x30, 0x20 };
                    break;
                case AngularResolution.ZERO_POINT_FIFTY_DEGREES:
                    // +5000d + space (0x20)
                    chosenAngularResolution = new byte[] { 0x2B, 0x35, 0x30, 0x30, 0x30, 0x20 };
                    break;
                default:
                    break;
            }

            // Build final command
            byte[] finalCommand = command.Concat(chosenScanFrequency).Concat(sectors).Concat(chosenAngularResolution).Concat(startAngle).Concat(stopAngle).ToArray();

            if (clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = this.ExecuteRaw(finalCommand);
                }
                catch (Exception ex)
                {
                    return new SetScanConfigurationResult() { IsError = true, ErrorException = ex };
                }

                if (rawData != null)
                {
                    SetScanConfigurationResult result = new SetScanConfigurationResult(rawData);
                    result.IsError = false;
                    result.ErrorException = null;

                    string[] blocs = result.RawDataString.Split(new string[] { " " }, StringSplitOptions.None);

                    for(int i = 0; i < 8; i++)
                    {
                        switch(i)
                        {
                            case (0): result.CommandType = blocs[i]; break;
                            case (1): result.Command = blocs[i]; break;
                            case (2): result.StatusCode = (SetScanConfigStatusCode)int.Parse(blocs[i]); break;
                            case (3): result.ScanFrequency = Convert.ToSingle(int.Parse(blocs[i], System.Globalization.NumberStyles.HexNumber)); break;
                            case (4): result.NumberOfActiveSectors = Convert.ToSingle(int.Parse(blocs[i], System.Globalization.NumberStyles.HexNumber)); break;
                            case (5): result.AngularResolution = Convert.ToSingle(int.Parse(blocs[i], System.Globalization.NumberStyles.HexNumber)); break;
                            case (6): result.StartAngle = Convert.ToSingle(int.Parse(blocs[i], System.Globalization.NumberStyles.HexNumber)); break;
                            // case (7): result.StopAngle = Convert.ToSingle(int.Parse(blocs[i], System.Globalization.NumberStyles.HexNumber)); break;
                        }
                    }

                    return result;
                }
                else
                {
                    return new SetScanConfigurationResult() { IsError = true, ErrorException = new Exception("Raw data is null") };
                }
            }
            else
            {
                return new SetScanConfigurationResult() { IsError = true, ErrorException = new Exception("Socket is not connected") };
            }
        }

        #endregion SetScanConfiguration

        #region ReadDeviceState

        public struct ReadDeviceStateResult
        {
            public bool IsError;
            public Exception ErrorException;
            public byte[] RawData;
            public String RawDataString;
            public String CommandType;
            public String Command;
            public StatusCode Status;

            public ReadDeviceStateResult(byte[] rawData)
            {
                IsError = true;
                ErrorException = null;
                RawData = rawData;
                RawDataString = Encoding.ASCII.GetString(rawData, 1, 19);
                CommandType = String.Empty;
                Command = String.Empty;
                Status = StatusCode.ERROR;
            }
        }

        /// <summary>
        /// Reads LIDAR's current status
        /// </summary>
        /// <remarks>0 = Busy, 1 = Ready, 2 = Error</remarks>
        /// <returns></returns>
        public ReadDeviceStateResult ReadDeviceState()
        {
            byte[] command = new byte[] { 0x02, 0x73, 0x52, 0x4E, 0x20, 0x53, 0x43, 0x64, 0x65, 0x76, 0x69, 0x63, 0x65, 0x73, 0x74, 0x61, 0x74, 0x65, 0x03 };

            if(clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = this.ExecuteRaw(command);
                }
                catch(Exception ex)
                {
                    return new ReadDeviceStateResult() { IsError = true, ErrorException = ex };
                }

                if(rawData != null)
                {
                    ReadDeviceStateResult result = new ReadDeviceStateResult(rawData);
                    result.IsError = false;
                    result.ErrorException = null;

                    string[] blocs = result.RawDataString.Split(new string[] { " " }, StringSplitOptions.None);

                    for(int i = 0; i < 3; i++)
                    {
                        switch(i)
                        {
                            case 0: result.CommandType = blocs[i]; break;
                            case 1: result.Command = blocs[i]; break;
                            case 2: result.Status = (StatusCode)int.Parse(blocs[i]); break;
                        }
                    }

                    return result;
                }
                else
                {
                    return new ReadDeviceStateResult() { IsError = true, ErrorException = new Exception("Raw data is null") };
                }
            }
            else
            {
                return new ReadDeviceStateResult() { IsError = true, ErrorException = new Exception("Socket is not connected") };
            }
        }


        #endregion ReadDeviceState

        #region ReadDeviceTemperature

        public struct ReadDeviceTemperatureResponse
        {
            public bool IsError;
            public Exception ErrorException;
            public byte[] RawData;
            public String RawDataString;
            public String CommandType;
            public String Command;
            public float Temperature;

            public ReadDeviceTemperatureResponse(byte[] rawData)
            {
                IsError = true;
                ErrorException = null;
                RawData = rawData;
                RawDataString = Encoding.ASCII.GetString(RawData, 1, 22);
                CommandType = String.Empty;
                Command = String.Empty;
                Temperature = 0f;
            }
        }

        /// <summary>
        /// Reads LIDAR's temperature
        /// </summary>
        /// <returns></returns>
        public ReadDeviceTemperatureResponse ReadDeviceTemperature()
        {
            byte[] command = { 0x02, 0x73, 0x52, 0x4E, 0x20, 0x4F, 0x50, 0x63, 0x75, 0x72, 0x74, 0x6D, 0x70, 0x64, 0x65, 0x76, 0x03 };

            if(clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = this.ExecuteRaw(command);
                }
                catch(Exception ex)
                {
                    return new ReadDeviceTemperatureResponse() { IsError = true, ErrorException = ex };
                }

                if(rawData != null)
                {
                    ReadDeviceTemperatureResponse result = new ReadDeviceTemperatureResponse(rawData);
                    result.IsError = false;
                    result.ErrorException = null;

                    string[] blocs = result.RawDataString.Split(new string[] { " " }, StringSplitOptions.None);

                    for(int i = 0; i < 3; i++)
                    {
                        switch (i)
                        {
                            case 0: result.CommandType = blocs[i]; break;
                            case 1: result.Command = blocs[i]; break;
                            case 2: result.Temperature = ConvertHexToSingle(blocs[i]); break;
                        }
                    }

                    return result;
                }
                else
                {
                    return new ReadDeviceTemperatureResponse() { IsError = true, ErrorException = new Exception("Raw data is null") };
                }
            }
            else
            {
                return new ReadDeviceTemperatureResponse() { IsError = true, ErrorException = new Exception("Socket is not connected") };
            }
        }

        #endregion ReadDeviceTemperature

        #endregion

        #region Relevé Asynchrone des données du Capteur

        public async Task<LMDScandataResult> LMDScandataFullModeAsync()
        {
            try
            {
                LMDScandataResult scandataResult;

                var connectionResult = await this.ConnectAsync();
                if (connectionResult == SocketConnectionResult.CONNECTED)
                {
                    var networkStreamResult = await this.StartAsync();
                    if (networkStreamResult == NetworkStreamResult.STARTED)
                    {
                        scandataResult = await this.LMDScandataAsync(); // TO FIX: First call doesn't return data ?
                        scandataResult = await this.LMDScandataAsync(); // TO FIX: Second call return datas

                        if (!scandataResult.IsError)
                        {
                            networkStreamResult = await this.StopAsync();
                            if (networkStreamResult == NetworkStreamResult.STOPPED)
                            {
                                this.Disconnect();
                                return scandataResult;
                            }
                            else
                            {
                                this.Disconnect();
                                scandataResult.IsError = true;
                                scandataResult.ErrorException = new Exception(string.Format("{0} Network stream improperly stopped.", scandataResult.ErrorException));
                                return scandataResult;
                            }
                        }
                        else
                        {
                            networkStreamResult = await this.StopAsync();
                            if (networkStreamResult == NetworkStreamResult.STOPPED)
                            {
                                this.Disconnect();
                                return scandataResult;
                            }
                            else
                            {
                                this.Disconnect();
                                scandataResult.IsError = true;
                                scandataResult.ErrorException = new Exception(string.Format("{0} Network stream improperly stopped.", scandataResult.ErrorException));
                                return scandataResult;
                            }
                        }
                    }
                    else
                        return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Network stream not started.") };
                }
                else
                    return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Client socket not connected.") };
            }
            catch (Exception ex)
            {
                return new LMDScandataResult() { IsError = true, ErrorException = ex };
            }
        }

        #endregion

        #region Utils

        /// <summary>
        /// Converts IEEE754 to Single
        /// Taken from https://www.codeproject.com/Questions/99483/Convert-value-by-IEEE-754-protocol
        /// </summary>
        /// <param name="hexVal"></param>
        /// <returns></returns>
        private static Single ConvertHexToSingle(string hexVal)
        {
            try
            {
                int i = 0, j = 0;
                byte[] bArray = new byte[4];
                for (i = 0; i <= hexVal.Length - 1; i += 2)
                {
                    bArray[j] = Byte.Parse(hexVal[i].ToString() + hexVal[i + 1].ToString(), System.Globalization.NumberStyles.HexNumber);
                    j += 1;
                }
                Array.Reverse(bArray);
                Single s = BitConverter.ToSingle(bArray, 0);
                return (s);
            }
            catch (Exception ex)
            {
                throw new FormatException("The supplied hex value is either empty or in an incorrect format.  Use the " +
                    "following format: 00000000", ex);
            }
        }

        #endregion Utils
    }
}
