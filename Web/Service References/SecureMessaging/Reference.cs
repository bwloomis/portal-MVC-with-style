﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Assmnts.SecureMessaging {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="SecureMessaging.ISecureMessaging")]
    public interface ISecureMessaging {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISecureMessaging/AdapAutomation", ReplyAction="http://tempuri.org/ISecureMessaging/AdapAutomationResponse")]
        void AdapAutomation(string json);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISecureMessaging/AdapAutomation", ReplyAction="http://tempuri.org/ISecureMessaging/AdapAutomationResponse")]
        System.Threading.Tasks.Task AdapAutomationAsync(string json);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ISecureMessagingChannel : Assmnts.SecureMessaging.ISecureMessaging, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class SecureMessagingClient : System.ServiceModel.ClientBase<Assmnts.SecureMessaging.ISecureMessaging>, Assmnts.SecureMessaging.ISecureMessaging {
        
        public SecureMessagingClient() {
        }
        
        public SecureMessagingClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public SecureMessagingClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public SecureMessagingClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public SecureMessagingClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void AdapAutomation(string json) {
            base.Channel.AdapAutomation(json);
        }
        
        public System.Threading.Tasks.Task AdapAutomationAsync(string json) {
            return base.Channel.AdapAutomationAsync(json);
        }
    }
}
