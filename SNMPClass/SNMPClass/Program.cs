using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Objects;
using Lextm.SharpSnmpLib.Pipeline;
using System.Linq;
using System.Net;


namespace SNMPClass
{
    class Program
    {
        static void Main(string[] args)
        {
            //list of agents
            List<Agent> agents = new List<Agent>();
            
            //1 agent
            Agent agent1 = new Agent("192.168.0.99", 1601)
            {
                MaxItemCountInRequest = 3,
                StartOId = ".1.3.6.1.2.1.5.24.0"
            };
            agent1.EndRequest += Agent_EndRequest;
            agents.Add(agent1);

            //2 agent
            //Agent agent2 = new Agent("192.168.0.2", 8001)
            //{
            //    MaxItemCountInRequest = 100,
            //    StartOId = ".1.3"
            //};
            //agent2.EndRequest += Agent_EndRequest;
            //agents.Add(agent2);

            //run request agents async
            foreach(Agent agent in agents)
            {
                agent.GetBulkRequestAsync();
            }

            Console.ReadKey();
        }


        //handler results 
        private static void Agent_EndRequest(object sender, Agent.ResultEventArgs e)
        {
            var vars = e.Variables;
        }
    }
}
