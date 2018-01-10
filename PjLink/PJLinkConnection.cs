// The C# PJLink library.
// Copyright (C) 2010 RV realtime visions GmbH (www.realtimevisions.com)
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace rv
{
    /// <summary>
    /// Simple class to control networked projectors via the PJLink protocol. 
    /// See full spec here: http://pjlink.jbmia.or.jp/english/data/PJLink%20Specifications100.pdf
    /// 
    /// This library was written in one day, so don't expect perfection. Most information must be 
    /// fetched from commands after execution (sendCommand). For most common tasks, there are some 
    /// shortcuts available in the PJLinkConnectionPJLinkConnection class. 
    /// 
    /// Basic usage: 
    /// <code> 
    /// // Create connection object, communicating with projector with IP 192.168.1.14
    /// // Projector has authentication enabled, password is rv (ignored if auth disabled)
    /// PJLinkConnection c = new PJLinkConnection("192.168.1.14", "rv");
    /// 
    /// //using shortcuts
    /// c.turnOn(); 
    /// c.turnOff(); 
    /// System.Console.WriteLine(c.getProjectorInfo());
    /// 
    /// //using low(er) level commands
    /// ErrorStatusCommand esc = new ErrorStatusCommand();
    /// if (c.sendCommand(esc) == Command.Response.SUCCESS)            
    ///     Console.WriteLine(esc.dumpToString());            
    /// else
    ///     Console.WriteLine("Communication Error");
    /// </code>     
    /// </summary>
    public class PJLinkConnection : IDisposable
    {        
        /// <summary>
        /// hostname or IP (as string, e.g. "192.168.1.12") of the projector
        /// </summary>
        private string _hostName = "";
        /// <summary>
        /// PJLink port, default ist 4352 (see Spec)
        /// </summary>
        private int _port = 4352;
        /// <summary>
        /// Flag is true, if the projector requires authentication
        /// </summary>
        private bool _useAuth = false;
        /// <summary>
        /// Password supplied by user if authentication is enabled
        /// </summary>
        private string _passwd = "";
        /// <summary>
        /// Random key returned by projector for MD5 sum generation 
        /// </summary>
        private string _pjKey = "";
        /// <summary>
        /// The connection client
        /// </summary>
        TcpClient _client = null;
        /// <summary>
        /// The Network stream the _client provides
        /// </summary>
        NetworkStream _stream = null;

        #region C'tors

        public PJLinkConnection(string host, int port, string passwd)
        {
            _hostName = host;
            _port = port;
            _passwd = passwd;
            _useAuth = (passwd != "");                 
        }

        public PJLinkConnection(string host, string passwd)
        {
            _hostName = host;
            _passwd = passwd;
            _useAuth = (passwd != ""); 
        }

        public PJLinkConnection(string host, int port)
        {
            _hostName = host;
            _port = port;
            _useAuth = false; 
        }

        public PJLinkConnection(string host)
        {
            _hostName = host;
            _useAuth = false;
        }

        #endregion

        public async Task<Command.Response> sendCommand(Command cmd)
        {
            if(await initConnection())
            {
                try
                {
                    string cmdString = cmd.getCommandString() + "\r";

                    if (_useAuth && _pjKey != "")
                        cmdString = getMD5Hash(_pjKey + _passwd) + cmdString;

                    byte[] sendBytes = Encoding.ASCII.GetBytes(cmdString);
                    _stream.Write(sendBytes, 0, sendBytes.Length);

                    byte[] recvBytes = new byte[_client.ReceiveBufferSize];
                    int bytesRcvd = _stream.Read(recvBytes, 0, (int)_client.ReceiveBufferSize);
                    string returndata = Encoding.ASCII.GetString(recvBytes, 0, bytesRcvd);
                    returndata = returndata.Trim();
                    cmd.processAnswerString(returndata);
                    return cmd.CmdResponse;
                }
                finally
                {
                    closeConnection();
                }
            }

            return Command.Response.COMMUNICATION_ERROR;
        }

        /// <summary>
        /// Sends a command asynchronously. The specified resultCallback will be called when 
        /// the command has executed.
        /// </summary>
        public async void sendCommandAsync(Command cmd, Command.CommandResultHandler resultCallback)
        {
            var response = await sendCommand(cmd);
                resultCallback(cmd, response);
        }

        #region Shortcuts

        /// <summary>
        /// Turn on projector. Returns true if projector answered with SUCCESS
        /// </summary>
        public async Task<bool> turnOn()
        {
            PowerCommand pc = new PowerCommand(PowerCommand.Power.ON);
            return (await sendCommand(pc) == Command.Response.SUCCESS);            
        }

        /// <summary>
        /// Turn off projector. Returns true if projector answered with SUCCESS
        /// </summary>
        public async Task<bool> turnOff()
        {
            PowerCommand pc = new PowerCommand(PowerCommand.Power.OFF);
            return (await sendCommand(pc) == Command.Response.SUCCESS);
        }

        /// <summary>
        /// Check power state of Projector. Returns unknown in case of an error
        /// </summary>
        public async Task<PowerCommand.PowerStatus> powerQuery()
        {
            PowerCommand pc3 = new PowerCommand(PowerCommand.Power.QUERY);
            if (await sendCommand(pc3) == Command.Response.SUCCESS)
                return pc3.Status;
            return PowerCommand.PowerStatus.UNKNOWN;            
        }

        /// <summary>
        /// Return String in the form
        /// Manufacturer Product (ProjectorName)
        /// or 
        /// Manufacturer Product 
        /// if no projector name is set. 
        /// </summary>
        public async Task<string> getProjectorInfo()
        {
            string toRet = ""; 
            ManufacturerNameCommand mnc = new ManufacturerNameCommand();
            if (await sendCommand(mnc) == Command.Response.SUCCESS)
                toRet = mnc.Manufacturer; 

            ProductNameCommand prnc = new ProductNameCommand();
            if (await sendCommand(prnc) == Command.Response.SUCCESS)
                toRet+= " " + prnc.ProductName;

            ProjectorNameCommand pnc = new ProjectorNameCommand();
            if (await sendCommand(pnc) == Command.Response.SUCCESS) {
                if (pnc.Name.Length > 0)
                    toRet += " (" + pnc.Name + ")";
            }                
            return toRet; 
        }

        public async Task<ProjectorInfo> getFullProjectorInfo()
        {            
            return await ProjectorInfo.create(this); 
        }

        #endregion

        public string HostName
        {
            get { return _hostName; }
        }

        #region private methods

        private async Task<bool> initConnection()
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_hostName, _port);
                    _stream = _client.GetStream();
                    byte[] recvBytes = new byte[_client.ReceiveBufferSize];
                    int bytesRcvd = _stream.Read(recvBytes, 0, (int)_client.ReceiveBufferSize);
                    string retVal = Encoding.ASCII.GetString(recvBytes, 0, bytesRcvd);
                    retVal = retVal.Trim(); 

                    if(retVal.IndexOf("PJLINK 0") >= 0)
                    {
                        _useAuth = false;  //pw provided but projector doesn't need it.
                        return true; 
                    }
                    else if (retVal.IndexOf("PJLINK 1 ") >= 0)
                    {
                        _pjKey = retVal.Replace("PJLINK 1 ", "");
                        return true; 
                    }
                }
                return false;
            }
            catch(Exception){
                return false;
            }

        }

        private void closeConnection()
        {
            if (_stream != null)
                _stream.Dispose(); 
            if (_client != null)
                _client.Dispose();
        }

        private string getMD5Hash(string str)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

        /*private string getMD5Hash(string input)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] bs = Encoding.ASCII.GetBytes(input);
            byte[] hash = x.ComputeHash(bs); 

            string toRet = "";
            foreach (byte b in hash)
            {
                toRet += b.ToString("x2"); 
            }
            return toRet;
        }*/

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: verwalteten Zustand (verwaltete Objekte) entsorgen.
                    ((IDisposable)_stream).Dispose();
                    ((IDisposable)_client).Dispose();
                }

                // TODO: nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer weiter unten überschreiben.
                // TODO: große Felder auf Null setzen.

                disposedValue = true;
            }
        }

        // TODO: Finalizer nur überschreiben, wenn Dispose(bool disposing) weiter oben Code für die Freigabe nicht verwalteter Ressourcen enthält.
        // ~PJLinkConnection() {
        //   // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
        //   Dispose(false);
        // }

        // Dieser Code wird hinzugefügt, um das Dispose-Muster richtig zu implementieren.
        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            Dispose(true);
            // TODO: Auskommentierung der folgenden Zeile aufheben, wenn der Finalizer weiter oben überschrieben wird.
            // GC.SuppressFinalize(this);
        }
        #endregion



    }
}
