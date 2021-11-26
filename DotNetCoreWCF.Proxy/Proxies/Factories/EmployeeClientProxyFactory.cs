using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using DotNetCoreWCF.Contracts.Interfaces;
using DotNetCoreWCF.Contracts.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace DotNetCoreWCF.Proxies.Factories
{
	public interface IEmployeeClientProxyFactory
	{
		IEmployeeClient GetClient(ClientType? clientType = null);
	}

	public enum ClientType
	{
		Default,
		ChannelFactory,
		ManualBindings
	}

	public class EmployeeClientProxyFactory : IEmployeeClientProxyFactory
	{
		private readonly IClientProxySettings _proxySettings;
		private Lazy<ChannelFactory<IEmployeeService>> _channelFactory;
		private readonly ILogger _logger;

		public EmployeeClientProxyFactory(IClientProxySettings proxySettings, ILogger logger)
		{
			_proxySettings = proxySettings;
			_logger = logger;
			_channelFactory = new Lazy<ChannelFactory<IEmployeeService>>(() =>
			{
				//var binding = _proxySettings.GetNetTcpBinding();
				var binding = _proxySettings.GetBasicHttpBinding();
				var endpointAddress = _proxySettings.GetEndpointAddress();

				var factory = new ChannelFactory<IEmployeeService>(binding, endpointAddress);

				if (factory.Credentials != null)
				{
					//factory.Credentials.ServiceCertificate.SetDefaultCertificate(
					//	StoreLocation.LocalMachine, StoreName.My,
					//	X509FindType.FindBySubjectName, "localhost:9118");

					//factory.Credentials.UserName.UserName = "";
					//factory.Credentials.UserName.Password = "password";
					/*
					 
					  byte[] bt = Convert.FromBase64String(secret);

            return new X509Certificate2(
                bt,
                string.Empty,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
					 */


					factory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My,
						X509FindType.FindBySubjectName, "localhost:9118");
					//factory.Credentials.ClientCertificate.Certificate = new X509Certificate2();

				}

				//binding.Security.Mode = BasicHttpSecurityMode.Transport;

				//binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
				//binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;

				factory.Open();
				factory.Endpoint.TrySetMaxItemsInObjectGraph(_proxySettings);
				
				return factory;
			});
		}

		public IEmployeeClient GetClient(ClientType? clientType = null)
		{
			bool isProxyEnabled = _proxySettings?.Enabled ?? false;

			if (isProxyEnabled && clientType == ClientType.ManualBindings)
			{
				var binding = _proxySettings.GetNetTcpBinding();
				var endpointAddress = _proxySettings.GetEndpointAddress();

				var client = new EmployeeClient(binding, endpointAddress);
				client.TrySetMaxItemsInObjectGraph(_proxySettings);

				return client;
			}

			if (isProxyEnabled && clientType == ClientType.ChannelFactory)
				return new ManuallyConfiguredEmployeeClient(_channelFactory.Value, _logger);

			// this does not work in netcore mode since this relies on automatic configuration
			return new EmployeeClient();
		}

		public IEmployeeClient GetHttpClient(ClientType? clientType = null)
		{
			bool isProxyEnabled = _proxySettings?.Enabled ?? false;

			if (isProxyEnabled && clientType == ClientType.ManualBindings)
			{
				var binding = _proxySettings.GetNetTcpBinding();

				var endpointAddress = _proxySettings.GetEndpointAddress();

				var client = new EmployeeClient(binding, endpointAddress);
				client.TrySetMaxItemsInObjectGraph(_proxySettings);

				return client;
			}

			if (isProxyEnabled && clientType == ClientType.ChannelFactory)
				return new ManuallyConfiguredEmployeeClient(_channelFactory.Value, _logger);

			// this does not work in netcore mode since this relies on automatic configuration
			return new EmployeeClient();
		}
	}
}