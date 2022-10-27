using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Cli;

namespace SqlAgent.Cli.Models;
public sealed class TypeRegistrar : ITypeRegistrar
{
	private readonly IServiceCollection _services;

	public TypeRegistrar(IServiceCollection services)
	{
		_services = services;
	}

	public ITypeResolver Build()
	{
		return new TypeResolver(_services.BuildServiceProvider());
	}

	public void Register(Type service, Type implementation)
	{
		_services.AddSingleton(service, implementation);
	}

	public void RegisterInstance(Type service, object implementation)
	{
		_services.AddSingleton(service, implementation);
	}

	public void RegisterLazy(Type service, Func<object> func)
	{
		if (func is null)
		{
			throw new ArgumentNullException(nameof(func));
		}

		_services.AddSingleton(service, (provider) => func());
	}
}