﻿// SampSharp
// Copyright 2019 Tim Potze
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SampSharp.Core;
using SampSharp.Core.Logging;

namespace SampSharp.Entities.Events
{
    /// <summary>
    /// Represents the event service.
    /// </summary>
    /// <seealso cref="IEventService" />
    public class EventService : IEventService
    {
        private static readonly Type[] DefaultParameterTypes =
        {
            typeof(int),
            typeof(bool),
            typeof(float),
            typeof(int[]),
            typeof(bool[]),
            typeof(float[]),
            typeof(string),
            typeof(Entity)
        };

        private static readonly MethodInfo GetComponentInfo =
            typeof(Entity).GetMethod(nameof(Entity.GetComponent), BindingFlags.Public | BindingFlags.Instance);

        private static readonly MethodInfo GetServiceInfo =
            typeof(EventService).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);

        private readonly Dictionary<string, Event> _events = new Dictionary<string, Event>();
        private readonly IGameModeClient _gameModeClient;
        private readonly IServiceProvider _serviceProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="EventService"/> class.
        /// </summary>
        public EventService(IGameModeClient gameModeClient, IServiceProvider serviceProvider)
        {
            _gameModeClient = gameModeClient;
            _serviceProvider = serviceProvider;

            Scanner();
        }


        /// <inheritdoc />
        public void EnableEvent(string name, Type[] parameters)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            // TODO: Callbacks with parameter length are not yet supported

            var handler = GetInvoke(name);
            _gameModeClient.RegisterCallback(name, handler.Target, handler.Method, parameters);

            if (!_events.ContainsKey(name)) _events[name] = new Event();
        }

        /// <inheritdoc />
        public void UseMiddleware(string name, Func<EventDelegate, EventDelegate> middleware)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));

            if (!_events.TryGetValue(name, out var evt))
                _events[name] = evt = new Event();

            evt.Middleware.Add(middleware);
        }

        private object InvokeEventInternal(EventContext context)
        {
            CoreLog.LogDebug($"InvokeEventInternal: {context.Name}({string.Join(", ", context.Arguments)})");

            object result = null;

            if (context.Name == null || !_events.TryGetValue(context.Name, out var evt)) return null;

            foreach (var sysEvt in evt.Events)
            {
                var system = _serviceProvider.GetService(sysEvt.Method.DeclaringType);

                if (system == null)
                    continue;

                result = sysEvt.Call(system, context) ?? result;
            }

            return null;
        }

        private void Scanner()
        {
            var assembly = Assembly.GetEntryAssembly();

            var assemblies = new List<Assembly>();

            void AddToScan(Assembly asm)
            {
                if (assemblies.Contains(asm))
                    return;

                assemblies.Add(asm);

                foreach (var assemblyRef in asm.GetReferencedAssemblies())
                {
                    if (assemblyRef.Name.StartsWith("System") || assemblyRef.Name.StartsWith("Microsoft") ||
                        assemblyRef.Name.StartsWith("netstandard"))
                        continue;

                    AddToScan(Assembly.Load(assemblyRef));
                }
            }

            AddToScan(assembly);

            CoreLog.LogDebug("Scan {0} assemblies for events", assemblies.Count);

            // Find eligible methods in system implementations
            var events = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISystem).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(m => (method: m, attribute: m.GetCustomAttribute<EventAttribute>()))
                .Where(x => x.attribute != null);

            // Add event methods to the event data
            foreach (var (method, attribute) in events)
            {
                CoreLog.LogDebug("Adding event listener on " + method.DeclaringType + "." + method.Name);

                var name = attribute.Name ?? method.Name;

                if (!_events.TryGetValue(name, out var evt))
                    _events[name] = evt = new Event();

                var parameterIndex = 0;
                var parameters = method.GetParameters();
                var parameterInfos = new SystemEventParameter[parameters.Length];
                var parameterTypes = new List<Type>();

                // Scan and arrange parameters.
                for (var i = 0; i < parameters.Length; i++)
                {
                    var type = parameters[i].ParameterType;

                    parameterInfos[i] = new SystemEventParameter();

                    if (typeof(Component).IsAssignableFrom(type))
                    {
                        // Get component of entity
                        parameterInfos[i].ParameterIndex = parameterIndex++;
                        parameterInfos[i].ComponentType = type;
                        parameterTypes.Add(typeof(Entity));
                    }
                    else if (DefaultParameterTypes.Contains(type))
                    {
                        // Default pass-through types
                        parameterInfos[i].ParameterIndex = parameterIndex++;
                        parameterTypes.Add(type);
                    }
                    else
                    {
                        // dependency injection
                        parameterInfos[i].ServiceType = type;
                    }
                }

                var caller = CompileForSystem(method, parameters, parameterInfos);

                var entry = new SystemEvent
                {
                    Method = method,
                    ParameterTypes = parameterTypes.ToArray(),
                    Call = (instance, eventContext) =>
                    {
                        // TODO: Check parameters match types?
                        return caller(instance, eventContext.Arguments, eventContext);
                    }
                };

                evt.Events.Add(entry);
            }
        }

        private object Invoke(EventContext context, int index, Event evt)
        {
            // TODO: Construct the middleware just once
            return index >= evt.Middleware.Count
                ? InvokeEventInternal(context)
                : evt.Middleware[index](eventContext => Invoke(eventContext, index + 1, evt))(context);
        }

        private Func<object[], object> GetInvoke(string name)
        {
            var context = new EventContextImpl();
            context.SetName(name);

            return args =>
            {
                context.SetArguments(args);
                context.SetEventServices(_serviceProvider); // TODO: Should I scope it?

                return _events.TryGetValue(name, out var evt)
                    ? Invoke(context, 0, evt)
                    : null;
            };
        }

        private static Func<object, object[], EventContext, object> CompileForSystem(MethodInfo methodInfo,
            ParameterInfo[] parameters, SystemEventParameter[] parameterInfos)
        {
            if (methodInfo.DeclaringType == null)
                throw new ArgumentException("Method must have declaring type", nameof(methodInfo));

            // Input arguments
            var instanceArg = Expression.Parameter(typeof(object), "instance");
            var argsArg = Expression.Parameter(typeof(object[]), "args");
            var eventContextArg = Expression.Parameter(typeof(EventContext), "eventContext");

            var methodArguments = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef) throw new NotSupportedException();

                if (parameterInfos[i].ComponentType != null)
                {
                    // Get component from entity
                    Expression index = Expression.Constant(parameterInfos[i].ParameterIndex);

                    Expression getValue = Expression.ArrayIndex(argsArg, index);
                    getValue = Expression.Convert(getValue, typeof(Entity));
                    methodArguments[i] = Expression.Call(getValue,
                        GetComponentInfo.MakeGenericMethod(parameterInfos[i].ComponentType));
                }
                else if (parameterInfos[i].ServiceType != null)
                {
                    // Get service
                    var getServiceCall = Expression.Call(GetServiceInfo, eventContextArg,
                        Expression.Constant(parameterType, typeof(Type)));
                    methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
                }
                else if (parameterInfos[i].ParameterIndex >= 0)
                {
                    // Pass through
                    Expression index = Expression.Constant(parameterInfos[i].ParameterIndex);

                    var getValue = Expression.ArrayIndex(argsArg, index);
                    methodArguments[i] = Expression.Convert(getValue, parameterType);
                }
            }

            var service = Expression.Convert(instanceArg, methodInfo.DeclaringType);
            Expression body = Expression.Call(service, methodInfo, methodArguments);

            if (body.Type == typeof(void))
                body = Expression.Block(body, Expression.Constant(null));
            else if (body.Type != typeof(object)) body = Expression.Convert(body, typeof(object));

            var lambda =
                Expression.Lambda<Func<object, object[], EventContext, object>>(body, instanceArg, argsArg,
                    eventContextArg);

            return lambda.Compile();
        }

        private static object GetService(EventContext eventContext, Type type)
        {
            var service = eventContext.EventServices.GetService(type);
            return service ?? throw new InvalidOperationException();
        }

        private class Event
        {
            public readonly List<Func<EventDelegate, EventDelegate>> Middleware =
                new List<Func<EventDelegate, EventDelegate>>();

            public readonly List<SystemEvent> Events = new List<SystemEvent>();
        }

        private class SystemEvent
        {
            public Func<object, EventContext, object> Call;
            public MethodInfo Method;
            public Type[] ParameterTypes;
        }

        private class SystemEventParameter
        {
            public Type ComponentType;
            public int ParameterIndex = -1;
            public Type ServiceType;
        }
    }
}