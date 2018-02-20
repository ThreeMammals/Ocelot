// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Removed code and changed RequestDelete to OcelotRequestDelete, HttpContext to DownstreamContext, removed some exception handling messages

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Middleware.Pipeline
{
    public static class OcelotPipelineBuilderExtensions
    {
        internal const string InvokeMethodName = "Invoke";
        internal const string InvokeAsyncMethodName = "InvokeAsync";
        private static readonly MethodInfo GetServiceInfo = typeof(OcelotPipelineBuilderExtensions).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);

        public static IOcelotPipelineBuilder UseMiddleware<TMiddleware>(this IOcelotPipelineBuilder app, params object[] args)
        {
            return app.UseMiddleware(typeof(TMiddleware), args);
        }

        public static IOcelotPipelineBuilder Use(this IOcelotPipelineBuilder app, Func<DownstreamContext, Func<Task>, Task> middleware)
        {
            return app.Use(next =>
            {
                return context =>
                {
                    Func<Task> simpleNext = () => next(context);
                    return middleware(context, simpleNext);
                };
            });
        }

        public static IOcelotPipelineBuilder UseMiddleware(this IOcelotPipelineBuilder app, Type middleware, params object[] args)
        {
            return app.Use(next =>
            {
                var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var invokeMethods = methods.Where(m =>
                    string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal)
                    || string.Equals(m.Name, InvokeAsyncMethodName, StringComparison.Ordinal)
                ).ToArray();

                if (invokeMethods.Length > 1)
                {
                    throw new InvalidOperationException();
                }

                if (invokeMethods.Length == 0)
                {
                    throw new InvalidOperationException();
                }

                var methodinfo = invokeMethods[0];
                if (!typeof(Task).IsAssignableFrom(methodinfo.ReturnType))
                {
                    throw new InvalidOperationException();
                }

                var parameters = methodinfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(DownstreamContext))
                {
                    throw new InvalidOperationException();
                }

                var ctorArgs = new object[args.Length + 1];
                ctorArgs[0] = next;
                Array.Copy(args, 0, ctorArgs, 1, args.Length);
                var instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, ctorArgs);
                if (parameters.Length == 1)
                {
                    return (OcelotRequestDelegate)methodinfo.CreateDelegate(typeof(OcelotRequestDelegate), instance);
                }

                var factory = Compile<object>(methodinfo, parameters);

                return context =>
                {
                    var serviceProvider = context.HttpContext.RequestServices ?? app.ApplicationServices;
                    if (serviceProvider == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return factory(instance, context, serviceProvider);
                };
            });
        }

        private static Func<T, DownstreamContext, IServiceProvider, Task> Compile<T>(MethodInfo methodinfo, ParameterInfo[] parameters)
        {
            var middleware = typeof(T);
            var httpContextArg = Expression.Parameter(typeof(HttpContext), "httpContext");
            var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
            var instanceArg = Expression.Parameter(middleware, "middleware");

            var methodArguments = new Expression[parameters.Length];
            methodArguments[0] = httpContextArg;
            for (int i = 1; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    throw new NotSupportedException();
                }

                var parameterTypeExpression = new Expression[]
                {
                    providerArg,
                    Expression.Constant(parameterType, typeof(Type)),
                    Expression.Constant(methodinfo.DeclaringType, typeof(Type))
                };

                var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
                methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
            }

            Expression middlewareInstanceArg = instanceArg;
            if (methodinfo.DeclaringType != typeof(T))
            {
                middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodinfo.DeclaringType);
            }

            var body = Expression.Call(middlewareInstanceArg, methodinfo, methodArguments);

            var lambda = Expression.Lambda<Func<T, DownstreamContext, IServiceProvider, Task>>(body, instanceArg, httpContextArg, providerArg);

            return lambda.Compile();
        }

        private static object GetService(IServiceProvider sp, Type type)
        {
            var service = sp.GetService(type);
            if (service == null)
            {
                throw new InvalidOperationException();
            }

            return service;
        }
    }
}
