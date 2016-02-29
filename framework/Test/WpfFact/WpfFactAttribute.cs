using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("WpfFactDiscoverer", "csTest")]
// ReSharper disable once CheckNamespace
public class WpfFactAttribute : FactAttribute { }