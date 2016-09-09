﻿// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Scope = "namespace", Target = "WebJobs.Script.WebHost.App_Start")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "WebJobs.Script.WebHost.App_Start.AutofacBootstrap.#Initialize(Autofac.ContainerBuilder)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "WebJobs.Script.WebHost.Controllers.HomeController.#Get()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope = "type", Target = "WebJobs.Script.WebHost.Filters.AuthorizationLevelAttribute")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_Start()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_Error(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ex", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_Error(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_End(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "WebJobs.Script.WebHost.SecretManager.#.ctor(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "WebJobs.Script.WebHost.Models.FunctionStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "WebJobs.Script.WebHost.App_Start.AutofacBootstrap.#Initialize(Autofac.ContainerBuilder,WebJobs.Script.WebHost.WebHostSettings)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "WebJobs.Script.WebHost.Models.HostStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.MetricsEventManager.#.cctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsMetricEvent(System.String,System.Int64,System.Int64,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsInfoEvent(System.String,System.String,System.String,System.String,System.String,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionExecutionEvent(System.String,System.String,System.Int32,System.String,System.String,System.String,System.Int64,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ms", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ms", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.AutofacBootstrap.#Initialize(Autofac.ContainerBuilder,Microsoft.Azure.WebJobs.Script.WebHost.WebHostSettings)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Controllers.HomeController.#Get()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsMetricEvent(System.String,System.Int64,System.Int64,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ms", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ms", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsInfoEvent(System.String,System.String,System.String,System.String,System.String,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionExecutionEvent(System.String,System.String,System.Int32,System.String,System.String,System.String,System.Int64,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.MetricsEventManager.#.cctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope = "type", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Filters.AuthorizationLevelAttribute")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebApiApplication.#Application_Start()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ex", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebApiApplication.#Application_Error(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Models.FunctionStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Models.HostStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.SecretManager.#.ctor(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#GetSecretManager(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.DependencyResolverExtensions.#GetService`1(System.Web.Http.Dependencies.IDependencyResolver)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "taskIgnore", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebScriptHostExceptionHandler.#OnTimeoutExceptionAsync(System.Runtime.ExceptionServices.ExceptionDispatchInfo,System.TimeSpan)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_activeHostManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_activeReceiverManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_standbyHostManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_standbyReceiverManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets.#FunctionKeys")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets.#Version")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets.#Version")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecretsInfo.#FunctionKeys")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ScriptSecretReader.#DeserializeFunctionSecrets(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ScriptSecretReader.#DeserializeHostSecrets(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ScriptSecretReader.#SerializeFunctionSecrets(System.Collections.Generic.IList`1<Microsoft.Azure.WebJobs.Script.WebHost.Key>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ScriptSecretReader.#SerializeHostSecrets(Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.AesCryptoKeyValueConverter.#EncryptValue(System.String,System.Byte[])")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.AesCryptoKeyValueConverter.#DecryptValue(System.String,System.Byte[])")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "key", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.DefaultKeyValueConverterFactory.#GetWriteValueConverter(Microsoft.Azure.WebJobs.Script.WebHost.Key)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.DefaultKeyValueConverterFactory.#GetReadValueConverter(Microsoft.Azure.WebJobs.Script.WebHost.Key)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "PlainText", Scope = "type", Target = "Microsoft.Azure.WebJobs.Script.WebHost.PlainTextKeyValueConverter")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.SecretManager.#.ctor(System.String,Microsoft.Azure.WebJobs.Script.WebHost.IKeyValueConverterFactory)")]