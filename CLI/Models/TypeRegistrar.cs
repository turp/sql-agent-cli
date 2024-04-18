using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System;

namespace SqlAgent.Cli.Models;
public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
	public ITypeResolver Build()
	{
		return new TypeResolver(services.BuildServiceProvider());
	}

	public void Register(Type service, Type implementation)
	{
		services.AddSingleton(service, implementation);
	}

	public void RegisterInstance(Type service, object implementation)
	{
		services.AddSingleton(service, implementation);
	}

	public void RegisterLazy(Type service, Func<object> func)
	{
		if (func is null)
		{
			throw new ArgumentNullException(nameof(func));
		}

		services.AddSingleton(service, (provider) => func());
	}
}