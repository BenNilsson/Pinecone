using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PineconeTCP
{
    public abstract class Common
    {
        public abstract void Tick();

        protected class Methods
        {

        }

        protected class MethodToProcess : Methods
        {
            public string objectId;
            public int behaviourIndex;
            public string methodName;
            public object[] parameters;

            public MethodToProcess(string objectId, int behaviourIndex, string methodName, object[] parameters)
            {
                this.objectId = objectId;
                this.behaviourIndex = behaviourIndex;
                this.methodName = methodName;
                this.parameters = parameters;
            }
        }

        protected class MethodToProcessSpawnPlayer : Methods
        {
            public string methodName;
            public object[] parameters;
            public int connectionId;

            public MethodToProcessSpawnPlayer(string methodName, object[] parameters, int connectionId = -1)
            {
                this.methodName = methodName;
                this.parameters = parameters;
                this.connectionId = connectionId;
            }
        }

        protected class MethodToProcessSyncVar : Methods
        {
            public string objectId;
            public int behaviourIndex;
            public string variableName;
            public object currentValue;

            public MethodToProcessSyncVar(string objectId, int behaviourIndex, string variableName, object currentValue)
            {
                this.objectId = objectId;
                this.behaviourIndex = behaviourIndex;
                this.variableName = variableName;
                this.currentValue = currentValue;
            }
        }

        protected class MethodToProcessDisconnected : Methods
        {
            public int connectionId;

            public MethodToProcessDisconnected(int connectionId = -1)
            {
                this.connectionId = connectionId;
            }
        }

    }
}
