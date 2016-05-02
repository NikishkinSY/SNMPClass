using System.Collections.Generic;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System.Net;
using System.Linq;
using System;

namespace SNMPClass
{
    /// <summary>
    /// SNMP Device
    /// </summary>
    public class Device_outdated
    {
        private VersionCode Version { get; set; }
        private IPAddress IPAddress { get; set; }
        private int Port { get; set; }
        private OctetString Community { get; set; }
        private int TimeOut { get; set; }
        private IEnumerable<Task> Tasks { get; set; }
        private int TimesToRepeat { get; set; }
        
        public Device_outdated(VersionCode version, string ipAddress, int port, string community, int timeOut, int timesToRepeat)
        {
            this.Version = version;
            this.IPAddress = IPAddress.Parse(ipAddress);
            this.Port = port;
            this.Community = new OctetString(community);
            this.TimeOut = timeOut;
            this.TimesToRepeat = timesToRepeat;
            this.Tasks = new List<Task>();
        }

        /// <summary>
        /// Operation sync
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        private OperationResult Operation(Operation operation)
        {
            for (int i=0; i < TimesToRepeat; i++)
            {
                try
                {
                    switch (operation.OperationType)
                    {
                        case OperationType.Get:
                            var resultGet = Messenger.Get(Version,
                                new IPEndPoint(IPAddress, Port),
                                Community,
                                new List<Variable> { new Variable(operation.OId) },
                                TimeOut);
                            return new OperationResult(resultGet, true);
                        case OperationType.Set:
                            var resultSet = Messenger.Set(Version,
                                new IPEndPoint(IPAddress, Port),
                                Community,
                                new List<Variable> { new Variable(operation.OId, operation.Data) },
                                TimeOut);
                            return new OperationResult(resultSet, true);
                        case OperationType.Walk:
                            List<Variable> variables = new List<Variable>();
                            var resultWalk = Messenger.Walk(Version,
                                new IPEndPoint(IPAddress, Port),
                                Community,
                                operation.OId,
                                variables,
                                TimeOut, 
                                WalkMode.WithinSubtree);
                            if (variables.Count > 0)
                                return new OperationResult(variables, true);
                            else
                                return new OperationResult(new Exception("Number of table elements is 0 or OId is not a table"), false);
                    }
                }
                catch (Lextm.SharpSnmpLib.Messaging.TimeoutException ex) {
                    return new OperationResult(ex, false);
                }
                catch (ErrorException ex) {
                    return new OperationResult(ex, false);
                }
            }
            return new OperationResult(new Exception("Amount repeats less than 1"), false);
        }

        #region Get
        private async Task<OperationResult> GetAsync(ObjectIdentifier OId)
        {
            return await Task<OperationResult>.Run(() =>
            {
                return Operation(new Operation(OId, OperationType.Get));
            });
        }
        
        /// <summary>
        /// Get operation async
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        public List<OperationResult> GetAsync(IEnumerable<ObjectIdentifier> oIds)
        {
            List<Task<OperationResult>> tasks = new List<Task<OperationResult>>();
            foreach (ObjectIdentifier objectIdentifier in oIds)
                tasks.Add(GetAsync(objectIdentifier));
            
            List<OperationResult> operationResults = new List<OperationResult>();
            foreach (Task<OperationResult> task in tasks)
            {
                operationResults.Add(task.GetAwaiter().GetResult());
            }
            return operationResults;
        }
        #endregion

        #region Set
        private async Task<OperationResult> SetAsync(Variable variable)
        {
            return await Task<OperationResult>.Run(() =>
            {
                return Operation(new Operation(variable, OperationType.Set));
            });
        }
        
        /// <summary>
        /// Set operation async
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        public List<OperationResult> SetAsync(IEnumerable<Variable> variables)
        {
            List<Task<OperationResult>> tasks = new List<Task<OperationResult>>();
            foreach (Variable variable in variables)
                tasks.Add(SetAsync(variable));

            List<OperationResult> operationResults = new List<OperationResult>();
            foreach (Task<OperationResult> task in tasks)
            {
                operationResults.Add(task.GetAwaiter().GetResult());
            }
            return operationResults;
        }
        #endregion

        #region Walk
        private async Task<OperationResult> WalkAsync(ObjectIdentifier table)
        {
            return await Task<OperationResult>.Run(() =>
            {
                return Operation(new Operation(table, OperationType.Walk));
            });
        }

        /// <summary>
        /// Get operation async
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        public List<OperationResult> WalkAsync(IEnumerable<ObjectIdentifier> tables)
        {
            List<Task<OperationResult>> tasks = new List<Task<OperationResult>>();
            foreach (ObjectIdentifier objectIdentifier in tables)
                tasks.Add(WalkAsync(objectIdentifier));

            List<OperationResult> operationResults = new List<OperationResult>();
            foreach (Task<OperationResult> task in tasks)
            {
                operationResults.Add(task.GetAwaiter().GetResult());
            }
            return operationResults;
        }
        #endregion
    }

    public class OperationResult
    {
        public IList<Variable> Variables { get; set; }
        public bool Result { get; set; }
        public Exception Exception { get; private set; }

        public OperationResult(IList<Variable> variables, bool result)
        {
            this.Variables = variables;
            this.Result = result;
        }

        public OperationResult(Exception exception, bool result)
        {
            this.Variables = new List<Variable>();
            this.Exception = exception;
            this.Result = result;
        }
    }

    public class Operation
    {
        public ObjectIdentifier OId { get; set; }
        public OperationType OperationType { get; set; }
        /// <summary>
        /// For Set operation
        /// </summary>
        public ISnmpData Data { get; set; }

        public Operation(ObjectIdentifier oId, OperationType operationType)
        {
            this.OId = oId;
            this.OperationType = operationType;
        }

        public Operation(Variable variable, OperationType operationType)
        {
            this.OId = variable.Id;
            this.Data = variable.Data;
            this.OperationType = operationType;
        }
    }
    
    public enum OperationType
    {
        Get,
        Walk,
        Set
    }
}

//Description

////Define device
//Device device = new Device(VersionCode.V2, "192.168.0.2", 8001, "public", 2000, 1);

////Define OIds for get operation
//List<ObjectIdentifier> oIds = new List<ObjectIdentifier>()
//            {
//                new ObjectIdentifier("1.1"),
//                new ObjectIdentifier("1.2"),
//                new ObjectIdentifier("1.3")
//            };
//var result = device.GetAsync(oIds);

////For set operation you need to define variables
////List<Variable> variables = new List<Variable>()
////{
////    new Variable(new ObjectIdentifier("1.1"), new OctetString("value")),
////    new Variable(new ObjectIdentifier("1.2"), new OctetString("value")),
////    new Variable(new ObjectIdentifier("1.3"), new OctetString("value"))
////};
////var result = device.SetAsync(variables);

////For walk (get table) you need to define list of table OId
////List<ObjectIdentifier> oIds = new List<ObjectIdentifier>()
////{
////    new ObjectIdentifier(".1.3.6.1.4.1.9.2.2.1"),
////};
////var result = device.WalkAsync(oIds);

////"result" contains all get resuts 
//var successResults = result.Where(x => x.Result).ToList();
//var unsuccessResults = result.Where(x => !x.Result).ToList();