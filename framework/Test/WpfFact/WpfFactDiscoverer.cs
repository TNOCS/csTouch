﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

public class WpfFactDiscoverer : IXunitTestCaseDiscoverer
{
    readonly FactDiscoverer factDiscoverer;

    public WpfFactDiscoverer(IMessageSink diagnosticMessageSink)
    {
        factDiscoverer = new FactDiscoverer(diagnosticMessageSink);
    }

    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return factDiscoverer.Discover(discoveryOptions, testMethod, factAttribute)
                             .Select(testCase => new WpfTestCase(testCase));
    }
}