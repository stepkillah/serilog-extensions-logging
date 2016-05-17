// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using FrameworkLogger = Microsoft.Extensions.Logging.ILogger;

namespace Serilog.Extensions.Logging
{
    class SerilogLoggerProvider : ILoggerProvider, ILogEventEnricher
    {
        public const string OriginalFormatPropertyName = "{OriginalFormat}";

        readonly AsyncLocal<SerilogLoggerScope> _value = new AsyncLocal<SerilogLoggerScope>();

        // May be null; if it is, Log.Logger will be lazily used
        readonly ILogger _logger;

        public SerilogLoggerProvider(ILogger logger = null)
        {
            if (logger != null)
                _logger = logger.ForContext(new[] { this });
        }

        public FrameworkLogger CreateLogger(string name)
        {
            return new SerilogLogger(this, _logger, name);
        }

        public IDisposable BeginScope<T>(string name, T state)
        {
            return new SerilogLoggerScope(this, name, state);
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            for (var scope = CurrentScope; scope != null; scope = scope.Parent)
            {
                var stateStructure = scope.State as IEnumerable<KeyValuePair<string, object>>;
                if (stateStructure != null)
                {
                    foreach (var keyValue in stateStructure)
                    {
                        if (keyValue.Key == OriginalFormatPropertyName && keyValue.Value is string)
                            continue;

                        var property = propertyFactory.CreateProperty(keyValue.Key, keyValue.Value);
                        logEvent.AddPropertyIfAbsent(property);
                    }
                }
            }
        }

        public SerilogLoggerScope CurrentScope
        {
            get
            {
                return _value.Value;
            }
            set
            {
                _value.Value = value;
            }
        }

        public void Dispose() { }
    }
}