using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SNMPClass
{
    public class Agent
    {
        private IPAddress IPAddress { get; set; }
        private int Port { get; set; }
        
        /// <summary>
        /// Maximum items in one request
        /// </summary>
        public int MaxItemCountInRequest { get; set; }
        /// <summary>
        /// Timout on one request
        /// </summary>
        public int TimeOutRequest { get; set; }
        /// <summary>
        /// Community
        /// </summary>
        public string Community { get; set; }
        /// <summary>
        /// Version of SNMP protocol
        /// </summary>
        public VersionCode VersionCode { get; set; }
        /// <summary>
        /// OId from which start poll
        /// </summary>
        public string StartOId { get; set; }
        /// <summary>
        /// maximum requests to node during poll, 0 is infinitely
        /// </summary>
        public int MaxRequsts { get; set; }


        private bool _stopRequest { get; set; }
        
        public Agent(string ipAddress, int port, 
            int maxItemCountInRequest = 10,
            int timeOutRequest = 1000,
            string community = "public",
            VersionCode versionCode = VersionCode.V2,
            string startOId = ".1.3",
            int maxRequst = 100)
        {
            this.IPAddress = IPAddress.Parse(ipAddress);
            this.Port = port;
            this.MaxItemCountInRequest = maxItemCountInRequest;
            this.TimeOutRequest = timeOutRequest;
            this.Community = community;
            this.VersionCode = versionCode;
            this.StartOId = startOId;
            this.MaxRequsts = maxRequst;
        }

        /// <summary>
        /// Callback result
        /// </summary>
        public event EventHandler<ResultEventArgs> EndRequest;
        protected virtual void OnEndRequest(ResultEventArgs e)
        {
            if (EndRequest != null)
                EndRequest(this, e);
        }
        public class ResultEventArgs: EventArgs
        {
            public IList<Variable> Variables { get; set; }
            public IList<Exception> Exceptions { get; set; }
            public bool HasErrors
            {
                get { return Exceptions != null ? Exceptions.Count > 0 : true; }
            }
            public ResultEventArgs(IList<Variable> variables, IList<Exception> exceptions)
            {
                this.Variables = variables;
                this.Exceptions = exceptions;
            }
        }

        /// <summary>
        /// Sync request node
        /// </summary>
        /// <returns></returns>
        public List<Variable> GetBulkRequest()
        {
            GetBulkRequestMessage requestMessage;
            ObjectIdentifier startOId = new ObjectIdentifier(this.StartOId);
            List<Variable> variables = new List<Variable>();
            int MaxItemCount = this.MaxItemCountInRequest;
            int requestId = 0;
            _stopRequest = true;
            List<Exception> exceptions = new List<Exception>();
            while (_stopRequest)
            {
                //max request is infinitely, Items in request less than 1, more than max requests to node
                if (this.MaxRequsts == 0 || MaxItemCount < 1 || requestId > this.MaxRequsts)
                    break;

                requestMessage = new GetBulkRequestMessage(requestId++, this.VersionCode, new OctetString(this.Community), 0, MaxItemCount,
                new List<Variable>() { new Variable(startOId) });
                
                try
                {
                    //get response
                    var response = requestMessage.GetResponse(this.TimeOutRequest, new IPEndPoint(this.IPAddress, this.Port));

                    variables.AddRange(response.Scope.Pdu.Variables);
                    startOId = response.Scope.Pdu.Variables.Last().Id;

                    //end of OIds
                    if (response.Scope.Pdu.Variables.Last().Data.TypeCode == SnmpType.EndOfMibView)
                        break;
                }
                //catch (SocketException ex)
                //{
                //    //can't connect to node
                //    throw;
                //}
                catch (Exception ex)
                {
                    //need to log exceptions
                    MaxItemCount--;
                    exceptions.Add(ex);
                }
            }
            OnEndRequest(new ResultEventArgs(variables, exceptions));
            return variables;
        }

        /// <summary>
        /// Async request node
        /// </summary>
        /// <returns></returns>
        public async Task<List<Variable>> GetBulkRequestAsync()
        {
            return await Task<OperationResult>.Run(() =>
            {
                return GetBulkRequest();
            });
        }

        /// <summary>
        /// Stop request to node
        /// </summary>
        public void StopRequest()
        {
            this._stopRequest = true;
        }
    }
}
